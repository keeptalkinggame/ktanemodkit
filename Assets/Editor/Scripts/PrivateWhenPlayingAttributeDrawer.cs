using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(PrivateWhenPlayingAttribute))]
public class PrivateWhenPlayingAttributeDrawer : PropertyDrawer
{
	// Necessary since some properties tend to collapse smaller than their content
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		if (Application.isPlaying) return 0;
		return EditorGUI.GetPropertyHeight(property, label, true);
	}

	// Hide a property field
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (Application.isPlaying) return;
		EditorGUI.PropertyField(position, property, label, true);
	}
}