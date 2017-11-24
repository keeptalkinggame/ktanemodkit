using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using Newtonsoft.Json;

public class FakeBombInfo : MonoBehaviour
{
    public abstract class Widget : Object
    {
        public abstract string GetResult(string key, string data);
    }

    public class PortWidget : Widget
    {
        List<string> ports;

        public PortWidget()
        {
            ports = new List<string>();
            string portList = "";

            if (Random.value > 0.5)
            {
                if (Random.value > 0.5)
                {
                    ports.Add("Parallel");
                    portList += "Parallel";
                }
                if (Random.value > 0.5)
                {
                    ports.Add("Serial");
                    if (portList.Length > 0) portList += ", ";
                    portList += "Serial";
                }
            }
            else
            {
                if (Random.value > 0.5)
                {
                    ports.Add("DVI");
                    portList += "DVI";
                }
                if (Random.value > 0.5)
                {
                    ports.Add("PS2");
                    if (portList.Length > 0) portList += ", ";
                    portList += "PS2";
                }
                if (Random.value > 0.5)
                {
                    ports.Add("RJ45");
                    if (portList.Length > 0) portList += ", ";
                    portList += "RJ45";
                }
                if (Random.value > 0.5)
                {
                    ports.Add("StereoRCA");
                    if (portList.Length > 0) portList += ", ";
                    portList += "StereoRCA";
                }
            }

            if (portList.Length == 0) portList = "Empty plate";
            Debug.Log("Added port widget: " + portList);
        }

        public override string GetResult(string key, string data)
        {
            if (key == KMBombInfo.QUERYKEY_GET_PORTS)
            {
                return JsonConvert.SerializeObject((object)new Dictionary<string, List<string>>()
                {
                    {
                        "presentPorts", ports
                    }
                });
            }
            return null;
        }
    }

    public class IndicatorWidget : Widget
    {
        static List<string> possibleValues = new List<string>(){
            "SND","CLR","CAR",
            "IND","FRQ","SIG",
            "NSA","MSA","TRN",
            "BOB","FRK"
        };

        private string val;
        private bool on;

        public IndicatorWidget()
        {
            int pos = Random.Range(0, possibleValues.Count);
            val = possibleValues[pos];
            possibleValues.RemoveAt(pos);
            on = Random.value > 0.4f;

            Debug.Log("Added indicator widget: " + val + " is " + (on ? "ON" : "OFF"));
        }

        public override string GetResult(string key, string data)
        {
            if (key == KMBombInfo.QUERYKEY_GET_INDICATOR)
            {
                return JsonConvert.SerializeObject((object)new Dictionary<string, string>()
                {
                    {
                        "label", val
                    },
                    {
                        "on", on?bool.TrueString:bool.FalseString
                    }
                });
            }
            else return null;
        }
    }

    public class BatteryWidget : Widget
    {
        private int batt;

        public BatteryWidget()
        {
            batt = Random.Range(1, 3);

            Debug.Log("Added battery widget: " + batt);
        }

        public override string GetResult(string key, string data)
        {
            if (key == KMBombInfo.QUERYKEY_GET_BATTERIES)
            {
                return JsonConvert.SerializeObject((object)new Dictionary<string, int>()
                {
                    {
                        "numbatteries", batt
                    }
                });
            }
            else return null;
        }
    }
    public Widget[] widgets;

    void Awake()
    {
        const int numWidgets = 5;
        widgets = new Widget[numWidgets];
        for (int a = 0; a < numWidgets; a++)
        {
            int r = Random.Range(0, 3);
            if (r == 0) widgets[a] = new PortWidget();
            else if (r == 1) widgets[a] = new IndicatorWidget();
            else widgets[a] = new BatteryWidget();
        }

        char[] possibleCharArray = new char[35]
        {
            'A','B','C','D','E',
            'F','G','H','I','J',
            'K','L','M','N','E',
            'P','Q','R','S','T',
            'U','V','W','X','Z',
            '0','1','2','3','4',
            '5','6','7','8','9'
        };
        string str1 = string.Empty;
        for (int index = 0; index < 2; ++index) str1 = str1 + possibleCharArray[Random.Range(0, possibleCharArray.Length)];
        string str2 = str1 + (object) Random.Range(0, 10);
        for (int index = 3; index < 5; ++index) str2 = str2 + possibleCharArray[Random.Range(0, possibleCharArray.Length - 10)];
        serial = str2 + Random.Range(0, 10);

        Debug.Log("Serial: " + serial);
    }

