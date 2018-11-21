using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EdgeworkConfigurator
{
    [CustomEditor(typeof(EdgeworkConfiguration))]
    public class EdgeworkConfigurationEditor : Editor
    {
        protected static string CONFIGURATIONS_FOLDER = "EdgeworkConfigurations";

        [MenuItem("Keep Talking ModKit/Create Edgework Configuration", false, 9999)]
        public static void CreateNewEdgeworkConfiguration()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TestHarness/" + CONFIGURATIONS_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/TestHarness", CONFIGURATIONS_FOLDER);
            }

            EdgeworkConfiguration config = ScriptableObject.CreateInstance<EdgeworkConfiguration>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/TestHarness/" + CONFIGURATIONS_FOLDER + "/config.asset");
            AssetDatabase.CreateAsset(config, path);
            AssetImporter.GetAtPath(path).assetBundleName = AssetBundler.BUNDLE_FILENAME;

            EditorGUIUtility.PingObject(config);
        }
        
        public override void OnInspectorGUI()
        {
            if (target != null)
            {
                serializedObject.Update();

                SerialNumberType serialNumberType = DrawSerialNumberTypePicker();
                if (serialNumberType == SerialNumberType.CUSTOM) EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomSerialNumber"));

	            EdgeworkConfiguration config = (EdgeworkConfiguration)serializedObject.targetObject;
	            if (config != null && config.Widgets.Any(x => x.Type == WidgetType.TWOFACTOR)) EditorGUILayout.PropertyField(serializedObject.FindProperty("TwoFactorResetTime"));

				EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Widgets:");

                SerializedProperty widgetListProperty = serializedObject.FindProperty("Widgets");
                EditorGUI.indentLevel++;
                for (int i = 0; i < widgetListProperty.arraySize; i++)
                {
                    DrawWidget(widgetListProperty, i);
                    EditorGUILayout.Space();
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();

                if (GUILayout.Button("Add Widget"))
                {
                    AddWidget();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected void AddWidget()
        {
            SerializedProperty widgets = serializedObject.FindProperty("Widgets");

            int index = widgets.arraySize;
            widgets.arraySize++;

            var element = widgets.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Type").enumValueIndex = 0;
            element.FindPropertyRelative("BatteryType").enumValueIndex = 1;
        }

        protected void RemoveWidget(int index)
        {
            SerializedProperty widgets = serializedObject.FindProperty("Widgets");

            widgets.DeleteArrayElementAtIndex(index);
        }

        /// <summary>
        /// Draw a single Widget editor
        /// </summary>
        /// <param name="widgetListProperty"></param>
        /// <param name="index"></param>
        protected void DrawWidget(SerializedProperty widgetListProperty, int index)
        {
            SerializedProperty widgetProperty = widgetListProperty.GetArrayElementAtIndex(index);
            EditorGUILayout.BeginHorizontal();

            //Count
            if (widgetProperty.FindPropertyRelative("Type").enumValueIndex == 2)
            {
                widgetProperty.FindPropertyRelative("Count").intValue = 1;
                EditorGUILayout.IntField(widgetProperty.FindPropertyRelative("Count").intValue, GUILayout.Width(45));
            }
            else
            {
                widgetProperty.FindPropertyRelative("Count").intValue = Math.Max(EditorGUILayout.IntField(widgetProperty.FindPropertyRelative("Count").intValue, GUILayout.Width(45)), 1);
            }
            
            //Widget type dropdown
            GUILayout.Label("Widget Type:");
            WidgetType widgetType = DrawWidgetTypePicker(index);

            //Delete button
            if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                RemoveWidget(index);
                return;
            }

            EditorGUILayout.EndHorizontal();

            //Draw widget options
            DrawWidgetOptions(widgetType, widgetProperty, index);
        }

        /// <summary>
        /// Draw the options for a Widget, depending on its type
        /// </summary>
        /// <param name="widgetType"></param>
        /// <param name="widgetProperty"></param>
        /// <param name="index"></param>
        protected void DrawWidgetOptions(WidgetType widgetType, SerializedProperty widgetProperty, int index) {
            switch (widgetType) {
                case WidgetType.BATTERY: //Batteries
                    BatteryType batteryType = DrawBatteryTypePicker(index);
                    EditorGUI.indentLevel++;

                    if (batteryType == BatteryType.CUSTOM) {
                        widgetProperty.FindPropertyRelative("BatteryCount").intValue = Math.Max(EditorGUILayout.IntField("Custom Count", widgetProperty.FindPropertyRelative("BatteryCount").intValue), 0);
                    } else if (batteryType == BatteryType.RANDOM) {
                        widgetProperty.FindPropertyRelative("MinBatteries").intValue = Math.Max(EditorGUILayout.IntField("Minimum Batteries", widgetProperty.FindPropertyRelative("MinBatteries").intValue), 0);
                        widgetProperty.FindPropertyRelative("MaxBatteries").intValue = Math.Max(EditorGUILayout.IntField("Maximum Batteries", widgetProperty.FindPropertyRelative("MaxBatteries").intValue), widgetProperty.FindPropertyRelative("MinBatteries").intValue);
                    }
                    break;
                case WidgetType.INDICATOR: //Indicators
                    IndicatorLabel indicatorLabel = DrawIndicatorLabelPicker(index);
                    EditorGUI.indentLevel++;

                    if (indicatorLabel == IndicatorLabel.CUSTOM) {
                        widgetProperty.FindPropertyRelative("CustomLabel").stringValue = EditorGUILayout.DelayedTextField("Custom Label", widgetProperty.FindPropertyRelative("CustomLabel").stringValue);
                    }
                    DrawIndicatorStatePicker(index);
                    break;
                case WidgetType.PORT_PLATE: //Port Plates
                    PortPlateType type = DrawPortPlateTypePicker(index);
                    if (type == PortPlateType.CUSTOM) {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("DVIPort"));
                        EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("PS2Port"));
                        EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("RJ45Port"));
                        EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("StereoRCAPort"));
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("ParallelPort"));
                        EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("SerialPort"));
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.EndHorizontal();
	                    EditorGUILayout.Space();
						EditorGUILayout.BeginHorizontal();

	                    EditorGUILayout.BeginVertical();
	                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("ComponentVideoPort"));
	                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("CompositeVideoPort"));
	                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("HDMIPort"));
	                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("VGAPort"));
						EditorGUILayout.EndVertical();

	                    EditorGUILayout.BeginVertical();
	                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("USBPort"));
	                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("ACPort"));
	                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("PCMCIAPort"));
						EditorGUILayout.EndVertical();

						EditorGUILayout.EndHorizontal();

					}
                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("CustomPorts"), true);
                    break;
				case WidgetType.TWOFACTOR:
					EditorGUI.indentLevel++;
					break;
                case WidgetType.RANDOM: //Random Widget
                    EditorGUI.indentLevel++;
                    break;
                case WidgetType.CUSTOM: //Custom Widget
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("CustomQueryKey"));
                    EditorGUILayout.PropertyField(widgetProperty.FindPropertyRelative("CustomData"));
                    break;
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Draws and responds to an enum picker for the widget type
        /// </summary>
        private WidgetType DrawWidgetTypePicker(int widgetIndex)
        {
            EdgeworkConfiguration config = (EdgeworkConfiguration)serializedObject.targetObject;
            THWidget widget = config.Widgets[widgetIndex];
            widget.Type = (WidgetType) EditorGUILayout.EnumPopup(widget.Type, GUILayout.MinWidth(120));
            return widget.Type;
        }

        /// <summary>
        /// Draws and responds to an enum picker for the battery type
        /// </summary>
        private BatteryType DrawBatteryTypePicker(int widgetIndex)
        {
            EdgeworkConfiguration config = (EdgeworkConfiguration)serializedObject.targetObject;
            THWidget widget = config.Widgets[widgetIndex];
            widget.BatteryType = (BatteryType) EditorGUILayout.EnumPopup("Battery Count", widget.BatteryType, GUILayout.MinWidth(200));
            return widget.BatteryType;
        }

        /// <summary>
        /// Draws and responds to an enum picker for the indicator label
        /// </summary>
        private IndicatorLabel DrawIndicatorLabelPicker(int widgetIndex)
        {
            EdgeworkConfiguration config = (EdgeworkConfiguration)serializedObject.targetObject;
            THWidget widget = config.Widgets[widgetIndex];
            widget.IndicatorLabel = (IndicatorLabel) EditorGUILayout.EnumPopup("Indicator Label", widget.IndicatorLabel, GUILayout.MinWidth(200));
            return widget.IndicatorLabel;
        }

        /// <summary>
        /// Draws and responds to an enum picker for the indicator state
        /// </summary>
        private IndicatorState DrawIndicatorStatePicker(int widgetIndex)
        {
            EdgeworkConfiguration config = (EdgeworkConfiguration)serializedObject.targetObject;
            THWidget widget = config.Widgets[widgetIndex];
            widget.IndicatorState = (IndicatorState)EditorGUILayout.EnumPopup("Indicator State", widget.IndicatorState, GUILayout.MinWidth(200));
            return widget.IndicatorState;
        }

        /// <summary>
        /// Draws and responds to an enum picker for the port plate type
        /// </summary>
        private PortPlateType DrawPortPlateTypePicker(int widgetIndex)
        {
            EdgeworkConfiguration config = (EdgeworkConfiguration)serializedObject.targetObject;
            THWidget widget = config.Widgets[widgetIndex];
            widget.PortPlateType = (PortPlateType)EditorGUILayout.EnumPopup("Port Plate Type", widget.PortPlateType, GUILayout.MinWidth(200));
            return widget.PortPlateType;
        }

        private SerialNumberType DrawSerialNumberTypePicker()
        {
            EdgeworkConfiguration config = (EdgeworkConfiguration)serializedObject.targetObject;
            config.SerialNumberType = (SerialNumberType)EditorGUILayout.EnumPopup("Serial Number", config.SerialNumberType, GUILayout.MinWidth(200));
            return config.SerialNumberType;
        }
    }
}