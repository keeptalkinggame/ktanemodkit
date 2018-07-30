using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class KMColorblindMode : MonoBehaviour
{
    void Awake()
    {
        settingsPath = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "ColorblindMode.json");
    }

    [SerializeField]
    private bool _colorblindMode = false;

    public bool ColorblindModeActive
    {
        get
        {
            if (Application.isEditor) return _colorblindMode;

            if (!File.Exists(settingsPath)) WriteSettings(new ColorblindModeSettings());

            ColorblindModeSettings settings = JsonConvert.DeserializeObject<ColorblindModeSettings>(File.ReadAllText(settingsPath));
            WriteSettings(settings);

            string moduleID = null;
            bool? moduleEnabled = null;

            KMBombModule bombModule = GetComponent<KMBombModule>();
            KMNeedyModule needyModule = GetComponent<KMNeedyModule>();
            if (bombModule)
            {
                moduleID = bombModule.ModuleType;
            }
            else if (needyModule)
            {
                moduleID = needyModule.ModuleType;
            }

            if (moduleID != null && !settings.EnabledModules.TryGetValue(moduleID, out moduleEnabled))
            {
                settings.EnabledModules[moduleID] = null;
                WriteSettings(settings);
            }

            return moduleEnabled ?? settings.Enabled;
        }
    }

    static string settingsPath;

    static void WriteSettings(ColorblindModeSettings settings)
    {
        File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
    }
}

internal class ColorblindModeSettings
{
    public bool Enabled = false;
    public Dictionary<string, bool?> EnabledModules = new Dictionary<string, bool?>();
}