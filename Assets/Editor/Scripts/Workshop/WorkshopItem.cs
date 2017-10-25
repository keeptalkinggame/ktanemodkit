using System.Collections.Generic;
using UnityEngine;

public class WorkshopItem : ScriptableObject
{
    public ulong WorkshopPublishedFileID;
    public string Title;
    
    public List<string> Tags;
}
