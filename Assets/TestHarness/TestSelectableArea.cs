using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestSelectableArea : MonoBehaviour
{
    public TestSelectable Selectable;
    public List<Collider> Colliders = new List<Collider>();
    public bool IsActive { get; protected set; }

    public void Awake()
    {
        if (GetComponent<Collider>() != null)
        {
            Colliders.Add(GetComponent<Collider>());
        }

        IsActive = false;
    }

    public void ActivateSelectableArea()
    {
        foreach (Collider c in Colliders)
        {
            c.enabled = true;
        }

        IsActive = true;
    }

    public void DeactivateSelectableArea()
    {
        foreach (Collider c in Colliders)
        {
            c.enabled = false;
        }

        IsActive = false;
    }
}