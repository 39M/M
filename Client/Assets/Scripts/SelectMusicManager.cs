using DG.Tweening;
using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

enum Direction
{
    Left, Right
}

public class SelectMusicManager : MonoBehaviour
{
    public LeapProvider provider;

    public static SelectMusicManager instance { get; private set; }

    new AudioSource audio;
    Coroutine playMusicCoroutine;

    public UnityEngine.UI.Image bannerBackground;
    float backgroundAlpha;

    Transform canvasTransform;
    Vector3 canvasBasePosition;
    Vector3 canvasVelocity;

    public List<Music> musicList { get; private set; }
    public GameObject musicUIGroup;
    public GameObject musicUIItemPrefab;
    List<MusicUIItem> musicUIItemList;
    int focusIndex;

    float itemWidth;
    float itemSpacing;

    float itemMaxScale;
    float itemMinScale;

    float swipeTransitionDuration;

    float minSwipeSpeed;

    bool lockLeftControl;
    bool lockRightControl;
    float positiveUnlockDelay;
    float negativeUnlockDelay;
    Coroutine unlockLeftCoroutine;
    Coroutine unlockRightCoroutine;

    void Awake()
    {
        audio = GetComponent<AudioSource>();
        canvasTransform = GameObject.Find("Canvas").transform;
        canvasBasePosition = canvasTransform.position;

        musicList = new List<Music>();

        instance = this;

        itemWidth = 240f;
        itemSpacing = 100f;
        itemMaxScale = 1f;
        itemMinScale = 0.7f;
        swipeTransitionDuration = 0.5f;

        minSwipeSpeed = 1f;

        focusIndex = 0;

        lockRightControl = false;
        lockLeftControl = false;

        positiveUnlockDelay = 0.15f;
        negativeUnlockDelay = 0.5f;

        backgroundAlpha = bannerBackground.color.a;
    }

    void Start()
    {
        // Init Leap
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;

        // Load Music List
        LoadMusicList();
        InitMusicGroup();
    }

    void Update()
    {
        CheckSwipe();
        CheckChangeDifficulty();
        CheckGameStart();

        MoveMusicGroup();
    }

    void LoadMusicList()
    {
        var assets = Resources.LoadAll<TextAsset>(GameConst.BEATMAP_PATH);
        foreach (var asset in assets)
        {
            Music music = Music.FromJson(asset.text);
            musicList.Add(music);

            Debug.Log("Loaded beatmap: " + asset.name);
        }
    }

    void InitMusicGroup()
    {
        musicUIItemList = new List<MusicUIItem>();

        for (int i = 0; i < musicList.Count; i++)
        {
            Music music = musicList[i];
            MusicUIItem item = CreateMusicUIItem(music);
            musicUIItemList.Add(item);

            item.transform.localPosition = new Vector3
            {
                x = i * (itemWidth + itemSpacing),
            };
        }

        // Set group position
        musicUIGroup.transform.localPosition = new Vector3
        {
            x = -focusIndex * (itemWidth + itemSpacing),
        };

        // Set default focut item
        SetDefaultFocusItem(musicUIItemList[focusIndex]);
    }

    MusicUIItem CreateMusicUIItem(Music music)
    {
        GameObject itemGameObject = Instantiate(musicUIItemPrefab, musicUIGroup.transform);
        itemGameObject.name = music.title;
        itemGameObject.SetActive(true);

        MusicUIItem item = new MusicUIItem
        {
            gameObject = itemGameObject,
            music = music,
            beatmapIndex = 0,
        };

        item.transform = item.gameObject.transform;
        item.albumImage = item.transform.Find("AlbumBackground/AlbumImage").GetComponent<UnityEngine.UI.Image>();
        item.textGroup = item.transform.Find("TextGroup").GetComponent<CanvasGroup>();
        item.titleLabel = item.transform.Find("TextGroup/TitleBackground/TitleLabel").GetComponent<Text>();
        item.artistLabel = item.transform.Find("TextGroup/ArtistBackground/ArtistLabel").GetComponent<Text>();
        item.difficultyLabel = item.transform.Find("TextGroup/DifficultyBackground/DifficultyLabel").GetComponent<Text>();

        item.transform.localScale = Vector3.one * itemMinScale;
        item.albumImage.sprite = Utils.LoadBanner(music.bannerFilename);
        item.titleLabel.text = music.title;
        item.artistLabel.text = music.artist;
        item.difficultyLabel.text = music.beatmapList[0].difficultyName;
        item.difficultyLabel.color = music.beatmapList[0].difficultyDisplayColor.ToColor();

        return item;
    }

