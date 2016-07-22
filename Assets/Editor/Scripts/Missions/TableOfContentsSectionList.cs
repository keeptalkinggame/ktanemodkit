using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Assets.Editor
{
    /// <summary>
    /// Helper class for the KMTableOfContentsEditor that provides a reorderable list for each
    /// section of missions in the Table of Contents.
    /// </summary>
    public class TableOfContentsSectionList
    {
        public ReorderableList List { get; protected set; }

        protected KMMissionTableOfContentsEditor editor;
        protected SerializedObject serializedTableOfContents;
        protected SerializedProperty serializedSection;
        protected int sectionNum;

        public TableOfContentsSectionList(
            KMMissionTableOfContentsEditor editor,
            SerializedObject serializedTableOfContents,
            SerializedProperty serializedSection,
            int sectionNum)
        {
            this.editor = editor;
            this.serializedTableOfContents = serializedTableOfContents;
            this.serializedSection = serializedSection;
            this.sectionNum = sectionNum;

            List = new ReorderableList(
                serializedTableOfContents,
                serializedSection.FindPropertyRelative("MissionIDs"),
                true,
                false,
                true,
                true);

            List.headerHeight = 1;

            List.drawElementCallback = DrawSectionMissionEntry;
            List.onAddDropdownCallback = ShowAddMissionContextMenu;
            List.onSelectCallback = OnSelect;
        }

        protected void DrawSectionMissionEntry(Rect rect, int index, bool isActive, bool isFocused)
        {
            var missionIDProperty = List.serializedProperty.GetArrayElementAtIndex(index);

            KMMission mission = KMMissionTableOfContentsEditor.GetMission(missionIDProperty.stringValue);

            string sectionLabel = String.Format("{0}.{1}",
                    sectionNum,
                    index + 1);

            EditorGUI.PrefixLabel(new Rect(rect.x, rect.y, 30, EditorGUIUtility.singleLineHeight), new GUIContent(sectionLabel));
            EditorGUI.LabelField(
                new Rect(rect.x + 30, rect.y, 200, EditorGUIUtility.singleLineHeight),
                missionIDProperty.stringValue);

            if (!IsValidMission(missionIDProperty.stringValue))
            {
                EditorGUI.HelpBox(new Rect(rect.x + 30 + 200, rect.y, 200, EditorGUIUtility.singleLineHeight), "Mission not found!", MessageType.Error);
            }
            else if (!IsMissionUnique(missionIDProperty.stringValue))
            {
                EditorGUI.HelpBox(new Rect(rect.x + 30 + 200, rect.y, 100, EditorGUIUtility.singleLineHeight), "Duplicate!", MessageType.Warning);
            }
            else if (mission != null)
            {
                float x = 30 + 200;

                //Display Name
                EditorGUI.LabelField(new Rect(rect.x + x, rect.y, 300, EditorGUIUtility.singleLineHeight),
                    mission.DisplayName);
                x += 300;
            }
        }

        protected int GetRectWidth(String str)
        {
            return str.Length * 8;
        }

        //Returns true if this is the only instance of the missionID in the Table of Contents
        protected bool IsMissionUnique(String id)
        {
            bool isUnique = true;
            int instanceCount = 0;

            KMMissionTableOfContents tableOfContents = (KMMissionTableOfContents)serializedTableOfContents.targetObject;

            foreach (var section in tableOfContents.Sections)
            {
                foreach (var missionID in section.MissionIDs)
                {
                    if (missionID.Equals(id))
                    {
                        instanceCount++;

                        if (instanceCount > 1)
                        {
                            isUnique = false;
                            break;
                        }
                    }
                }
            }

            return isUnique;
        }

        protected bool IsValidMission(String id)
        {
            return KMMissionTableOfContentsEditor.GetMission(id) != null;
        }

        protected void OnSelect(ReorderableList list)
        {
            //Deselect all other section lists, because otherwise each section will have
            //an active selection
            int oldIndex = list.index;
            editor.ClearTableOfContentsSectionSelections();
            list.index = oldIndex;

            SerializedProperty missionIDProperty = list.serializedProperty.GetArrayElementAtIndex(list.index);

            KMMission mission = KMMissionTableOfContentsEditor.GetMission(missionIDProperty.stringValue);

            if (mission != null)
            {
                EditorGUIUtility.PingObject(mission);
            }
        }

        protected void ShowAddMissionContextMenu(Rect buttonRect, ReorderableList l)
        {
            var menu = new GenericMenu();

            List<String> missionIDs = KMMissionTableOfContentsEditor.GetAllMissions().Where(x => x != null).Select(x => x.ID).ToList();

            for (int i = 0; i < missionIDs.Count; i++)
            {
                menu.AddItem(new GUIContent(missionIDs[i]),
                false,
                OnAddMissionContextMenuClick,
                missionIDs[i]);
            }

            menu.ShowAsContext();
        }

        protected void OnAddMissionContextMenuClick(object target)
        {
            String missionID = (String)target;
            int index = List.serializedProperty.arraySize;
            List.serializedProperty.arraySize++;
            List.index = index;

            var element = List.serializedProperty.GetArrayElementAtIndex(index);
            element.stringValue = missionID;

            serializedTableOfContents.ApplyModifiedProperties();
        }
    }
}
