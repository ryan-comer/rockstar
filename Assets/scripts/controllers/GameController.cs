using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class GameController : MonoBehaviour
{

    public SongNotesController songNotesController;
    public GameObject videoTarget;

    private RenderTexture videoRenderTexture;
    private VideoPlayer videoPlayer;

    // Start is called before the first frame update
    void Start()
    {
        videoRenderTexture = videoTarget.GetComponent<MeshRenderer>().material.mainTexture as RenderTexture;
        videoPlayer = videoTarget.GetComponent<VideoPlayer>();

        initializeSong();

        videoPlayer.Play();
        songNotesController.StartSong();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (songNotesController.IsRandomNotesActive)
            {
                songNotesController.StopRandomNotes();
            }
            else
            {
                songNotesController.StartRandomNotes();
            }
        } 
    }

    private void initializeSong()
    {
        string songId = SongController.Instance.GetSelectedSongOption().SongId;
        string songPath = Application.dataPath + "/" + "songs" + "/" + songId;
        string songName = SongController.Instance.findSongName(songPath);

        string notesPath = songPath + "/" + "notes.txt";

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
