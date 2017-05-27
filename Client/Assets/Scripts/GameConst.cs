using UnityEngine;

public static class GameConst
{
    public const string BEATMAP_PATH = "Music/Beatmaps/";
    public const string AUDIO_PATH = "Music/Audio/";
    public const string SOUND_EFFECT_PATH = "Music/SoundEffect/";
    public const string BANNER_PATH = "Music/Banner/";

    public const string HIT_NAME = "Hit";
    public const string MISS_NAME = "Miss";
    public readonly static Color MISS_COLOR = new Color(255 / 255f, 50 / 255f, 50 / 255f, 72 / 255f);
    public readonly static Color HIT_COLOR = new Color(102 / 255f, 204 / 255f, 255 / 255f, 72 / 255f);

    public readonly static Color JUDGEMENT_LINE_DEFAULT_COLOR = new Color(1, 1, 1, 127 / 255f);
    public readonly static Color JUDGEMENT_LINE_HIT_COLOR = Color.white;
    public readonly static Color JUDGEMENT_LINE_MISS_COLOR = new Color(255 / 255f, 50 / 255f, 50 / 255f);
}
