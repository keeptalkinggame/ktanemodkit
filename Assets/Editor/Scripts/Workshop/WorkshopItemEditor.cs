using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorkshopItem))]
public class WorkshopItemEditor : Editor
{
    private static string[] tags = { "Regular Module", "Needy Module", "Missions", "Setup Room", "Gameplay Room", "Audio", "Bomb Casing", "Widget", "Other" };

    public override void OnInspectorGUI()
    {
        var modConfigSO = new UnityEditor.SerializedObject(ModConfig.Instance);
        modConfigSO.Update();
        serializedObject.Update();

        WorkshopItem item = (WorkshopItem)target;

        //Basic Info
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("WorkshopPublishedFileID"), new GUIContent("Workshop File ID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Title"));
        EditorGUILayout.PropertyField(modConfigSO.FindProperty("description"));
        EditorGUILayout.EndVertical();

        //Preview Image
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Preview Image:");
            EditorGUILayout.PropertyField(modConfigSO.FindProperty("previewImage"), new GUIContent());

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

        //Tags
        if (item.Tags == null)
        {
            item.Tags = new List<string>();
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(128));
        EditorGUILayout.LabelField("Tags:");
        for(int i = 0; i < tags.Length; i++)
        {
            bool hasTag = item.Tags.Contains(tags[i]);

            bool toggled = EditorGUILayout.Toggle(tags[i], hasTag);

            if (hasTag && !toggled)
            {
                item.Tags.Remove(tags[i]);
            }
            else if (!hasTag && toggled)
            {
                item.Tags.Add(tags[i]);
            }
        }
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
        modConfigSO.ApplyModifiedProperties();
    }
}
