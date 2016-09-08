using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TestHarness : MonoBehaviour
{
    public GameObject HighlightPrefab;
    TestSelectable currentSelectable;
    TestSelectableArea currentSelectableArea;

    AudioSource audioSource;
    List<AudioClip> audioClips;

    void Awake()
    {
        AddHighlightables();
        AddSelectables();
    }

    void Start()
    {
        currentSelectable = GetComponent<TestSelectable>();

        KMBombModule[] modules = FindObjectsOfType<KMBombModule>();
        KMNeedyModule[] needyModules = FindObjectsOfType<KMNeedyModule>();
        currentSelectable.Children = new TestSelectable[modules.Length + needyModules.Length];
        for (int i = 0; i < modules.Length; i++)
        {
            currentSelectable.Children[i] = modules[i].GetComponent<TestSelectable>();
            modules[i].GetComponent<TestSelectable>().Parent = currentSelectable;

            modules[i].OnPass = delegate () { Debug.Log("Module Passed"); return false; };
            modules[i].OnStrike = delegate () { Debug.Log("Strike"); return false; };
        }

        for (int i = 0; i < needyModules.Length; i++)
        {
            currentSelectable.Children[modules.Length + i] = needyModules[i].GetComponent<TestSelectable>();
            needyModules[i].GetComponent<TestSelectable>().Parent = currentSelectable;

            needyModules[i].OnPass = delegate ()
            {
                Debug.Log("Module Passed");
                return false;
            };
            needyModules[i].OnStrike = delegate ()
            {
                Debug.Log("Strike");
                return false;
            };
        }

        currentSelectable.ActivateChildSelectableAreas();


        //Load all the audio clips in the asset database
        audioClips = new List<AudioClip>();
        string[] audioClipAssetGUIDs = AssetDatabase.FindAssets("t:AudioClip");

        foreach (var guid in audioClipAssetGUIDs)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));

            if (clip != null)
            {
                audioClips.Add(clip);
            }
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        KMAudio[] kmAudios = FindObjectsOfType<KMAudio>();
        foreach (KMAudio kmAudio in kmAudios)
        {
            kmAudio.HandlePlaySoundAtTransform += PlaySoundHandler;
        }
    }

    protected void PlaySoundHandler(string clipName, Transform t)
    {
        if (audioClips.Count > 0)
        {
            AudioClip clip = audioClips.Where(a => a.name == clipName).First();

            if (clip != null)
            {
                audioSource.transform.position = t.position;
                audioSource.PlayOneShot(clip);
            }
        }
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction);
        RaycastHit hit;
        int layerMask = 1 << 11;
        bool rayCastHitSomething = Physics.Raycast(ray, out hit, 1000, layerMask);
        if (rayCastHitSomething)
        {
            TestSelectableArea hitArea = hit.collider.GetComponent<TestSelectableArea>();
            if (hitArea != null)
            {
                if (currentSelectableArea != hitArea)
                {
                    if (currentSelectableArea != null)
                    {
                        currentSelectableArea.Selectable.Deselect();
                    }

                    hitArea.Selectable.Select();
                    currentSelectableArea = hitArea;
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
        }
        else
        {
            if (currentSelectableArea != null)
            {
                currentSelectableArea.Selectable.Deselect();
                currentSelectableArea = null;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (currentSelectableArea != null && currentSelectableArea.Selectable.Interact())
            {
                currentSelectable.DeactivateChildSelectableAreas(currentSelectableArea.Selectable);
                currentSelectable = currentSelectableArea.Selectable;
                currentSelectable.ActivateChildSelectableAreas();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentSelectableArea != null)
            {
                currentSelectableArea.Selectable.InteractEnded();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (currentSelectable.Parent != null && currentSelectable.Cancel())
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

        foreach (KMHighlightable highlightable in highlightables)
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
                if (selectable.Children[i] != null)
                {
                    testSelectable.Children[i] = selectable.Children[i].GetComponent<TestSelectable>();
                }
            }
        }
    }

    void OnGUI()
    {
        if (GUILayout.Button("Activate Module"))
        {
            foreach (KMBombModule module in GameObject.FindObjectsOfType<KMBombModule>())
            {
                if (module.OnActivate != null)
                {
                    module.OnActivate();
                }
            }
            foreach (KMNeedyModule module in GameObject.FindObjectsOfType<KMNeedyModule>())
            {
            	if (module.OnActivate != null)
            	{
            		module.OnActivate();
            	}
            }
        }

        if (GUILayout.Button("Activate Needy Modules"))
        {
            foreach (KMNeedyModule needyModule in GameObject.FindObjectsOfType<KMNeedyModule>())
            {
                if (needyModule.OnNeedyActivation != null)
                {
                    needyModule.OnNeedyActivation();
                }
            }
        }

        if (GUILayout.Button("Deactivate Needy Modules"))
        {
            foreach (KMNeedyModule needyModule in GameObject.FindObjectsOfType<KMNeedyModule>())
            {
                if (needyModule.OnNeedyDeactivation != null)
                {
                    needyModule.OnNeedyDeactivation();
                }
            }
        }

        if (GUILayout.Button("Match game lighting"))
        {
            MatchGameLighting();
        }
    }

    //Sets up lighting to be the same for light a module at 0,0 as in the unmodded gameplay room on the picked up bomb
    protected void MatchGameLighting()
    {
        QualitySettings.pixelLightCount = 0;

        //Set ambient light
        RenderSettings.ambientLight = new Color(151f / 255f, 150f / 255f, 144f / 255f);
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        //Disable all other lights
        foreach (Light l in FindObjectsOfType<Light>())
        {
            l.enabled = false;
        }

        GameObject pointLight = new GameObject("Lamp");
        Light light = pointLight.AddComponent<Light>();
        
        light.type = LightType.Point;
        light.range = 4.245148f;
        light.transform.position = new Vector3(-1.089771f, 0.9635483f, 0.5165237f);
        light.color = new Color(255f / 255f, 245f / 255f, 227f / 255f);
        light.intensity = 2.7f;
    }
}
