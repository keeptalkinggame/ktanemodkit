using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

[CustomEditor(typeof(KMMission))]
public class CustomKMMissionEditor : Editor
{
    public const string MULTIPLE_BOMBS_COMPONENT_POOL_ID = "Multiple Bombs";
    public const string FACTORY_MODE_COMPONENT_POOL_ID = "Factory Mode";
    public enum FactoryMode
    {
        Static,
        FiniteSequence,
        FiniteSequenceGlobalTime,
        FiniteSequenceGlobalStrikes,
        FiniteSequenceGlobalTimeStrikes,
        InfiniteSequence,
        InfiniteSequenceGlobalTime,
        InfiniteSequenceGlobalStrikes,
        InfiniteSequenceGlobalTimeStrikes
    }
    public static string[] FactoryModeFriendlyNames = new string[] { "Static", "Finite Sequence", "Finite Sequence + Global Time", "Finite Sequence + Global Strikes", "Finite Sequence + Global Time & Strikes", "Infinite Sequence", "Infinite Sequence + Global Time", "Infinite Sequence + Global Strikes", "Infinite Sequence + Global Time & Strikes" };

    private int totalBombCount;
    private List<int> multipleBombsComponentPools;
    private Dictionary<int, KeyValuePair<KMGeneratorSetting, int>> multipleBombsGeneratorSettings;
    private FactoryMode factoryMode;
    private int factoryModeComponentPool;
    private int activeGeneratorSetting;
    private int activeComponentPool;
    private int currentAddGeneratorSettingIndex;
    private Vector2 scrollPosition;
    private Vector2 dmgScrollPosition;
    
    private string errorMessage = null;
    private string dmgString;

    public void OnEnable()
    {
        activeGeneratorSetting = 0;
        activeComponentPool = -1;
        currentAddGeneratorSettingIndex = 1;
        scrollPosition = Vector2.zero;
        dmgScrollPosition = Vector2.zero;
        if (target != null)
            readCurrentMission();
        Undo.undoRedoPerformed += onUndoRedoPerformed;
        
        dmgString = DMGMissionLoader.GetDmgString(serializedObject.targetObject as KMMission);
        errorMessage = null;
    }

    public void OnDisable()
    {
        Undo.undoRedoPerformed -= onUndoRedoPerformed;
    }

    private void readCurrentMission()
    {
        totalBombCount = 1;
        multipleBombsComponentPools = new List<int>();
        multipleBombsGeneratorSettings = new Dictionary<int, KeyValuePair<KMGeneratorSetting, int>>();
        factoryMode = FactoryMode.Static;
        factoryModeComponentPool = -1;
        SerializedProperty componentPools = serializedObject.FindProperty("GeneratorSetting.ComponentPools");
        for (int i = 0; i < componentPools.arraySize; i++)
        {
            SerializedProperty componentPool = componentPools.GetArrayElementAtIndex(i);
            SerializedProperty modTypes = componentPool.FindPropertyRelative("ModTypes");
            readComponentPool(modTypes, componentPool.FindPropertyRelative("Count").intValue, i);
        }
    }

    private void readComponentPool(SerializedProperty modTypes, int count, int index)
    {
        if (modTypes.arraySize == 1)
        {
            string modType = modTypes.GetArrayElementAtIndex(0).stringValue;
            if (modType == MULTIPLE_BOMBS_COMPONENT_POOL_ID)
            {
                totalBombCount += count;
                multipleBombsComponentPools.Add(index);
            }
            else if (modType.StartsWith(FACTORY_MODE_COMPONENT_POOL_ID))
            {
                factoryMode = (FactoryMode)count;
                factoryModeComponentPool = index;
            }
            else if (modType.StartsWith(MULTIPLE_BOMBS_COMPONENT_POOL_ID + ":"))
            {
                string[] strings = modType.Split(new char[] { ':' }, 3);
                if (strings.Length != 3)
                    return;
                int bombIndex;
                if (!int.TryParse(strings[1], out bombIndex))
                    return;
                if (!multipleBombsGeneratorSettings.ContainsKey(bombIndex) && bombIndex > 0)
                {
                    KMGeneratorSetting generatorSetting;
                    try
                    {
                        generatorSetting = JsonConvert.DeserializeObject<KMGeneratorSetting>(strings[2]);
                    }
                    catch
                    {
                        return;
                    }
                    multipleBombsGeneratorSettings.Add(bombIndex, new KeyValuePair<KMGeneratorSetting, int>(generatorSetting, index));
                }
            }
        }
    }

