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

    float noteSpawnPosXMultiplier;
    float noteSpawnPosYMultiplier;

    new AudioSource audio;
    AudioClip hitSoundClip;

    void Awake()
    {
        noteObjectList = new List<NoteObject>();
        audio = GetComponent<AudioSource>();

        Camera camera = Camera.main;

        //noteSpawnPosYMultiplier = Mathf.Tan(camera.fieldOfView / 2 * Mathf.Deg2Rad) * defaultNoteSpawnDistance;
        //noteSpawnPosXMultiplier = noteSpawnPosYMultiplier * camera.aspect;

        noteDestroyDistance = camera.transform.position.z;

        noteSpawnPosXMultiplier = defaultNoteSpawnDistance / -noteDestroyDistance / 3;
        noteSpawnPosYMultiplier = defaultNoteSpawnDistance / -noteDestroyDistance / 3;
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

        Vector3 targetPos = new Vector3
        {
            x = worldWidth * (currentNote.x - osuWidth) / osuWidth,
            y = -worldHeight * (currentNote.y - osuHeight) / osuHeight,
            z = 0,
        };

        Vector3 spawnPos = new Vector3
        {
            x = targetPos.x * noteSpawnPosXMultiplier,
            y = targetPos.y * noteSpawnPosYMultiplier,
            z = defaultNoteSpawnDistance,
        };

        GameObject noteGameObject = Instantiate(notePrefab, spawnPos, Quaternion.identity);

        noteObjectList.Add(new NoteObject
        {
            gameObject = noteGameObject,
            collider = noteGameObject.GetComponent<Collider>(),
            note = currentNote,
            spawnPosition = spawnPos,
            targetPosition = targetPos,
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

            if (noteGameObject.transform.position.z > 0)
            {
                float totalTime = defaultNoteSpawnDistance / defaultNoteSpeed;
                float t = (audio.time - (note.time - totalTime)) / totalTime;
                noteGameObject.transform.position = Vector3.LerpUnclamped(noteObject.spawnPosition, noteObject.targetPosition, t);
            }
            else
            {
                Vector3 targetPos = noteObject.targetPosition;
                targetPos.z = (note.time - audio.time) * defaultNoteSpeed;
                noteGameObject.transform.position = targetPos;
            }

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
