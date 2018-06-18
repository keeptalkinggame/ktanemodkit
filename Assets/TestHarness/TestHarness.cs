using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using EdgeworkConfigurator;

public class FakeBombInfo : MonoBehaviour
{
    public abstract class Widget : Object
    {
        public abstract string GetResult(string key, string data);
    }

    public class PortWidget : Widget
    {
        List<string> ports;

        public PortWidget(List<string> portNames=null)
        {
            ports = new List<string>();
            string portList = "";
            if (portNames == null)
            {
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
            }
            else
            {
                ports = portNames;
                portList = string.Join(", ", portNames.ToArray());
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
        static List<string> possibleValues = new List<string>()
        {
            "SND","CLR","CAR",
            "IND","FRQ","SIG",
            "NSA","MSA","TRN",
            "BOB","FRK"
        };

        private string val;
        private bool on;

        public IndicatorWidget(string label=null, IndicatorState state=IndicatorState.RANDOM)
        {
            if (label == null)
            {
                int pos = Random.Range(0, possibleValues.Count);
                val = possibleValues[pos];
                possibleValues.RemoveAt(pos);
            }
            else
            {
                if (possibleValues.Contains(label))
                {
                    val = label;
                    possibleValues.Remove(label);
                }
                else
                {
                    val = "NLL";
                }
            }
            if (state == IndicatorState.RANDOM)
            {
                on = Random.value > 0.4f;
            }
            else
            {
                on = state == IndicatorState.ON ? true : false;
            }

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

        public BatteryWidget(int battCount=-1)
        {
            if (battCount == -1)
            {
                batt = Random.Range(1, 3);
            }
            else
            {
                batt = battCount;
            }

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

    public class CustomWidget : Widget
    {
        private string key;
        private string data;

        public CustomWidget(string queryKey, string dataString)
        {
            key = queryKey;
            data = dataString;

            Debug.Log("Added custom widget (" + key + "): " + data);
        }

        public override string GetResult(string query, string passedData)
        {
            if (query == key)
            {
                return data;
            }
            else
            {
                return null;
            }
        }
    }

    public Widget[] widgets;

    float startupTime = .5f;

    public delegate void LightsOn();
    public LightsOn ActivateLights;

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

    readonly char[] SerialNumberPossibleCharArray = new char[35]
    {
        'A','B','C','D','E',
        'F','G','H','I','J',
        'K','L','M','N','E',
        'P','Q','R','S','T',
        'U','V','W','X','Z',
        '0','1','2','3','4',
        '5','6','7','8','9'
    };

    /// <summary>
    /// Sets up the edgework of the FakeBombInfo according to the provided edgework configuration.
    /// </summary>
    /// <param name="config"></param>
    public void SetupEdgework(EdgeworkConfiguration config)
    {
        if (config == null) 
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
            string str1 = string.Empty;
            for (int index = 0; index < 2; ++index) str1 = str1 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length)];
            string str2 = str1 + (object)Random.Range(0, 10);
            for (int index = 3; index < 5; ++index) str2 = str2 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length - 10)];
            serial = str2 + Random.Range(0, 10);

            Debug.Log("Serial: " + serial);
        } 
        else
        {
            if (config.SerialNumberType == SerialNumberType.RANDOM_NORMAL)
            {
                string str1 = string.Empty;
                for (int index = 0; index < 2; ++index) str1 = str1 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length)];
                string str2 = str1 + (object)Random.Range(0, 10);
                for (int index = 3; index < 5; ++index) str2 = str2 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length - 10)];
                serial = str2 + Random.Range(0, 10);
            } 
            else if (config.SerialNumberType == SerialNumberType.RANDOM_ANY)
            {
                string res = string.Empty;
                for (int index = 0; index < 6; ++index) res = res + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length)];
                serial = res;
            }
            else
            {
                serial = config.CustomSerialNumber;
            }
            Debug.Log("Serial: " + serial);

            List<Widget> widgetsResult = new List<Widget>();
            List<THWidget> RandomIndicators = new List<THWidget>();
            List<THWidget> RandomWidgets = new List<THWidget>();
            foreach (THWidget widgetConfig in config.Widgets)
            {
                if (widgetConfig.Type == WidgetType.RANDOM)
                {
                    RandomWidgets.Add(widgetConfig);
                }
                else if (widgetConfig.Type == WidgetType.INDICATOR && widgetConfig.IndicatorLabel == IndicatorLabel.RANDOM)
                {
                    RandomIndicators.Add(widgetConfig);
                }
                else
                {
                    switch (widgetConfig.Type)
                    {
                        case WidgetType.BATTERY:
                            for (int i = 0; i < widgetConfig.Count; i++)
                            {
                                if (widgetConfig.BatteryType == BatteryType.CUSTOM)
                                {
                                    widgetsResult.Add(new BatteryWidget(widgetConfig.BatteryCount));
                                } 
                                else if (widgetConfig.BatteryType == BatteryType.RANDOM)
                                {
                                    widgetsResult.Add(new BatteryWidget(Random.Range(widgetConfig.MinBatteries, widgetConfig.MaxBatteries + 1)));
                                }
                                else
                                {
                                    widgetsResult.Add(new BatteryWidget((int)widgetConfig.BatteryType));
                                }
                            }
                            break;
                        case WidgetType.INDICATOR:
                            if (widgetConfig.IndicatorLabel == IndicatorLabel.CUSTOM)
                            {
                                widgetsResult.Add(new IndicatorWidget(widgetConfig.CustomLabel, widgetConfig.IndicatorState));
                            }
                            else
                            {
                                widgetsResult.Add(new IndicatorWidget(widgetConfig.IndicatorLabel.ToString(), widgetConfig.IndicatorState));
                            }
                            break;
                        case WidgetType.PORT_PLATE:
                            for (int i = 0; i < widgetConfig.Count; i++)
                            {
                                List<string> ports = new List<string>();
                                if (widgetConfig.PortPlateType == PortPlateType.CUSTOM)
                                {
                                    if (widgetConfig.DVIPort) ports.Add("DVI");
                                    if (widgetConfig.PS2Port) ports.Add("PS2");
                                    if (widgetConfig.RJ45Port) ports.Add("RJ45");
                                    if (widgetConfig.StereoRCAPort) ports.Add("StereoRCA");
                                    if (widgetConfig.ParallelPort) ports.Add("Parallel");
                                    if (widgetConfig.SerialPort) ports.Add("Serial");
                                    ports.AddRange(widgetConfig.CustomPorts);
                                }
                                else if (widgetConfig.PortPlateType == PortPlateType.RANDOM_ANY)
                                {
                                    if (Random.value > 0.5f) ports.Add("DVI");
                                    if (Random.value > 0.5f) ports.Add("PS2");
                                    if (Random.value > 0.5f) ports.Add("RJ45");
                                    if (Random.value > 0.5f) ports.Add("StereoRCA");
                                    if (Random.value > 0.5f) ports.Add("Parallel");
                                    if (Random.value > 0.5f) ports.Add("Serial");
                                    foreach (string port in widgetConfig.CustomPorts)
                                    {
                                        if (Random.value > 0.5f) ports.Add(port);
                                    }
                                }
                                else
                                {
                                    if (Random.value > 0.5)
                                    {
                                        if (Random.value > 0.5) ports.Add("Parallel");
                                        if (Random.value > 0.5) ports.Add("Serial");
                                    }
                                    else
                                    {
                                        if (Random.value > 0.5) ports.Add("DVI");
                                        if (Random.value > 0.5) ports.Add("PS2");
                                        if (Random.value > 0.5) ports.Add("RJ45");
                                        if (Random.value > 0.5) ports.Add("StereoRCA");
                                    }
                                    foreach (string port in widgetConfig.CustomPorts)
                                    {
                                        if (Random.value > 0.5f) ports.Add(port);
                                    }
                                }
                                widgetsResult.Add(new PortWidget(ports));
                            }
                            break;
                        case WidgetType.CUSTOM:
                            for (int i = 0; i < widgetConfig.Count; i++)
                            {
                                widgetsResult.Add(new CustomWidget(widgetConfig.CustomQueryKey, widgetConfig.CustomData));
                            }
                            break;
                    }
                }
            }
            foreach (THWidget randIndWidget in RandomIndicators)
            {
                widgetsResult.Add(new IndicatorWidget());
            }
            foreach (THWidget randIndWidget in RandomWidgets)
            {
                for (int i = 0; i < randIndWidget.Count; i++)
                {
                    int r = Random.Range(0, 3);
                    if (r == 0) widgetsResult.Add(new BatteryWidget());
                    else if (r == 1) widgetsResult.Add(new IndicatorWidget());
                    else widgetsResult.Add(new PortWidget());
                }
            }
            widgets = widgetsResult.ToArray();
        }
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

    public EdgeworkConfiguration EdgeworkConfiguration;

    void Awake()
    {
        PrepareLights();

        fakeInfo = gameObject.AddComponent<FakeBombInfo>();
        fakeInfo.SetupEdgework(EdgeworkConfiguration);

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
                    KMBombInfo component = (KMBombInfo) f.GetValue(s);
                    fakeInfo.Detonate += delegate { if (component.OnBombExploded != null) component.OnBombExploded(); };
                    fakeInfo.HandleSolved += delegate { if (component.OnBombSolved != null) component.OnBombSolved(); };
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
            modules[i].OnPass = delegate ()
            {
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
            modules[i].OnStrike = delegate ()
            {
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
        AudioClip clip = AudioClips == null ? null : AudioClips.Where(a => a.name == clipName).FirstOrDefault();

        if (clip != null)
        {
            audioSource.transform.position = t.position;
            audioSource.PlayOneShot(clip);
        }
        else
            Debug.Log("Audio clip not found: " + clipName);
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

    // TPK Methods
    protected void DoInteractionStart(KMSelectable interactable)
    {
        interactable.OnInteract();
    }

    protected void DoInteractionEnd(KMSelectable interactable)
    {
        if (interactable.OnInteractEnded != null)
        {
            interactable.OnInteractEnded();
        }
    }

    Dictionary<Component, HashSet<KMSelectable>> ComponentHelds = new Dictionary<Component, HashSet<KMSelectable>> { };
    IEnumerator SimulateModule(Component component, Transform moduleTransform, MethodInfo method, string command)
    {
        // Simple Command
        if (typeof(IEnumerable<KMSelectable>).IsAssignableFrom(method.ReturnType))
        {
            IEnumerable<KMSelectable> selectableSequence = null;
            try
            {
                selectableSequence = (IEnumerable<KMSelectable>) method.Invoke(component, new object[] { command });
                if (selectableSequence == null)
                {
                    Debug.LogFormat("Twitch Plays handler reports invalid command (by returning null).", method.DeclaringType.FullName, method.Name);
                    yield break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", method.DeclaringType.FullName, method.Name);
                Debug.LogException(ex);
                yield break;
            }

            int initialStrikes = fakeInfo.strikes;
            int initialSolved = fakeInfo.GetSolvedModuleNames().Count;
            foreach (KMSelectable selectable in selectableSequence)
            {
                DoInteractionStart(selectable);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(selectable);

                if (fakeInfo.strikes != initialStrikes || fakeInfo.GetSolvedModuleNames().Count != initialSolved)
                {
                    break;
                }
            };
        }

        // Complex Commands
        if (method.ReturnType == typeof(IEnumerator))
        {
            IEnumerator responseCoroutine = null;
            try
            {
                responseCoroutine = (IEnumerator) method.Invoke(component, new object[] { command });
            }
            catch (System.Exception ex)
            {
                Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", method.DeclaringType.FullName, method.Name);
                Debug.LogException(ex);
                yield break;
            }

            if (responseCoroutine == null)
            {
                Debug.LogFormat("Twitch Plays handler reports invalid command (by returning null).", method.DeclaringType.FullName, method.Name);
                yield break;
            }

            if (!ComponentHelds.ContainsKey(component))
                ComponentHelds[component] = new HashSet<KMSelectable>();
            HashSet<KMSelectable> heldSelectables = ComponentHelds[component];

            int initialStrikes = fakeInfo.strikes;
            int initialSolved = fakeInfo.GetSolvedModuleNames().Count;

            if (!responseCoroutine.MoveNext())
            {
                Debug.LogFormat("Twitch Plays handler reports invalid command (by returning empty sequence).", method.DeclaringType.FullName, method.Name);
                yield break;
            }

            if (responseCoroutine.Current is string)
            {
                var str = (string) responseCoroutine.Current;
                if (str.StartsWith("sendtochat"))
                    Debug.Log("Twitch handler sent: " + str);
                yield break;
            }

            while (responseCoroutine.MoveNext())
            {
                object currentObject = responseCoroutine.Current;
                if (currentObject is KMSelectable)
                {
                    KMSelectable selectable = (KMSelectable) currentObject;
                    if (heldSelectables.Contains(selectable))
                    {
                        DoInteractionEnd(selectable);
                        heldSelectables.Remove(selectable);
                        if (fakeInfo.strikes != initialStrikes || fakeInfo.GetSolvedModuleNames().Count != initialSolved)
                            yield break;
                    }
                    else
                    {
                        DoInteractionStart(selectable);
                        heldSelectables.Add(selectable);
                    }
                }
                else if (currentObject is IEnumerable<KMSelectable>)
                {
                    foreach (var selectable in (IEnumerable<KMSelectable>) currentObject)
                    {
                        DoInteractionStart(selectable);
                        yield return new WaitForSeconds(.1f);
                        DoInteractionEnd(selectable);
                    }
                }
                else if (currentObject is string)
                {
                    string currentString = (string) currentObject;
                    float waitTime;
                    Match match = Regex.Match(currentString, "^trywaitcancel ([0-9]+(?:\\.[0-9])?)((?: (?:.|\\n)+)?)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    if (match.Success && float.TryParse(match.Groups[1].Value, out waitTime))
                    {
                        yield return new WaitForSeconds(waitTime);
                    }

                    Debug.Log("Twitch handler sent: " + currentObject);
                    yield return currentObject;
                }
                else if (currentObject is Quaternion)
                {
                    moduleTransform.localRotation = (Quaternion) currentObject;
                }
                else
                    yield return currentObject;

                if (fakeInfo.strikes != initialStrikes || fakeInfo.GetSolvedModuleNames().Count != initialSolved)
                    yield break;
            }
        }
    }

    string command = "";
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

        GUILayout.Space(10);

        command = GUILayout.TextField(command);
        if ((GUILayout.Button("Simulate Twitch Command") || Event.current.keyCode == KeyCode.Return) && command != "")
        {
            Debug.Log("Twitch Command: " + command);

            foreach (KMBombModule module in FindObjectsOfType<KMBombModule>())
            {
                Component[] allComponents = module.gameObject.GetComponentsInChildren<Component>(true);
                foreach (Component component in allComponents)
                {
                    System.Type type = component.GetType();
                    MethodInfo method = type.GetMethod("ProcessTwitchCommand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (method != null)
                        StartCoroutine(SimulateModule(component, module.transform, method, command));
                }
            }
            command = "";
        }
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
        o.transform.localRotation = Quaternion.Euler(new Vector3(130, -30, 0));
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
