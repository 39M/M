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
    float noteSpawnAdvanceTime;

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

    BeatDetection beatDetector;
    AudioSource detectionAudio;

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

    void Awake()
    {
        noteObjectList = new List<NoteObject>();

        camera = Camera.main;

        audio = GetComponent<AudioSource>();

        Transform beatDetectorObject = transform.GetChild(0);
        if (RuntimeData.useCustomMusic)
        {
            beatDetector = beatDetectorObject.GetComponent<BeatDetection>();
            detectionAudio = beatDetectorObject.GetComponent<AudioSource>();
        }
        else
        {
            beatDetectorObject.gameObject.SetActive(false);
        }

        //noteSpawnPosYMultiplier = Mathf.Tan(camera.fieldOfView / 2 * Mathf.Deg2Rad) * defaultNoteSpawnDistance;
        //noteSpawnPosXMultiplier = noteSpawnPosYMultiplier * camera.aspect;

        noteDestroyDistance = missDistance;

        noteSpawnPosXMultiplier = defaultNoteSpawnDistance / -camera.transform.position.z * noteSpawnDispersion;
        //noteSpawnPosYMultiplier = defaultNoteSpawnDistance / -noteDestroyDistance * noteSpawnDispersion;

        noteSpawnAdvanceTime = defaultNoteSpawnDistance / defaultNoteSpeed;
    }

    void Start()
    {
        // Init Leap
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;

        // Init Music
        if (RuntimeData.useCustomMusic)
        {
            InitWithCustomMusic();
        }
        else
        {
            InitWithBuildInBeatmap();
        }

        // Init UI
        hpBar.value = 1;
        judgementLabel.text = "";
    }

    void InitWithBuildInBeatmap()
    {
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

        // Load and play audio
        audio.clip = Utils.LoadAudio(music.audioFilename);
        if (currentNote.time < noteSpawnAdvanceTime / 2)
        {
            audio.PlayDelayed(2f);
        }
        else
        {
            audio.Play();
        }

        if (music.soundEffectFilename != null && music.soundEffectFilename.Length > 0)
        {
            hitSoundClip = Utils.LoadSoundEffect(music.soundEffectFilename);
        }
        else
        {
            hitSoundClip = Utils.LoadSoundEffect(GameConst.DEFAULT_SOUND_EFFECT_FILENAME);
        }
    }

    void InitWithCustomMusic()
    {
        // Set audio and play
        Debug.Log(RuntimeData.selectedClip + RuntimeData.selectedClip.name);
        audio.clip = RuntimeData.selectedClip;
        hitSoundClip = Utils.LoadSoundEffect(GameConst.DEFAULT_SOUND_EFFECT_FILENAME);

        beatDetector.CallBackFunction = OnBeat;
        detectionAudio.clip = RuntimeData.selectedClip;
        detectionAudio.pitch = 7.5f;
        detectionAudio.Play();

        StartCoroutine(Utils.WaitAndAction(noteSpawnAdvanceTime / detectionAudio.pitch, () =>
        {
            detectionAudio.pitch = 1;
            audio.Play();
        }));
    }

    void OnBeat(BeatDetection.EventInfo eventInfo)
    {
        CreateOneNote(GetRandomNote());

        //switch (eventInfo.messageInfo)
        //{
        //    case BeatDetection.EventType.Energy:
        //        break;
        //    case BeatDetection.EventType.HitHat:
        //        break;
        //    case BeatDetection.EventType.Kick:
        //        break;
        //    case BeatDetection.EventType.Snare:
        //        break;
        //}
    }

    int osuMaxX = 512;
    int osuMaxY = 384;
    Note lastRandomNote;
    Note GetRandomNote()
    {
        int posX;
        int posY;

        if (lastRandomNote == null)
        {
            posX = Random.Range(0, osuMaxX);
            posY = Random.Range(0, osuMaxY);
        }
        else
        {
            float deltaTime = detectionAudio.time - lastRandomNote.time;
            deltaTime = Mathf.Clamp01(deltaTime);
            int rangeX = (int)(deltaTime * 512);
            int rangeY = (int)(deltaTime * 384);
            posX = lastRandomNote.x + Random.Range(Mathf.Clamp(-rangeX, -lastRandomNote.x, 0), Mathf.Clamp(rangeX, 0, osuMaxX - lastRandomNote.x));
            posY = lastRandomNote.y + Random.Range(Mathf.Clamp(-rangeY, -lastRandomNote.y, 0), Mathf.Clamp(rangeY, 0, osuMaxX - lastRandomNote.y));
        }

        Note note = new Note
        {
            time = detectionAudio.time,
            x = posX,
            y = posY,
        };
        lastRandomNote = note;
        return note;
    }

    void Update()
    {
        MoveCameraWithHands();
        MoveHandMarker();

        if (!gameEnd)
        {
            if (RuntimeData.useCustomMusic)
            {
                CustomModeUpdate();
            }
            else
            {
                BuildInModeUpdate();
            }
        }
    }

    void BuildInModeUpdate()
    {
        CreateNotes();
        MoveNotes();
        CheckHit();
    }

    void CustomModeUpdate()
    {
        MoveNotes();
        CheckHit();
    }

    public void SkipPreview()
    {
        if (RuntimeData.useCustomMusic)
        {
            return;
        }

        if (noteList[0].time - audio.time > 4f)
        {
            audio.DOFade(0, 0.5f).OnComplete(() =>
            {
                audio.time = noteList[0].time - 3f;
                audio.DOFade(1, 0.5f);
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
            CreateOneNote(currentNote);

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
        return (currentNote.time - noteSpawnAdvanceTime < audio.time);
    }

    int osuWidth = 512 / 2;
    int osuHeight = 384 / 2;
    float worldWidth = 0.25f;
    //float worldHeight = 0.03f;
    float baseSpawnY = 6f;
    float floatRangeY = 1.5f;
    void CreateOneNote(Note note)
    {
        Vector3 targetPos = new Vector3
        {
            x = worldWidth * (note.x - osuWidth) / osuWidth,
            //y = -worldHeight * (currentNote.y - osuHeight) / osuHeight,
            y = 0,
            z = 0,
        };

        Vector3 spawnPos = new Vector3
        {
            x = targetPos.x * noteSpawnPosXMultiplier,
            //y = targetPos.y * noteSpawnPosYMultiplier + 6,
            y = baseSpawnY + floatRangeY * (note.y - osuHeight) / osuHeight,
            z = defaultNoteSpawnDistance,
        };

        GameObject noteGameObject = Instantiate(notePrefab, spawnPos, Quaternion.identity);

        noteObjectList.Add(new NoteObject
        {
            gameObject = noteGameObject,
            collider = noteGameObject.GetComponent<Collider>(),
            note = note,
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

            float t = (audio.time - (note.time - noteSpawnAdvanceTime)) / noteSpawnAdvanceTime;
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
        AddHitScore();

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

    void AddHitScore()
    {
        if (RuntimeData.useCustomMusic)
        {
            // DO Something
            score += 100 + (comboCount - 1) * 10;
            return;
        }

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
                judgementLabelTweener = judgementLabel.DOFade(0, 1).SetDelay(0.5f);
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
        audio.DOFade(0, audioFadeTime).SetDelay(audioFadeDelay);

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
