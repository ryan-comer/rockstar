using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    public int scorePerNote = 100;
    public SongNotesController songNotesController;
    public GameObject videoTarget;
    public FrameManager frameManager;
    public Slider volumeSlider;
    public Text scoreText;

    private RenderTexture videoRenderTexture;
    private VideoPlayer videoPlayer;

    private bool isPaused = false;

    private int currentScore = 0;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        videoRenderTexture = videoTarget.GetComponent<MeshRenderer>().material.mainTexture as RenderTexture;
        videoPlayer = videoTarget.GetComponent<VideoPlayer>();

        initializeSong();
        volumeSlider.value = 0.2f;

        videoPlayer.Play();
        songNotesController.StartSong();
    }

    // Update is called once per frame
    void Update()
    {
        checkInput();
    }

    private void checkInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            togglePause();
        }
    }

    public void NoteHit()
    {
        currentScore += scorePerNote;
        scoreText.text = "Score: " + currentScore.ToString();
    }

    public void ToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void UpdateVolume(float newVolume)
    {
        songNotesController.UpdateVolume(newVolume);
    }

    private void togglePause()
    {
        if (isPaused)
        {
            if (frameManager.IsAnimating)
            {
                return;
            }

            songNotesController.ResumeSong();
            videoPlayer.Play();
            frameManager.HideActiveFrame();
            isPaused = false;
        }
        else
        {
            if (frameManager.IsAnimating)
            {
                return;
            }

            videoPlayer.Pause();
            songNotesController.PauseSong();
            frameManager.ShowFrame(0);
            isPaused = true;
        }
    }

    private void initializeSong()
    {
        string songId = SongController.Instance.GetSelectedSongOption().SongId;
        string songPath = Application.dataPath + "/" + "songs" + "/" + songId;
        string songName = SongController.Instance.findSongName(songPath);

        string notesPath = songPath + "/" + "notes.txt";

        switch (SongController.Instance.GetSelectedSpeedOption().gameSpeed)
        {
            case (GameSpeed.SLOW):
                songNotesController.noteTimeToHit = 3.0f;
                break;
            case (GameSpeed.MEDIUM):
                songNotesController.noteTimeToHit = 2.5f;
                break;
            case (GameSpeed.FAST):
                songNotesController.noteTimeToHit = 1.5f;
                break;
        }

        songNotesController.numNoteColums = SongController.Instance.GetNumberOfKeys();

        // Init audio
        AudioClip audioClip = createAudioClip(songId, songPath + "/" + songName + ".wav");
        songNotesController.Init(audioClip, notesPath);

        // Init video
        videoPlayer.url = songPath + "/" + songName + ".mp4";
        videoPlayer.SetDirectAudioVolume(0, 0.0f);
    }

    private AudioClip createAudioClip(string songId, string audioFilePath)
    {
        byte[] wav = System.IO.File.ReadAllBytes(audioFilePath);

        int channels = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels
        int sampleRate = wav[24] + (wav[25] << 8) + (wav[26] << 16) + (wav[27] << 24);
        int lengthSamples = wav.Length - 44;
        int bitsPerSample = wav[34] + (wav[35] << 8);
        int bytesPerSample = bitsPerSample / 8;

        List<float> data = new List<float>();
        for(int i = 44; i < wav.Length; i += bytesPerSample)
        {
            short sample = 0;
            for(int j = 0; j < bytesPerSample; j++)
            {
                sample += (short)(wav[i + j] << (j*8));
            }

            float sampleF = 0;
            if(bytesPerSample == 1)
            {
                sampleF = (float)sample / 255.0f;
            }else if(bytesPerSample == 2)
            {
                sampleF = (float)sample / 32767.0f;
            }

            data.Add(sampleF);
        }

        AudioClip audioClip = AudioClip.Create(songId, lengthSamples, channels, sampleRate, false);
        audioClip.SetData(data.ToArray(), 0);

        Debug.Log("Done with audio clip");

        return audioClip;
    }
}
