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
            CreateModConfig(out modConfig);
        }
        UnityEditor.Selection.activeObject = modConfig;
    }

    public static void CreateModConfig(out ModConfig modConfig)
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

    public override void OnInspectorGUI()
    {
        //Basic Info
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var idProperty = serializedObject.FindProperty("id");
        EditorGUILayout.PropertyField(idProperty);
        idProperty.stringValue = idProperty.stringValue.Trim();

        var titleProperty = serializedObject.FindProperty("title");
        EditorGUILayout.PropertyField(titleProperty);
        titleProperty.stringValue = titleProperty.stringValue.Trim();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("version"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("outputFolder"));

        EditorGUILayout.EndVertical();

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
    }
}
