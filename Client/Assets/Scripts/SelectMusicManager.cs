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

        musicList = new List<Music>();

        instance = this;

        itemWidth = 240f;
        itemSpacing = 100f;
        itemMaxScale = 1f;
        itemMinScale = 0.7f;
        swipeTransitionDuration = 0.5f;

        minSwipeSpeed = 1f;

        focusIndex = 1;

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

        //StartGame(musicList[0]);
    }

    void Update()
    {
        CheckSwipe();
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
        };

        item.transform = item.gameObject.transform;
        item.albumImage = item.transform.Find("AlbumBackground/AlbumImage").GetComponent<UnityEngine.UI.Image>();
        item.textGroup = item.transform.Find("TextGroup").GetComponent<CanvasGroup>();
        item.titleLabel = item.transform.Find("TextGroup/TitleLabel").GetComponent<Text>();
        item.artistLabel = item.transform.Find("TextGroup/ArtistLabel").GetComponent<Text>();
        item.difficultyLabel = item.transform.Find("TextGroup/DifficultyLabel").GetComponent<Text>();

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
        musicUIGroup.transform.DOLocalMoveX(updatedGroupPosition, swipeTransitionDuration, true).SetEase(Ease.OutQuad).Play().OnComplete(() =>
        {
            Music nextMusic = nextItem.music;
            if (playMusicCoroutine != null)
            {
                StopCoroutine(playMusicCoroutine);
            }
            playMusicCoroutine = StartCoroutine(LoadAsyncAndPlay(nextMusic));
            bannerBackground.sprite = nextItem.albumImage.sprite;
            bannerBackground.DOFade(backgroundAlpha, swipeTransitionDuration).SetEase(Ease.OutQuad).Play();
        });

        currentItem.transform.DOScale(itemMinScale, swipeTransitionDuration).SetEase(Ease.OutQuad).Play();
        nextItem.transform.DOScale(itemMaxScale, swipeTransitionDuration).SetEase(Ease.OutQuad).Play();

        currentItem.textGroup.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad).Play();
        nextItem.textGroup.DOFade(1, swipeTransitionDuration).SetEase(Ease.OutQuad).Play();

        audio.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad).Play();
        bannerBackground.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad).Play();

        focusIndex = nextFocus;
    }

    IEnumerator LoadAsyncAndPlay(Music music)
    {
        ResourceRequest request = Utils.LoadAudioAsync(music.audioFilename);
        yield return request;
        audio.clip = request.asset as AudioClip;
        audio.time = music.previewTime;
        audio.Play();
        audio.DOFade(1, swipeTransitionDuration).SetEase(Ease.InQuad).Play();
    }

    void StartGame(Music music)
    {
        Debug.Assert(music != null);

        RuntimeData.selectedMusic = music;
        SceneManager.LoadScene("Game");
    }
}
