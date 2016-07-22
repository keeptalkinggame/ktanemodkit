using System.Collections.Generic;
using UnityEngine;

public class WorkshopItem : ScriptableObject
{
    public ulong WorkshopPublishedFileID;
    public string Title;

    [TextArea(5, 10)]
    public string Description;

    public Texture2D PreviewImage;
    public List<string> Tags;
}
