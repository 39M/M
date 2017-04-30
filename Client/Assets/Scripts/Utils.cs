using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static AudioClip LoadAudio(string filename)
    {
        string path = GameConst.AUDIO_PATH + filename;
        path = path.Remove(path.LastIndexOf('.'));
        return Resources.Load<AudioClip>(path);
    }

    public static AudioClip LoadSoundEffect(string filename)
    {
        string path = GameConst.SOUND_EFFECT_PATH + filename;
        path = path.Remove(path.LastIndexOf('.'));
        return Resources.Load<AudioClip>(path);
    }
}
