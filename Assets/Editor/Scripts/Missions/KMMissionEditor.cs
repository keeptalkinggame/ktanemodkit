using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Custom editor for KMMission, allowing you to easy create KMComponentPools and control the 
/// generation of bombs for this mission, among other parameters.
/// </summary>
[CustomEditor(typeof(KMMission))]
public class KMMissionEditor : Editor
{
    protected Vector2 descriptionScrollPos;
    protected int expandedComponentPoolIndex = -1;

    public override void OnInspectorGUI()
    {
        if (target != null)
        {
            serializedObject.Update();

            //Basic mission meta-data
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUILayout.SelectableLabel(serializedObject.targetObject.name);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID In-Game");
            EditorGUILayout.SelectableLabel(string.Format("mod_{0}_{1}", ModConfig.ID, serializedObject.targetObject.name));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("DisplayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PacingEventsEnabled"));

            //Generator Settings
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Generator Settings:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GeneratorSetting.TimeLimit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GeneratorSetting.NumStrikes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GeneratorSetting.TimeBeforeNeedyActivation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GeneratorSetting.FrontFaceOnly"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GeneratorSetting.OptionalWidgetCount"));

            //Component Pools
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Component Pools:");
            DrawModuleCountWarning();

            SerializedProperty componentPoolListProperty = serializedObject.FindProperty("GeneratorSetting.ComponentPools");
            EditorGUI.indentLevel++;
            for (int i = 0; i < componentPoolListProperty.arraySize; i++)
            {
                DrawComponentPool(componentPoolListProperty, i);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            //Component Pools
            if (GUILayout.Button("Add Component Pool"))
            {
                AddComponentPool();
                expandedComponentPoolIndex = componentPoolListProperty.arraySize - 1; //Expand the newly created pool
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    protected void DrawModuleCountWarning()
    {
        KMMission mission = (KMMission)serializedObject.targetObject;

        if (mission != null && mission.GeneratorSetting != null)
        {
            int moduleCount = 0;
            foreach (KMComponentPool pool in mission.GeneratorSetting.ComponentPools)
            {
                moduleCount += pool.Count;
            }

            int limit = mission.GeneratorSetting.FrontFaceOnly ? 5 : 11;

            if (moduleCount > limit)
            {
                EditorGUILayout.HelpBox(
                    string.Format("Total module count is {0} (default limit is {1}). Mission may not work as you intend!", moduleCount,
                    mission.GeneratorSetting.FrontFaceOnly ? limit + " when FrontFaceOnly=true" : limit.ToString()),
                    MessageType.Error);
            }
        }
    }

    protected void AddComponentPool()
    {
        SerializedProperty componentPools = serializedObject.FindProperty("GeneratorSetting.ComponentPools");

        int index = componentPools.arraySize;
        componentPools.arraySize++;

        var element = componentPools.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("Count").intValue = 1;
        element.FindPropertyRelative("ComponentTypes").arraySize = 0;
        element.FindPropertyRelative("SpecialComponentType").intValue = (int)KMComponentPool.SpecialComponentTypeEnum.None;
        element.FindPropertyRelative("ModTypes").arraySize = 0;
    }

    protected void RemoveComponentPool(int index)
    {
        SerializedProperty componentPools = serializedObject.FindProperty("GeneratorSetting.ComponentPools");

        componentPools.DeleteArrayElementAtIndex(index);
    }

    /// <summary>
    /// Draw a single Component Pool editor
    /// </summary>
    /// <param name="componentPoolListProperty"></param>
    /// <param name="index"></param>
    protected void DrawComponentPool(SerializedProperty componentPoolListProperty, int index)
    {
        SerializedProperty componentPoolProperty = componentPoolListProperty.GetArrayElementAtIndex(index);

        //Draw the summary line and control buttons
        bool isOnlyComponentPool = (componentPoolListProperty.arraySize == 1);
        DrawComponentPoolEntry(index, componentPoolProperty, isOnlyComponentPool);

        bool isExpanded = (expandedComponentPoolIndex == index);

        //Expandable section showing component type check boxes
        if (isExpanded)
        {
            DrawExpandedComponentTypeSelection(index, componentPoolProperty);
        }
    }

    private void DrawComponentPoolEntry(int poolIndex, SerializedProperty componentPoolProperty, bool isOnlyComponentPool)
    {
        EditorGUILayout.BeginHorizontal();

        //Count
        componentPoolProperty.FindPropertyRelative("Count").intValue = Math.Max(EditorGUILayout.IntField(
            componentPoolProperty.FindPropertyRelative("Count").intValue, GUILayout.Width(60)), 1);


        //Summary of types in this pool
        DrawComponentPoolSummaryLabel(poolIndex);

        //Edit and Delete buttons
        if (GUILayout.Button("Edit", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
        {
            if (expandedComponentPoolIndex == poolIndex)
            {
                expandedComponentPoolIndex = -1;
            }
            else
            {
                expandedComponentPoolIndex = poolIndex;
            }
        }

        if (isOnlyComponentPool) { GUI.enabled = false; } //Disable the delete button if this is only pool
        if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, GUILayout.Width(60)))
        {
            RemoveComponentPool(poolIndex);
            GUI.enabled = true;
            if (expandedComponentPoolIndex == poolIndex)
            {
                expandedComponentPoolIndex = -1;
            }
        }
        if (isOnlyComponentPool) { GUI.enabled = true; } //Reenable the GUI going forward if needed
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws a label summarizing the component types selected, or a warning if none are.
    /// </summary>
    /// <param name="componentFlags"></param>
    private void DrawComponentPoolSummaryLabel(int poolIndex)
    {
        KMMission mission = (KMMission)serializedObject.targetObject;
        KMComponentPool pool = mission.GeneratorSetting.ComponentPools[poolIndex];

        string[] nonEmptyModNames = pool.ModTypes.Where(t => !string.IsNullOrEmpty(t)).ToArray();

        if (pool.SpecialComponentType != KMComponentPool.SpecialComponentTypeEnum.None)
        {
            string specialSummary = string.Format("{0} ({1})",
                pool.SpecialComponentType.ToString(),
                pool.AllowedSources.ToString());
            EditorGUILayout.LabelField(specialSummary);
        }
        else if (pool.ComponentTypes.Count > 0 || nonEmptyModNames.Length > 0)
        {
            EditorStyles.label.wordWrap = true;
            string componentTypeNames = String.Join(", ", pool.ComponentTypes.Select(x => x.ToString()).ToArray());

            if (nonEmptyModNames.Length > 0)
            {
                if (componentTypeNames.Length > 0)
                {
                    componentTypeNames += ", ";
                }

                componentTypeNames += String.Join(", ", nonEmptyModNames);
            }

            EditorGUILayout.LabelField(componentTypeNames);
        }
        else
        {
            EditorGUILayout.HelpBox("No component type selected!", MessageType.Error);
        }
    }

    private void DrawExpandedComponentTypeSelection(int poolIndex, SerializedProperty componentPool)
    {
        EditorGUI.indentLevel++;
        //Component Pool Editing
        using (new EditorGUILayout.VerticalScope("box"))
        {
            //Special Component Types
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("All Types:");

                //Special
                DrawSpecialPicker(poolIndex);

                //Sources
                DrawComponentSourcePicker(poolIndex);
            }

            //Custom, specific component types
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label("Specific Types:");

                using (new EditorGUILayout.VerticalScope())
                {
                    //Base game modules
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

                            if ((componentType >= KMComponentPool.ComponentTypeEnum.Wires) &&
                                (componentType < KMComponentPool.ComponentTypeEnum.NeedyVentGas))
                            {
                                DrawToggle(poolIndex, componentType);
                            }
                        }
                        EditorGUILayout.EndVertical();


                        //Needy
                        using (new EditorGUILayout.VerticalScope())
                        {
                            EditorGUILayout.LabelField("Needy:");
                            for (int i = 0; i < componentTypes.Length; i++)
                            {
                                KMComponentPool.ComponentTypeEnum componentType = (KMComponentPool.ComponentTypeEnum)componentTypes.GetValue(i);

                                if (componentType >= KMComponentPool.ComponentTypeEnum.NeedyVentGas &&
                                    componentType <= KMComponentPool.ComponentTypeEnum.NeedyKnob)
                                {
                                    DrawToggle(poolIndex, componentType);
                                }
                            }
                        }
                    }

                    //Mod modules
                    DrawModTypesList(poolIndex);
                }
            }
        }

        EditorGUILayout.Separator();
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Draws and responds to a toggle box for a component type. Clears Special types, if set.
    /// </summary>
    private void DrawToggle(int poolIndex, KMComponentPool.ComponentTypeEnum typeToToggle)
    {
        KMMission mission = (KMMission)serializedObject.targetObject;
        KMComponentPool componentPool = mission.GeneratorSetting.ComponentPools[poolIndex];

        bool previousValue = componentPool.ComponentTypes.Contains(typeToToggle);

        if (EditorGUILayout.ToggleLeft(
            typeToToggle.ToString(),
            previousValue))
        {
            if (!componentPool.ComponentTypes.Contains(typeToToggle))
            {
                componentPool.ComponentTypes.Add(typeToToggle);
            }
        }
        else
        {
            componentPool.ComponentTypes.RemoveAll(x => x == typeToToggle);
        }

        //If we just toggled something, clear any special flags too
        bool currentValue = componentPool.ComponentTypes.Contains(typeToToggle);
        if (previousValue != currentValue)
        {
            componentPool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.None;
        }
    }

    /// <summary>
    /// Draws and responds to an enum picker for a special component type. Clears regular and special types, if set.
    /// </summary>
    private void DrawSpecialPicker(int poolIndex)
    {
        KMMission mission = (KMMission)serializedObject.targetObject;
        KMComponentPool componentPool = mission.GeneratorSetting.ComponentPools[poolIndex];

        KMComponentPool.SpecialComponentTypeEnum previousValue = componentPool.SpecialComponentType;

        componentPool.SpecialComponentType = (KMComponentPool.SpecialComponentTypeEnum)EditorGUILayout.EnumPopup(
            "Type:",
            componentPool.SpecialComponentType, GUILayout.MinWidth(400));

        //If we just changed the special type, clear any component types too
        KMComponentPool.SpecialComponentTypeEnum currentValue = componentPool.SpecialComponentType;
        if (previousValue != currentValue)
        {
            componentPool.ComponentTypes.Clear();
            componentPool.ModTypes.Clear();
        }
    }

    /// <summary>
    /// Draws and responds to a picker for a component sources.
    /// </summary>
    private void DrawComponentSourcePicker(int poolIndex)
    {
        string[] options = new string[] { "Base", "Mods", "Base and Mods" };

        KMMission mission = (KMMission)serializedObject.targetObject;
        KMComponentPool componentPool = mission.GeneratorSetting.ComponentPools[poolIndex];

        KMComponentPool.ComponentSource previousValue = componentPool.AllowedSources;

        bool allowBase = (previousValue & KMComponentPool.ComponentSource.Base) == KMComponentPool.ComponentSource.Base;
        bool allowMods = (previousValue & KMComponentPool.ComponentSource.Mods) == KMComponentPool.ComponentSource.Mods;

        int index = 0;

        if (allowBase)
        {
            if (allowMods)
            {
                index = 2;
            }
            else
            {
                index = 0;
            }
        }
        else
        {
            index = 1;
        }

        index = EditorGUILayout.Popup("Source:", index, options, GUILayout.MinWidth(400));

        switch (index)
        {
            case 0: { componentPool.AllowedSources = KMComponentPool.ComponentSource.Base; } break;
            case 1: { componentPool.AllowedSources = KMComponentPool.ComponentSource.Mods; } break;
            case 2: { componentPool.AllowedSources = KMComponentPool.ComponentSource.Base | KMComponentPool.ComponentSource.Mods; } break;
        }
    }

    /// <summary>
    /// Draw the array of mod module names to be selected from.
    /// </summary>
    /// <param name="poolIndex"></param>
    protected void DrawModTypesList(int poolIndex)
    {
        KMMission mission = (KMMission)serializedObject.targetObject;
        KMComponentPool componentPool = mission.GeneratorSetting.ComponentPools[poolIndex];
        SerializedProperty componentPools = serializedObject.FindProperty("GeneratorSetting.ComponentPools");

        var element = componentPools.GetArrayElementAtIndex(poolIndex);
        EditorGUILayout.PropertyField(element.FindPropertyRelative("ModTypes"), true);

        //Clear any special flags if needed
        if (componentPool.ModTypes != null && componentPool.ModTypes.Count > 0)
        {
            componentPool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.None;
        }
    }
}
