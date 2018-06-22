using UnityEngine;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
    None
}

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
                if (ModSelectable != null && ModSelectable.SelectableColliders.Length > 0)
                {
                    _selectableArea = new GameObject("SelectableArea").AddComponent<TestSelectableArea>();
                    _selectableArea.Selectable = this;
                    _selectableArea.transform.parent = transform;

                    foreach(Collider collider in ModSelectable.SelectableColliders)
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
    public KMSelectable ModSelectable;
    public TestSelectable LastSelectedChild;
    public int x;
    public int y;
    int _childRowLength;
    public int ChildRowLength { get { return ModSelectable ? ModSelectable.ChildRowLength : _childRowLength; } set { _childRowLength = value; } }
    public bool AllowSelectionWrapX { get { return ModSelectable ? ModSelectable.AllowSelectionWrapX : false; } }
    public bool AllowSelectionWrapY { get { return ModSelectable? ModSelectable.AllowSelectionWrapY : false; } }
    public int DefaultSelectableIndex { get { return ModSelectable ? ModSelectable.DefaultSelectableIndex : 0; } }
    
    void Start()
    {
        ModSelectable = GetComponent<KMSelectable>();

        if (ChildRowLength == 0 || Children == null)
        {
            return;
        }
        for (int i = 0; i <= Children.Length / ChildRowLength; i++)
        {
            for (int j = 0; j < ChildRowLength; j++)
            {
                int num = i * ChildRowLength + j;
                if (num < Children.Length && Children[num] != null)
                {
                    Children[num].y = i;
                    Children[num].x = j;
                }
            }
        }
    }

    public bool Interact()
    {
        bool shouldDrill = Children.Length > 0;

        if(ModSelectable.OnInteract != null)
        {
            shouldDrill = ModSelectable.OnInteract();
        }

        return shouldDrill;
    }

    public void InteractEnded()
    {
        if (ModSelectable.OnInteractEnded != null)
        {
            ModSelectable.OnInteractEnded();
        }
    }

    public void Select()
    {
        Highlight.On();
        if (ModSelectable.OnSelect != null)
        {
            ModSelectable.OnSelect();
        }
        if (ModSelectable.OnHighlight != null)
        {
            ModSelectable.OnHighlight();
        }
    }

    public bool Cancel()
    {
        if (ModSelectable.OnCancel != null)
        {
            return ModSelectable.OnCancel();
        }

        return true;
    }

    public void Deselect()
    {
        Highlight.Off();
        if (ModSelectable.OnDeselect != null)
        {
            ModSelectable.OnDeselect();
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

    public TestSelectable GetChild(int cX, int cY)
    {
        int num = cY * ChildRowLength + cX;
        if (num < Children.Length && num >= 0 && cX < ChildRowLength && cX >= 0)
        {
            return Children[num];
        }
        return null;
    }

    public TestSelectable GetNearestSelectable(Direction direction)
    {
        if (Parent == null || ModSelectable.IsPassThrough)
        {
            return null;
        }

        int num = Mathf.Max(Parent.ChildRowLength, Parent.Children.Length / Parent.ChildRowLength);
        for (int i = 0; i < num; i++)
        {
            for (int j = 1; j < num; j++)
            {
                TestSelectable childInDirection = GetChildInDirection(direction, i, j);
                if (childInDirection != null)
                {
                    return childInDirection;
                }
            }
        }
        if ((Parent != null && (direction == Direction.Down || direction == Direction.Up) && Parent.AllowSelectionWrapY) || ((direction == Direction.Left || direction == Direction.Right) && Parent.AllowSelectionWrapX))
        {
            for (int k = 0; k < num; k++)
            {
                for (int l = -num; l < 0; l++)
                {
                    TestSelectable childInDirection2 = GetChildInDirection(direction, k, l);
                    if (childInDirection2 != null)
                    {
                        return childInDirection2;
                    }
                }
            }
        }
        return null;
    }

    public TestSelectable GetChildInDirection(Direction direction, int i, int j)
    {
        TestSelectable result = null;
        TestSelectable result2 = null;
        switch (direction)
        {
            case Direction.Up:
                result = Parent.GetChild(x - i, y - j);
                result2 = Parent.GetChild(x + i, y - j);
                break;
            case Direction.Down:
                result = Parent.GetChild(x - i, y + j);
                result2 = Parent.GetChild(x + i, y + j);
                break;
            case Direction.Left:
                result = Parent.GetChild(x - j, y - i);
                result2 = Parent.GetChild(x - j, y + i);
                break;
            case Direction.Right:
                result = Parent.GetChild(x + j, y - i);
                result2 = Parent.GetChild(x + j, y + i);
                break;
        }
        if (result != null)
        {
            return result;
        }
        if (result2 != null)
        {
            return result2;
        }
        return null;
    }

    public TestSelectable GetCurrentChild()
    {
        return LastSelectedChild ?? GetDefaultChild();
    }

    public TestSelectable GetDefaultChild()
    {
        if (Children.Length > 0)
        {
            if (ModSelectable != null && DefaultSelectableIndex >= 0 && DefaultSelectableIndex < Children.Length && Children[DefaultSelectableIndex] != null)
            {
                return Children[DefaultSelectableIndex];
            }
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i] != null)
                {
                    return Children[i];
                }
            }
        }
        return null;
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
