using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public sealed class ModConfig : ScriptableObject
{
    public static string ID
    {
        get { return Instance.id; }
        set { Instance.id = value; }
    }

    public static string Title
    {
        get { return Instance.title; }
        set { Instance.title = value; }
    }

    public static string Version
    {
        get { return Instance.version; }
        set { Instance.version = value; }
    }

    public static string OutputFolder
    {
        get { return Instance.outputFolder; }
        set { Instance.outputFolder = value; }
    }

    [SerializeField]
    private string id = "";
    [SerializeField]
    private string title = "";
    [SerializeField]
    private string version = "";
    [SerializeField]
    private string outputFolder = "build";


    private static ModConfig instance;
    public static ModConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ModConfig>("ModConfig");
            }
            return instance;
        }

        set
        {
            instance = value;
        }
    }

    public string ToJson()
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("id", id);
        dict.Add("title", title);
        dict.Add("version", version);
        dict.Add("unityVersion", Application.unityVersion);

        return JsonConvert.SerializeObject(dict); ;
    }
}