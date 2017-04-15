using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{

    void Start()
    {
        #region Json.Net Test
        string jsonString = new Music
        {
            title = "title",
            artist = "artist",
            audioFilename = "audio.mp3",
            previewTime = 0f,
            soundEffectFilename = "sound.wav",
            bannerFilename = "banner.png",
            beatmapList = new List<Beatmap>
            {
                new Beatmap
                {
                    creator = "creator",
                    version = "1.0",
                    difficulty = 1,
                    difficultyName = "Easy",
                    difficultyDisplayColor = new SimpleColor(Color.green),
                    noteList = new List<Note>
                    {
                        new Note
                        {
                            type = NoteType.Hit,
                            time = 0f,
                            speed = 1f,
                            color = new SimpleColor(Color.grey)
                        },
                        new Note
                        {
                            type = NoteType.Slide,
                            time = 1f,
                            speed = 2f,
                            color = new SimpleColor(Color.white)
                        }
                    }
                },
                new Beatmap
                {

                    creator = "creator",
                    version = "1.0",
                    difficulty = 5,
                    difficultyName = "Normal",
                    difficultyDisplayColor = new SimpleColor(Color.blue),
                    noteList = new List<Note>
                    {
                        new Note
                        {
                            type = NoteType.Hit,
                            time = 0f,
                            speed = 1f,
                            color = new SimpleColor(Color.grey)
                        },
                        new Note
                        {
                            type = NoteType.Slide,
                            time = 1f,
                            speed = 2f,
                            color = new SimpleColor(Color.white)
                        }
                    }
                }
            }
        }.ToJson();

        Debug.Log(jsonString);

        Music music = Music.FromJson(jsonString);

        Debug.Log(JsonConvert.SerializeObject(music.beatmapList[1], Formatting.Indented));
        #endregion

    }

    void Update()
    {

    }
}