    float startupTime = .5f;

    public delegate void LightsOn();
    public LightsOn ActivateLights;

    private Widget widgetHandler;

    void FixedUpdate()
    {
        if (solved) return;
        if (startupTime > 0)
        {
            startupTime -= Time.fixedDeltaTime;
            if (startupTime < 0)
            {
                ActivateLights();
                foreach (KeyValuePair<KMBombModule, bool> m in modules)
                {
                    if (m.Key.OnActivate != null) m.Key.OnActivate();
                }
                foreach (KMNeedyModule m in needyModules)
                {
                    if (m.OnActivate != null) m.OnActivate();
                }
            }
        }
        else
        {
            timeLeft -= Time.fixedDeltaTime;
            if (timeLeft < 0) timeLeft = 0;
        }
    }

    public const int numStrikes = 3;

    public bool solved;
    public float timeLeft = 600f;
    public int strikes = 0;
    public string serial;

    public float GetTime()
    {
        return timeLeft;
    }

    public string GetFormattedTime()
    {
        string time = "";
        if (timeLeft < 60)
        {
            if (timeLeft < 10) time += "0";
            time += (int)timeLeft;
            time += ".";
            int s = (int)(timeLeft * 100);
            if (s < 10) time += "0";
            time += s;
        }
        else
        {
            if (timeLeft < 600) time += "0";
            time += (int)timeLeft / 60;
            time += ":";
            int s = (int)timeLeft % 60;
            if (s < 10) time += "0";
            time += s;
        }
        return time;
    }

    public int GetStrikes()
    {
        return strikes;
    }

    public List<KeyValuePair<KMBombModule, bool>> modules = new List<KeyValuePair<KMBombModule, bool>>();
    public List<KMNeedyModule> needyModules = new List<KMNeedyModule>();

    public List<string> GetModuleNames()
    {
        List<string> moduleList = new List<string>();
        foreach (KeyValuePair<KMBombModule, bool> m in modules)
        {
            moduleList.Add(m.Key.ModuleDisplayName);
        }
        foreach (KMNeedyModule m in needyModules)
        {
            moduleList.Add(m.ModuleDisplayName);
        }
        return moduleList;
    }

    public List<string> GetSolvableModuleNames()
    {
        List<string> moduleList = new List<string>();
        foreach (KeyValuePair<KMBombModule, bool> m in modules)
        {
            moduleList.Add(m.Key.ModuleDisplayName);
        }
        return moduleList;
    }

    public List<string> GetSolvedModuleNames()
    {
        List<string> moduleList = new List<string>();
        foreach (KeyValuePair<KMBombModule, bool> m in modules)
        {
            if(m.Value) moduleList.Add(m.Key.ModuleDisplayName);
        }
        return moduleList;
    }

    public List<string> GetWidgetQueryResponses(string queryKey, string queryInfo)
    {
        List<string> responses = new List<string>();
        if (queryKey == KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER)
        {
            responses.Add(JsonConvert.SerializeObject((object)new Dictionary<string, string>()
            {
                {
                    "serial", serial
                }
            }));
        }
        foreach (Widget w in widgets)
        {
            string r = w.GetResult(queryKey, queryInfo);
            if (r != null) responses.Add(r);
        }
        if (queryKey == "Unity")
            responses.Add(JsonConvert.SerializeObject(new Dictionary<string, bool>() { { "Unity", true } }));
        return responses;
    }

