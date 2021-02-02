using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using System.Net;
using UnityEngine.SceneManagement;

public class SongController : MonoBehaviour
{
    public static SongController Instance;

    [HideInInspector]
    public List<SongOption> songOptions = new List<SongOption>();
    public SongOption songOption_p;
    public RectTransform songOptionsParent;
    private List<SongOption> activeSongOptions = new List<SongOption>();
    private SongOption selectedSongOption;

    public GameSpeedOption[] gameSpeedOptions;
    private GameSpeedOption selectedSpeedOption;

    public Slider numberOfKeysSlider;
    private int numberOfKeys;

    public string songsFolderName = "songs";

    public Text statusText;
    public InputField urlInput;
    public ProgressBar progressBar;

    private string pythonExe;
    private string youtubeDlPy;
    private string spleeterExe;
    private string noteGenerationPy;
    private string ffmpegPath;
    private string ffmpegExe;

    private string songsPath;

    private Queue<System.Action> actions = new Queue<System.Action>();

    private bool downloadingSong;

    private System.Diagnostics.Process downloadProcess;
    private System.Diagnostics.Process extractAudioProcess;
    private System.Diagnostics.Process spleeterProcess;
    private System.Diagnostics.Process noteGenerationProcess;

    private string lastCmdMessage;
    private List<string> runErrorMessages = new List<string>();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        initConfig();
        initializeSongsFolder();

        pythonExe = Application.streamingAssetsPath + "/" + "python/runtime/python.exe";
        youtubeDlPy = Application.streamingAssetsPath + "/" + "python/youtube_dl/__main__.py";
        ffmpegPath = Application.streamingAssetsPath + "/" + "python/ffmpeg";
        ffmpegExe = Application.streamingAssetsPath + "/" + "python/ffmpeg/ffmpeg.exe";
        spleeterExe = Application.streamingAssetsPath + "/" + "python/spleeter/bin/spleeter.exe";
        noteGenerationPy = Application.streamingAssetsPath + "/" + "python/note_generation/__main__.py";
        songsPath = Application.dataPath + "/" + songsFolderName;

        progressBar.SetPercentage(0.0f);
        progressBar.gameObject.SetActive(false);

        clearSongOptions();
        populateSongOptions();

