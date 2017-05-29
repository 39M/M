using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class Utils
{
    public static AudioClip LoadAudio(string filename)
    {
        string path = GameConst.AUDIO_PATH + filename;
        path = path.Remove(path.LastIndexOf('.'));
        return Resources.Load<AudioClip>(path);
    }

    public static ResourceRequest LoadAudioAsync(string filename)
    {
        string path = GameConst.AUDIO_PATH + filename;
        path = path.Remove(path.LastIndexOf('.'));
        return Resources.LoadAsync<AudioClip>(path);
    }

    public static AudioClip LoadSoundEffect(string filename)
    {
        string path = GameConst.SOUND_EFFECT_PATH + filename;
        path = path.Remove(path.LastIndexOf('.'));
        return Resources.Load<AudioClip>(path);
    }

    public static Sprite LoadBanner(string filename)
    {
        string path = GameConst.BANNER_PATH + filename;
        path = path.Remove(path.LastIndexOf('.'));
        return Resources.Load<Sprite>(path);
    }

    public static void FadeOut(float duration, TweenCallback onComplete = null, float delay = 0)
    {
        GameObject uiCanvas = GameObject.Find("UICanvas");
        Debug.Assert(uiCanvas != null);

        for (int i = 0; i < uiCanvas.transform.childCount; i++)
        {
            Transform childTransform = uiCanvas.transform.GetChild(i);
            if (childTransform.name == "FadePanel")
            {
                childTransform.gameObject.SetActive(true);
                Image image = childTransform.Find("Mask").GetComponent<Image>();
                image.DOFade(1.1f, duration).SetDelay(delay).OnComplete(onComplete);
                break;
            }
        }
    }

    public static void FadeIn(float duration, TweenCallback onComplete = null, float delay = 0)
    {
        GameObject uiCanvas = GameObject.Find("UICanvas");
        Debug.Assert(uiCanvas != null);

        Transform fadePanelTransform = uiCanvas.transform.Find("FadePanel");
        Image image = fadePanelTransform.Find("Mask").GetComponent<Image>();
        image.DOFade(-0.1f, duration).SetDelay(delay).OnComplete(() =>
        {
            fadePanelTransform.gameObject.SetActive(false);
            if (onComplete != null)
            {
                onComplete();
            }
        });
    }

    public static IEnumerator WaitAndAction(float time, Action action)
    {
        yield return new WaitForSeconds(time);
        action();
    }
}
