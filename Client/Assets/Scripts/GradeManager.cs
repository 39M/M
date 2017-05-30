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
    public Text rankCountLabel;
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
        Color color = rankCountLabel.color;
        color.a = 0;
        rankCountLabel.color = color;
    }

    void Update()
    {
        MoveMusicGroup();
        PlayScoreAnimation();
        PlayLensFlareAnimation();

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
            string scoreString = (score * 100).ToString("0.00");
            scoreCountLabel.text = scoreString.Substring(0, Mathf.Min(6, scoreString.Length)) + "%";

            if ((Mathf.RoundToInt(hitCount) == RuntimeData.hitCount) &&
                (Mathf.RoundToInt(missCount) == RuntimeData.missCount) &&
                (Mathf.RoundToInt(maxCombo) == RuntimeData.maxCombo) &&
                ((int)(score * 10000) == Mathf.RoundToInt(RuntimeData.score * 10000)))
            {
                scoreAnimationDone = true;
                rankCountLabel.DOFade(1, 0.75f).SetEase(Ease.InOutCubic).SetDelay(0.5f);
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

    void CheckRestartGame()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RestartGame();
        }
    }

    bool restartingGame = false;
    void RestartGame()
    {
        if (restartingGame || backingToMenu)
        {
            return;
        }
        restartingGame = true;

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

    bool backingToMenu = false;
    void BackToMenu()
    {
        if (restartingGame || backingToMenu)
        {
            return;
        }
        backingToMenu = true;

        Utils.FadeOut(1, () =>
        {
            string scene = RuntimeData.useCustomMusic ? "CustomMusic" : "SelectMusic";
            SceneManager.LoadScene(scene);
        });
    }
}