    public bool IsBombPresent()
    {
        return true;
    }

    public void HandleStrike()
    {
        strikes++;
        Debug.Log(strikes + "/" + numStrikes);
        if (strikes == numStrikes)
        {
            if (Detonate != null) Detonate();
            Debug.Log("KABOOM!");
        }
    }

    public delegate void OnDetonate();
    public OnDetonate Detonate;

    public void HandleStrike(string reason)
    {
        Debug.Log("Strike: " + reason);
        HandleStrike();
    }

    public delegate void OnSolved();
    public OnSolved HandleSolved;

    public void Solved()
    {
        solved = true;
        if (HandleSolved != null) HandleSolved();
        Debug.Log("Bomb defused!");
    }

    public delegate void LightState(bool state);
    public LightState OnLights;
    public void OnLightsOn()
    {
        if (OnLights != null) OnLights(true);
    }

    public void OnLightsOff()
    {
        if (OnLights != null) OnLights(false);
    }
}

public class TestHarness : MonoBehaviour
{
    private FakeBombInfo fakeInfo;

    public GameObject HighlightPrefab;
    TestSelectable currentSelectable;
    TestSelectableArea currentSelectableArea;

    AudioSource audioSource;
    public List<AudioClip> AudioClips;

    void Awake()
    {
        PrepareLights();

        fakeInfo = gameObject.AddComponent<FakeBombInfo>();
        fakeInfo.ActivateLights += delegate()
        {
            TurnLightsOn();
            fakeInfo.OnLightsOn();
        };
        TurnLightsOff();

        ReplaceBombInfo();
        AddHighlightables();
        AddSelectables();
    }

