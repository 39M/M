using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class Music
{
    // 音乐标题
    public string title;
    // 音乐作者
    public string artist;

    // 音乐文件名称
    public string audioFilename;
    // 音乐预览起始时间
    public float previewTime;

    // 音效文件名称
    public string soundEffectFilename;
    // 音乐配图文件名称
    public string bannerFilename;

    // 音乐谱面列表
    public List<Beatmap> beatmapList = new List<Beatmap>();

    public static Music FromJson(string json)
    {
        return JsonConvert.DeserializeObject<Music>(json);
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}

public class Beatmap
{
    // 创建者
    public string creator;
    // 版本
    public string version;

    // 难度值
    public int difficulty;
    // 难度名称
    public string difficultyName;
    // 难度显示颜色
    public SimpleColor difficultyDisplayColor;

    // Note 序列
    public List<Note> noteList = new List<Note>();
}

public enum NoteType
{
    Hit, Slide
}

public class Note
{
    // Note 类型
    public NoteType type;
    // Note 对应的节奏点时间
    public float time;
    // Note 速度
    public float speed;
    // Note 颜色
    public SimpleColor color;

    // Note 出现位置
    public int x;
    public int y;
}

#region Basic Defination

public class SimpleColor
{
    public float r;
    public float g;
    public float b;
    public float a;

    public SimpleColor(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }

    public Color ToColor()
    {
        return new Color(r / 255, g / 255, b / 255, a);
    }
}

#endregion
