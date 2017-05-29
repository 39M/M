using UnityEngine;

public static class GameConst
{
    public const int noteMaxX = 512;
    public const int noteMaxY = 384;
    public const int noteHalfX = noteMaxX / 2;
    public const int noteHalfY = noteMaxY / 2;

    public const string BEATMAP_PATH = "Music/Beatmaps/";
    public const string AUDIO_PATH = "Music/Audio/";
    public const string SOUND_EFFECT_PATH = "Music/SoundEffect/";
    public const string BANNER_PATH = "Music/Banner/";
    public const string DEFAULT_SOUND_EFFECT_FILENAME = "HitSound.wav";

    public const string HIT_NAME = "Hit";
    public const string MISS_NAME = "Miss";
    public readonly static Color HIT_COLOR = new Color(102 / 255f, 204 / 255f, 255 / 255f, 192 / 255f);
    public readonly static Color MISS_COLOR = new Color(255 / 255f, 50 / 255f, 50 / 255f, 150 / 255f);

    public readonly static Color RANK_S_COLOR = new Color(255 / 255f, 200 / 255f, 0);
    public readonly static Color RANK_A_COLOR = new Color(172 / 255f, 1, 128 / 255f);
    public readonly static Color RANK_B_COLOR = new Color(102 / 255f, 204 / 255f, 1);
    public readonly static Color RANK_C_COLOR = new Color(1, 100 / 255f, 1);
    public readonly static Color RANK_D_COLOR = new Color(255 / 255f, 50 / 255f, 50 / 255f);

    public readonly static Color JUDGEMENT_LINE_DEFAULT_COLOR = new Color(1, 1, 1, 127 / 255f);
    public readonly static Color JUDGEMENT_LINE_HIT_COLOR = Color.white;
    public readonly static Color JUDGEMENT_LINE_MISS_COLOR = new Color(255 / 255f, 50 / 255f, 50 / 255f);
}
