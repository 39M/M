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
    List<NoteObject> noteObjectList;
    float defaultNoteSpawnDistance = 15f;
    float defaultNoteSpeed = 1.5f;

    float checkStartDistance = 0.25f;
    float missDistance = -0.15f;
    float noteDestroyDistance;

    new AudioSource audio;
    AudioClip hitSoundClip;

    void Awake()
    {
        noteObjectList = new List<NoteObject>();
        audio = GetComponent<AudioSource>();

        noteDestroyDistance = Camera.main.transform.position.z;
    }

    void Start()
    {
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

        CheckHit();
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
        int osuWidth = 512 / 2;
        int osuHeight = 384 / 2;

        float worldWidth = 0.15f;
        float worldHeight = 0.075f;


        Vector3 spawnPos = new Vector3(
            worldWidth * (currentNote.x - osuWidth) / osuWidth,
            -worldHeight * (currentNote.y - osuHeight) / osuHeight,
            defaultNoteSpawnDistance
            );
        GameObject noteGameObject = Instantiate(notePrefab, spawnPos, Quaternion.identity);
        noteObjectList.Add(new NoteObject
        {
            gameObject = noteGameObject,
            collider = noteGameObject.GetComponent<Collider>(),
            note = currentNote,
        });
    }

    void MoveNotes()
    {
        int i = 0;
        while (i < noteObjectList.Count)
        {
            NoteObject noteObject = noteObjectList[i];
            GameObject noteGameObject = noteObject.gameObject;
            Note note = noteObject.note;

            float posOffset = (note.time - audio.time) * defaultNoteSpeed;
            Vector3 updatedPos = noteGameObject.transform.position;
            updatedPos.z = posOffset;
            noteGameObject.transform.position = updatedPos;

            if (noteGameObject.transform.position.z < noteDestroyDistance)
            {
                // Time to destroy
                Destroy(noteGameObject);
                noteObjectList.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    void CheckHit()
    {
        Frame frame = provider.CurrentFrame;

        int i = 0;
        while (i < noteObjectList.Count)
        {
            NoteObject noteObject = noteObjectList[i];
            GameObject noteGameObject = noteObject.gameObject;

            float posZ = noteGameObject.transform.position.z;
            if (posZ > checkStartDistance)
            {
                break;
            }
            if (posZ < missDistance)
            {
                i++;
                continue;
            }

            Collider noteCollider = noteObject.collider;
            bool hit = false;

            foreach (Hand hand in frame.Hands)
            {
                foreach (Finger finger in hand.Fingers)
                {
                    Vector3 fingerTipPosition = finger.TipPosition.ToVector3();

                    if (noteCollider.bounds.Contains(fingerTipPosition))
                    {
                        hit = true;
                        break;
                    }
                }

                if (hit)
                {
                    break;
                }
            }

            if (hit)
            {
                audio.PlayOneShot(hitSoundClip);

                Destroy(noteGameObject);
                noteObjectList.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }
}
