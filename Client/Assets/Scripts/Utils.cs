using DG.Tweening;
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

    public static void FadeOut(TweenCallback onComplete)
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
                image.DOFade(1, 1).SetEase(Ease.InOutCubic).Play().OnComplete(onComplete);
                break;
            }
        }
    }
}
