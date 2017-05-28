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
    Beatmap beatmap;

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
        beatmap = RuntimeData.selectedBeatmap;
        if (beatmap == null)
        {
            beatmap = music.beatmapList[0];
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
        PlayLensFlareAnimation();
    }

    bool scoreAnimationDone = false;
    void PlayScoreAnimation()
    {
        if (!scoreAnimationDone)
        {
            hitCount = Mathf.SmoothStep(hitCount, RuntimeData.hitCount, 0.1f);
            perfectCountLabel.text = string.Format("{0}/{1}", Mathf.RoundToInt(hitCount), beatmap.noteList.Count);

            missCount = Mathf.SmoothStep(missCount, RuntimeData.missCount, 0.1f);
            missCountLabel.text = (Mathf.RoundToInt(missCount)).ToString();

            maxCombo = Mathf.SmoothStep(maxCombo, RuntimeData.maxCombo, 0.1f);
            comboCountLabel.text = (Mathf.RoundToInt(maxCombo)).ToString();

            score += Mathf.Clamp((RuntimeData.score - score), 2f, float.MaxValue) * 5 * Time.deltaTime;
            score = Mathf.Clamp(score, 0, RuntimeData.score);
            scoreCountLabel.text = (score / 10000).ToString().Substring(0, 5) + "%";

            if ((Mathf.RoundToInt(hitCount) == RuntimeData.hitCount) &&
                (Mathf.RoundToInt(missCount) == RuntimeData.missCount) &&
                (Mathf.RoundToInt(maxCombo) == RuntimeData.maxCombo) &&
                (Mathf.RoundToInt(score) / 100 == RuntimeData.score / 100))
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
}
