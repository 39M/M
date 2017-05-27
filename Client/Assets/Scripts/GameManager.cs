using DG.Tweening;
using Leap;
using Leap.Unity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    float missDistance = 0f;
    float noteDestroyDistance;

    float noteSpawnPosXMultiplier;
    //float noteSpawnPosYMultiplier;
    float noteSpawnDispersion = 1.5f;

    public GameObject hitParticlePrefab;
    public GameObject missParticlePrefab;

    new AudioSource audio;
    AudioClip hitSoundClip;
    new Camera camera;

    [SerializeField]
    int hitCount = 0;
    [SerializeField]
    int missCount = 0;
    [SerializeField]
    int comboCount = 0;
    [SerializeField]
    int maxCombo = 0;
    [SerializeField]
    float score = 0;
    const float maxScore = 1000000;
    const float hitScorePercentage = 0.8f;
    float displayScore = 0;

    public Slider hpBar;
    public Text judgementLabel;
    public Tweener judgementLabelTweener;
    public UnityEngine.UI.Image judgementLine;

    bool gameEnd = false;

    Transform canvasTransform;
    Vector3 canvasBaseRotation;
    Vector3 canvasVelocity;

    void Awake()
    {
        noteObjectList = new List<NoteObject>();
        audio = GetComponent<AudioSource>();
        canvasTransform = GameObject.Find("Canvas").transform;
        canvasBaseRotation = canvasTransform.eulerAngles;
        //Debug.Log(canvasTransform.forward);

        camera = Camera.main;

        //noteSpawnPosYMultiplier = Mathf.Tan(camera.fieldOfView / 2 * Mathf.Deg2Rad) * defaultNoteSpawnDistance;
        //noteSpawnPosXMultiplier = noteSpawnPosYMultiplier * camera.aspect;

        noteDestroyDistance = missDistance;

        noteSpawnPosXMultiplier = defaultNoteSpawnDistance / -camera.transform.position.z * noteSpawnDispersion;
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
        //int remainNote = 100;
        //noteList.RemoveRange(remainNote, noteList.Count - remainNote);
        noteEnum = noteList.GetEnumerator();
        noteEnum.MoveNext();
        currentNote = noteEnum.Current;

        // Load audio
        audio.clip = Utils.LoadAudio(music.audioFilename);
        audio.Play();
        hitSoundClip = Utils.LoadSoundEffect("HitSound.wav");

        // Init UI
        hpBar.value = 1;
        judgementLabel.text = "";
    }

    void Update()
    {
        MoveCameraWithHands();
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
                //leftHandMarker.transform.rotation = rotation;
            }
            else if (hand.IsRight)
            {
                rightHandMarker.transform.position = position;
                //rightHandMarker.transform.rotation = rotation;
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
        CreateHitParticle(noteGameObject.transform.position, hitParticlePrefab);
        ShowHitJudgement();

        Destroy(noteGameObject);
        noteObjectList.RemoveAt(i);

        if (noteObjectList.Count <= 0)
        {
            EndGame();
        }
    }

    void MissedNote(int i)
    {
        missCount++;
        comboCount = 0;

        ShowMissJudgement();

        GameObject noteGameObject = noteObjectList[i].gameObject;
        CreateHitParticle(noteGameObject.transform.position, missParticlePrefab);
        Destroy(noteGameObject);
        noteObjectList.RemoveAt(i);

        if (noteObjectList.Count <= 0)
        {
            EndGame();
        }
    }

    void CreateHitParticle(Vector3 position, GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }
        var p = Instantiate(prefab);
        p.transform.position = position;
        Destroy(p, p.GetComponent<ParticleSystem>().main.duration);
    }

    float judgementScaleTweenDuration = 0.025f;
    float judgementScaleTweenDelay = 0.05f;
    void ShowHitJudgement()
    {
        ShowJudgement(GameConst.HIT_NAME, 250, GameConst.HIT_COLOR, GameConst.JUDGEMENT_LINE_HIT_COLOR);
        ScreenShake(0.01f);
    }

    void ShowMissJudgement()
    {
        ShowJudgement(GameConst.MISS_NAME, 200, GameConst.MISS_COLOR, GameConst.JUDGEMENT_LINE_MISS_COLOR);
    }

    void ShowJudgement(string name, int fontSize, Color color, Color judgementLineColor)
    {
        judgementLabel.text = name;
        judgementLabel.fontSize = fontSize;

        if (judgementLabelTweener != null && judgementLabelTweener.IsPlaying())
        {
            DOTween.Kill(judgementLabel);
        }

        judgementLabel.DOColor(color, judgementScaleTweenDuration);
        judgementLabel.transform.DOScale(1.3f, judgementScaleTweenDuration).OnComplete(() =>
        {
            judgementLabel.transform.DOScale(1f, judgementScaleTweenDuration).SetDelay(judgementScaleTweenDelay);
            judgementLabel.DOFade(0.1f, judgementScaleTweenDuration).SetDelay(judgementScaleTweenDelay).OnComplete(() =>
            {
                judgementLabelTweener = judgementLabel.DOFade(0, 2).SetDelay(0.5f);
            });
        });

        judgementLine.DOColor(judgementLineColor, judgementScaleTweenDuration).OnComplete(() =>
        {
            judgementLine.DOColor(GameConst.JUDGEMENT_LINE_DEFAULT_COLOR, judgementScaleTweenDuration).SetDelay(judgementScaleTweenDelay);
        });
    }

    void ScreenShake(float range)
    {
        Vector3 shakeDirection = new Vector3(Random.Range(-range, range), Random.Range(-range, range));
        camera.transform.DOMove(camera.transform.position + shakeDirection, judgementScaleTweenDuration).OnComplete(() =>
        {
            camera.transform.DOMove(camera.transform.position - shakeDirection, judgementScaleTweenDuration);
        });
    }

    void EndGame()
    {
        gameEnd = true;

        RuntimeData.hitCount = hitCount;
        RuntimeData.missCount = missCount;
        RuntimeData.maxCombo = maxCombo;
        RuntimeData.score = Mathf.RoundToInt(score);

        float audioFadeDelay = 2;
        float audioFadeTime = 5;
        audio.DOFade(0, audioFadeTime).SetDelay(audioFadeDelay).Play();

        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("Grade");
        }, audioFadeDelay + audioFadeTime - 1);
    }

    void MoveCameraWithHands()
    {
        if (provider.CurrentFrame.Hands.Count >= 2)
        {
            Hand hand1 = provider.CurrentFrame.Hands[0];
            Hand hand2 = provider.CurrentFrame.Hands[1];

            Vector3 mid = Vector3.Lerp(hand1.PalmPosition.ToVector3(), hand2.PalmPosition.ToVector3(), 0.5f) / 3;
            mid.y = 0;
            mid += new Vector3(0, 0.1f, 0);

            camera.transform.LookAt(mid);
            Vector3 rotation = camera.transform.eulerAngles;
            rotation.z = mid.x * 125;
            camera.transform.eulerAngles = rotation;
        }
    }
}
