using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

            string key = null;

            KMBombModule bombModule = GetComponent<KMBombModule>();
            KMNeedyModule needyModule = GetComponent<KMNeedyModule>();

            if (bombModule != null)
                key = bombModule.ModuleType;
            else if (needyModule != null)
                key = needyModule.ModuleType;
            else
                key = Regex.Replace(gameObject.name, @"\(Clone\)$", "");

            try
            {
                var settingsPath = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "ColorblindMode.json");

                ColorblindModeSettings settings = new ColorblindModeSettings();
                if (File.Exists(settingsPath))
                    settings = JsonConvert.DeserializeObject<ColorblindModeSettings>(File.ReadAllText(settingsPath));

                bool? isEnabled = null;
                if (!string.IsNullOrEmpty(key) && !settings.EnabledModules.TryGetValue(key, out isEnabled))
                    settings.EnabledModules[key] = null;

                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                return isEnabled ?? settings.Enabled;
            }
            catch (Exception e)
            {
                Debug.LogFormat(@"[Colorblind Mode] Error in ""{0}"": {1} ({2})\n{3}", key ?? "<null>", e.Message, e.GetType().FullName, e.StackTrace);
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