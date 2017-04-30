using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectMusicManager : MonoBehaviour
{
    List<Music> musicList;

    void Awake()
    {
        musicList = new List<Music>();
    }

    void Start()
    {
        var assets = Resources.LoadAll<TextAsset>(GameConst.BEATMAP_PATH);
        foreach (var asset in assets)
        {
            Music music = Music.FromJson(asset.text);
            musicList.Add(music);

            Debug.Log("Loaded beatmap: " + asset.name);
        }

        //StartGame(musicList[0]);
    }

    void Update()
    {

    }

    void StartGame(Music music)
    {
        RuntimeData.selectedMusic = music;
        SceneManager.LoadScene("Game");
    }
}
