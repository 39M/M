using DG.Tweening;
using Leap;
using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GradeManager : MonoBehaviour
{
    public LeapProvider provider;

    new AudioSource audio;

    public UnityEngine.UI.Image bannerBackground;

    Transform canvasTransform;
    Vector3 canvasBasePosition;
    Vector3 canvasVelocity;

    Music music;

    public Text perfectCountLabel;
    public Text missCountLabel;
    public Text comboCountLabel;
    public Text scoreCountLabel;
    public CanvasGroup rankGroup;
    public Text rankCountLabel;
    public RectTransform backOption;
    public RectTransform retryOption;

    float hitCount = 0;
    float missCount = 0;
    float maxCombo = 0;
    float score = 0;

    public LensFlare lensFlare;
    public GameObject lensFlareReference;

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

        music = RuntimeData.selectedMusic;
        if (music == null)
        {
            var beatmapAsset = Resources.Load<TextAsset>(GameConst.BEATMAP_PATH + "Croatian_Rhapsody");
            music = Music.FromJson(beatmapAsset.text);
        }
        bannerBackground.sprite = Utils.LoadBanner(music.bannerFilename);

        perfectCountLabel.text = "0";
        missCountLabel.text = "0";
        comboCountLabel.text = "0";
        scoreCountLabel.text = "0";

        SetRank();

        lensFlare.transform.localPosition = Vector3.zero;
        lensFlare.transform.DOLocalMove(new Vector3(-290f, 140f), 2).SetDelay(0.5f);
        lensFlare.brightness = 0;

        if (RuntimeData.useCustomMusic)
        {
            audio.clip = RuntimeData.selectedClip;
        }
        else
        {
            audio.clip = Utils.LoadAudio(music.audioFilename);
            audio.time = music.previewTime;
        }
        audio.volume = 0;
        audio.DOFade(0.05f, 1f);
        audio.Play();
    }

    void SetRank()
    {
        if (RuntimeData.score > 0.9f)
        {
            rankCountLabel.text = "S";
            rankCountLabel.color = GameConst.RANK_S_COLOR;
        }
        else if (RuntimeData.score > 0.8f)
        {
            rankCountLabel.text = "A";
            rankCountLabel.color = GameConst.RANK_A_COLOR;
        }
        else if (RuntimeData.score > 0.7f)
        {
            rankCountLabel.text = "B";
            rankCountLabel.color = GameConst.RANK_B_COLOR;
        }
        else if (RuntimeData.score > 0.6f)
        {
            rankCountLabel.text = "C";
            rankCountLabel.color = GameConst.RANK_C_COLOR;
        }
        else
        {
            rankCountLabel.text = "D";
            rankCountLabel.color = GameConst.RANK_D_COLOR;
        }

        rankGroup.alpha = 0f;
    }

    void Update()
    {
        MoveMusicGroup();
        PlayScoreAnimation();
        PlayLensFlareAnimation();

        CheckSwipe();
        CheckRestartGame();
        CheckBackToMenu();
    }

    bool scoreAnimationDone = false;
    void PlayScoreAnimation()
    {
        if (!scoreAnimationDone)
        {
            hitCount = Mathf.SmoothStep(hitCount, RuntimeData.hitCount, 0.1f);
            perfectCountLabel.text = string.Format("{0}/{1}", Mathf.RoundToInt(hitCount), RuntimeData.hitCount + RuntimeData.missCount);

            missCount = Mathf.SmoothStep(missCount, RuntimeData.missCount, 0.1f);
            missCountLabel.text = (Mathf.RoundToInt(missCount)).ToString();

            maxCombo = Mathf.SmoothStep(maxCombo, RuntimeData.maxCombo, 0.1f);
            comboCountLabel.text = (Mathf.RoundToInt(maxCombo)).ToString();

            score += Mathf.Max((RuntimeData.score - score), 0.0001f) * 4 * Time.deltaTime;
            score = Mathf.Min(score, RuntimeData.score);
            string scoreString = (score * 100).ToString("0.00");
            scoreCountLabel.text = scoreString.Substring(0, Mathf.Min(6, scoreString.Length)) + "%";

            if ((Mathf.RoundToInt(hitCount) == RuntimeData.hitCount) &&
                (Mathf.RoundToInt(missCount) == RuntimeData.missCount) &&
                (Mathf.RoundToInt(maxCombo) == RuntimeData.maxCombo) &&
                (Mathf.Abs(RuntimeData.score - score) < 0.00005f))
            {
                scoreAnimationDone = true;
                rankGroup.DOFade(1, 0.75f).SetEase(Ease.InOutCubic).SetDelay(0.5f);
            }
        }
    }

    bool lensFlareAnimationDone = false;
    void PlayLensFlareAnimation()
    {
        if (!lensFlareAnimationDone)
        {
            lensFlare.brightness = Mathf.SmoothStep(lensFlare.brightness, 0.5f, 0.1f);
            if (lensFlare.brightness > 0.499)
            {
                lensFlareAnimationDone = true;
            }
        }

        if (lensFlareAnimationDone)
        {
            lensFlare.brightness = lensFlareReference.transform.position.x;
        }
    }

    void MoveMusicGroup()
    {
        if (provider.CurrentFrame.Hands.Count > 0)
        {
            Hand hand = provider.CurrentFrame.Hands[0];
            Vector3 position = canvasBasePosition;
            position.x = hand.PalmPosition.x / 7.5f;
            canvasTransform.position = Vector3.SmoothDamp(canvasTransform.position, position, ref canvasVelocity, 0.1f);
        }
        else
        {
            canvasTransform.position = Vector3.SmoothDamp(canvasTransform.position, canvasBasePosition, ref canvasVelocity, 0.1f);
        }
    }

    enum Choices { None, Retry, Back }
    Choices choice = Choices.None;
    float minSwipeSpeed = 1f;
    void CheckSwipe()
    {
        if (!scoreAnimationDone || choosing || madeChoice)
        {
            return;
        }

        Frame frame = provider.CurrentFrame;
        foreach (Hand hand in frame.Hands)
        {
            if (hand.PalmVelocity.x > minSwipeSpeed)
            {
                SwipeTo(Direction.Right);
            }
            else if (hand.PalmVelocity.x < -minSwipeSpeed)
            {
                SwipeTo(Direction.Left);
            }

            break;
        }
    }

    void SwipeTo(Direction direction)
    {
        if (direction == Direction.Left)
        {
            choice = Choices.Retry;
            retryOption.DOScale(0.625f, 0.3f);
            backOption.DOScale(0.5f, 0.3f);
        }
        else if (direction == Direction.Right)
        {
            choice = Choices.Back;
            backOption.DOScale(0.625f, 0.3f);
            retryOption.DOScale(0.5f, 0.3f);
        }
    }

    bool choosing = false;
    public void ActiveChoosing()
    {
        choosing = true;

        if (choice == Choices.Back)
        {
            transform.DOScale(0, 1).OnComplete(() =>
            {
                BackToMenu();
            });
            backOption.DOScale(0.875f, 2);
        }
        else if (choice == Choices.Retry)
        {
            transform.DOScale(0, 1).OnComplete(() =>
            {
                RestartGame();
            });
            retryOption.DOScale(0.875f, 2);
        }
    }

    public void DeactiveChoosing()
    {
        if (madeChoice)
        {
            return;
        }

        choosing = false;

        transform.DOPause();
        transform.localScale = Vector3.one;
        if (choice == Choices.Back)
        {
            backOption.DOPause();
            backOption.DOScale(0.625f, 0.3f);
        }
        else if (choice == Choices.Retry)
        {
            retryOption.DOPause();
            retryOption.DOScale(0.625f, 0.3f);
        }
    }

    void CheckRestartGame()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RestartGame();
        }
    }

    bool madeChoice = false;
    void RestartGame()
    {
        if (madeChoice)
        {
            return;
        }
        madeChoice = true;

        audio.DOFade(0, 1f);
        Utils.FadeOut(1, () =>
        {
            SceneManager.LoadScene("Game");
        });
    }

    void CheckBackToMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToMenu();
        }
    }

    void BackToMenu()
    {
        if (madeChoice)
        {
            return;
        }
        madeChoice = true;

        audio.DOFade(0, 1f);
        Utils.FadeOut(1, () =>
        {
            string scene = RuntimeData.useCustomMusic ? "CustomMusic" : "SelectMusic";
            SceneManager.LoadScene(scene);
        });
    }
}
