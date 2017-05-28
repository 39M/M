using UnityEngine;
using UnityEngine.UI;

public class MusicUIItem
{
    public GameObject gameObject;
    public Transform transform;
    public Image albumImage;
    public CanvasGroup textGroup;
    public Text titleLabel;
    public Text artistLabel;
    public Text difficultyLabel;

    public Music music;
    public int beatmapIndex;
}

public class CustomMusicUIItem
{
    public GameObject gameObject;
    public Transform transform;
    public Text titleLabel;
    public Button listenButton;
    public Button playButton;
    public AudioClip clip;
}
