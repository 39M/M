using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class BeatmapConverter
{
    static string beatmapPath = Path.GetFullPath(Application.dataPath + "/../Beatmaps");

    [MenuItem("Tools/Convert Beatmap")]
    private static void ClearPlayerPrefs()
    {
        if (EditorUtility.DisplayDialog("转换谱面", "确定要把 Beatmaps 目录下的所有 osu! 谱面转换为 Json 格式吗？", "确定", "取消"))
        {
            Convert();
        }
    }

    static void Convert()
    {
        Debug.Log(string.Format("Source path: {0}", beatmapPath));

        string[] sourceFiles = Directory.GetFiles(beatmapPath, "*.osu");

        // 分别处理 .osu 文件并导出为 .json 格式
        foreach (string path in sourceFiles)
        {
            string[] lines = File.ReadAllLines(path);

            Music music = new Music();
            Beatmap beatmap = new Beatmap();
            music.beatmapList.Add(beatmap);

            bool startProcessNotes = false;
            foreach (string line in lines)
            {
                if (line.StartsWith("[HitObjects]"))
                {
                    startProcessNotes = true;
                    continue;
                }

                if (!startProcessNotes)
                {
                    // 处理谱面信息

                    int lastIndex = line.LastIndexOf(':');
                    if (lastIndex < 0)
                    {
                        // 如果不是有效信息行则跳过
                        continue;
                    }

                    string value = line.Substring(lastIndex + 1).Trim();

                    if (line.StartsWith("Title"))
                    {
                        music.title = value;
                    }
                    else if (line.StartsWith("Artist"))
                    {
                        music.artist = value;
                    }
                    else if (line.StartsWith("AudioFilename"))
                    {
                        music.audioFilename = value;
                        music.bannerFilename = value;
                        music.soundEffectFilename = value;
                    }
                    else if (line.StartsWith("PreviewTime"))
                    {
                        music.previewTime = float.Parse(value) / 1000;
                    }
                    else if (line.StartsWith("Creator"))
                    {
                        beatmap.creator = value;
                    }
                    else if (line.StartsWith("Version"))
                    {
                        beatmap.version = value;
                    }
                    //else if (line.StartsWith("OverallDifficulty"))
                    //{
                    //    beatmap.difficulty = int.Parse(value);
                    //}
                    beatmap.difficultyDisplayColor = new SimpleColor(new Color(58, 183, 239));
                    beatmap.difficultyName = "Normal";
                }
                else
                {
                    // 开始处理 HitObject

                    string[] noteInfo = line.Split(',');
                    int type = int.Parse(noteInfo[3]);

                    if ((type & 0x01) != 0)
                    {
                        // Circle
                        beatmap.noteList.Add(new Note
                        {
                            x = int.Parse(noteInfo[0]),
                            y = int.Parse(noteInfo[1]),
                            time = float.Parse(noteInfo[2]) / 1000,
                            // 其他 Circle 相关的处理
                        });
                    }
                    else if ((type & 0x02) != 0)
                    {
                        // Slider
                        beatmap.noteList.Add(new Note
                        {
                            x = int.Parse(noteInfo[0]),
                            y = int.Parse(noteInfo[1]),
                            time = float.Parse(noteInfo[2]) / 1000,
                            // 其他 Slider 相关的处理
                        });
                    }
                    else if ((type & 0x08) != 0)
                    {
                        // Spinner
                        beatmap.noteList.Add(new Note
                        {
                            x = int.Parse(noteInfo[0]),
                            y = int.Parse(noteInfo[1]),
                            time = float.Parse(noteInfo[2]) / 1000,
                            // 其他 Spinner 相关的处理
                        });

                        beatmap.noteList.Add(new Note
                        {
                            x = int.Parse(noteInfo[0]),
                            y = int.Parse(noteInfo[1]),
                            time = float.Parse(noteInfo[5]) / 1000,
                            // 其他 Spinner 相关的处理
                        });
                    }
                }
            }

            string targetPath = path.Replace("osu", "json");
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.WriteAllText(targetPath, music.ToJson());

            Debug.Log(string.Format("Converted osu! file\n[{0}]\nto json file\n[{1}]", path, targetPath));
        }

        Debug.Log(string.Format("All done, converted {0} files.", sourceFiles.Length));
    }
}
