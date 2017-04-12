using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class Music
{
    public string title;
    public string artist;

    public string audioFilename;
    public float previewTime;

    public string soundEffectFilename;

    public string bannerFilename;

    public List<Beatmap> beatmapList;

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
    public string creator;
    public string version;

    public int difficulty;
    public string difficultyName;
    public SimpleColor difficultyDisplayColor;

    public List<Note> noteList;
}

public enum NoteType
{
    Hit, Slide
}

public class Note
{
    public NoteType type;
    public float time;
    public float speed;
    public SimpleColor color;
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
        return new Color(r, g, b, a);
    }
}

#endregion