        subscribeToEvents();
    }

    // Update is called once per frame
    void Update()
    {
        checkForActions();
    }

    private void initConfig()
    {
        string numKeys = Assets.scripts.utility.ConfigUtilities.ReadConfig(Application.dataPath + "/" + "config.json", "numKeys");
        numberOfKeys = int.Parse(numKeys);
        numberOfKeysSlider.value = numberOfKeys;
    }

    private void subscribeToEvents()
    {
        foreach(var option in gameSpeedOptions)
        {
            option.OnClicked += gameSpeedOptionSelected;
        }

        numberOfKeysSlider.onValueChanged.AddListener(numberOfKeysValueChanged);
    }

    private void gameSpeedOptionSelected(GameSpeedOption selectedOption)
    {
        foreach(var option in gameSpeedOptions)
        {
            if(option != selectedOption)
            {
                option.UnselectOption();
            }
        }

        selectedSpeedOption = selectedOption;
    }

    private void numberOfKeysValueChanged(float newValue)
    {
        numberOfKeys = (int)newValue;
        Assets.scripts.utility.ConfigUtilities.WriteConfig(Application.dataPath + "/" + "config.json", "numKeys", newValue.ToString());
    }

    private void clearSongOptions()
    {
        activeSongOptions.ForEach(option =>
        {
            Destroy(option.gameObject);
        });
        activeSongOptions.Clear();
    }

    private void populateSongOptions()
    {
        foreach(var songPath in Directory.GetDirectories(songsPath))
        {
            string songName = findSongName(songPath);
            string songId = findSongId(songPath);
            string thumbnailPath = Directory.GetFiles(songPath, "*.jpg")[0];

            SongOption newOption = Instantiate(songOption_p);
            newOption.Title = songName;
            newOption.SetImage(thumbnailPath);
            newOption.transform.SetParent(songOptionsParent);
            newOption.SongId = songId;

            newOption.GetComponent<RectTransform>().localScale = Vector3.one;

            activeSongOptions.Add(newOption);
        }
    }

    public SongOption GetSelectedSongOption()
    {
        return selectedSongOption;
    }

    public GameSpeedOption GetSelectedSpeedOption()
    {
        return selectedSpeedOption;
    }

    public int GetNumberOfKeys()
    {
        return numberOfKeys;
    }

    public void UpdateSelectedSongOption(SongOption newOption)
    {
        if (selectedSongOption != null && selectedSongOption != newOption)
        {
            selectedSongOption.UnSelectOption();
        }

        selectedSongOption = newOption;
    }

    private void checkForActions()
    {
        while(actions.Count > 0)
        {
            var action = actions.Dequeue();
            action();
        }
    }

    public void PlaySelectedSong()
    {
        if(selectedSongOption == null || selectedSpeedOption == null)
        {
            return;
        }

        SceneManager.LoadScene(1);        
    }

    // Download the song and put in the songs folder
    public void DownloadSong()
    {
        // Already downloading
        if (downloadingSong)
        {
            return;
        }

        runErrorMessages.Clear();

        string youtubeUrl = urlInput.text;
        System.Uri uri = new System.Uri(youtubeUrl);
        string videoId = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("v");
        string songPath = songsPath + "/" + videoId;

        if (Directory.Exists(songPath))
        {
            statusText.text = "Song is already downloaded!";
            return;
        }

        downloadingSong = true;
        progressBar.gameObject.SetActive(true);

        string cmdline = string.Format("/c \"\"{0}\" \"{1}\" -o \"{2}\" -f mp4 \"{3}\"\"", new object[] {pythonExe, youtubeDlPy, songPath + "/" + "%(title)s.%(ext)s", youtubeUrl});
        statusText.text = "Downloading Video";

        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            CreateNoWindow = true,
            Arguments = cmdline,
            FileName = "cmd",
            RedirectStandardError = true,
            UseShellExecute = false
        };
        downloadProcess = new System.Diagnostics.Process { StartInfo = startInfo, EnableRaisingEvents = true };
        downloadProcess.Exited += (sender, e) =>
        {
            actions.Enqueue(() =>
            {
                if(downloadProcess.ExitCode != 0)
                {
                    failedDownload(songPath);
                    statusText.text = "Failed to download song";
                    return;
                }

                downloadThumbnail(songPath, videoId);
                progressBar.SetPercentage(0.25f);
                statusText.text = "Extracting audio from video";
                extractAudio(songPath);
            });
        };
        downloadProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lastCmdMessage = e.Data;
                runErrorMessages.Add(e.Data);
            }
        };
        downloadProcess.Start();
        downloadProcess.BeginErrorReadLine();
    }


    // Extract the .wav file from the .mp4
    private void extractAudio(string songPath)
    {
        string songName = findSongName(songPath);
        string cmdline = string.Format("/c \"\"{0}\" -i \"{1}\" -ac 2 -f wav \"{2}\"\"", new object[] { ffmpegExe, songPath + "/" + songName + ".mp4", songPath + "/" + songName + ".wav" });

        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            CreateNoWindow = true,
            Arguments = cmdline,
            FileName = "cmd",
            RedirectStandardError = true,
            UseShellExecute = false
        };
        extractAudioProcess = new System.Diagnostics.Process { StartInfo = startInfo, EnableRaisingEvents = true };
        extractAudioProcess.Exited += (sender, e) =>
        {
            actions.Enqueue(() =>
            {
                if(extractAudioProcess.ExitCode != 0)
                {
                    failedDownload(songPath);
                    statusText.text = "Failed to extract the audio from the song";
                    return;
                }

                progressBar.SetPercentage(0.5f);
                statusText.text = "Extracting vocals from audio";
                spleeter(songPath);
            });
        };
        extractAudioProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lastCmdMessage = e.Data;
                runErrorMessages.Add(e.Data);
            }
        };
        extractAudioProcess.Start();
        extractAudioProcess.BeginErrorReadLine();

    }

    // Use spleeter to extract the audio from the song
    private void spleeter(string songPath)
    {
        string songName = findSongName(songPath);
        string inputFile = songPath + "/" + songName + ".wav";
        string cmdline = string.Format("/c \"set \"PATH=%PATH%;{0}\" && \"{1}\" separate -p spleeter:2stems -o \"{2}\" \"{3}\"\"", new object[] { ffmpegPath, spleeterExe, songPath, inputFile});

        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            CreateNoWindow = true,
            Arguments = cmdline,
            FileName = "cmd",
            RedirectStandardError = true,
            UseShellExecute = false
        };
        spleeterProcess = new System.Diagnostics.Process { StartInfo = startInfo, EnableRaisingEvents = true };
        spleeterProcess.Exited += (sender, e) =>
        {
            actions.Enqueue(() =>
            {
                if(spleeterProcess.ExitCode != 0)
                {
                    failedDownload(songPath);
                    statusText.text = "Failed to extract the vocals from the song";
                    return;
                }

                progressBar.SetPercentage(0.75f);
                statusText.text = "Creating notes from vocals";
                createNotes(songPath);
            });
        };
        spleeterProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lastCmdMessage = e.Data;
                runErrorMessages.Add(e.Data);
            }
        };

        spleeterProcess.Start();
        spleeterProcess.BeginErrorReadLine();
    }

    // Create the notes file
    private void createNotes(string songPath)
    {
        string songName = findSongName(songPath);
        string vocalsWav = songPath + "/" + songName + "/vocals.wav";
        string cmdline = string.Format("/c \"set \"PATH=%PATH%;{0}\" && \"{1}\" \"{2}\" \"{3}\"\"", new object[] { ffmpegPath, noteGenerationPy, vocalsWav, songPath });

        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            CreateNoWindow = true,
            Arguments = cmdline,
            FileName = "cmd",
            RedirectStandardError = true,
            UseShellExecute = false
        };

        noteGenerationProcess = new System.Diagnostics.Process { StartInfo = startInfo, EnableRaisingEvents = true };
        noteGenerationProcess.Exited += (sender, e) =>
        {
            actions.Enqueue(() =>
            {
                if(noteGenerationProcess.ExitCode != 0)
                {
                    failedDownload(songPath);
                    statusText.text = "Failed to create the notes for the song";
                    return;
                }
                statusText.text = "Done!";
                downloadingSong = false;

                progressBar.SetPercentage(0.0f);
                progressBar.gameObject.SetActive(false);

                clearSongOptions();
                populateSongOptions();
            });
        };
        noteGenerationProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lastCmdMessage = e.Data;
                runErrorMessages.Add(e.Data);
            }
        };
        noteGenerationProcess.Start();
        noteGenerationProcess.BeginErrorReadLine();
    }

    private void downloadThumbnail(string songPath, string videoId)
    {
        string url = string.Format("https://img.youtube.com/vi/{0}/0.jpg", videoId);

        using (WebClient client = new WebClient())
        {
            client.DownloadFileAsync(new System.Uri(url), songPath + "/" + videoId + ".jpg");
        }
    }

    public string findSongName(string songPath)
    {
        DirectoryInfo d = new DirectoryInfo(songPath);
        foreach(var fileInfo in d.GetFiles("*.mp4"))
        {
            string filePath = fileInfo.FullName;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }

        return null;
    }

    public string findSongId(string songPath)
    {
        string[] parts = songPath.Split('/');
        parts = parts[parts.Length - 1].Split('\\');
        return parts[parts.Length - 1].Replace("\\", string.Empty).Replace("/", string.Empty);
    }

    private void failedDownload(string songPath)
    {
        try
        {
            if(File.Exists(songPath + ".meta"))
            {
                File.Delete(songPath + ".meta");
            }
            Directory.Delete(songPath, true);
        }catch(System.Exception e)
        {
            Debug.Log(e.Message);
        }

        downloadingSong = false;
        progressBar.SetPercentage(0.0f);

        writeErrorLog();

    }

    private void writeErrorLog()
    {
        string logPath = Application.dataPath + "/log.txt";
        using (StreamWriter w = File.AppendText(logPath))
        {
            foreach(var line in runErrorMessages)
            {
                w.WriteLine(line);
            }
        }
    }

    // Create the songs folder if it doesn't exist
    private void initializeSongsFolder()
    {
        string directoryPath = Application.dataPath + "/" + songsFolderName;
        if(!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

}
