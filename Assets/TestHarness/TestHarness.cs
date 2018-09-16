using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using EdgeworkConfigurator;
using Random = UnityEngine.Random;

public class FakeBombInfo : MonoBehaviour
{
    public abstract class Widget : MonoBehaviour
    {
        public abstract string GetResult(string key, string data);
    }

	public class SerialNumber : Widget
	{
		public string serial;

		static readonly char[] SerialNumberPossibleCharArray = new char[35]
		{
			'A','B','C','D','E',
			'F','G','H','I','J',
			'K','L','M','N','E',
			'P','Q','R','S','T',
			'U','V','W','X','Z',
			'0','1','2','3','4',
			'5','6','7','8','9'
		};

		public static SerialNumber CreateComponent(GameObject where, EdgeworkConfiguration config)
		{
			SerialNumberType sntype = config == null ? SerialNumberType.RANDOM_NORMAL : config.SerialNumberType;
			string sn = config == null ? string.Empty : config.CustomSerialNumber;

			SerialNumber widget = where.AddComponent<SerialNumber>();
			if (string.IsNullOrEmpty(sn) && sntype == SerialNumberType.CUSTOM)
				sntype = SerialNumberType.RANDOM_NORMAL;

			if (sntype == SerialNumberType.RANDOM_NORMAL)
			{
				string str1 = string.Empty;
				for (int index = 0; index < 2; ++index) str1 = str1 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length)];
				string str2 = str1 + (object)Random.Range(0, 10);
				for (int index = 3; index < 5; ++index) str2 = str2 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length - 10)];
				widget.serial = str2 + Random.Range(0, 10);
			}
			else if (sntype == SerialNumberType.RANDOM_ANY)
			{
				string res = string.Empty;
				for (int index = 0; index < 6; ++index) res = res + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length)];
				widget.serial = res;
			}
			else
			{
				widget.serial = sn;
			}

			Debug.Log("Serial: " + widget.serial);
			return widget;
		}

		public override string GetResult(string key, string data)
		{
			if (key == KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER)
			{
				return JsonConvert.SerializeObject((object)new Dictionary<string, string>()
				{
					{
						"serial", serial
					}
				});
			}
			return null;
		}
	}

    public class PortWidget : Widget
    {
		public List<string> ports;

        public static PortWidget CreateComponent(GameObject where, List<string> portNames=null)
        {
	        PortWidget widget = where.AddComponent<PortWidget>();
	        widget.ports = new List<string>();
            string portList = "";
            if (portNames == null)
            {
                if (Random.value > 0.5)
                {
                    if (Random.value > 0.5)
                    {
	                    widget.ports.Add("Parallel");
                        portList += "Parallel";
                    }
                    if (Random.value > 0.5)
                    {
	                    widget.ports.Add("Serial");
                        if (portList.Length > 0) portList += ", ";
                        portList += "Serial";
                    }
                }
                else
                {
                    if (Random.value > 0.5)
                    {
	                    widget.ports.Add("DVI");
                        portList += "DVI";
                    }
                    if (Random.value > 0.5)
                    {
	                    widget.ports.Add("PS2");
                        if (portList.Length > 0) portList += ", ";
                        portList += "PS2";
                    }
                    if (Random.value > 0.5)
                    {
	                    widget.ports.Add("RJ45");
                        if (portList.Length > 0) portList += ", ";
                        portList += "RJ45";
                    }
                    if (Random.value > 0.5)
                    {
	                    widget.ports.Add("StereoRCA");
                        if (portList.Length > 0) portList += ", ";
                        portList += "StereoRCA";
                    }
                }
            }
            else
            {
	            widget.ports = portNames;
                portList = string.Join(", ", portNames.ToArray());
            }
            if (portList.Length == 0) portList = "Empty plate";
            Debug.Log("Added port widget: " + portList);
	        return widget;
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

	    public string val;
	    public bool on;

        public static IndicatorWidget CreateComponent(GameObject where, string label=null, IndicatorState state=IndicatorState.RANDOM)
        {
	        IndicatorWidget widget = where.AddComponent<IndicatorWidget>();

            if (label == null)
            {
                int pos = Random.Range(0, possibleValues.Count);
                widget.val = possibleValues[pos];
                possibleValues.RemoveAt(pos);
            }
            else
            {
                if (possibleValues.Contains(label))
                {
	                widget.val = label;
                    possibleValues.Remove(label);
                }
                else
                {
	                widget.val = "NLL";
                }
            }
            if (state == IndicatorState.RANDOM)
            {
	            widget.on = Random.value > 0.4f;
            }
            else
            {
	            widget.on = state == IndicatorState.ON ? true : false;
            }

            Debug.Log("Added indicator widget: " + widget.val + " is " + (widget.on ? "ON" : "OFF"));
	        return widget;
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
	    public int batt;

        public static BatteryWidget CreateComponent(GameObject where, int battCount=-1)
        {
	        BatteryWidget widget = where.AddComponent<BatteryWidget>();
            if (battCount == -1)
            {
	            widget.batt = Random.Range(1, 3);
            }
            else
            {
	            widget.batt = battCount;
            }

            Debug.Log("Added battery widget: " + widget.batt);
	        return widget;
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

	public class TwoFactorWidget : Widget
	{
		private static int counter = 1;
		public int instance;
		public int code;
		private float newcodetime;
		public float timeremaining;

		public static TwoFactorWidget CreateComponent(GameObject where, float newcode = 30)
		{
			TwoFactorWidget widget = where.AddComponent<TwoFactorWidget>();
			widget.instance = counter++;

			if (newcode < 30)
				newcode = 30;
			if (newcode > 999)
				newcode = 999;

			widget.newcodetime = newcode;
			widget.timeremaining = newcode;
			widget.code = Random.Range(0, 1000000);

			Debug.LogFormat("Added Two factor widget #{0}: {1,6}.", widget.instance, widget.code);
			return widget;
		}

		public override string GetResult(string key, string data)
		{
			if (key == "twofactor")
			{
				return JsonConvert.SerializeObject((object) new Dictionary<string, int>()
				{
					{
						"twofactor_key", code
					}
				});
			}
			else return null;
		}

		private void FixedUpdate()
		{
			timeremaining -= Time.fixedDeltaTime;
			if (timeremaining < 0)
			{
				timeremaining = newcodetime;
				code = Random.Range(0, 1000000);
				Debug.LogFormat("[Two Factor #{0}] code is now {1,6}.",instance,code);
			}
		}
	}

    public class CustomWidget : Widget
    {

	    public string key;
	    public string data;

        public static CustomWidget CreateComponent(GameObject where, string queryKey, string dataString)
        {
	        CustomWidget widget = where.AddComponent<CustomWidget>();
	        widget.key = queryKey;
            widget.data = dataString;

            Debug.Log("Added custom widget (" + widget.key + "): " + widget.data);
	        return widget;
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


    float startupTime = .5f;

    public delegate void LightsOn();
    public LightsOn ActivateLights;
	public TimerModule timerModule;

    void FixedUpdate()
    {
        if (solved || exploded) return;
	    if (timerModule == null) return;
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

	            foreach (KMWidget w in widgets)
	            {
		            if (w.OnWidgetActivate != null) w.OnWidgetActivate();
	            }

	            timerModule.TimerRunning = true;
            }
        }
        else
        {
	        timeLeft = timerModule.TimeRemaining;
	        if (timerModule.ExplodedToTime)
	        {
		        exploded = true;
		        if (Detonate != null) Detonate();
		        Debug.Log("KABOOM!");
	        }
        }
    }

    public const int numStrikes = 3;

    public bool solved;
	public bool exploded;
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
			int s = ((int)(timeLeft * 100)) % 100;
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
	public List<KMWidget> widgets = new List<KMWidget>();

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
        foreach (Widget w in transform.Find("Edgework").GetComponents<Widget>())
        {
            string r = w.GetResult(queryKey, queryInfo);
            if (r != null) responses.Add(r);
        }

	    foreach (KMWidget w in widgets)
	    {
		    if (w.OnQueryRequest == null) continue;
		    string r = w.OnQueryRequest(queryKey, queryInfo);
		    if (r != null) responses.Add(r);
	    }

        return responses;
    }

    public bool IsBombPresent()
    {
        return true;
    }

    public void HandleStrike()
    {
        strikes++;
	    timerModule.StrikeCount++;
        Debug.Log(strikes + "/" + numStrikes);
        if (strikes == numStrikes)
        {
	        exploded = true;
	        timerModule.TimerRunning = false;
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
	    if (exploded) return;
        solved = true;
	    timerModule.TimerRunning = false;
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

    /// <summary>
    /// Sets up the edgework of the FakeBombInfo according to the provided edgework configuration.
    /// </summary>
    /// <param name="config"></param>
    public void SetupEdgework(EdgeworkConfiguration config)
    {
	    GameObject edgework = transform.Find("Edgework").gameObject;
	    List<Widget> widgetsResult = new List<Widget>();
	    List<THWidget> RandomIndicators = new List<THWidget>();
	    List<THWidget> RandomWidgets = new List<THWidget>();

		widgetsResult.Add(SerialNumber.CreateComponent(edgework, config));
	    serial = ((SerialNumber) widgetsResult[0]).serial;

		if (config == null) 
        {
            const int numWidgets = 5;
            for (int a = 0; a < numWidgets; a++) 
            {
                int r = Random.Range(0, 3);
	            if (r == 0)  widgetsResult.Add(PortWidget.CreateComponent(edgework));
	            else if (r == 1) widgetsResult.Add(IndicatorWidget.CreateComponent(edgework));
	            else widgetsResult.Add(BatteryWidget.CreateComponent(edgework));
			}
        } 
        else
        {
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
                                    widgetsResult.Add(BatteryWidget.CreateComponent(edgework, widgetConfig.BatteryCount));
                                } 
                                else if (widgetConfig.BatteryType == BatteryType.RANDOM)
                                {
                                    widgetsResult.Add(BatteryWidget.CreateComponent(edgework, Random.Range(widgetConfig.MinBatteries, widgetConfig.MaxBatteries + 1)));
                                }
                                else
                                {
                                    widgetsResult.Add(BatteryWidget.CreateComponent(edgework, (int)widgetConfig.BatteryType));
                                }
                            }
                            break;
                        case WidgetType.INDICATOR:
                            if (widgetConfig.IndicatorLabel == IndicatorLabel.CUSTOM)
                            {
                                widgetsResult.Add(IndicatorWidget.CreateComponent(edgework, widgetConfig.CustomLabel, widgetConfig.IndicatorState));
                            }
                            else
                            {
                                widgetsResult.Add(IndicatorWidget.CreateComponent(edgework, widgetConfig.IndicatorLabel.ToString(), widgetConfig.IndicatorState));
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
									if (widgetConfig.ComponentVideoPort) ports.Add("ComponentVideo");
									if (widgetConfig.CompositeVideoPort) ports.Add("CompositeVideo");
									if (widgetConfig.HDMIPort) ports.Add("HDMI");
									if (widgetConfig.VGAPort) ports.Add("VGA");
									if (widgetConfig.USBPort) ports.Add("USB");
									if (widgetConfig.PCMCIAPort) ports.Add("PCMCIA");
									if (widgetConfig.ACPort) ports.Add("AC");
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
									if (Random.value > 0.5f) ports.Add("ComponentVideo");
	                                if (Random.value > 0.5f) ports.Add("CompositeVideo");
	                                if (Random.value > 0.5f) ports.Add("HDMI");
	                                if (Random.value > 0.5f) ports.Add("VGA");
	                                if (Random.value > 0.5f) ports.Add("USB");
	                                if (Random.value > 0.5f) ports.Add("PCMCIA");
	                                if (Random.value > 0.5f) ports.Add("AC");

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
                                widgetsResult.Add(PortWidget.CreateComponent(edgework, ports));
                            }
                            break;
						case WidgetType.TWOFACTOR:
							for (int i = 0; i < widgetConfig.Count; i++)
							{
								widgetsResult.Add(TwoFactorWidget.CreateComponent(edgework, widgetConfig.TwoFactorResetTime));
							}
							break;
                        case WidgetType.CUSTOM:
                            for (int i = 0; i < widgetConfig.Count; i++)
                            {
                                widgetsResult.Add(CustomWidget.CreateComponent(edgework, widgetConfig.CustomQueryKey, widgetConfig.CustomData));
                            }
                            break;
                    }
                }
            }
            foreach (THWidget randIndWidget in RandomIndicators)
            {
                widgetsResult.Add(IndicatorWidget.CreateComponent(edgework));
            }
            foreach (THWidget randIndWidget in RandomWidgets)
            {
                for (int i = 0; i < randIndWidget.Count; i++)
                {
                    int r = Random.Range(0, 3);
                    if (r == 0) widgetsResult.Add(BatteryWidget.CreateComponent(edgework));
                    else if (r == 1) widgetsResult.Add(IndicatorWidget.CreateComponent(edgework));
                    else widgetsResult.Add(PortWidget.CreateComponent(edgework));
                }
            }
        }
    }
}

