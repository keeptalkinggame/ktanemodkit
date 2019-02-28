using Assets.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for creating the table of contents used by the mod to populate the 
/// bomb binder with missions.
/// </summary>
[CustomEditor(typeof(KMMissionTableOfContents))]
public class KMMissionTableOfContentsEditor : Editor
{
    protected static string MISSION_FOLDER = "Missions";

    protected Vector2 scrollPos;
    protected List<TableOfContentsSectionList> sectionLists;

    [MenuItem("Keep Talking ModKit/Missions/Create new Mission")]
    public static void CreateNewMission()
    {
        if (!AssetDatabase.IsValidFolder("Assets/" + MISSION_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets", MISSION_FOLDER);
        }

        KMMission mission = ScriptableObject.CreateInstance<KMMission>();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + MISSION_FOLDER + "/mission.asset");
        AssetDatabase.CreateAsset(mission, path);
        AssetImporter.GetAtPath(path).assetBundleName = AssetBundler.BUNDLE_FILENAME;

        EditorGUIUtility.PingObject(mission);
    }

    [MenuItem("Keep Talking ModKit/Missions/Create new Table of Contents")]
    public static void CreateNewTableOfContents()
    {
        if (!AssetDatabase.IsValidFolder("Assets/" + MISSION_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets", MISSION_FOLDER);
        }

        KMMissionTableOfContents tableOfContents = ScriptableObject.CreateInstance<KMMissionTableOfContents>();

        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + MISSION_FOLDER + "/TableOfContents.asset");
        AssetDatabase.CreateAsset(tableOfContents, path);
        AssetImporter.GetAtPath(path).assetBundleName = AssetBundler.BUNDLE_FILENAME;

        EditorGUIUtility.PingObject(tableOfContents);

        int numToCs = AssetDatabase.FindAssets("t:KMMissionTableOfContents").Length;
        if (numToCs > 1)
        {
            Debug.LogWarningFormat("Project has {0} KMMissionTableOfContents. Only one table of contents per mod is supported!", numToCs);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var titleProperty = serializedObject.FindProperty("Title");
        EditorGUILayout.PropertyField(titleProperty);
        titleProperty.stringValue = titleProperty.stringValue.Trim();

        DrawTableOfContentsToolbar();
        DrawTableOfContents();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Search the AssetDatabase for a mission with this ID and return it if it exists.
    /// </summary>
    /// <param name="missionID"></param>
    /// <returns></returns>
    public static KMMission GetMission(string missionID)
    {
        string[] guids = AssetDatabase.FindAssets(string.Format("t:KMMission {0}", missionID));

        if (guids.Length > 0)
        {
            foreach (string guid in guids)
            {
                KMMission mission = AssetDatabase.LoadAssetAtPath<KMMission>(AssetDatabase.GUIDToAssetPath(guid));
                if (mission.ID.Equals(missionID))
                {
                    return mission;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Search the AssetDatabase for all missions.
    /// </summary>
    /// <param name="missionID"></param>
    /// <returns></returns>
    public static List<KMMission> GetAllMissions()
    {
        string[] guids = AssetDatabase.FindAssets("t:KMMission");
        List<KMMission> missions = new List<KMMission>();

        foreach(string guid in guids)
        {
            KMMission mission = AssetDatabase.LoadAssetAtPath<KMMission>(AssetDatabase.GUIDToAssetPath(guid));

            if (mission != null)
            {
                missions.Add(mission);
            }
        }

        return missions;
    }

    private void InitializeSectionLists()
    {
        SerializedProperty sections = serializedObject.FindProperty("Sections");

        sectionLists = new List<TableOfContentsSectionList>();

        for (int i = 0; i < sections.arraySize; i++)
        {
            SerializedProperty section = sections.GetArrayElementAtIndex(i);
            TableOfContentsSectionList list = new TableOfContentsSectionList(this, serializedObject, section, i + 1);
            sectionLists.Add(list);
        }
    }

    protected void DrawTableOfContentsToolbar()
    {
        SerializedProperty sectionsProperty = serializedObject.FindProperty("Sections");

        if (GUILayout.Button("Add Section", GUILayout.Width(100)))
        {
            AddSection(sectionsProperty);
        }
    }

    protected void DrawTableOfContents()
    {
        SerializedProperty sectionsProperty = serializedObject.FindProperty("Sections");

        if (sectionLists == null || sectionLists.Count != sectionsProperty.arraySize)
        {
            InitializeSectionLists();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < sectionsProperty.arraySize; i++)
        {
            DrawSection(i);
            EditorGUILayout.Separator();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSection(int sectionIndex)
    {
        SerializedProperty sectionsProperty = serializedObject.FindProperty("Sections");
        SerializedProperty sectionProperty = sectionsProperty.GetArrayElementAtIndex(sectionIndex);

        EditorGUILayout.BeginVertical("box");

        //header line
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(string.Format("{0}.", sectionIndex + 1), GUILayout.Width(30));

        SerializedProperty titleProperty = sectionProperty.FindPropertyRelative("Title");
        titleProperty.stringValue = EditorGUILayout.TextField(titleProperty.stringValue).Trim();

        GUILayout.FlexibleSpace();

        //Move up/down
        if (sectionIndex > 0 && GUILayout.Button("Move Up", GUILayout.Width(80)))
        {
            MoveSection(sectionsProperty, sectionIndex, sectionIndex - 1);
        }

        if ((sectionIndex < (sectionsProperty.arraySize - 1)) &&
            GUILayout.Button("Move Down", GUILayout.Width(80)))
        {
            MoveSection(sectionsProperty, sectionIndex, sectionIndex + 1);
        }

        //Delete
        bool removedSection = false;
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            RemoveSection(sectionsProperty, sectionIndex);
            removedSection = true;
        }
        EditorGUILayout.EndHorizontal();

        //missions
        if (!removedSection)
        {
            EditorGUI.indentLevel++;
            {
                sectionLists[sectionIndex].List.DoLayoutList();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void AddSection(SerializedProperty sectionsProperty)
    {
        if (sectionsProperty.arraySize == 0)
        {
            sectionsProperty.InsertArrayElementAtIndex(0);
        }
        else
        {
            sectionsProperty.InsertArrayElementAtIndex(sectionsProperty.arraySize - 1);
        }

        var section = sectionsProperty.GetArrayElementAtIndex(sectionsProperty.arraySize - 1);
        section.FindPropertyRelative("Title").stringValue = "New Section";
        section.FindPropertyRelative("MissionIDs").arraySize = 0;
    }

    private void RemoveSection(SerializedProperty sectionsProperty, int index)
    {
        sectionsProperty.DeleteArrayElementAtIndex(index);
    }

    private void MoveSection(SerializedProperty sectionsProperty, int srcIndex, int destIndex)
    {
        EditorGUIUtility.keyboardControl = -1;
        sectionsProperty.MoveArrayElement(srcIndex, destIndex);
    }

    public static int CalculateTotalComponents(KMMission mission)
    {
        int numComponents = 0;

        if ((mission.GeneratorSetting != null) && (mission.GeneratorSetting.ComponentPools != null))
        {
            foreach (KMComponentPool pool in mission.GeneratorSetting.ComponentPools)
            {
                numComponents += pool.Count;
            }
        }

        return numComponents;
    }

    protected bool IsMissionInTableOfContents(KMMission mission)
    {
        bool isInToC = false;

        KMMissionTableOfContents tableOfContents = (KMMissionTableOfContents)serializedObject.targetObject;

        foreach (var section in tableOfContents.Sections)
        {
            foreach (var missionID in section.MissionIDs)
            {
                if (missionID.Equals(mission.ID))
                {
                    isInToC = true;
                    break;
                }
            }
        }

        return isInToC;
    }

    public void ClearTableOfContentsSectionSelections()
    {
        foreach (var sectionList in sectionLists)
        {
            sectionList.List.index = -1;
        }
    }
}
