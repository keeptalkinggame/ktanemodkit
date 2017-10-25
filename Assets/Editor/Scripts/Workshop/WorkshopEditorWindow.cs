using UnityEditor;
using Steamworks;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class WorkshopEditorWindow : EditorWindow
{
    private Vector2 scrollPosition;

    protected static readonly AppId_t KTANE_APP_ID = new AppId_t(341800);
    protected static readonly AppId_t EDITOR_APP_ID = new AppId_t(341800); //For now, the same AppID

    protected bool isInitialized;
    protected string userName;

    protected WorkshopItem currentWorkshopItem;
    protected WorkshopItemEditor workshopItemEditor;
    protected string changeNotes;

    protected CallResult<CreateItemResult_t> onCreateItemCallResultHandler;
    protected CallResult<SubmitItemUpdateResult_t> onItemUpdateCallResultHandler;

    protected UGCUpdateHandle_t ugcUpdateHandle = UGCUpdateHandle_t.Invalid;
    protected string ugcUpdateStatus;
    protected string lastUpdateStatus;
    protected ulong bytesProcessed;
    protected ulong bytesTotal;

    private SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;
    private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
    {
        Debug.LogWarning(pchDebugText);
    }

    [MenuItem("Keep Talking ModKit/Steam Workshop Tool _#F6", priority = 20)]
    protected static void ShowWindow()
    {
        WorkshopEditorWindow window = EditorWindow.GetWindow<WorkshopEditorWindow>("Workshop");
        window.Show();
    }

    protected void OnGUI()
    {
        if (!SteamAPI.IsSteamRunning())
        {
            EditorGUILayout.HelpBox("Steam is not running. Please start Steam to continue.", MessageType.Error);
        }
        else if (ModConfig.Instance == null || string.IsNullOrEmpty(ModConfig.ID))
        {
            EditorGUILayout.HelpBox("You must configure your ModConfig using \"Keep Talking Mod Kit -> Configure Mod\".", MessageType.Error);
        }
        else if (!isInitialized)
        {
            EditorGUILayout.HelpBox("You must log in to Steam to continue.", MessageType.Error);
        }
        else
        {
            if (currentWorkshopItem == null)
            {
                string workshopItemAssetPath = "Assets/Editor/Resources/WorkshopItem.asset";

                currentWorkshopItem = AssetDatabase.LoadAssetAtPath<WorkshopItem>(workshopItemAssetPath);

                if (currentWorkshopItem == null)
                {
                    currentWorkshopItem = ScriptableObject.CreateInstance<WorkshopItem>();

                    if (ModConfig.Instance != null)
                    {
                        currentWorkshopItem.Title = ModConfig.Title;
                    }

                    AssetDatabase.CreateAsset(currentWorkshopItem, workshopItemAssetPath);
                }

                if (workshopItemEditor != null && workshopItemEditor.target != currentWorkshopItem)
                {
                    DestroyImmediate(workshopItemEditor);
                    workshopItemEditor = null;
                }

                if (workshopItemEditor == null)
                {
                    workshopItemEditor = (WorkshopItemEditor) Editor.CreateEditor(currentWorkshopItem, typeof(WorkshopItemEditor));
                }
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            workshopItemEditor.OnInspectorGUI();

            //Publishing Tools
            EditorGUILayout.Separator();
            Color oldBGColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.5f, 0.7f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = oldBGColor;

            EditorGUILayout.LabelField("Publishing Tools", EditorStyles.largeLabel);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("User:", userName);
            EditorGUILayout.LabelField("Content Folder:", GetContentPath());

            DirectoryInfo dir = new DirectoryInfo(GetContentPath());

            if (dir.Exists)
            {
                FileInfo[] files = dir.GetFiles("*.*", SearchOption.AllDirectories);
                long totalSize = 0;
                foreach (var file in files)
                {
                    totalSize += file.Length;
                }

                EditorGUILayout.LabelField("File Count:", files.Length.ToString());
                EditorGUILayout.LabelField("Total Size:", FormatFileSize(totalSize));
            }
            else
            {
                EditorGUILayout.HelpBox("Content folder not found", MessageType.Error);
            }

            string[] tags = GetTags();
            if (tags == null)
            {
                EditorGUILayout.LabelField("Tags [0]: (none set)");
            }
            else
            {
                EditorGUILayout.LabelField(string.Format("Tags [{0}]: {1}", tags == null ? "0" : tags.Length.ToString(), string.Join(", ", tags)));
            }

            //Change Notes
            EditorGUILayout.PrefixLabel("Change Notes:");
            changeNotes = EditorGUILayout.TextArea(changeNotes, GUILayout.MinHeight(64));

            if (string.IsNullOrEmpty(changeNotes))
            {
                EditorGUILayout.HelpBox("Change notes must be entered before publishing to Workshop", MessageType.Warning);
            }


            //Publishing changes
            if (currentWorkshopItem.WorkshopPublishedFileID == 0)
            {
                //Create and Publish
                GUI.enabled = (onCreateItemCallResultHandler != null && !onCreateItemCallResultHandler.IsActive() && !string.IsNullOrEmpty(changeNotes));
                if (GUILayout.Button("Create New Workshop Item and Publish to Steam"))
                {
                    Debug.Log("CreateItem");
                    var createItemCall = SteamUGC.CreateItem(KTANE_APP_ID, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                    onCreateItemCallResultHandler.Set(createItemCall);
                }
                GUI.enabled = true;
            }
            else
            {
                //Publish to Existing Item
                GUI.enabled = (onItemUpdateCallResultHandler != null && !onItemUpdateCallResultHandler.IsActive() && !string.IsNullOrEmpty(changeNotes));
                if (GUILayout.Button("Publish Changes to Steam"))
                {
                    PublishWorkshopChanges();
                }

                if (!string.IsNullOrEmpty(ugcUpdateStatus))
                {
                    EditorGUILayout.LabelField(ugcUpdateStatus);
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }

    protected string GetContentPath()
    {
        return Path.GetFullPath(ModConfig.OutputFolder + "/" + ModConfig.ID);
    }

    protected string[] GetTags()
    {
        if (currentWorkshopItem == null || currentWorkshopItem.Tags == null)
        {
            return null;
        }
        else
        {
            var nonEmptyTags = currentWorkshopItem.Tags.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t));

            return nonEmptyTags.ToArray();
        }
    }

    protected void PublishWorkshopChanges()
    {
        Debug.LogFormat("SubmitItemUpdate for File ID {0}", currentWorkshopItem.WorkshopPublishedFileID);
        ugcUpdateHandle = SteamUGC.StartItemUpdate(KTANE_APP_ID, new PublishedFileId_t(currentWorkshopItem.WorkshopPublishedFileID));

        SteamUGC.SetItemTitle(ugcUpdateHandle, currentWorkshopItem.Title);
        SteamUGC.SetItemDescription(ugcUpdateHandle, ModConfig.Description);

        string[] tags = GetTags();
        if (tags != null && tags.Length > 0)
        {
            SteamUGC.SetItemTags(ugcUpdateHandle, GetTags());
        }

        if (ModConfig.PreviewImage != null)
        {
            string previewImagePath = AssetDatabase.GetAssetPath(ModConfig.PreviewImage);
            previewImagePath = Path.GetFullPath(previewImagePath);
            Debug.LogFormat("Setting preview image path to: {0}", previewImagePath);
            SteamUGC.SetItemPreview(ugcUpdateHandle, previewImagePath);
        }

        //Currently just uploads whatever is in the mod's build directory
        string folder = GetContentPath();
        Debug.LogFormat("Uploading contents of {0}", folder);
        SteamUGC.SetItemContent(ugcUpdateHandle, folder);

        var updateUGCCall = SteamUGC.SubmitItemUpdate(ugcUpdateHandle, changeNotes);
        onItemUpdateCallResultHandler.Set(updateUGCCall);
    }

    public void OnEnable()
    {
        Initialize();
    }

    public void OnDisable()
    {
        if (isInitialized)
        {
            SteamAPI.Shutdown();
        }
    }

    protected void Initialize()
    {
        if (!isInitialized)
        {
            isInitialized = SteamAPI.Init();

            if (isInitialized)
            {
                userName = SteamFriends.GetPersonaName();

                onCreateItemCallResultHandler = CallResult<CreateItemResult_t>.Create(OnCreateItem);
                onItemUpdateCallResultHandler = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdate);

                if (m_SteamAPIWarningMessageHook == null)
                {
                    m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
                    SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
                }
            }
        }
    }

    [ExecuteInEditMode()]
    protected void Update()
    {
        if (isInitialized)
        {
            SteamAPI.RunCallbacks();

            //Update the status of an in-progress UGCUpdate
            if (ugcUpdateHandle != UGCUpdateHandle_t.Invalid)
            {
                ulong tempBytesProcessed;
                ulong tempBytesTotal;
                var itemStatus = SteamUGC.GetItemUpdateProgress(ugcUpdateHandle, out tempBytesProcessed, out tempBytesTotal);

                if (tempBytesProcessed != 0)
                {
                    bytesProcessed = tempBytesProcessed;
                }
                if (tempBytesTotal != 0)
                {
                    bytesTotal = tempBytesTotal;
                }

                if (tempBytesTotal != 0)
                {
                    ugcUpdateStatus = string.Format("Upload Status: {0} ({1} / {2} bytes)",
                        itemStatus,
                        bytesProcessed,
                        bytesTotal);
                }
                else
                {
                    ugcUpdateStatus = string.Format("Upload Status: {0}", itemStatus);
                }

                //Log to console, sparingly
                if (ugcUpdateStatus != lastUpdateStatus)
                {
                    Debug.Log(ugcUpdateStatus);
                    lastUpdateStatus = ugcUpdateStatus;
                }

                Repaint();
            }
        }
    }

    protected void OnCreateItem(CreateItemResult_t result, bool failed)
    {
        if (result.m_eResult == EResult.k_EResultOK)
        {
            Debug.LogFormat("OnCreateItem complete: {0}", result.m_eResult);
        }
        else
        {
            Debug.LogErrorFormat("OnCreateItem complete: {0}", result.m_eResult);
        }

        if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            Debug.LogError("You must accept the Workshop Legal Agreement before continuing.");
        }

        if (result.m_eResult == EResult.k_EResultOK)
        {
            currentWorkshopItem.WorkshopPublishedFileID = result.m_nPublishedFileId.m_PublishedFileId;
            PublishWorkshopChanges();
        }
    }

    protected void OnSubmitItemUpdate(SubmitItemUpdateResult_t result, bool failed)
    {
        if (result.m_eResult == EResult.k_EResultOK)
        {
            Debug.LogFormat("OnSubmitItemUpdate complete: {0}", result.m_eResult);
        }
        else
        {
            Debug.LogErrorFormat("OnSubmitItemUpdate complete: {0}", result.m_eResult);
        }

        ugcUpdateHandle = UGCUpdateHandle_t.Invalid;
        ugcUpdateStatus = string.Format("Upload Status: {0} ({1})", result.m_eResult, System.DateTime.Now.ToShortTimeString());
        Repaint();
    }

    public static string FormatFileSize(long fileSize)
    {
        if (fileSize <= 0)
        {
            return "0";
        }
        else
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = (int)Mathf.Log(fileSize, 1024);
            order = Mathf.Clamp(order, 0, sizes.Length);
            float value = fileSize / Mathf.Pow(1024, order);

            return string.Format("{0:0.##} {1}", value, sizes[order]);
        }
    }
}
