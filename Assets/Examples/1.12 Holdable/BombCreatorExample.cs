using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombCreatorExample : MonoBehaviour
{
    public TextMesh TimeText;
    public KMSelectable TimeMinusButton;
    public KMSelectable TimePlusButton;
    int time = 300;

    public TextMesh ModulesText;
    public KMSelectable ModulesMinusButton;
    public KMSelectable ModulesPlusButton;
    int modules = 3;

    public TextMesh WidgetsText;
    public KMSelectable WidgetsMinusButton;
    public KMSelectable WidgetsPlusButton;
    int widgets = 5;

    public TextMesh ModuleDisableText;
    public KMSelectable ModuleDisableMinusButton;
    public KMSelectable ModuleDisablePlusButton;
    public KMSelectable ModuleDisableButton;
    int moduleDisableIndex = 0;

    public KMSelectable StartButton;
    List<KMGameInfo.KMModuleInfo> availableModules;
    List<string> disabledModuleIds;

    int maxModules = 11;

    void Start()
    {
        availableModules = GetComponent<KMGameInfo>().GetAvailableModuleInfo();
        maxModules = GetComponent<KMGameInfo>().GetMaximumBombModules();
        if (availableModules == null)
        {
            availableModules = CreateTempModules();
        }
        disabledModuleIds = new List<string>();

        UpdateDisplay();
        UpdateModuleDisableDisplay();
        TimeMinusButton.OnInteract += delegate () { time -= 30; UpdateDisplay(); return false; };
        TimePlusButton.OnInteract += delegate () { time += 30; UpdateDisplay(); return false; };
        ModulesMinusButton.OnInteract += delegate () { modules--; modules = Mathf.Clamp(modules, 1, maxModules); UpdateDisplay(); return false; };
        ModulesPlusButton.OnInteract += delegate () { modules++; modules = Mathf.Clamp(modules, 1, maxModules); UpdateDisplay(); return false; };
        WidgetsMinusButton.OnInteract += delegate () { widgets--; widgets = Mathf.Clamp(widgets, 0, 20); UpdateDisplay(); return false; };
        WidgetsPlusButton.OnInteract += delegate () { widgets++; widgets = Mathf.Clamp(widgets, 0, 20); UpdateDisplay(); return false; };
        ModuleDisableMinusButton.OnInteract += delegate () { ChangeModuleDisableIndex(-1); return false; };
        ModuleDisablePlusButton.OnInteract += delegate () { ChangeModuleDisableIndex(1); return false; };
        ModuleDisableButton.OnInteract += delegate () { ModuleDisableButtonPressed(); return false; };

        StartButton.OnInteract += StartMission;
    }

    private List<KMGameInfo.KMModuleInfo> CreateTempModules()
    {
        List<KMGameInfo.KMModuleInfo> tempModules = new List<KMGameInfo.KMModuleInfo>();

        KMGameInfo.KMModuleInfo info1 = new KMGameInfo.KMModuleInfo();
        info1.DisplayName = "Module 1";
        info1.ModuleId = "Module1";
        tempModules.Add(info1);

        KMGameInfo.KMModuleInfo info2 = new KMGameInfo.KMModuleInfo();
        info2.DisplayName = "Module 2";
        info1.ModuleId = "Module2";
        tempModules.Add(info2);
        
        return tempModules;
    }

    void UpdateDisplay()
    {
        TimeText.text = "" + time;
        ModulesText.text = "" + modules;
        WidgetsText.text = "" + widgets;
    }

    void ChangeModuleDisableIndex(int diff)
    {
        moduleDisableIndex += diff;
        if(moduleDisableIndex < 0)
        {
            moduleDisableIndex = availableModules.Count - 1;
        }
        else if(moduleDisableIndex >= availableModules.Count)
        {
            moduleDisableIndex = 0;
        }

        UpdateModuleDisableDisplay();
    }

    void UpdateModuleDisableDisplay()
    {
        if(availableModules.Count > 0)
        {
            KMGameInfo.KMModuleInfo moduleInfo = availableModules[moduleDisableIndex];
            ModuleDisableText.text = moduleInfo.DisplayName;
            if(disabledModuleIds.Contains(moduleInfo.ModuleId))
            {
                ModuleDisableText.color = Color.red;
            }
            else
            {
                ModuleDisableText.color = Color.white;
            }
        }
    }

    void ModuleDisableButtonPressed()
    {
        if(availableModules.Count > 0)
        {
            KMGameInfo.KMModuleInfo moduleInfo = availableModules[moduleDisableIndex];
            if(disabledModuleIds.Contains(moduleInfo.ModuleId))
            {
                disabledModuleIds.Remove(moduleInfo.ModuleId);
            }
            else
            {
                disabledModuleIds.Add(moduleInfo.ModuleId);
            }

            UpdateModuleDisableDisplay();
        }
    }

    bool StartMission()
    {
        KMGeneratorSetting generatorSettings = new KMGeneratorSetting();
        generatorSettings.NumStrikes = 3;
        generatorSettings.TimeLimit = time;
        
        generatorSettings.ComponentPools = BuildComponentPools();
        generatorSettings.OptionalWidgetCount = widgets;

        KMMission mission = ScriptableObject.CreateInstance<KMMission>() as KMMission;
        mission.DisplayName = "Custom Freeplay";
        mission.GeneratorSetting = generatorSettings;

        GetComponent<KMGameCommands>().StartMission(mission, "" + UnityEngine.Random.Range(0, int.MaxValue));
        return false;
    }

    List<KMComponentPool> BuildComponentPools()
    {
        List<KMComponentPool> pools = new List<KMComponentPool>();

        KMComponentPool solvablePool = new KMComponentPool();
        solvablePool.ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>();
        solvablePool.ModTypes = new List<string>();

        KMComponentPool needyPool = new KMComponentPool();
        needyPool.ComponentTypes = new List<KMComponentPool.ComponentTypeEnum>();
        needyPool.ModTypes = new List<string>();

        foreach (KMGameInfo.KMModuleInfo moduleInfo in availableModules)
        {
            if(!disabledModuleIds.Contains(moduleInfo.ModuleId))
            {
                KMComponentPool pool = moduleInfo.IsNeedy ? needyPool : solvablePool;
                if(moduleInfo.IsMod)
                {
                    pool.ModTypes.Add(moduleInfo.ModuleId);
                }
                else
                {
                    pool.ComponentTypes.Add(moduleInfo.ModuleType);
                }
                
            }
        }
        solvablePool.Count = needyPool.ComponentTypes.Count + needyPool.ModTypes.Count > 0 ? modules - 1 : modules;
        needyPool.Count = needyPool.ComponentTypes.Count + needyPool.ModTypes.Count > 0 ? 1 : 0;

        pools.Add(solvablePool);
        pools.Add(needyPool);

        return pools;
    }
}
