using System;
using System.Collections.Generic;
using UnityEngine;

public class KMBossModule : MonoBehaviour
{
    public string[] GetIgnoredModules(KMBombModule module, string[] @default = null)
    {
        return GetIgnoredModules(module.ModuleDisplayName, @default);
    }

    public string[] GetIgnoredModules(string moduleDisplayName, string[] @default = null)
    {
        if (Application.isEditor)
            return @default ?? new string[0];

        var bossModuleManagerAPIGameObject = GameObject.Find("BossModuleManager");
        if (bossModuleManagerAPIGameObject == null) // Boss Module Manager is not installed
            return @default ?? new string[0];

        var bossModuleManagerAPI = bossModuleManagerAPIGameObject.GetComponent<IDictionary<string, object>>();
        if (bossModuleManagerAPI == null || !bossModuleManagerAPI.ContainsKey("GetIgnoredModules"))
            return @default ?? new string[0];

        return ((Func<string, string[]>) bossModuleManagerAPI["GetIgnoredModules"])(moduleDisplayName) ?? @default ?? new string[0];
    }
}