    void SetDefaultFocusItem(MusicUIItem item)
    {
        item.transform.localScale = Vector3.one * itemMaxScale;

        item.textGroup.alpha = 1;

        bannerBackground.sprite = item.albumImage.sprite;

        playMusicCoroutine = StartCoroutine(LoadAsyncAndPlay(item.music));
    }

    void CheckSwipe()
    {
        // Keyboard
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SwipeTo(Direction.Left);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            SwipeTo(Direction.Right);

        // Leap Motion
        if (lockLeftControl && lockRightControl)
        {
            return;
        }

        Frame frame = provider.CurrentFrame;
        foreach (Hand hand in frame.Hands)
        {
            if (hand.PalmVelocity.x > minSwipeSpeed && !lockLeftControl)
            {
                SwipeTo(Direction.Left);
            }
            else if (hand.PalmVelocity.x < -minSwipeSpeed && !lockRightControl)
            {
                SwipeTo(Direction.Right);
            }

            break;
        }
    }

    void SwipeTo(Direction direction)
    {
        if (changingDifficulty || callingStartGame)
        {
            return;
        }

        int nextFocus = focusIndex + ((direction == Direction.Left) ? -1 : 1);

        if (nextFocus < 0 || nextFocus >= musicUIItemList.Count)
        {
            return;
        }

        // Set lock
        lockLeftControl = true;
        lockRightControl = true;

        // Stop running coroutine
        if (unlockLeftCoroutine != null)
        {
            StopCoroutine(unlockLeftCoroutine);
        }
        if (unlockRightCoroutine != null)
        {
            StopCoroutine(unlockRightCoroutine);
        }

        // Start unlock coroutine
        unlockLeftCoroutine = StartCoroutine(UnlockControlAfter(positiveUnlockDelay, direction));
        unlockRightCoroutine = StartCoroutine(UnlockControlAfter(negativeUnlockDelay, direction == Direction.Left ? Direction.Right : Direction.Left));

        SwitchMusic(nextFocus);
    }

    IEnumerator UnlockControlAfter(float time, Direction direction)
    {
        yield return new WaitForSeconds(time);

        switch (direction)
        {
            case Direction.Left:
                lockLeftControl = false;
                break;
            case Direction.Right:
                lockRightControl = false;
                break;
        }
    }

