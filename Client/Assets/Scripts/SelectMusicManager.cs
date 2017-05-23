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

    public List<Music> musicList { get; private set; }
    public GameObject musicUIGroup;
    public GameObject musicUIItemPrefab;
    List<MusicUIItem> musicUIItemList;
    int focusIndex;

    float itemHeight;
    float itemWidth;
    float itemSpacing;

    float itemMaxHeight;
    float itemMaxWidth;

    float minSwipeSpeed;

    bool lockControl;
    bool lockLeftControl;
    bool lockRightControl;
    Coroutine unlockLeftCoroutine;
    Coroutine unlockRightCoroutine;


    void Awake()
    {
        musicList = new List<Music>();

        instance = this;

        //itemHeight = 0f;
        itemWidth = 240f;
        itemSpacing = 100f;
        itemMaxHeight = 0f;
        itemMaxWidth = 0f;

        minSwipeSpeed = 1f;

        focusIndex = 1;

        lockControl = false;
        lockRightControl = false;
        lockLeftControl = false;
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
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SwipeTo(Direction.Left);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            SwipeTo(Direction.Right);

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

        musicUIGroup.transform.localPosition = new Vector3
        {
            x = -focusIndex * (itemWidth + itemSpacing),
        };

        MusicUIItem focusItem = musicUIItemList[focusIndex];
        focusItem.transform.localScale = Vector3.one * 1.5f;
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
        item.albumImage = item.transform.Find("AlbumImage").GetComponent<UnityEngine.UI.Image>();
        item.titleLabel = item.transform.Find("TitleLabel").GetComponent<Text>();

        //item.albumImage.sprite = null;
        item.titleLabel.text = music.title;

        return item;
    }

    void SwipeTo(Direction direction)
    {
        int directionCode = 0;

        switch (direction)
        {
            case Direction.Left:
                directionCode = -1;
                break;
            case Direction.Right:
                directionCode = 1;
                break;
        }

        int nextFocus = focusIndex + directionCode;

        if (nextFocus < 0 || nextFocus >= musicUIItemList.Count)
        {
            return;
        }

        //lockControl = true;
        lockLeftControl = true;
        lockRightControl = true;
        if (unlockLeftCoroutine != null)
        {
            StopCoroutine(unlockLeftCoroutine);
        }
        if (unlockRightCoroutine != null)
        {
            StopCoroutine(unlockRightCoroutine);
        }
        unlockLeftCoroutine = StartCoroutine(UnlockControlAfter(0.15f, direction));
        unlockRightCoroutine = StartCoroutine(UnlockControlAfter(0.5f, direction == Direction.Left ? Direction.Right : Direction.Left));

        float updatedGroupPosition = -nextFocus * (itemWidth + itemSpacing);
        musicUIGroup.transform.DOLocalMoveX(updatedGroupPosition, 0.5f, true).SetEase(Ease.OutQuad).Play();
        //.OnComplete(() =>
        //{
        //    lockControl = false;
        //});

        musicUIItemList[focusIndex].transform.DOScale(1, 0.5f).SetEase(Ease.OutQuad).Play();
        musicUIItemList[nextFocus].transform.DOScale(1.5f, 0.5f).SetEase(Ease.OutQuad).Play();

        focusIndex += directionCode;
    }

    void CheckSwipe()
    {
        //if (lockControl)
        if (lockLeftControl && lockRightControl)
        {
            return;
        }

        Frame frame = provider.CurrentFrame;
        foreach (Hand hand in frame.Hands)
        {
            //Debug.Log(hand.PalmVelocity.x);
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

    IEnumerator UnlockControlAfter(float time, Direction direction)
    {
        yield return new WaitForSeconds(time);
        //lockControl = false;
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

    void StartGame(Music music)
    {
        Debug.Assert(music != null);

        RuntimeData.selectedMusic = music;
        SceneManager.LoadScene("Game");
    }
}
