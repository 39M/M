using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Leap;
using Leap.Unity;

public class GameManager : MonoBehaviour
{
    LeapProvider provider;

    Music music;
    Beatmap beatmap;
    List<Note> noteList;
    List<Note>.Enumerator noteEnum;
    Note currentNote;

    public GameObject notePrefab;
    List<GameObject> noteObjectList;
    private float defaultNoteSpawnDistance = 50f;
    private float defaultNoteSpeed = 10f;
    int notePassed = 0;

    new AudioSource audio;
    AudioClip hitSoundClip;

    void Awake()
    {
        noteObjectList = new List<GameObject>();

        audio = GetComponent<AudioSource>();
    }

    void Start()
    {
        #region Json.Net Test
        string jsonString = new Music
        {
            title = "title",
            artist = "artist",
            audioFilename = "audio.mp3",
            previewTime = 0f,
            soundEffectFilename = "sound.wav",
            bannerFilename = "banner.png",
            beatmapList = new List<Beatmap>
            {
                new Beatmap
                {
                    creator = "creator",
                    version = "1.0",
                    difficulty = 1,
                    difficultyName = "Easy",
                    difficultyDisplayColor = new SimpleColor(Color.green),
                    noteList = new List<Note>
                    {
                        new Note
                        {
                            type = NoteType.Hit,
                            time = 0f,
                            speed = 1f,
                            color = new SimpleColor(Color.grey)
                        },
                        new Note
                        {
                            type = NoteType.Slide,
                            time = 1f,
                            speed = 2f,
                            color = new SimpleColor(Color.white)
                        }
                    }
                },
                new Beatmap
                {

                    creator = "creator",
                    version = "1.0",
                    difficulty = 5,
                    difficultyName = "Normal",
                    difficultyDisplayColor = new SimpleColor(Color.blue),
                    noteList = new List<Note>
                    {
                        new Note
                        {
                            type = NoteType.Hit,
                            time = 0f,
                            speed = 1f,
                            color = new SimpleColor(Color.grey)
                        },
                        new Note
                        {
                            type = NoteType.Slide,
                            time = 1f,
                            speed = 2f,
                            color = new SimpleColor(Color.white)
                        }
                    }
                }
            }
        }.ToJson();

        Debug.Log(jsonString);

        Music music = Music.FromJson(jsonString);

        Debug.Log(JsonConvert.SerializeObject(music.beatmapList[1], Formatting.Indented));
        #endregion

        // Init Leap
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;

        // Load beatmap
        var beatmapAsset = Resources.Load<TextAsset>("Music/Sample");
        music = Music.FromJson(beatmapAsset.text);
        beatmap = music.beatmapList[0];
        noteList = beatmap.noteList;
        noteEnum = noteList.GetEnumerator();
        noteEnum.MoveNext();
        currentNote = noteEnum.Current;

        // Load audio
        string audioPath = "Music/" + music.audioFilename;
        audioPath = audioPath.Remove(audioPath.LastIndexOf('.'));
        audio.clip = Resources.Load<AudioClip>(audioPath);
        audio.Play();
        hitSoundClip = Resources.Load<AudioClip>("Music/HitSound");
    }

    void Update()
    {
        CreateNotes();

        MoveNotes();

        Frame frame = provider.CurrentFrame;
        foreach (Hand hand in frame.Hands)
        {
            if (hand.IsLeft)
            {
                transform.position = hand.PalmPosition.ToVector3() +
                                     hand.PalmNormal.ToVector3() *
                                    (transform.localScale.y * .5f + .02f);
                transform.rotation = hand.Basis.rotation.ToQuaternion();
            }
        }
    }

    void CreateNotes()
    {
        // 同时可能有多个 Note, 所以用 while
        while ((currentNote != null) && TimesToCreate(currentNote))
        {
            CreateNextNote();

            if (noteEnum.MoveNext())
            {
                currentNote = noteEnum.Current;
            }
            else
            {
                // 已经生成了最后一个 Note
                currentNote = null;
            }
        }
    }

    bool TimesToCreate(Note note)
    {
        float timeAdvance = defaultNoteSpawnDistance / defaultNoteSpeed;
        return (currentNote.time - timeAdvance < audio.time);
    }

    void CreateNextNote()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), Random.Range(-1.5f, 1.5f), defaultNoteSpawnDistance);
        GameObject noteObject = Instantiate(notePrefab, spawnPos, Quaternion.identity);
        noteObjectList.Add(noteObject);
    }

    void MoveNotes()
    {
        for (int i = 0; i < noteObjectList.Count; i++)
        {
            GameObject noteObject = noteObjectList[i];
            Note note = noteList[i + notePassed];

            float posOffset = (note.time - audio.time) * defaultNoteSpeed;
            Vector3 updatedPos = noteObject.transform.position;
            updatedPos.z = posOffset;
            noteObject.transform.position = updatedPos;

            if (noteObject.transform.position.z < 0)
            {
                Destroy(noteObject);
                noteObjectList[i] = null;

                audio.PlayOneShot(hitSoundClip);
            }
        }

        while (noteObjectList.Count > 0 && noteObjectList[0] == null)
        {
            noteObjectList.RemoveAt(0);
            notePassed++;
        }
    }
}
