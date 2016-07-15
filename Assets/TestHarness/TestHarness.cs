using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestHarness : MonoBehaviour
{
    public GameObject HighlightPrefab;
    TestSelectable currentSelectable;
    TestSelectableArea currentSelectableArea;

    void Awake()
    {
        AddHighlightables();
        AddSelectables();
    }

    void Start()
    {
        currentSelectable = GetComponent<TestSelectable>();
        
        KMBombModule[] modules = FindObjectsOfType<KMBombModule>();
        currentSelectable.Children = new TestSelectable[modules.Length];
        for (int i=0; i < modules.Length; i++)
        {
            currentSelectable.Children[i] = modules[i].GetComponent<TestSelectable>();
            modules[i].GetComponent<TestSelectable>().Parent = currentSelectable;

            modules[i].OnPass = delegate () { Debug.Log("Module Passed"); return false; };
            modules[i].OnStrike = delegate () { Debug.Log("Strike"); return false; };
        }

        currentSelectable.ActivateChildSelectableAreas();
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction);
        RaycastHit hit;
        int layerMask = 1 << 11;
        bool rayCastHitSomething = Physics.Raycast(ray, out hit, 1000, layerMask);
        if(rayCastHitSomething)
        {
            TestSelectableArea hitArea = hit.collider.GetComponent<TestSelectableArea>();
            if (hitArea != null)
            {
                if (currentSelectableArea != hitArea)
                {
                    if(currentSelectableArea != null)
                    {
                        currentSelectableArea.Selectable.Deselect();
                    }
                    
                    hitArea.Selectable.Select();
                    currentSelectableArea = hitArea;
                }
            }
            else
            {
                if(currentSelectableArea != null)
                {
                    currentSelectableArea.Selectable.Deselect();
                    currentSelectableArea = null;
                }
            }
        }
        else
        {
            if (currentSelectableArea != null)
            {
                currentSelectableArea.Selectable.Deselect();
                currentSelectableArea = null;
            }
        }

        if(Input.GetMouseButtonDown(0))
        {
            if(currentSelectableArea != null && currentSelectableArea.Selectable.Interact())
            {
                currentSelectable.DeactivateChildSelectableAreas(currentSelectableArea.Selectable);
                currentSelectable = currentSelectableArea.Selectable;
                currentSelectable.ActivateChildSelectableAreas();
            }
        }

        if(Input.GetMouseButtonDown(1))
        {
            if(currentSelectable.Parent != null)
            {
                currentSelectable.DeactivateChildSelectableAreas(currentSelectable.Parent);
                currentSelectable = currentSelectable.Parent;
                currentSelectable.ActivateChildSelectableAreas();
            }
        }
    }

    void AddHighlightables()
    {
        List<KMHighlightable> highlightables = new List<KMHighlightable>(GameObject.FindObjectsOfType<KMHighlightable>());

        foreach(KMHighlightable highlightable in highlightables)
        {
            TestHighlightable highlight = highlightable.gameObject.AddComponent<TestHighlightable>();

            highlight.HighlightPrefab = HighlightPrefab;
            highlight.HighlightScale = highlightable.HighlightScale;
            highlight.OutlineAmount = highlightable.OutlineAmount;
        }
    }

    void AddSelectables()
    {
        List<KMSelectable> selectables = new List<KMSelectable>(GameObject.FindObjectsOfType<KMSelectable>());

        foreach (KMSelectable selectable in selectables)
        {
            TestSelectable testSelectable = selectable.gameObject.AddComponent<TestSelectable>();
            testSelectable.Highlight = selectable.Highlight.GetComponent<TestHighlightable>();
        }

        foreach (KMSelectable selectable in selectables)
        {
            TestSelectable testSelectable = selectable.gameObject.GetComponent<TestSelectable>();
            testSelectable.Children = new TestSelectable[selectable.Children.Length];
            for (int i = 0; i < selectable.Children.Length; i++)
            {
                testSelectable.Children[i] = selectable.Children[i].GetComponent<TestSelectable>();
            }
        }
    }
}
