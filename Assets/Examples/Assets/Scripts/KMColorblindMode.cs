using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class KMColorblindMode : MonoBehaviour
{
    [SerializeField]
    private bool _colorblindMode = false;

    public bool ColorblindModeActive
    {
        get
        {
            if (Application.isEditor)
                return _colorblindMode;

            var settingsPath = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "ColorblindMode.json");

            ColorblindModeSettings settings = new ColorblindModeSettings();
            try
            {
                if (File.Exists(settingsPath))
                    settings = JsonConvert.DeserializeObject<ColorblindModeSettings>(File.ReadAllText(settingsPath));

                string moduleID = null;
                bool? moduleEnabled = null;

                KMBombModule bombModule = GetComponent<KMBombModule>();
                KMNeedyModule needyModule = GetComponent<KMNeedyModule>();
                if (bombModule != null)
                    moduleID = bombModule.ModuleType;
                else if (needyModule != null)
                    moduleID = needyModule.ModuleType;

                if (moduleID != null && !settings.EnabledModules.TryGetValue(moduleID, out moduleEnabled))
                    settings.EnabledModules[moduleID] = null;

                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                return moduleEnabled ?? settings.Enabled;
            }
            catch (Exception e)
            {
                Debug.LogFormat(@"[Colorblind Mode] Error: {0} ({1})", e.Message, e.GetType().FullName);
                return false;
            }
        }
    }
}

internal class ColorblindModeSettings
{
    public bool Enabled = false;
    public Dictionary<string, bool?> EnabledModules = new Dictionary<string, bool?>();
}