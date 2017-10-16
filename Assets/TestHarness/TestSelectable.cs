using UnityEngine;
using System.Collections;
using System;

public class TestSelectable : MonoBehaviour
{
    public TestHighlightable Highlight;
    public TestSelectable[] Children;
    protected TestSelectableArea _selectableArea;
    public TestSelectableArea SelectableArea
    {
        get
        {
            if(_selectableArea == null)
            {
                if (GetComponent<KMSelectable>() != null && GetComponent<KMSelectable>().SelectableColliders.Length > 0)
                {
                    _selectableArea = new GameObject("SelectableArea").AddComponent<TestSelectableArea>();
                    _selectableArea.Selectable = this;
                    _selectableArea.transform.parent = transform;

                    foreach(Collider collider in GetComponent<KMSelectable>().SelectableColliders)
                    {
                        TestSelectableArea colSelectableArea = collider.gameObject.AddComponent<TestSelectableArea>();
                        collider.isTrigger = false;
                        collider.gameObject.layer = 11;
                        colSelectableArea.Selectable = this;
                        _selectableArea.Colliders.Add(collider);
                    }
                    
                    _selectableArea.DeactivateSelectableArea();
                }

                else if (Highlight != null)
                {
                    MeshRenderer meshRenderer = Highlight.gameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                    {
                        //Adding a BoxCollider will take on an appropriate size/position based on
                        //a MeshRenderer (rather than just a MeshFilter, as it appeared to work in 4.6)
                        //Thus, we add a MeshRenderer if needed but immediately disable it.
                        meshRenderer = Highlight.gameObject.AddComponent<MeshRenderer>();
                        meshRenderer.enabled = false;
                    }
                    
                    BoxCollider collider = Highlight.gameObject.AddComponent<BoxCollider>();
                    collider.isTrigger = true;
                    _selectableArea = Highlight.gameObject.AddComponent<TestSelectableArea>();
                    _selectableArea.Selectable = this;
                    _selectableArea.gameObject.layer = 11;
                    _selectableArea.DeactivateSelectableArea();      
                }
            }

            return _selectableArea;

        }
    }
    public TestSelectable Parent;
    
    void Start()
    {

    }

    public bool Interact()
    {
        bool shouldDrill = Children.Length > 0;

        if(GetComponent<KMSelectable>().OnInteract != null)
        {
            shouldDrill = GetComponent<KMSelectable>().OnInteract();
        }

        return shouldDrill;
    }

    public void InteractEnded()
    {
        if (GetComponent<KMSelectable>().OnInteractEnded != null)
        {
            GetComponent<KMSelectable>().OnInteractEnded();
        }
    }

    public void Select()
    {
        Highlight.On();
        if (GetComponent<KMSelectable>().OnSelect != null)
        {
            GetComponent<KMSelectable>().OnSelect();
        }
        if (GetComponent<KMSelectable>().OnHighlight != null)
        {
            GetComponent<KMSelectable>().OnHighlight();
        }
    }

    public bool Cancel()
    {
        if (GetComponent<KMSelectable>().OnCancel != null)
        {
            return GetComponent<KMSelectable>().OnCancel();
        }

        return true;
    }

    public void Deselect()
    {
        Highlight.Off();
        if (GetComponent<KMSelectable>().OnDeselect != null)
        {
            GetComponent<KMSelectable>().OnDeselect();
        }
    }

    public void OnDrillAway(TestSelectable newParent)
    {
        DeactivateChildSelectableAreas(newParent);
    }

    public void OnDrillTo()
    {
        ActivateChildSelectableAreas();
    }

    public void ActivateChildSelectableAreas()
    {
        if (this.SelectableArea != null)
        {
            this.SelectableArea.DeactivateSelectableArea();
        }
        for (int i = 0; i < Children.Length; i++)
        {
            if (Children[i] != null)
            {
                if (Children[i].SelectableArea != null)
                {
                    Children[i].SelectableArea.ActivateSelectableArea();
                }
            }
        }
    }

    public void DeactivateImmediateChildSelectableAreas()
    {
        for (int i = 0; i < Children.Length; i++)
        {
            if (Children[i] != null)
            {
                if (Children[i].SelectableArea != null)
                {
                    Children[i].SelectableArea.DeactivateSelectableArea();
                }
            }
        }
    }

    public void DeactivateChildSelectableAreas(TestSelectable newParent)
    {
        TestSelectable parent = newParent;
        while (parent != null)
        {
            if (parent == this)
                return;
            parent = parent.Parent;
        }

        parent = this;

        while (parent != newParent && parent != null)
        {
            for (int i = 0; i < parent.Children.Length; i++)
            {
                if (parent.Children[i] != null)
                {
                    if (parent.Children[i].SelectableArea != null)
                    {
                        parent.Children[i].SelectableArea.DeactivateSelectableArea();
                    }
                }
            }

            parent = parent.Parent;

            if (parent != null && parent == newParent && parent.SelectableArea != null)
            {
                parent.SelectableArea.ActivateSelectableArea();
            }
        }
    }
}
