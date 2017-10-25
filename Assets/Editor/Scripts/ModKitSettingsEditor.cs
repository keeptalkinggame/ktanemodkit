using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModConfig))]
public class ModKitSettingsEditor : Editor
{
    [MenuItem("Keep Talking ModKit/Configure Mod", priority = 1)]
    public static void ConfigureMod()
    {
        var modConfig = ModConfig.Instance;
        if (modConfig == null)
        {
            modConfig = ScriptableObject.CreateInstance<ModConfig>();
            string properPath = Path.Combine(Path.Combine(Application.dataPath, "Editor"), "Resources");
            if (!Directory.Exists(properPath))
            {
                AssetDatabase.CreateFolder("Assets/Editor", "Resources");
            }

            string fullPath = Path.Combine(
                Path.Combine("Assets", "Editor"),
                Path.Combine("Resources", "ModConfig.asset")
            );
            AssetDatabase.CreateAsset(modConfig, fullPath);
            ModConfig.Instance = modConfig;
        }
        UnityEditor.Selection.activeObject = modConfig;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        
        GUIContent idLabel = new GUIContent("Mod ID", "Identifier for the mod. Affects assembly name and output name.");
        GUI.changed = false;
        ModConfig.ID = EditorGUILayout.TextField(idLabel, ModConfig.ID);
        SetDirtyOnGUIChange();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        GUIContent titleLabel = new GUIContent("Mod Title", "Name of the mod as it appears in game.");
        GUI.changed = false;
        ModConfig.Title = EditorGUILayout.TextField(titleLabel, ModConfig.Title);
        SetDirtyOnGUIChange();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        SetDirtyOnGUIChange();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        GUIContent versionLabel = new GUIContent("Mod Version", "Current version of the mod.");
        GUI.changed = false;
        ModConfig.Version = EditorGUILayout.TextField(versionLabel, ModConfig.Version);
        SetDirtyOnGUIChange();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        GUIContent outputFolderLabel = new GUIContent("Mod Output Folder", "Folder relative to the project where the built mod bundle will be placed.");
        GUI.changed = false;
        ModConfig.OutputFolder = EditorGUILayout.TextField(outputFolderLabel, ModConfig.OutputFolder);
        SetDirtyOnGUIChange();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("This folder will be cleaned with each build.", MessageType.Warning);

        //Preview Image
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Preview Image:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewImage"), new GUIContent());

            if (ModConfig.PreviewImage != null)
            {
                FileInfo f = new FileInfo(AssetDatabase.GetAssetPath(ModConfig.PreviewImage));
                if (f.Exists)
                {
                    EditorGUILayout.LabelField(string.Format("File Size: {0}", WorkshopEditorWindow.FormatFileSize(f.Length)));

                    if (f.Length > 1024 * 1024)
                    {
                        EditorGUILayout.HelpBox("Max allowed size is 1MB", MessageType.Error);
                    }
                }
            }
        }
        GUILayout.Label(ModConfig.PreviewImage, GUILayout.MaxWidth(128), GUILayout.MaxHeight(128));
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
        GUI.enabled = true;
    }

    private void SetDirtyOnGUIChange()
    {
        if (GUI.changed)
        {
            EditorUtility.SetDirty(ModConfig.Instance);
            GUI.changed = false;
        }
    }
}
