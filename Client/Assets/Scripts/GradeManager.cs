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
    }

    void SetRank()
    {
        if (RuntimeData.score > 900000)
        {
            rankCountLabel.text = "S";
            rankCountLabel.color = new Color(255 / 255f, 200 / 255f, 0);
        }
        else if (RuntimeData.score > 800000)
        {
            rankCountLabel.text = "A";
            rankCountLabel.color = new Color(255 / 255f, 200 / 255f, 0);
        }
        else if (RuntimeData.score > 700000)
        {
            rankCountLabel.text = "B";
            rankCountLabel.color = new Color(255 / 255f, 200 / 255f, 0);
        }
        else if (RuntimeData.score > 600000)
        {
            rankCountLabel.text = "C";
            rankCountLabel.color = new Color(255 / 255f, 200 / 255f, 0);
        }
        else
        {
            rankCountLabel.text = "D";
            rankCountLabel.color = new Color(255 / 255f, 200 / 255f, 0);
        }
        Color color = rankCountLabel.color;
        color.a = 0;
        rankCountLabel.color = color;
    }

    void Update()
    {
        MoveMusicGroup();
        PlayScoreAnimation();
    }

    bool scoreAnimationDone = false;
    void PlayScoreAnimation()
    {
        if (!scoreAnimationDone)
        {
            hitCount = Mathf.SmoothStep(hitCount, RuntimeData.hitCount, 0.1f);
            perfectCountLabel.text = (Mathf.RoundToInt(hitCount)).ToString();

            missCount = Mathf.SmoothStep(missCount, RuntimeData.missCount, 0.1f);
            missCountLabel.text = (Mathf.RoundToInt(missCount)).ToString();

            maxCombo = Mathf.SmoothStep(maxCombo, RuntimeData.maxCombo, 0.1f);
            comboCountLabel.text = (Mathf.RoundToInt(maxCombo)).ToString();

            score += Mathf.Clamp((RuntimeData.score - score), 2f, float.MaxValue) * 3 * Time.deltaTime;
            scoreCountLabel.text = (Mathf.RoundToInt(score)).ToString();

            if ((Mathf.RoundToInt(hitCount) == RuntimeData.hitCount) &&
                (Mathf.RoundToInt(missCount) == RuntimeData.missCount) &&
                (Mathf.RoundToInt(maxCombo) == RuntimeData.maxCombo) &&
                (Mathf.RoundToInt(score) == RuntimeData.score))
            {
                scoreAnimationDone = true;
                rankCountLabel.DOFade(1, 0.75f).SetEase(Ease.InOutCubic).SetDelay(1).Play();
            }
        }
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
