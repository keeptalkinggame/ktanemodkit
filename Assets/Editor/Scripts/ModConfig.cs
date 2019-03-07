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

    public static string Author
    {
        get { return Instance.title; }
        set { Instance.title = value; }
    }

    public static string Description
    {
        get { return Instance.description; }
        set { Instance.description = value; }
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

    public static Texture2D PreviewImage
    {
        get { return Instance.previewImage; }
        set { Instance.previewImage = value; }
    }

    [SerializeField]
    private string id = "";
    [SerializeField]
    private string title = "";
    [SerializeField]
    private string author = "";
    [SerializeField]
    [TextArea(5, 10)]
    private string description = "";
    [SerializeField]
    private string version = "";
    [SerializeField]
    private string outputFolder = "build";
    [SerializeField]
    private Texture2D previewImage = null;


    private static ModConfig instance;
    public static ModConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ModConfig>("ModConfig");
                if (instance == null)
                {
                    ModKitSettingsEditor.CreateModConfig(out instance);
                }
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
        dict.Add("author", author);
        dict.Add("description", description);
        dict.Add("version", version);
        dict.Add("unityVersion", Application.unityVersion);

        return JsonConvert.SerializeObject(dict); ;
    }
}