    void SwitchMusic(int nextFocus)
    {
        MusicUIItem currentItem = musicUIItemList[focusIndex];
        MusicUIItem nextItem = musicUIItemList[nextFocus];

        float updatedGroupPosition = -nextFocus * (itemWidth + itemSpacing);
        musicUIGroup.transform.DOPause();
        musicUIGroup.transform.DOLocalMoveX(updatedGroupPosition, swipeTransitionDuration, true).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            Music nextMusic = nextItem.music;
            if (playMusicCoroutine != null)
            {
                StopCoroutine(playMusicCoroutine);
            }
            playMusicCoroutine = StartCoroutine(LoadAsyncAndPlay(nextMusic));
            bannerBackground.sprite = nextItem.albumImage.sprite;
            bannerBackground.DOFade(backgroundAlpha, swipeTransitionDuration).SetEase(Ease.OutQuad);
        });

        currentItem.transform.DOScale(itemMinScale, swipeTransitionDuration).SetEase(Ease.OutQuad);
        nextItem.transform.DOScale(itemMaxScale, swipeTransitionDuration).SetEase(Ease.OutQuad);

        currentItem.textGroup.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);
        nextItem.textGroup.DOFade(1, swipeTransitionDuration).SetEase(Ease.OutQuad);

        audio.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);
        bannerBackground.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);

        focusIndex = nextFocus;
    }

    IEnumerator LoadAsyncAndPlay(Music music)
    {
        ResourceRequest request = Utils.LoadAudioAsync(music.audioFilename);
        yield return request;
        audio.clip = request.asset as AudioClip;
        audio.time = music.previewTime;
        audio.Play();
        audio.DOFade(1, swipeTransitionDuration).SetEase(Ease.InQuad);
    }

    void CheckChangeDifficulty()
    {
        int directionCode = 0;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            directionCode = 1;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            directionCode = -1;
        }

        if (directionCode != 0)
        {
            ChangeDifficulty(directionCode);
        }
    }

    bool changingDifficulty = false;
    public void ChangeDifficulty(int directionCode = 1)
    {
        if (callingStartGame)
        {
            return;
        }

        MusicUIItem item = musicUIItemList[focusIndex];

        int beatmapCount = item.music.beatmapList.Count;

        if (beatmapCount <= 1)
        {
            return;
        }

        item.beatmapIndex += directionCode;
        if (item.beatmapIndex < 0)
        {
            item.beatmapIndex = beatmapCount - 1;
        }
        if (item.beatmapIndex >= beatmapCount)
        {
            item.beatmapIndex = 0;
        }

        Beatmap nextBeatmap = item.music.beatmapList[item.beatmapIndex];

        changingDifficulty = true;
        item.difficultyLabel.DOFade(0, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            Color color = nextBeatmap.difficultyDisplayColor.ToColor();
            color.a = 0;
            item.difficultyLabel.color = color;
            item.difficultyLabel.text = nextBeatmap.difficultyName;
            item.difficultyLabel.DOFade(1, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                changingDifficulty = false;
            });
        });
    }

    void CheckGameStart()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
    }

    bool calledStartGame = false;
    public void StartGame()
    {
        Music music = musicList[focusIndex];
        int beatmapIndex = musicUIItemList[focusIndex].beatmapIndex;

        calledStartGame = true;

        RuntimeData.useCustomMusic = false;
        RuntimeData.selectedMusic = music;
        RuntimeData.selectedBeatmap = music.beatmapList[beatmapIndex];

        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("Game");
        });
    }

    bool callingStartGame = false;
    public void ActiveGameStart()
    {
        callingStartGame = true;
        transform.DOScale(0, 1).OnComplete(() =>
        {
            StartGame();
        });
        musicUIItemList[focusIndex].transform.DOScale(1.2f, 2).SetEase(Ease.Linear);
    }

    public void DeactiveGameStart()
    {
        if (calledStartGame)
        {
            return;
        }
        callingStartGame = false;

        transform.DOPause();
        transform.localScale = Vector3.one;
        musicUIItemList[focusIndex].transform.DOPause();
        musicUIItemList[focusIndex].transform.DOScale(1, 0.3f).SetEase(Ease.OutQuad);
    }

    void MoveMusicGroup()
    {
        if (provider.CurrentFrame.Hands.Count > 0)
        {
            Hand hand = provider.CurrentFrame.Hands[0];
            Vector3 position = canvasBasePosition;
            position.x = hand.PalmPosition.x / 10;
            canvasTransform.position = Vector3.SmoothDamp(canvasTransform.position, position, ref canvasVelocity, 0.1f);
        }
        else
        {
            //canvasTransform.DOMove(canvasBasePosition, 0.3f).SetEase(Ease.OutQuad).Play();
            canvasTransform.position = Vector3.SmoothDamp(canvasTransform.position, canvasBasePosition, ref canvasVelocity, 0.1f);
        }
    }

    public void DebugLog()
    {
        Debug.LogWarning("Logged");
    }
}
