using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongNotesController : MonoBehaviour
{

    public static SongNotesController Instance;

    public Transform[] noteRails;
    public Transform noteStart, noteTrigger;
    public float triggerRadius = 5.0f;
    public Note note_p;
    public float noteTimeToHit = 5.0f;  // Time for the note to hit the trigger area (in seconds)
    public int numNoteColums = 4;

    public Color[] noteColors;

    public AudioSource audioSource;

    private Coroutine randomNotesCoroutineObject;
    private List<Tuple<int, int>> notes = new List<Tuple<int, int>>();  // <note_time_ms, note_frequency>
    private int[] possibleNoteIndices;

    private AudioClip songClip;

    private int currentNoteIndex = 0;

    private ObjectPool objectPool;
    private HashSet<Note> activeNotes = new HashSet<Note>();

    // Used to figure out the relative pitch between notes
    private string[] noteOrder = new string[] {"C0", "C#0", "D0", "D#0", "E0", "F0", "F#0", "G0", "G#0", "A0", "A#0", "B0", 
        "C1", "C#1", "D1", "D#1", "E1", "F1", "F#1", "G1", "G#1", "A1", "A#1", "B1", 
        "C2", "C#2", "D2", "D#2", "E2", "F2", "F#2", "G2", "G2#", "A2", "A2#", "B2", 
        "C3", "C3#", "D3", "D3#", "E3", "F3", "F3#", "G3", "G3#", "A3", "A3#", "B3", 
        "C4", "C4#", "D4", "D4#", "E4", "F4", "F4#", "G4", "G4#", "A4", "A4#", "B4", 
        "C5", "C5#", "D5", "D5#", "E5", "F5", "F5#", "G5", "G5#", "A5", "A5#", "B5", 
        "C6", "C6#", "D6", "D6#", "E6", "F6", "F6#", "G6", "G6#", "A6", "A6#", "B6", 
        "C7", "C7#", "D7", "D7#", "E7", "F7", "F7#", "G7", "G7#", "A7", "A7#", "B7", 
        "C8", "C8#", "D8", "D8#", "E8", "F8", "F8#", "G8", "G8#", "A8", "A8#", "B8", "Beyond B8"};

    public bool IsRandomNotesActive
    {
        get
        {
            return randomNotesCoroutineObject != null;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public void Init(AudioClip audioClip, string notesFile)
    {
        this.songClip = audioClip;
        SortedSet<int> possibleNotesSet = new SortedSet<int>();

        string[] lines = System.IO.File.ReadAllLines(notesFile);
        for(var i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] data = line.Split(',');
            int noteTimeMs = int.Parse(data[0].Trim());
            string note = data[1].Trim();
            int notePosition = findNoteIndex(note);

            notes.Add(new Tuple<int, int>(noteTimeMs, notePosition));
            possibleNotesSet.Add(notePosition);
        }

        possibleNoteIndices = new int[possibleNotesSet.Count];
        possibleNotesSet.CopyTo(possibleNoteIndices);
    }

    // Start is called before the first frame update
    void Start()
    {
        //objectPool = GetComponent<ObjectPool>();
        //objectPool.AddToPool(0, 50);
    }

    // Update is called once per frame
    void Update()
    {
        createNotes();
        checkInput();
    }

    public void UpdateVolume(float newVolume)
    {
        newVolume = Mathf.Clamp(newVolume, 0.0f, 1.0f);
        audioSource.volume = newVolume;
    }

    public void RemoveNote(Note note)
    {
        activeNotes.Remove(note);
        Destroy(note.gameObject);
    }

    // See if you need to create any notes
    private void createNotes()
    {
        if(currentNoteIndex >= notes.Count)
        {
            return;
        }

        float timeUntilNote = 0.0f;
        if (checkCreateNote(notes[currentNoteIndex], out timeUntilNote))
        {
            spawnNote(getRailNumber(notes[currentNoteIndex]), timeUntilNote);
            currentNoteIndex += 1;
        }
    }

    private int getRailNumber(Tuple<int, int> note)
    {
        int notePosition = note.Item2;
        int railNumber = System.Array.IndexOf(possibleNoteIndices, notePosition) % numNoteColums;

        return railNumber;
    }

    // Check if the note should be played yet
    private bool checkCreateNote(Tuple<int, int> note, out float timeUntilNote)
    {
        float secondsInSong = note.Item1 / 1000.0f;
        timeUntilNote = secondsInSong - audioSource.time;

        if(secondsInSong < noteTimeToHit)
        {
            // Too early in the song - skip this note
            currentNoteIndex += 1;
            return false;
        }

        if(secondsInSong - audioSource.time <= noteTimeToHit)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    
    public void StartSong()
    {
        audioSource.clip = songClip;
        audioSource.Play();
    }

    public void PauseSong()
    {
        audioSource.Pause();
        foreach(Note note in activeNotes)
        {
            note.IsFalling = false;
        }
    }

    public void ResumeSong()
    {
        audioSource.Play();
        foreach(Note note in activeNotes)
        {
            note.IsFalling = true;
        }
    }

    public void StartRandomNotes()
    {
        randomNotesCoroutineObject = StartCoroutine(randomNotesCoroutine());
    }

    public void StopRandomNotes()
    {
        StopCoroutine(randomNotesCoroutineObject);
        randomNotesCoroutineObject = null;
    }

    private int findNoteIndex(string note)
    {
        for(var i = 0; i < noteOrder.Length; i++)
        {
            if (noteOrder[i].Equals(note))
            {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator randomNotesCoroutine()
    {
        while (true)
        {
            spawnNote(UnityEngine.Random.Range(0, 8), 5.0f);
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void spawnNote(int railNumber, float timeUntilNote)
    {
        Vector3 startingPosition = noteStart.position;
        startingPosition.x = noteRails[railNumber].position.x;

        // Get note speed
        float distanceToTravel = noteStart.position.y - noteTrigger.position.y;
        float distancePerSecond = distanceToTravel / timeUntilNote;

        Note newNote = Instantiate(note_p);
        newNote.distancePerSecond = distancePerSecond;
        newNote.transform.position = startingPosition;
        Color noteColor = noteColors[railNumber];

        newNote.GetComponentInChildren<Light>().color = noteColor;

        Material newMaterial = Material.Instantiate(newNote.GetComponent<MeshRenderer>().material);
        newMaterial.color = noteColor;
        newMaterial.SetColor("_EmissionColor", noteColor);
        newMaterial.EnableKeyword("_EMISSION");

        newNote.GetComponent<MeshRenderer>().material = newMaterial;

        newNote.IsFalling = true;

        activeNotes.Add(newNote);

        //StartCoroutine(returnNoteCoroutine(newObject));
    }

    private IEnumerator returnNoteCoroutine(GameObject gameObject)
    {
        yield return new WaitForSeconds(10.0f);
        objectPool.ReturnToPool(0, gameObject);
    }

    private void checkInput()
    {
        Note[] notesHit = checkNotesHit();
        addPointsForNotes(notesHit);
    }

    private void addPointsForNotes(Note[] notesHit)
    {
        if(notesHit.Length == 0)
        {
            return;
        }

        // Find lowest note
        Note lowestNote = notesHit[0];
        foreach(var note in notesHit)
        {
            if(note.transform.position.y < lowestNote.transform.position.y)
            {
                lowestNote = note;
            }
        }

        GameController.Instance.NoteHit();
        Destroy(lowestNote.gameObject);
    }

    private Note[] checkNotesHit()
    {
        List<Note> notesHit = new List<Note>();
        
        // Check for rail keypress
        if (Input.GetKeyDown(KeyCode.Q))
        {
            notesHit.AddRange(checkNoteTrigger(0));
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            notesHit.AddRange(checkNoteTrigger(1));
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            notesHit.AddRange(checkNoteTrigger(2));
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            notesHit.AddRange(checkNoteTrigger(3));
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            notesHit.AddRange(checkNoteTrigger(4));
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            notesHit.AddRange(checkNoteTrigger(5));
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            notesHit.AddRange(checkNoteTrigger(6));
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            notesHit.AddRange(checkNoteTrigger(7));
        }

        return notesHit.ToArray();
    }

    private Note[] checkNoteTrigger(int railNumber)
    {
        Transform rail = noteRails[railNumber];
        Vector3 triggerStart = noteTrigger.position;
        triggerStart.x = rail.position.x;

        Collider[] colliders = Physics.OverlapBox(triggerStart, Vector3.one * triggerRadius, Quaternion.identity, LayerMask.GetMask(new string[] { "note" }));
        List<Note> notesHit = new List<Note>();
        foreach(var collider in colliders)
        {
            notesHit.Add(collider.GetComponent<Note>());
        }

        return notesHit.ToArray();
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(noteTrigger.position, Vector3.one * triggerRadius);
    }

}
