using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(KMBombModule))]
public class KMBombModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (target != null)
        {
            serializedObject.Update();

            var moduleTypeProperty = serializedObject.FindProperty("ModuleType");
            EditorGUILayout.PropertyField(moduleTypeProperty);
            moduleTypeProperty.stringValue = moduleTypeProperty.stringValue.Trim();

            var moduleDisplayNameProperty = serializedObject.FindProperty("ModuleDisplayName");
            EditorGUILayout.PropertyField(moduleDisplayNameProperty);
            moduleDisplayNameProperty.stringValue = moduleDisplayNameProperty.stringValue.Trim();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RequiresTimerVisibility"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}

[CustomEditor(typeof(KMNeedyModule))]
public class KMNeedyModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (target != null)
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("CountdownTime"));

            var moduleTypeProperty = serializedObject.FindProperty("ModuleType");
            EditorGUILayout.PropertyField(moduleTypeProperty);
            moduleTypeProperty.stringValue = moduleTypeProperty.stringValue.Trim();

            var moduleDisplayNameProperty = serializedObject.FindProperty("ModuleDisplayName");
            EditorGUILayout.PropertyField(moduleDisplayNameProperty);
            moduleDisplayNameProperty.stringValue = moduleDisplayNameProperty.stringValue.Trim();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("RequiresTimerVisibility"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("WarnAtFiveSeconds"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}