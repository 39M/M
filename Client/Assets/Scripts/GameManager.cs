using DG.Tweening;
using Leap;
using Leap.Unity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    LeapProvider provider;

    public GameObject leftHandMarker;
    public GameObject rightHandMarker;

    Music music;
    Beatmap beatmap;
    List<Note> noteList;
    List<Note>.Enumerator noteEnum;
    Note currentNote;

    public GameObject notePrefab;
    List<NoteObject> noteObjectList;
    float defaultNoteSpawnDistance = 15f;
    float defaultNoteSpeed = 1f;

    float checkStartDistance = 0.25f;
    float missDistance = -0.15f;
    float noteDestroyDistance;

    float noteSpawnPosXMultiplier;
    //float noteSpawnPosYMultiplier;
    float noteSpawnDispersion = 1.5f;

    public GameObject particlePrefab;

    new AudioSource audio;
    AudioClip hitSoundClip;

    [SerializeField]
    int hitCount = 0;
    [SerializeField]
    int comboCount = 0;
    [SerializeField]
    int maxCombo = 0;
    [SerializeField]
    float score = 0;
    const float maxScore = 1000000;
    const float hitScorePercentage = 0.8f;
    float displayScore = 0;

    bool gameEnd = false;

    void Awake()
    {
        noteObjectList = new List<NoteObject>();
        audio = GetComponent<AudioSource>();

        Camera camera = Camera.main;

        //noteSpawnPosYMultiplier = Mathf.Tan(camera.fieldOfView / 2 * Mathf.Deg2Rad) * defaultNoteSpawnDistance;
        //noteSpawnPosXMultiplier = noteSpawnPosYMultiplier * camera.aspect;

        noteDestroyDistance = camera.transform.position.z;

        noteSpawnPosXMultiplier = defaultNoteSpawnDistance / -noteDestroyDistance * noteSpawnDispersion;
        //noteSpawnPosYMultiplier = defaultNoteSpawnDistance / -noteDestroyDistance * noteSpawnDispersion;
    }

    void Start()
    {
        // Init Leap
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;

        // Load beatmap
        music = RuntimeData.selectedMusic;
        if (music == null)
        {
            var beatmapAsset = Resources.Load<TextAsset>(GameConst.BEATMAP_PATH + "Croatian_Rhapsody");
            music = Music.FromJson(beatmapAsset.text);
        }
        beatmap = RuntimeData.selectedBeatmap;
        if (beatmap == null)
        {
            beatmap = music.beatmapList[0];
        }
        noteList = beatmap.noteList;
        //int remainNote = 20;
        //noteList.RemoveRange(remainNote, noteList.Count - remainNote);
        noteEnum = noteList.GetEnumerator();
        noteEnum.MoveNext();
        currentNote = noteEnum.Current;

        // Load audio
        audio.clip = Utils.LoadAudio(music.audioFilename);
        audio.Play();
        hitSoundClip = Utils.LoadSoundEffect("HitSound.wav");
    }

    void Update()
    {
        MoveHandMarker();

        if (!gameEnd)
        {
            CreateNotes();
            MoveNotes();

            CheckHit();
        }
    }

    public void SkipPreview()
    {
        if (noteList[0].time - audio.time > 4f)
        {
            audio.DOFade(0, 0.5f).Play().OnComplete(() =>
            {
                audio.time = noteList[0].time - 3f;
                audio.DOFade(1, 0.5f).Play();
            });
            Utils.FadeOut(0.5f, () =>
            {
                Utils.FadeIn(0.5f);
            });
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
        int osuWidth = 512 / 2;
        int osuHeight = 384 / 2;

        float worldWidth = 0.25f;
        //float worldHeight = 0.03f;

        Vector3 targetPos = new Vector3
        {
            x = worldWidth * (currentNote.x - osuWidth) / osuWidth,
            //y = -worldHeight * (currentNote.y - osuHeight) / osuHeight,
            y = 0,
            z = 0,
        };

        float baseSpawnY = 6f;
        float floatRangeY = 1.5f;

        Vector3 spawnPos = new Vector3
        {
            x = targetPos.x * noteSpawnPosXMultiplier,
            //y = targetPos.y * noteSpawnPosYMultiplier + 6,
            y = baseSpawnY + floatRangeY * (currentNote.y - osuHeight) / osuHeight,
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

            float totalTime = defaultNoteSpawnDistance / defaultNoteSpeed;
            float t = (audio.time - (note.time - totalTime)) / totalTime;
            noteGameObject.transform.position = Vector3.LerpUnclamped(noteObject.spawnPosition, noteObject.targetPosition, t);

            #region Legacy move logic
            //if (noteGameObject.transform.position.z > 0)
            //{
            //    float totalTime = defaultNoteSpawnDistance / defaultNoteSpeed;
            //    float t = (audio.time - (note.time - totalTime)) / totalTime;
            //    noteGameObject.transform.position = Vector3.LerpUnclamped(noteObject.spawnPosition, noteObject.targetPosition, t);
            //}
            //else
            //{
            //    float totalTime = defaultNoteSpawnDistance / defaultNoteSpeed;
            //    float t = (audio.time - (note.time - totalTime)) / totalTime;
            //    Vector3 position = Vector3.LerpUnclamped(noteObject.spawnPosition, noteObject.targetPosition, t);
            //    position.x = noteObject.targetPosition.x;
            //    noteGameObject.transform.position = position;
            //}
            #endregion

            if (noteGameObject.transform.position.z < noteDestroyDistance)
            {
                MissedNote(i);
            }
            else
            {
                i++;
            }
        }
    }

    void MoveHandMarker()
    {
        Frame frame = provider.CurrentFrame;

        foreach (Hand hand in frame.Hands)
        {
            var position = Vector3.zero;
            position.x = Mathf.Clamp(hand.PalmPosition.x, -0.3f, 0.3f);

            var rotation = Quaternion.identity;
            rotation.z = hand.Rotation.z;

            if (hand.IsLeft)
            {
                leftHandMarker.transform.position = position;
                leftHandMarker.transform.rotation = rotation;
            }
            else if (hand.IsRight)
            {
                rightHandMarker.transform.position = position;
                rightHandMarker.transform.rotation = rotation;
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

            #region Legacy check hit logic
            //foreach (Hand hand in frame.Hands)
            //{
            //    foreach (Finger finger in hand.Fingers)
            //    {
            //        Vector3 fingerTipPosition = finger.TipPosition.ToVector3();

            //        if (noteCollider.bounds.Contains(fingerTipPosition))
            //        {
            //            hit = true;
            //            break;
            //        }
            //    }

            //    if (hit)
            //    {
            //        break;
            //    }
            //}
            #endregion

            if (noteCollider.bounds.Contains(leftHandMarker.transform.position)
                || noteCollider.bounds.Contains(rightHandMarker.transform.position))
            {
                hit = true;
            }

            if (hit)
            {
                HitNote(i);
            }
            else
            {
                i++;
            }
        }
    }

    void HitNote(int i)
    {
        hitCount++;
        // Percentage x Single Hit Score
        score += (maxScore * hitScorePercentage) * 1 / noteList.Count;

        comboCount++;
        if (comboCount > maxCombo)
        {
            maxCombo = comboCount;
        }
        // Percentage x Combo Count x Single Combo Score
        score += (maxScore * (1 - hitScorePercentage)) * comboCount / (noteList.Count * (noteList.Count + 1) / 2);

        // Snap to max score
        if (comboCount == noteList.Count)
        {
            score = maxScore;
        }

        GameObject noteGameObject = noteObjectList[i].gameObject;
        audio.PlayOneShot(hitSoundClip);
        CreateHitParticle(noteGameObject.transform.position);

        Destroy(noteGameObject);
        noteObjectList.RemoveAt(i);

        if (noteObjectList.Count <= 0)
        {
            EndGame();
        }
    }

    void MissedNote(int i)
    {
        comboCount = 0;

        GameObject noteGameObject = noteObjectList[i].gameObject;
        Destroy(noteGameObject);
        noteObjectList.RemoveAt(i);

        if (noteObjectList.Count <= 0)
        {
            EndGame();
        }
    }

    void CreateHitParticle(Vector3 position)
    {
        var p = Instantiate(particlePrefab);
        p.transform.position = position;
        Destroy(p, p.GetComponent<ParticleSystem>().main.duration);
    }

    void EndGame()
    {
        gameEnd = true;

        RuntimeData.hitCount = hitCount;
        RuntimeData.maxCombo = maxCombo;
        RuntimeData.score = Mathf.RoundToInt(score);

        float audioFadeDelay = 2;
        float audioFadeTime = 5;
        audio.DOFade(0, audioFadeTime).SetDelay(audioFadeDelay).Play();

        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("SelectMusic");
        }, audioFadeDelay + audioFadeTime - 1);
    }
}
