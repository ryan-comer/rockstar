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

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
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
    }

    // Update is called once per frame
    void Update()
    {
        checkForActions();
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

            activeSongOptions.Add(newOption);
        }
    }

    public SongOption GetSelectedSongOption()
    {
        return selectedSongOption;
    }

    public void UpdateSelectedSongOption(SongOption newOption)
    {
        if (selectedSongOption != null)
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
        if(selectedSongOption == null)
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
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        System.Diagnostics.Process downloadProcess = System.Diagnostics.Process.Start(startInfo);
        downloadProcess.EnableRaisingEvents = true;
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
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        System.Diagnostics.Process downloadProcess = System.Diagnostics.Process.Start(startInfo);
        downloadProcess.EnableRaisingEvents = true;
        downloadProcess.Exited += (sender, e) =>
        {
            actions.Enqueue(() =>
            {
                if(downloadProcess.ExitCode != 0)
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
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        System.Diagnostics.Process spleeterProcess = System.Diagnostics.Process.Start(startInfo);
        spleeterProcess.EnableRaisingEvents = true;
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
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        System.Diagnostics.Process noteGenerationProcess = System.Diagnostics.Process.Start(startInfo);
        noteGenerationProcess.EnableRaisingEvents = true;
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
            return fileInfo.Name.Split('.')[0];
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
        Directory.Delete(songPath);
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