    void ReplaceBombInfo()
    {
        MonoBehaviour[] scripts = MonoBehaviour.FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour s in scripts)
        {
            IEnumerable<FieldInfo> fields = s.GetType().GetFields();
            foreach (FieldInfo f in fields)
            {
                if (f.FieldType.Equals(typeof(KMBombInfo)))
                {
                    KMBombInfo component = (KMBombInfo)f.GetValue(s);
                    component.TimeHandler += new KMBombInfo.GetTimeHandler(fakeInfo.GetTime);
                    component.FormattedTimeHandler += new KMBombInfo.GetFormattedTimeHandler(fakeInfo.GetFormattedTime);
                    component.StrikesHandler += new KMBombInfo.GetStrikesHandler(fakeInfo.GetStrikes);
                    component.ModuleNamesHandler += new KMBombInfo.GetModuleNamesHandler(fakeInfo.GetModuleNames);
                    component.SolvableModuleNamesHandler += new KMBombInfo.GetSolvableModuleNamesHandler(fakeInfo.GetSolvableModuleNames);
                    component.SolvedModuleNamesHandler += new KMBombInfo.GetSolvedModuleNamesHandler(fakeInfo.GetSolvedModuleNames);
                    component.WidgetQueryResponsesHandler += new KMBombInfo.GetWidgetQueryResponsesHandler(fakeInfo.GetWidgetQueryResponses);
                    component.IsBombPresentHandler += new KMBombInfo.KMIsBombPresent(fakeInfo.IsBombPresent);
                    continue;
                }
                if (f.FieldType.Equals(typeof(KMGameInfo)))
                {
                    KMGameInfo component = (KMGameInfo)f.GetValue(s);
                    component.OnLightsChange += new KMGameInfo.KMLightsChangeDelegate(fakeInfo.OnLights);
                    //component.OnAlarmClockChange += new KMGameInfo.KMAlarmClockChangeDelegate(fakeInfo.OnAlarm);
                    continue;
                }
                if (f.FieldType.Equals(typeof(KMGameCommands)))
                {
                    KMGameCommands component = (KMGameCommands)f.GetValue(s);
                    component.OnCauseStrike += new KMGameCommands.KMCauseStrikeDelegate(fakeInfo.HandleStrike);
                    continue;
                }
            }
        }
    }

    void Start()
    {
        MonoBehaviour[] scripts = MonoBehaviour.FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour s in scripts)
        {
            IEnumerable<FieldInfo> fields = s.GetType().GetFields();
            foreach (FieldInfo f in fields)
            {
                if (f.FieldType.Equals(typeof(KMBombInfo)))
                {
                    KMBombInfo component = (KMBombInfo)f.GetValue(s);
                    if (component.OnBombExploded != null) fakeInfo.Detonate += new FakeBombInfo.OnDetonate(component.OnBombExploded);
                    if (component.OnBombSolved != null) fakeInfo.HandleSolved += new FakeBombInfo.OnSolved(component.OnBombSolved);
                    continue;
                }
            }
        }

        currentSelectable = GetComponent<TestSelectable>();

        KMBombModule[] modules = FindObjectsOfType<KMBombModule>();
        KMNeedyModule[] needyModules = FindObjectsOfType<KMNeedyModule>();
        fakeInfo.needyModules = needyModules.ToList();
        currentSelectable.Children = new TestSelectable[modules.Length + needyModules.Length];
        for (int i = 0; i < modules.Length; i++)
        {
            KMBombModule mod = modules[i];

            currentSelectable.Children[i] = modules[i].GetComponent<TestSelectable>();
            modules[i].GetComponent<TestSelectable>().Parent = currentSelectable;

            fakeInfo.modules.Add(new KeyValuePair<KMBombModule, bool>(modules[i], false));
            modules[i].OnPass = delegate () {
                Debug.Log("Module Passed");
                fakeInfo.modules.Remove(fakeInfo.modules.First(t => t.Key.Equals(mod)));
                fakeInfo.modules.Add(new KeyValuePair<KMBombModule, bool>(mod, true));
                bool allSolved = true;
                foreach (KeyValuePair<KMBombModule, bool> m in fakeInfo.modules)
                {
                    if (!m.Value)
                    {
                        allSolved = false;
                        break;
                    }
                }
                if (allSolved) fakeInfo.Solved();
                return false;
            };
            modules[i].OnStrike = delegate () {
                Debug.Log("Strike");
                fakeInfo.HandleStrike();
                return false;
            };
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
                fakeInfo.HandleStrike();
                return false;
            };
        }

        currentSelectable.ActivateChildSelectableAreas();

        audioSource = gameObject.AddComponent<AudioSource>();
        KMAudio[] kmAudios = FindObjectsOfType<KMAudio>();
        foreach (KMAudio kmAudio in kmAudios)
        {
            kmAudio.HandlePlaySoundAtTransform += PlaySoundHandler;
        }
    }

    protected void PlaySoundHandler(string clipName, Transform t)
    {
        if (AudioClips != null && AudioClips.Count > 0)
        {
            AudioClip clip = AudioClips.Where(a => a.name == clipName).First();

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

        if (GUILayout.Button("Lights On"))
        {
            TurnLightsOn();
            fakeInfo.OnLightsOn();
        }

        if (GUILayout.Button("Lights Off"))
        {
            TurnLightsOff();
            fakeInfo.OnLightsOff();
        }

        GUILayout.Label("Time remaining: " + fakeInfo.GetFormattedTime());
    }

    private Light testLight;

    public void PrepareLights()
    {
        foreach (Light l in FindObjectsOfType<Light>())
        {
            if (l.transform.parent == null) Destroy(l.gameObject);
        }

        GameObject o = new GameObject("Light");
        o.transform.localPosition = new Vector3(0, 3, 0);
        o.transform.localRotation = Quaternion.Euler(new Vector3(50, -30, 0));
        testLight = o.AddComponent<Light>();
        testLight.type = LightType.Directional;
    }

    public void TurnLightsOn()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;
        DynamicGI.UpdateEnvironment();

        testLight.enabled = true;
    }

    public void TurnLightsOff()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.1f;
        DynamicGI.UpdateEnvironment();

        testLight.enabled = false;
    }
}