public class TestHarness : MonoBehaviour
{
	public StatusLight StatusLightPrefab;
	public TimerModule TimerModulePrefab;

	private FakeBombInfo fakeInfo;

    public GameObject HighlightPrefab;
    TestSelectable currentSelectable;
    TestSelectableArea currentSelectableArea;

    bool gamepadEnabled = false;
    TestSelectable lastSelected;

    AudioSource audioSource;
    public List<AudioClip> AudioClips;

    public EdgeworkConfiguration EdgeworkConfiguration;

	public float turnSpeed = 128.0f;      // Speed of camera turning when mouse moves in along an axis
	public float panSpeed = 4.0f;       // Speed of the camera when being panned
	public float zoomSpeed = 16.0f;      // Speed of the camera going back and forth

	private Vector3 mouseOrigin;    // Position of cursor when mouse dragging starts
	private bool isPanning;     // Is the camera being panned?
	private bool isRotating;    // Is the camera being rotated?
	private bool isZooming;     // Is the camera zooming?
	private float mouseDownTIme;

	private Transform _camera;
	private Transform _bomb;

	private TimerModule _timer;

	void Awake()
    {
	    _camera = Camera.main.transform;
	    _camera.localPosition = new Vector3(0, 0.7f, 0);
	    _camera.localEulerAngles = new Vector3(90, 0, 0);
	    _camera.localScale = Vector3.one;
	    Camera.main.nearClipPlane = 0.01f;
	    Camera.main.farClipPlane = 3.0f;

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

	void PrepareBomb(List<KMBombModule> bombModules, List<KMNeedyModule> needyModules, KMWidget[] widgets)
	{
		Transform bombTransform;
		List<Transform> timerSideModules = new List<Transform>();
		List<Transform> modules = new List<Transform>();
		List<List<Transform>> anchors = new List<List<Transform>>();
		List<List<Transform>> timerAnchors = new List<List<Transform>>();

		timerSideModules.AddRange(bombModules.Where(x => x.RequiresTimerVisibility).Select(x => x.transform));
		timerSideModules.AddRange(needyModules.Where(x => x.RequiresTimerVisibility).Select(x => x.transform));

		modules.AddRange(bombModules.Where(x => !x.RequiresTimerVisibility).Select(x => x.transform));
		modules.AddRange(needyModules.Where(x => !x.RequiresTimerVisibility).Select(x => x.transform));

		KMBomb bomb = FindObjectOfType<KMBomb>();
		if (bomb != null)
		{
			bombTransform = bomb.transform;
			foreach (KMBombFace face in bomb.Faces)
			{
				anchors.Add(face.Anchors);
				if (face.TimerAnchors.Count > 0)
					timerAnchors.Add(face.TimerAnchors);
				else
					timerAnchors.Add(face.Anchors);
			}
			while ((modules.Count + timerSideModules.Count + 1) > anchors.SelectMany(x => x).ToList().Count)
			{
				Transform module;
				if (Random.value < 0.5f)
				{
					if (timerSideModules.Count == 0) continue;
					module = timerSideModules[Random.Range(0, timerSideModules.Capacity)];
					timerSideModules.Remove(module);
				}
				else
				{
					if (modules.Count == 0) continue;
					module = modules[Random.Range(0, modules.Count)];
					modules.Remove(module);
				}

				bombModules.Remove(module.GetComponent<KMBombModule>());
				needyModules.Remove(module.GetComponent<KMNeedyModule>());
				Destroy(module.gameObject);
			}
		}
		else
		{
			bombTransform = new GameObject().transform;
			bombTransform.gameObject.AddComponent<KMBomb>();
			bombTransform.name = "Bomb";

			int square = 1;
			while ((square * square * 2) < (modules.Count + timerSideModules.Count + 1))
				square++;
			float squaresize = 0.2f * square;
			for (int bombFace = 0; bombFace < 2; bombFace++)
			{
				Transform bombFaceTransform = new GameObject().transform;
				
				anchors.Add(new List<Transform>());
				timerAnchors.Add(new List<Transform>());

				for (float i = (-squaresize / 2) + 0.1f; i < squaresize / 2; i += 0.2f)
				{
					Transform rightwall = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
					rightwall.localPosition = new Vector3((squaresize / 2), -0.1f, i);
					rightwall.localEulerAngles = new Vector3(-180f, 90f, 0f);
					rightwall.localScale = new Vector3(0.2f, 0.2f, 0.2f);
					rightwall.SetParent(bombFaceTransform, false);

					Transform topwall = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
					topwall.localPosition = new Vector3(i, -0.1f, (squaresize / 2) - (squaresize * bombFace));
					topwall.localEulerAngles = new Vector3(-180f + (180f * bombFace), 0f, 0f);
					topwall.localScale = new Vector3(0.2f, 0.2f, 0.2f);
					topwall.SetParent(bombFaceTransform, false);

					for (float j = (-squaresize / 2) + 0.1f; j < squaresize / 2; j += 0.2f)
					{
						Transform anchor = new GameObject().transform;
						anchor.localPosition = new Vector3(i, 0, j);
						anchor.SetParent(bombFaceTransform, true);
						anchors[bombFace].Add(anchor);
						timerAnchors[bombFace].Add(anchor);
					}
				}

				bombFaceTransform.localPosition = new Vector3(0, (bombFace * 0.2f) - 0.1f, 0);
				bombFaceTransform.localEulerAngles = new Vector3(0, 0, 180f - (bombFace * 180f));
				bombFaceTransform.SetParent(bombTransform, true);
			}
		}
		_bomb = bombTransform;

		for (int i = 0; i < anchors.Count; i++)
			anchors[i] = anchors[i].OrderBy(x => Random.value).ToList();

		int timerFace = Random.Range(0, timerAnchors.Count);

		Transform timerAnchor = timerAnchors[timerFace][Random.Range(0, timerAnchors[timerFace].Count)];
		anchors[timerFace].Remove(timerAnchor);
		_timer = Instantiate(TimerModulePrefab);
		_timer.transform.localPosition = Vector3.zero;
		_timer.transform.localRotation = Quaternion.identity;
		_timer.transform.localScale = Vector3.one;
		_timer.transform.SetParent(timerAnchor, false);
		_timer.gameObject.SetActive(true);
		fakeInfo.timerModule = _timer;

		foreach (Transform module in timerSideModules)
		{
			module.localPosition = Vector3.zero;
			module.localRotation = Quaternion.identity;
			module.localScale = Vector3.one;
			Transform anchor = anchors[timerFace].FirstOrDefault();
			while (anchor == null)
			{
				anchors.Remove(anchors[timerFace]);
				timerFace = Random.Range(0, anchors.Count);
				anchor = anchors[timerFace].FirstOrDefault();
			}
			anchors[timerFace].Remove(anchor);
			module.SetParent(anchor, false);
		}

		foreach (Transform module in modules)
		{
			module.localPosition = Vector3.zero;
			module.localRotation = Quaternion.identity;
			module.localScale = Vector3.one;
			timerFace = Random.Range(0, anchors.Count);
			Transform anchor = anchors[timerFace].FirstOrDefault();
			while (anchor == null)
			{
				anchors.Remove(anchors[timerFace]);
				timerFace = Random.Range(0, anchors.Count);
				anchor = anchors[timerFace].FirstOrDefault();
			}
			anchors[timerFace].Remove(anchor);
			module.SetParent(anchor, false);
		}

		foreach (KMWidget widget in widgets)
			widget.transform.SetParent(bombTransform, true);
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

		List<KMBombModule> modules = FindObjectsOfType<KMBombModule>().ToList();
        List<KMNeedyModule> needyModules = FindObjectsOfType<KMNeedyModule>().ToList();
	    KMWidget[] widgets = FindObjectsOfType<KMWidget>();
	    PrepareBomb(modules, needyModules, widgets);

	    fakeInfo.timerModule = _timer;
        fakeInfo.needyModules = needyModules.ToList();
	    fakeInfo.widgets = widgets.ToList();
        currentSelectable.Children = new TestSelectable[modules.Count + needyModules.Count];
        currentSelectable.ChildRowLength = currentSelectable.Children.Length;
        for (int i = 0; i < modules.Count; i++)
        {
            KMBombModule mod = modules[i];

            KMStatusLightParent statuslightparent = modules[i].GetComponentInChildren<KMStatusLightParent>();
            var statuslight = Instantiate<StatusLight>(StatusLightPrefab);
	        statuslight.transform.SetParent(statuslightparent.transform, false);
            statuslight.transform.localPosition = Vector3.zero;
            statuslight.transform.localScale = Vector3.one;
            statuslight.transform.localRotation = Quaternion.identity;
            statuslight.SetInActive();
            TestSelectable testSelectable = modules[i].GetComponent<TestSelectable>();
            currentSelectable.Children[i] = testSelectable;
            testSelectable.Parent = currentSelectable;
            testSelectable.x = i;

            fakeInfo.modules.Add(new KeyValuePair<KMBombModule, bool>(modules[i], false));
            modules[i].OnPass = delegate ()
            {
                Debug.Log("Module Passed");
                statuslight.SetPass();

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
                statuslight.FlashStrike();
                fakeInfo.HandleStrike();
                return false;
            };
        }

        for (int i = 0; i < needyModules.Count; i++)
        {
            TestSelectable testSelectable = needyModules[i].GetComponent<TestSelectable>();
            currentSelectable.Children[modules.Count + i] = testSelectable;
            testSelectable.Parent = currentSelectable;
            testSelectable.x = modules.Count + i;

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
	    mouseDownTIme += Time.deltaTime;
		//Camera/bomb control
		// Get the left mouse button
		if (Input.GetMouseButtonDown(1))
		{
			// Get mouse origin
			mouseOrigin = Input.mousePosition;
			isRotating = true;
		}

		// Get the right mouse button
		if (Input.GetMouseButtonDown(2))
		{
			// Get mouse origin
			mouseOrigin = Input.mousePosition;
			isPanning = true;
		}

		// Disable movements on button release
		if (!Input.GetMouseButton(1)) isRotating = false;
		if (!Input.GetMouseButton(2)) isPanning = false;

		// Rotate camera along X and Y axis
		if (isRotating)
		{
			Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);
			var speed = pos.y * turnSpeed;

			if (speed < 0 && _bomb.localEulerAngles.x > 180 && (_bomb.localEulerAngles.x + speed) < 270.5f)
				speed = 270.5f - _bomb.localEulerAngles.x;
			else if (speed > 0 && _bomb.localEulerAngles.x < 180 && (_bomb.localEulerAngles.x + speed) > 89.5f)
				speed = 89.5f - _bomb.localEulerAngles.x;

			//_bomb.RotateAround(_bomb.position, _bomb.right, pos.y * turnSpeed);
			//_bomb.RotateAround(_bomb.position, Vector3.forward, pos.x * turnSpeed);
			_bomb.localEulerAngles += new Vector3(speed, 0, -pos.x * turnSpeed * 2);
			_bomb.localEulerAngles = new Vector3(_bomb.localEulerAngles.x, 0, _bomb.localEulerAngles.z);

			mouseOrigin = Input.mousePosition;
		}

		// Move the camera on it's XY plane
		if (isPanning)
		{
			Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

			Vector3 move = new Vector3(pos.x * -panSpeed, pos.y * -panSpeed, 0);
			_camera.Translate(move, Space.Self);
			mouseOrigin = Input.mousePosition;
		}

		float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
		if (mouseWheel != 0)
		{
			Vector3 move = mouseWheel * zoomSpeed * _camera.forward;
			Debug.LogFormat("X:{0} Y:{1} Z:{2}", move.x, move.y, move.z);
			//_camera.Translate(move, Space.World);
			Camera.main.fieldOfView += (-mouseWheel * zoomSpeed);
		}

		if (!gamepadEnabled)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction);
            RaycastHit hit;
            int layerMask = 1 << 11;
            bool rayCastHitSomething = Physics.Raycast(ray, out hit, 1000, layerMask);

            if (rayCastHitSomething) {
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

            if (Input.GetMouseButtonDown(0)) Interact();
            if (Input.GetMouseButtonUp(0)) InteractEnded();
	        if (Input.GetMouseButtonDown(1))
	        {
		        //mouseDownTIme = 0;
	        }

	        if (Input.GetMouseButtonUp(1) && mouseDownTIme < 0.1f)
	        {
				Cancel();
	        }
        }
        else
        {
            TestSelectable previousSelectable = lastSelected;
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Return)) Interact();
            if (Input.GetKeyUp(KeyCode.X) || Input.GetKeyUp(KeyCode.Return)) InteractEnded();
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Backspace)) Cancel();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) EmulateDirection(Direction.Left);
            if (Input.GetKeyDown(KeyCode.RightArrow)) EmulateDirection(Direction.Right);
            if (Input.GetKeyDown(KeyCode.UpArrow)) EmulateDirection(Direction.Up);
            if (Input.GetKeyDown(KeyCode.DownArrow)) EmulateDirection(Direction.Down);

            if (previousSelectable != lastSelected)
            {
                previousSelectable.Deselect();
                lastSelected.Select();
                currentSelectableArea = lastSelected.SelectableArea;
            }
        }
    }

    void EmulateDirection(Direction direction)
    {
        TestSelectable selectable = lastSelected.GetNearestSelectable(direction);
        if (selectable)
        {
            lastSelected = selectable;
            currentSelectable.LastSelectedChild = lastSelected;
        }
    }

    void Interact()
    {
        if (currentSelectableArea != null && currentSelectableArea.Selectable.Interact())
        {
            currentSelectable.DeactivateChildSelectableAreas(currentSelectableArea.Selectable);
            currentSelectable = currentSelectableArea.Selectable;
            currentSelectable.ActivateChildSelectableAreas();
            lastSelected = currentSelectable.GetCurrentChild();
        }
    }

    void InteractEnded()
    {
        if (currentSelectableArea != null)
        {
            currentSelectableArea.Selectable.InteractEnded();
        }
    }

    void Cancel()
    {
        if (currentSelectable.Parent != null && currentSelectable.Cancel())
        {
            currentSelectable.DeactivateChildSelectableAreas(currentSelectable.Parent);
            currentSelectable = currentSelectable.Parent;
            currentSelectable.ActivateChildSelectableAreas();
            lastSelected = currentSelectable.GetCurrentChild();
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
	        try
	        {
		        TestSelectable testSelectable = selectable.gameObject.AddComponent<TestSelectable>();
		        testSelectable.Highlight = selectable.Highlight.GetComponent<TestHighlightable>();
	        }
	        catch (System.Exception ex)
	        {
		        Debug.LogException(ex);
	        }
        }

        foreach (KMSelectable selectable in selectables)
        {
            TestSelectable testSelectable = selectable.gameObject.GetComponent<TestSelectable>();
            testSelectable.Parent = selectable.Parent ? selectable.Parent.GetComponent<TestSelectable>() : null;
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

        bool previous = gamepadEnabled;
        gamepadEnabled = GUILayout.Toggle(gamepadEnabled, "Emulate Gamepad");
        if (!previous && gamepadEnabled)
        {
            lastSelected = currentSelectable.GetCurrentChild();
            lastSelected.Select();
            currentSelectableArea = lastSelected.SelectableArea;
        }

        GUILayout.Label("Time remaining: " + fakeInfo.GetFormattedTime());

        GUILayout.Space(10);

        GUI.SetNextControlName("commandField");
        command = GUILayout.TextField(command);
        if ((GUILayout.Button("Simulate Twitch Command") || Event.current.keyCode == KeyCode.Return) && GUI.GetNameOfFocusedControl() == "commandField" && command != "")
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
