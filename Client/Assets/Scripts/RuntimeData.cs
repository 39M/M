using UnityEngine;

public static class RuntimeData
{
    public static int selectedMusicIndex = 0;
    public static int selectedBeatmapIndex = 0;
    public static Music selectedMusic;
    public static Beatmap selectedBeatmap;
    public static AudioClip selectedClip;
    public static bool useCustomMusic;
    public static int hitCount;
    public static int missCount;
    public static int maxCombo;
    public static float score;
}
