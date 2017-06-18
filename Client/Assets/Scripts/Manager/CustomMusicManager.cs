using DG.Tweening;
using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CustomMusicManager : MonoBehaviour
{
    public LeapProvider provider;

    new AudioSource audio;

    Transform canvasTransform;
    Vector3 canvasBasePosition;
    Vector3 canvasVelocity;

    public GameObject musicUIGroup;
    public GameObject musicUIItemPrefab;
    List<CustomMusicUIItem> musicUIItemList;

    float fadeDuration = 0.5f;

    void Awake()
    {
        audio = GetComponent<AudioSource>();
        canvasTransform = GameObject.Find("Canvas").transform;
        canvasBasePosition = canvasTransform.position;
    }

    void Start()
    {
        // Init Leap
        provider = FindObjectOfType<LeapProvider>() as LeapProvider;

        // Init Music Group
        InitMusicGroup();
    }

    void Update()
    {
        CheckBackToStartup();

        MoveMusicGroup();
    }

    void InitMusicGroup()
    {
        musicUIItemList = new List<CustomMusicUIItem>();

        string[] fileList = Directory.GetFiles(Application.streamingAssetsPath, "*.mp3");

        Debug.Log(string.Format("{0} files found.", fileList.Length));

        foreach (var filePath in fileList)
        {
            Debug.Log("Found music: " + filePath);

            string fileName = filePath.Substring(filePath.LastIndexOf(@"\") + 1);
            CustomMusicUIItem item = CreateMusicUIItem(fileName);
            musicUIItemList.Add(item);
        }
    }

    CustomMusicUIItem CreateMusicUIItem(string fileName)
    {
        GameObject itemGameObject = Instantiate(musicUIItemPrefab, musicUIGroup.transform, false);
        itemGameObject.name = fileName;
        itemGameObject.SetActive(true);

        CustomMusicUIItem item = new CustomMusicUIItem
        {
            gameObject = itemGameObject,
        };

        item.transform = item.gameObject.transform;
        item.titleLabel = item.transform.Find("TitleLabel").GetComponent<Text>();
        item.titleLabel.text = fileName;

        item.listenButton = item.transform.Find("ListenButton").GetComponent<Button>();
        item.listenButton.onClick.AddListener(() =>
        {
            if (audio.clip == item.clip)
            {
                return;
            }
            audio.DOFade(0, fadeDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                audio.clip = item.clip;
                audio.Play();
                audio.DOFade(1, fadeDuration).SetEase(Ease.InQuad);
            });
        });
        item.playButton = item.transform.Find("PlayButton").GetComponent<Button>();
        item.playButton.onClick.AddListener(() =>
        {
            StartGame(item.clip);
        });

        StartCoroutine(LoadAudioClipAsync(item, fileName));

        return item;
    }

    IEnumerator LoadAudioClipAsync(CustomMusicUIItem item, string fileName)
    {
        string wwwPath = Path.Combine("file:///" + Application.streamingAssetsPath, fileName);

        using (var loader = new WWW(wwwPath))
        {
            yield return loader;
            if (!string.IsNullOrEmpty(loader.error))
            {
                // Error
                Debug.LogError("Load Streaming Assets Error: " + loader.error);
                yield break;
            }

            item.clip = NAudioPlayer.FromMp3Data(loader.bytes);
            item.clip.name = fileName;
        }
    }

    public void StartGame(AudioClip clip)
    {
        RuntimeData.useCustomMusic = true;
        RuntimeData.selectedClip = clip;

        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("Game");
        });
    }

    float minSwipeSpeed = 1f;
    void CheckBackToStartup()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToStartup();
        }

        var hands = provider.CurrentFrame.Hands;
        if (hands.Count >= 2)
        {
            bool leftSwipe = false;
            bool rightSwipe = false;
            foreach (var hand in hands)
            {
                if (hand.IsRight && hand.PalmVelocity.x > minSwipeSpeed)
                {
                    rightSwipe = true;
                }

                if (hand.IsLeft && hand.PalmVelocity.x < -minSwipeSpeed)
                {
                    leftSwipe = true;
                }
            }

            if (rightSwipe && leftSwipe)
            {
                BackToStartup();
            }
        }
    }

    bool backingToStartup = false;
    void BackToStartup()
    {
        if (backingToStartup)
        {
            return;
        }
        backingToStartup = true;

        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("Startup");
        });
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
            canvasTransform.position = Vector3.SmoothDamp(canvasTransform.position, canvasBasePosition, ref canvasVelocity, 0.1f);
        }
    }
}
