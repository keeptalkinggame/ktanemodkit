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

    public KMSelectable StartButton;

    void Start()
    {
        UpdateDisplay();
        TimeMinusButton.OnInteract += delegate () { time -= 30; UpdateDisplay(); return false; };
        TimePlusButton.OnInteract += delegate () { time += 30; UpdateDisplay(); return false; };
        ModulesMinusButton.OnInteract += delegate () { modules--; UpdateDisplay(); return false; };
        ModulesPlusButton.OnInteract += delegate () { modules++; UpdateDisplay(); return false; };
        StartButton.OnInteract += StartMission;
    }
	
	void Update ()
    {

    }

    void UpdateDisplay()
    {
        TimeText.text = "" + time;
        ModulesText.text = "" + modules;
    }

    bool StartMission()
    {
        KMGeneratorSetting generatorSettings = new KMGeneratorSetting();
        generatorSettings.NumStrikes = 3;
        generatorSettings.TimeLimit = time;

        KMComponentPool pool = new KMComponentPool();
        pool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE;
        pool.Count = modules;
        pool.AllowedSources = KMComponentPool.ComponentSource.Mods;
        generatorSettings.ComponentPools = new List<KMComponentPool> { pool };

        KMMission mission = ScriptableObject.CreateInstance<KMMission>() as KMMission;
        mission.DisplayName = "Custom Freeplay";
        mission.GeneratorSetting = generatorSettings;

        GetComponent<KMGameCommands>().StartMission(mission, "" + Random.Range(0, int.MaxValue));
        return false;
    }
}