    private void onUndoRedoPerformed()
    {
        if (target != null)
        {
            serializedObject.Update();
            readCurrentMission();
        }
    }

    public override void OnInspectorGUI()
    {
        bool validModification = true;

        if (target != null)
        {
            serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();

            //Basic mission meta-data
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUILayout.SelectableLabel(serializedObject.targetObject.name);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID In-Game");
            EditorGUILayout.SelectableLabel(string.Format("mod_{0}_{1}", ModConfig.ID, serializedObject.targetObject.name));
            EditorGUILayout.EndHorizontal();

            SerializedProperty displayNameProperty = serializedObject.FindProperty("DisplayName");
            EditorGUILayout.PropertyField(displayNameProperty);
            displayNameProperty.stringValue = displayNameProperty.stringValue.Trim();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PacingEventsEnabled"));

            SerializedProperty componentPools = serializedObject.FindProperty("GeneratorSetting.ComponentPools");

            if (factoryMode <= FactoryMode.FiniteSequenceGlobalTimeStrikes)
            {
                int newTotalBombCount = EditorGUILayout.IntField("Bomb Count", totalBombCount);
                setTotalBombCount(newTotalBombCount);
            }
            else
            {
                bool wasEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.TextField("Bomb Count", "Infinite");
                GUI.enabled = wasEnabled;
            }

            FactoryMode newFactoryMode = (FactoryMode)EditorGUILayout.Popup("Factory Mode", (int)factoryMode, FactoryModeFriendlyNames);
            if (newFactoryMode != factoryMode)
            {
                if (newFactoryMode == FactoryMode.Static)
                {
                    if (factoryModeComponentPool != -1)
                    {
                        componentPools.DeleteArrayElementAtIndex(factoryModeComponentPool);
                        readCurrentMission();
                    }
                }
                else if (factoryModeComponentPool == -1)
                {
                    int index = addComponentPool(serializedObject.FindProperty("GeneratorSetting"));
                    SerializedProperty componentPool = componentPools.GetArrayElementAtIndex(index);
                    componentPool.FindPropertyRelative("Count").intValue = (int)newFactoryMode;
                    componentPool.FindPropertyRelative("ModTypes").arraySize = 1;
                    componentPool.FindPropertyRelative("ModTypes").GetArrayElementAtIndex(0).stringValue = FACTORY_MODE_COMPONENT_POOL_ID;
                    factoryModeComponentPool = index;
                }
                else
                {
                    componentPools.GetArrayElementAtIndex(factoryModeComponentPool).FindPropertyRelative("Count").intValue = (int)newFactoryMode;
                }
                if (newFactoryMode >= FactoryMode.InfiniteSequence)
                    setTotalBombCount(1);
                factoryMode = newFactoryMode;
            }

            //Generator Settings
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Generator Settings");

            List<string> unusedGeneratorSettings = new List<string>();
            List<KeyValuePair<int, string>> tabMap = new List<KeyValuePair<int, string>>();
            tabMap.Add(new KeyValuePair<int, string>(0, "Bomb 0"));
            foreach (KeyValuePair<int, KeyValuePair<KMGeneratorSetting, int>> kv in multipleBombsGeneratorSettings)
            {
                if (kv.Key < totalBombCount || factoryMode >= FactoryMode.InfiniteSequence)
                    tabMap.Add(new KeyValuePair<int, string>(kv.Key, "Bomb " + kv.Key));
                else
                    unusedGeneratorSettings.Add(kv.Key.ToString());
            }
            tabMap.Sort((x, y) => x.Key.CompareTo(y.Key));

            if (unusedGeneratorSettings.Count > 0)
            {
                string unusedGeneratorSettingsWarningMessage = "The mission contains unused generator settings (for " + (unusedGeneratorSettings.Count > 1 ? "bombs " + string.Join(", ", unusedGeneratorSettings.ToArray()) : "bomb " + unusedGeneratorSettings[0]) + ").";
                EditorGUILayout.HelpBox(unusedGeneratorSettingsWarningMessage, MessageType.Warning);
            }

            EditorGUILayout.BeginVertical("box");
            int currentTab = activeGeneratorSetting != -1 ? tabMap.FindIndex((x) => x.Key == activeGeneratorSetting) : tabMap.Count;
            if (currentTab == -1)
            {
                activeGeneratorSetting = 0;
                currentTab = 0;
            }
            List<string> tabs = new List<string>();
            tabs.Add(tabMap[0].Value);
            float minWidth = new GUIStyle("ButtonLeft").CalcSize(new GUIContent(tabMap[0].Value)).x;
            for (int i = 1; i < tabMap.Count; i++)
            {
                tabs.Add(tabMap[i].Value);
                float width = new GUIStyle("Button").CalcSize(new GUIContent(tabMap[i].Value)).x;
                if (width > minWidth)
                    minWidth = width;
            }
            tabs.Add("+");
            bool fits = Screen.width / tabs.Count > minWidth; //Screen.width is not an accurate measure of the available width but having the bar space always visible was too ugly
            if (!fits)
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(40));
            int newTab = GUILayout.Toolbar(currentTab, tabs.ToArray());
            if (!fits)
                EditorGUILayout.EndScrollView();
            if (newTab != currentTab)
            {
                if (newTab == tabs.Count - 1)
                {
                    activeGeneratorSetting = -1;
                }
                else
                {
                    activeGeneratorSetting = tabMap[newTab].Key;
                    activeComponentPool = -1;
                    GUIUtility.keyboardControl = 0;
                }
            }

            if (activeGeneratorSetting == -1)
            {
                if (factoryMode <= FactoryMode.FiniteSequenceGlobalTimeStrikes)
                {
                    List<int> vaildBombs = new List<int>();
                    for (int i = 1; i < totalBombCount; i++)
                    {
                        if (!multipleBombsGeneratorSettings.ContainsKey(i))
                        {
                            vaildBombs.Add(i);
                        }
                    }
                    if (vaildBombs.Count == 0)
                    {
                        EditorGUILayout.HelpBox("All of the bombs have a Generator Setting.", MessageType.None);
                    }
                    else
                    {
                        if (!vaildBombs.Contains(currentAddGeneratorSettingIndex))
                            currentAddGeneratorSettingIndex = vaildBombs[0];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Add Generator Setting for bomb");
                        currentAddGeneratorSettingIndex = vaildBombs[EditorGUILayout.Popup(vaildBombs.IndexOf(currentAddGeneratorSettingIndex), vaildBombs.Select((x) => x.ToString()).ToArray(), GUILayout.Width(60))];
                        EditorGUILayout.EndHorizontal();
                    }
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = vaildBombs.Count != 0;
                    if (GUILayout.Button("Add Generator Setting"))
                    {
                        addGeneratorSetting(currentAddGeneratorSettingIndex);
                    }
                    GUI.enabled = wasEnabled;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Add Generator Setting for bomb");
                    currentAddGeneratorSettingIndex = EditorGUILayout.IntField(currentAddGeneratorSettingIndex, GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                    bool isVaildBomb = currentAddGeneratorSettingIndex != 0 && !multipleBombsGeneratorSettings.ContainsKey(currentAddGeneratorSettingIndex);
                    if (!isVaildBomb)
                        EditorGUILayout.HelpBox("Bomb " + currentAddGeneratorSettingIndex + " already has a Generator Setting.", MessageType.None);
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = isVaildBomb;
                    if (GUILayout.Button("Add Generator Setting"))
                    {
                        addGeneratorSetting(currentAddGeneratorSettingIndex);
                    }
                    GUI.enabled = wasEnabled;
                }
            }
            else if (activeGeneratorSetting == 0)
            {
                bool wasEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Button("Delete");
                EditorGUILayout.EndHorizontal();
                GUI.enabled = wasEnabled;
                drawGeneratorSetting(serializedObject.FindProperty("GeneratorSetting"), true);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool delete = GUILayout.Button("Delete");
                EditorGUILayout.EndHorizontal();
                if (delete)
                {
                    removeGeneratorSetting(activeGeneratorSetting);
                }
                else
                {
                    KeyValuePair<KMGeneratorSetting, int> activeKV = multipleBombsGeneratorSettings[activeGeneratorSetting];
                    KMMission dummyMission = CreateInstance<KMMission>();
                    dummyMission.GeneratorSetting = activeKV.Key;
                    SerializedObject dummyMissionObject = new SerializedObject(dummyMission);
                    drawGeneratorSetting(dummyMissionObject.FindProperty("GeneratorSetting"), false);
                    if (dummyMissionObject.ApplyModifiedProperties())
                    {
                        serializedObject.FindProperty("GeneratorSetting").FindPropertyRelative("ComponentPools").GetArrayElementAtIndex(activeKV.Value).FindPropertyRelative("ModTypes").GetArrayElementAtIndex(0).stringValue = "Multiple Bombs:" + activeGeneratorSetting + ":" + JsonConvert.SerializeObject(activeKV.Key);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            
            //DMG String
            EditorGUILayout.PrefixLabel("DMG Mission String");

            if (EditorGUI.EndChangeCheck())
            {
                dmgString = DMGMissionLoader.GetDmgString((KMMission) serializedObject.targetObject);
            }

            dmgScrollPosition = EditorGUILayout.BeginScrollView(dmgScrollPosition, GUILayout.Height(15 * EditorGUIUtility.singleLineHeight));
            dmgString = EditorGUILayout.TextArea(dmgString,  GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Refresh DMG Mission String"))
            {
                activeComponentPool = -1;
                dmgString = DMGMissionLoader.GetDmgString((KMMission) serializedObject.targetObject);
                errorMessage = null;
            }

            if (GUILayout.Button("Load from DMG Mission String"))
            {
                activeComponentPool = -1;
                validModification = LoadFromDMGString();
            }

            if (GUILayout.Button("Open DMG Documentation"))
            {
                Application.OpenURL("https://github.com/red031000/ktane-DynamicMissionGenerator/blob/master/README.md");
            }

            if (errorMessage != null)
            {
                GUIStyle s = new GUIStyle(EditorStyles.boldLabel);
                s.normal.textColor = Color.red;
                EditorGUILayout.LabelField("Error: " + errorMessage, s);
            }
        }
        
        if (validModification)
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
    
     private bool LoadFromDMGString()
    {
        // Reset error message
        errorMessage = null;

        // Parse mission
        KMMission mission;
        try
        {
            mission = DMGMissionLoader.CreateMissionFromDmgString(dmgString);
        }
        catch (DMGMissionLoader.ParseException e)
        {
            errorMessage = e.Message;
            return false;
        }

        // Load mission properties
        serializedObject.FindProperty("PacingEventsEnabled").boolValue = mission.PacingEventsEnabled;
        serializedObject.FindProperty("DisplayName").stringValue = mission.DisplayName;
        serializedObject.FindProperty("Description").stringValue = mission.Description;

        serializedObject.FindProperty("GeneratorSetting.TimeLimit").floatValue = mission.GeneratorSetting.TimeLimit;
        serializedObject.FindProperty("GeneratorSetting.NumStrikes").intValue = mission.GeneratorSetting.NumStrikes;
        serializedObject.FindProperty("GeneratorSetting.TimeBeforeNeedyActivation").intValue =
            mission.GeneratorSetting.TimeBeforeNeedyActivation;
        serializedObject.FindProperty("GeneratorSetting.OptionalWidgetCount").intValue =
            mission.GeneratorSetting.OptionalWidgetCount;
        serializedObject.FindProperty("GeneratorSetting.FrontFaceOnly").boolValue =
            mission.GeneratorSetting.FrontFaceOnly;

        // Delete current pools
        var componentPools = serializedObject.FindProperty("GeneratorSetting.ComponentPools");
        if (componentPools.arraySize > 0)
        {
            for (int i = componentPools.arraySize - 1; i >= 0; i--)
            {
                componentPools.DeleteArrayElementAtIndex(i);
            }
        }

        // Save pools
        var pools = mission.GeneratorSetting.ComponentPools;
        for (int i = 0; i < pools.Count; i++)
        {
            componentPools.InsertArrayElementAtIndex(i);
            var element = componentPools.GetArrayElementAtIndex(i);
            var pool = pools[i];
            element.FindPropertyRelative("Count").intValue = pool.Count;
            element.FindPropertyRelative("SpecialComponentType").intValue = (int) pool.SpecialComponentType;

            element.FindPropertyRelative("ComponentTypes").arraySize =
                pool.ComponentTypes == null ? 0 : pool.ComponentTypes.Count;
            if (pool.ComponentTypes != null)
            {
                for (int j = 0; j < pool.ComponentTypes.Count; j++)
                {
                    element.FindPropertyRelative("ComponentTypes").GetArrayElementAtIndex(j).intValue =
                        (int) pool.ComponentTypes[j];
                }
            }

            element.FindPropertyRelative("ModTypes").arraySize = pool.ModTypes == null ? 0 : pool.ModTypes.Count;
            if (pool.ModTypes != null)
            {
                for (int j = 0; j < pool.ModTypes.Count; j++)
                {
                    element.FindPropertyRelative("ModTypes").GetArrayElementAtIndex(j).stringValue =
                        pool.ModTypes[j];
                }
            }
        }

        return true;
    }

    private void drawGeneratorSetting(SerializedProperty generatorSetting, bool isDefaultGeneratorSetting)
    {
        EditorGUILayout.PropertyField(generatorSetting.FindPropertyRelative("TimeLimit"));
        EditorGUILayout.PropertyField(generatorSetting.FindPropertyRelative("NumStrikes"));
        EditorGUILayout.PropertyField(generatorSetting.FindPropertyRelative("TimeBeforeNeedyActivation"));
        EditorGUILayout.PropertyField(generatorSetting.FindPropertyRelative("FrontFaceOnly"));
        EditorGUILayout.PropertyField(generatorSetting.FindPropertyRelative("OptionalWidgetCount"));

        //Component Pools
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Component Pools:");
        List<SerializedProperty> cleanComponentPools = drawModuleCountWarning(generatorSetting, generatorSetting.FindPropertyRelative("FrontFaceOnly").boolValue);
        EditorGUI.indentLevel++;
        for (int i = 0; i < generatorSetting.FindPropertyRelative("ComponentPools").arraySize; i++)
        {
            SerializedProperty componentPool = generatorSetting.FindPropertyRelative("ComponentPools").GetArrayElementAtIndex(i);
            if (cleanComponentPools.Any((x) => SerializedProperty.EqualContents(x, componentPool)))
            {
                bool wasDeleted = drawComponentPool(generatorSetting, i, cleanComponentPools.Count == 1, isDefaultGeneratorSetting);
                if (isDefaultGeneratorSetting && !wasDeleted)
                {
                    readComponentPool(componentPool.FindPropertyRelative("ModTypes"), componentPool.FindPropertyRelative("Count").intValue, i);
                }
            }
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        if (GUILayout.Button("Add Component Pool"))
        {
            activeComponentPool = addComponentPool(generatorSetting);
        }

        //EditorGUILayout.Separator();
        //EditorGUILayout.Separator();
        //EditorGUILayout.Separator();
    }

    private List<SerializedProperty> drawModuleCountWarning(SerializedProperty generatorSetting, bool frontFaceOnly)
    {
        List<SerializedProperty> cleanComponentPools = new List<SerializedProperty>();
        int moduleCount = 0;
        SerializedProperty componentPools = generatorSetting.FindPropertyRelative("ComponentPools");
        for (int i = 0; i < componentPools.arraySize; i++)
        {
            SerializedProperty componentPool = componentPools.GetArrayElementAtIndex(i);
            int count = componentPool.FindPropertyRelative("Count").intValue;
            if (componentPool.FindPropertyRelative("ModTypes").arraySize == 1)
            {
                string modType = componentPool.FindPropertyRelative("ModTypes").GetArrayElementAtIndex(0).stringValue;
                if (modType == MULTIPLE_BOMBS_COMPONENT_POOL_ID || modType == FACTORY_MODE_COMPONENT_POOL_ID || modType.StartsWith(MULTIPLE_BOMBS_COMPONENT_POOL_ID + ":"))
                    continue;
            }
            moduleCount += count;
            cleanComponentPools.Add(componentPool);
        }

        int limit = frontFaceOnly ? 5 : 11;
        if (moduleCount > limit)
        {
            EditorGUILayout.HelpBox("Total module count is " + moduleCount + " (default limit is " + (frontFaceOnly ? limit + " when FrontFaceOnly=true" : limit.ToString()) + "). Mission may not work as you intend!", MessageType.Error);
        }

        return cleanComponentPools;
    }

    private int addComponentPool(SerializedProperty generatorSetting)
    {
        SerializedProperty componentPools = generatorSetting.FindPropertyRelative("ComponentPools");
        componentPools.arraySize++;

        SerializedProperty componentPool = componentPools.GetArrayElementAtIndex(componentPools.arraySize - 1);
        componentPool.FindPropertyRelative("Count").intValue = 1;
        componentPool.FindPropertyRelative("ComponentTypes").arraySize = 0;
        componentPool.FindPropertyRelative("SpecialComponentType").intValue = (int)KMComponentPool.SpecialComponentTypeEnum.None;
        componentPool.FindPropertyRelative("ModTypes").arraySize = 0;
        return componentPools.arraySize - 1;
    }

    private void removeComponentPool(SerializedProperty generatorSetting, int index, bool isDefaultGeneratorSetting)
    {
        generatorSetting.FindPropertyRelative("ComponentPools").DeleteArrayElementAtIndex(index);
        if (isDefaultGeneratorSetting)
        {
            readCurrentMission();
        }
    }

    private bool drawComponentPool(SerializedProperty generatorSetting, int index, bool isOnlyComponentPool, bool isDefaultGeneratorSetting)
    {
        bool wasDeleted = drawComponentPoolEntry(generatorSetting, index, isOnlyComponentPool, isDefaultGeneratorSetting);
        if (!wasDeleted)
        {
            SerializedProperty componentPool = generatorSetting.FindPropertyRelative("ComponentPools").GetArrayElementAtIndex(index);
            if (activeComponentPool == index)
                drawExpandedComponentTypeSelection(componentPool);
        }
        return wasDeleted;
    }

    private bool drawComponentPoolEntry(SerializedProperty generatorSetting, int index, bool isOnlyComponentPool, bool isDefaultGeneratorSetting)
    {
        SerializedProperty componentPool = generatorSetting.FindPropertyRelative("ComponentPools").GetArrayElementAtIndex(index);

        EditorGUILayout.BeginHorizontal();
        componentPool.FindPropertyRelative("Count").intValue = Math.Max(EditorGUILayout.IntField(componentPool.FindPropertyRelative("Count").intValue, GUILayout.Width(60)), 1);
        drawComponentPoolSummaryLabel(componentPool);
        if (GUILayout.Button("Edit", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
        {
            if (activeComponentPool == index)
                activeComponentPool = -1;
            else
                activeComponentPool = index;
        }
        bool deleted = false;
        bool wasEnabled = GUI.enabled;
        GUI.enabled = !isOnlyComponentPool;
        if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, GUILayout.Width(60)))
        {
            removeComponentPool(generatorSetting, index, isDefaultGeneratorSetting);
            if (activeComponentPool == index)
                activeComponentPool = -1;
            deleted = true;
        }
        GUI.enabled = wasEnabled;
        EditorGUILayout.EndHorizontal();
        return deleted;
    }

    private void drawComponentPoolSummaryLabel(SerializedProperty componentPool)
    {
        if (componentPool.FindPropertyRelative("SpecialComponentType").intValue != (int)KMComponentPool.SpecialComponentTypeEnum.None)
        {
            string specialSummary = ((KMComponentPool.SpecialComponentTypeEnum)componentPool.FindPropertyRelative("SpecialComponentType").intValue).ToString() + " (" + ((KMComponentPool.ComponentSource)componentPool.FindPropertyRelative("AllowedSources").intValue).ToString() + ")";
            EditorGUILayout.LabelField(specialSummary);
        }
        else
        {
            string componentTypeNames = "";
            SerializedProperty componentTypes = componentPool.FindPropertyRelative("ComponentTypes");
            for (int i = 0; i < componentTypes.arraySize; i++)
            {
                if (i != 0)
                    componentTypeNames += ", ";
                componentTypeNames += ((KMComponentPool.ComponentTypeEnum)componentTypes.GetArrayElementAtIndex(i).intValue).ToString();
            }
            SerializedProperty modTypes = componentPool.FindPropertyRelative("ModTypes");
            for (int i = 0; i < modTypes.arraySize; i++)
            {
                string modType = modTypes.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrEmpty(modType))
                {
                    if (componentTypeNames.Length > 0)
                        componentTypeNames += ", ";
                    componentTypeNames += modType;
                }
            }

            if (componentTypeNames.Length > 0)
            {
                EditorStyles.label.wordWrap = true;
                EditorGUILayout.LabelField(componentTypeNames);
            }
            else
            {
                EditorGUILayout.HelpBox("No component type selected!", MessageType.Error);
            }
        }
    }

    private void drawExpandedComponentTypeSelection(SerializedProperty componentPool)
    {
        EditorGUI.indentLevel++;
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                SerializedProperty specialComponentTypeProperty = componentPool.FindPropertyRelative("SpecialComponentType");
                int oldSpecialComponentType = specialComponentTypeProperty.intValue;
                EditorGUILayout.PropertyField(specialComponentTypeProperty);
                if (specialComponentTypeProperty.intValue != oldSpecialComponentType)
                {
                    componentPool.FindPropertyRelative("ComponentTypes").ClearArray();
                    componentPool.FindPropertyRelative("ModTypes").ClearArray();
                }
                SerializedProperty allowedSourcesProperty = componentPool.FindPropertyRelative("AllowedSources");
                allowedSourcesProperty.intValue = EditorGUILayout.IntPopup("Source:", allowedSourcesProperty.intValue, new string[] { "Base", "Mods", "Base and Mods" }, new int[] { (int)KMComponentPool.ComponentSource.Base, (int)KMComponentPool.ComponentSource.Mods, (int)(KMComponentPool.ComponentSource.Base | KMComponentPool.ComponentSource.Mods) });
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Specific types:");

                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        //Here we make an assumption that ComponentTypeFlags has individual solvable components,
                        //followed by individual needy components
                        Array componentTypes = Enum.GetValues(typeof(KMComponentPool.ComponentTypeEnum));

                        //Solvable
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("Solvable:");
                        for (int i = 0; i < componentTypes.Length; i++)
                        {
                            KMComponentPool.ComponentTypeEnum componentType = (KMComponentPool.ComponentTypeEnum)componentTypes.GetValue(i);
                            if ((componentType >= KMComponentPool.ComponentTypeEnum.Wires) && (componentType < KMComponentPool.ComponentTypeEnum.NeedyVentGas))
                            {
                                drawToggle(componentPool, componentType);
                            }
                        }
                        EditorGUILayout.EndVertical();

                        //Needy
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("Needy:");
                        for (int i = 0; i < componentTypes.Length; i++)
                        {
                            KMComponentPool.ComponentTypeEnum componentType = (KMComponentPool.ComponentTypeEnum)componentTypes.GetValue(i);
                            if (componentType >= KMComponentPool.ComponentTypeEnum.NeedyVentGas && componentType <= KMComponentPool.ComponentTypeEnum.NeedyKnob)
                            {
                                drawToggle(componentPool, componentType);
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }

                    SerializedProperty modTypesProperty = componentPool.FindPropertyRelative("ModTypes");
                    EditorGUILayout.PropertyField(modTypesProperty, true);
                    for (int i = 0; i < modTypesProperty.arraySize; i++)
                    {
                        SerializedProperty element = modTypesProperty.GetArrayElementAtIndex(i);
                        element.stringValue = element.stringValue.Trim();
                    }

                    if (componentPool.FindPropertyRelative("ModTypes").arraySize != 0)
                        componentPool.FindPropertyRelative("SpecialComponentType").intValue = (int)KMComponentPool.SpecialComponentTypeEnum.None;
                }
            }
        }
        EditorGUILayout.Separator();
        EditorGUI.indentLevel--;
    }

    private void drawToggle(SerializedProperty componentPool, KMComponentPool.ComponentTypeEnum componentType)
    {
        SerializedProperty componentTypes = componentPool.FindPropertyRelative("ComponentTypes");
        bool previousValue = false;
        for (int i = 0; i < componentTypes.arraySize; i++)
        {
            if (componentTypes.GetArrayElementAtIndex(i).intValue == (int)componentType)
            {
                previousValue = true;
                break;
            }
        }
        bool newValue = EditorGUILayout.ToggleLeft(componentType.ToString(), previousValue);
        if (newValue != previousValue)
        {
            if (previousValue)
            {
                for (int i = componentTypes.arraySize - 1; i >= 0; i--)
                {
                    if (componentTypes.GetArrayElementAtIndex(i).intValue == (int)componentType)
                    {
                        componentTypes.DeleteArrayElementAtIndex(i);
                    }
                }
            }
            else
            {
                componentTypes.InsertArrayElementAtIndex(componentTypes.arraySize);
                componentTypes.GetArrayElementAtIndex(componentTypes.arraySize - 1).intValue = (int)componentType;
            }
            componentPool.FindPropertyRelative("SpecialComponentType").intValue = (int)KMComponentPool.SpecialComponentTypeEnum.None;
        }
    }

    private void setTotalBombCount(int newTotalBombCount)
    {
        if (newTotalBombCount != totalBombCount && newTotalBombCount > 0)
        {
            SerializedProperty componentPools = serializedObject.FindProperty("GeneratorSetting.ComponentPools");
            if (multipleBombsComponentPools.Count == 0 && newTotalBombCount > 1)
            {
                addComponentPool(serializedObject.FindProperty("GeneratorSetting"));
                SerializedProperty componentPool = componentPools.GetArrayElementAtIndex(componentPools.arraySize - 1);
                componentPool.FindPropertyRelative("ModTypes").arraySize = 1;
                componentPool.FindPropertyRelative("ModTypes").GetArrayElementAtIndex(0).stringValue = MULTIPLE_BOMBS_COMPONENT_POOL_ID;
                componentPool.FindPropertyRelative("Count").intValue = newTotalBombCount - 1;
                multipleBombsComponentPools.Add(componentPools.arraySize - 1);
            }
            else
            {
                int delta = newTotalBombCount - totalBombCount;
                for (int i = multipleBombsComponentPools.Count - 1; i >= 0; i--)
                {
                    int index = multipleBombsComponentPools[i];
                    SerializedProperty countProperty = componentPools.GetArrayElementAtIndex(index).FindPropertyRelative("Count");
                    int poolDelta = Math.Max(delta, -countProperty.intValue);
                    countProperty.intValue += poolDelta;
                    if (countProperty.intValue > 0)
                        break;
                    delta -= poolDelta;
                    multipleBombsComponentPools.RemoveAt(i);
                    componentPools.DeleteArrayElementAtIndex(index);
                    if (delta == 0)
                        break;
                }
            }
            readCurrentMission();
        }
    }

    private void addGeneratorSetting(int bombIndex)
    {
        KMGeneratorSetting newGeneratorSetting = new KMGeneratorSetting();
        newGeneratorSetting.ComponentPools = new List<KMComponentPool>() { new KMComponentPool() { Count = 1, ModTypes = new List<string>() } };
        int componentPoolIndex = addComponentPool(serializedObject.FindProperty("GeneratorSetting"));
        SerializedProperty componentPool = serializedObject.FindProperty("GeneratorSetting.ComponentPools").GetArrayElementAtIndex(componentPoolIndex);
        SerializedProperty modTypes = componentPool.FindPropertyRelative("ModTypes");
        modTypes.arraySize = 1;
        modTypes.GetArrayElementAtIndex(0).stringValue = MULTIPLE_BOMBS_COMPONENT_POOL_ID + ":" + bombIndex + ":" + JsonConvert.SerializeObject(newGeneratorSetting);
        multipleBombsGeneratorSettings.Add(bombIndex, new KeyValuePair<KMGeneratorSetting, int>(newGeneratorSetting, componentPoolIndex));
    }

    private void removeGeneratorSetting(int bombIndex)
    {
        serializedObject.FindProperty("GeneratorSetting.ComponentPools").DeleteArrayElementAtIndex(multipleBombsGeneratorSettings[bombIndex].Value);
        readCurrentMission();
    }
}
