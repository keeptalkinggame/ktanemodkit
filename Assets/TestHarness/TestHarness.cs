using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using EdgeworkConfigurator;
using Random = UnityEngine.Random;

public class FakeBombInfo : MonoBehaviour
{
    float startupTime = .5f;

    public delegate void LightsOn();
    public LightsOn ActivateLights;
	public TimerModule timerModule;

	public SerialNumber SerialNumberWidget;
	public BatteryWidget BatteryWidget;
	public PortWidget PortWidget;
	public IndicatorWidget IndicatorWidget;
	public TwoFactorWidget TwoFactorWidget;
	public CustomWidget CustomWidget;

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

	            foreach (Widget w in widgets)
	            {
		            w.Activate();
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
	public List<Widget> widgets = new List<Widget>();

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
        foreach (Widget w in widgets)
        {
            string r = w.GetResult(queryKey, queryInfo);
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
	    widgets = new List<Widget>();
	    List<THWidget> RandomIndicators = new List<THWidget>();
	    List<THWidget> RandomWidgets = new List<THWidget>();

		widgets.Add(SerialNumber.CreateComponent(SerialNumberWidget, config));
	    serial = ((SerialNumber) widgets[0]).serial;

	    foreach (KMWidget widget in FindObjectsOfType<KMWidget>())
		    widgets.Add(widget.gameObject.AddComponent<ModWidget>());

		if (config == null) 
        {
            const int numWidgets = 5;
            for (int a = 0; a < numWidgets; a++) 
            {
                int r = Random.Range(0, 3);
	            if (r == 0)  widgets.Add(PortWidget.CreateComponent(PortWidget));
	            else if (r == 1) widgets.Add(IndicatorWidget.CreateComponent(IndicatorWidget));
	            else widgets.Add(BatteryWidget.CreateComponent(BatteryWidget));
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
                                    widgets.Add(BatteryWidget.CreateComponent(BatteryWidget, widgetConfig.BatteryCount));
                                } 
                                else if (widgetConfig.BatteryType == BatteryType.RANDOM)
                                {
                                    widgets.Add(BatteryWidget.CreateComponent(BatteryWidget, Random.Range(widgetConfig.MinBatteries, widgetConfig.MaxBatteries + 1)));
                                }
                                else
                                {
                                    widgets.Add(BatteryWidget.CreateComponent(BatteryWidget, (int)widgetConfig.BatteryType));
                                }
                            }
                            break;
                        case WidgetType.INDICATOR:
                            if (widgetConfig.IndicatorLabel == IndicatorLabel.CUSTOM)
                            {
                                widgets.Add(IndicatorWidget.CreateComponent(IndicatorWidget, widgetConfig.CustomLabel, widgetConfig.IndicatorState));
                            }
                            else
                            {
                                widgets.Add(IndicatorWidget.CreateComponent(IndicatorWidget, widgetConfig.IndicatorLabel.ToString(), widgetConfig.IndicatorState));
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
                                widgets.Add(PortWidget.CreateComponent(PortWidget, ports));
                            }
                            break;
						case WidgetType.TWOFACTOR:
							for (int i = 0; i < widgetConfig.Count; i++)
							{
								widgets.Add(TwoFactorWidget.CreateComponent(TwoFactorWidget, widgetConfig.TwoFactorResetTime));
							}
							break;
                        case WidgetType.CUSTOM:
                            for (int i = 0; i < widgetConfig.Count; i++)
                            {
                                widgets.Add(CustomWidget.CreateComponent(CustomWidget, widgetConfig.CustomQueryKey, widgetConfig.CustomData));
                            }
                            break;
                    }
                }
            }
            foreach (THWidget randIndWidget in RandomIndicators)
            {
                widgets.Add(IndicatorWidget.CreateComponent(IndicatorWidget));
            }
            foreach (THWidget randIndWidget in RandomWidgets)
            {
                for (int i = 0; i < randIndWidget.Count; i++)
                {
                    int r = Random.Range(0, 3);
                    if (r == 0) widgets.Add(BatteryWidget.CreateComponent(BatteryWidget));
                    else if (r == 1) widgets.Add(IndicatorWidget.CreateComponent(IndicatorWidget));
                    else widgets.Add(PortWidget.CreateComponent(PortWidget));
                }
            }
        }
	}
}

public class TestHarness : MonoBehaviour
{
	public StatusLight StatusLightPrefab;
	public TimerModule TimerModulePrefab;
	public Transform ModuleCoverPrefab;
	public TwitchPlaysID TwitchIDPrefab;

	public SerialNumber SerialNumberWidget;
	public BatteryWidget BatteryWidget;
	public PortWidget PortWidget;
	public IndicatorWidget IndicatorWidget;
	public TwoFactorWidget TwoFactorWidget;
	public CustomWidget CustomWidget;

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
	private readonly List<Transform> _twitchPlayModules = new List<Transform>();

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
	    fakeInfo.SerialNumberWidget = SerialNumberWidget;
	    fakeInfo.BatteryWidget = BatteryWidget;
	    fakeInfo.PortWidget = PortWidget;
	    fakeInfo.IndicatorWidget = IndicatorWidget;
	    fakeInfo.TwoFactorWidget = TwoFactorWidget;
	    fakeInfo.CustomWidget = CustomWidget;

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
        MonoBehaviour[] scripts = FindObjectsOfType<MonoBehaviour>();
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

	void PrepareBomb(List<KMBombModule> bombModules, List<KMNeedyModule> needyModules, List<Widget> widgets)
	{
		Transform bombTransform;
		List<Transform> timerSideModules = new List<Transform>();
		List<Transform> modules = new List<Transform>();
		List<List<Transform>> anchors = new List<List<Transform>>();
		List<List<Transform>> timerAnchors = new List<List<Transform>>();
		List<WidgetZone> widgetZones = new List<WidgetZone>();

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

			widgetZones.AddRange(bomb.WidgetAreas.Select(WidgetZone.CreateZone));

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

					widgetZones.Add(WidgetZone.CreateZone(rightwall.gameObject));
					widgetZones.Add(WidgetZone.CreateZone(topwall.gameObject));

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
			_twitchPlayModules.Add(module);
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

			KMStatusLightParent statusLight = module.GetComponentInChildren<KMStatusLightParent>();
			TwitchPlaysID tpID = Instantiate(TwitchIDPrefab);
			tpID.Module = module;
			tpID.gameObject.SetActive(true);
			if (statusLight == null)
			{
				tpID.transform.localPosition = new Vector3(0.075167f, 0.06316f, 0.076057f);
				tpID.transform.SetParent(module, false);
			}
			else
			{
				tpID.transform.localPosition = new Vector3(0, 0.0432f, 0);
				tpID.transform.SetParent(statusLight.transform, false);
			}
		}

		foreach (Transform module in modules)
		{
			_twitchPlayModules.Add(module);
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

			KMStatusLightParent statusLight = module.GetComponentInChildren<KMStatusLightParent>();
			TwitchPlaysID tpID = Instantiate(TwitchIDPrefab);
			tpID.Module = module;
			tpID.gameObject.SetActive(true);
			if (statusLight == null)
			{
				tpID.transform.localPosition = new Vector3(0.075167f, 0.06316f, 0.076057f);
				tpID.transform.SetParent(module, false);
			}
			else
			{
				tpID.transform.localPosition = new Vector3(0, 0.0432f, 0);
				tpID.transform.SetParent(statusLight.transform, false);
			}
		}

		foreach (Transform anchor in anchors.SelectMany(x => x))
		{
			Transform cover = Instantiate(ModuleCoverPrefab);
			cover.transform.localPosition = Vector3.zero;
			cover.transform.localRotation = Quaternion.identity;
			cover.transform.localScale = Vector3.one;
			cover.transform.SetParent(anchor, false);
			cover.gameObject.SetActive(true);
		}

		SerialNumber sn = widgets.FirstOrDefault(x => x.GetType() == typeof(SerialNumber)) as SerialNumber;
		if(sn == null) throw new Exception("Could not locate the serial number widget. Cannot continue");
		widgets = widgets.Where(x => x.GetType() != typeof(SerialNumber)).OrderBy(x => Random.value).ToList();
		widgets.Insert(0, sn);

		for (int i = 0; i < widgets.Count; i++)
		{
			//do things with each widget in the pool.  (If one widget won't fit on the bomb, discard it.)
			Widget widget = widgets[i];
			WidgetZone zone = WidgetZone.GetZone(widgetZones, widget);
			if (zone != null)
			{
				widgetZones.Remove(zone);
				List<WidgetZone> subZones = WidgetZone.SubdivideZoneForWidget(zone, widget);
				if (subZones != null)
				{
					zone = subZones[0];
					subZones.Remove(zone);
					widgetZones.AddRange(subZones);
				}

				widget.transform.rotation = zone.WorldRotation;
				widget.transform.parent = zone.Parent.transform;
				widget.transform.localPosition = zone.LocalPosition;
				//widget.transform.parent = zone.Parent.transform.parent;
				continue;
			}
			if (i == 0)
				continue;

			widgets.Remove(widget);
			Destroy(widget);
			i--;
		}

	}

    void Start()
    {
        MonoBehaviour[] scripts = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour s in scripts)
        {
            IEnumerable<FieldInfo> fields = s.GetType().GetFields();
            foreach (FieldInfo f in fields)
            {
	            if (f.FieldType != typeof(KMBombInfo)) continue;

	            KMBombInfo component = (KMBombInfo) f.GetValue(s);
	            fakeInfo.Detonate += delegate { if (component.OnBombExploded != null) component.OnBombExploded(); };
	            fakeInfo.HandleSolved += delegate { if (component.OnBombSolved != null) component.OnBombSolved(); };
            }
        }

        currentSelectable = GetComponent<TestSelectable>();

		List<KMBombModule> modules = FindObjectsOfType<KMBombModule>().ToList();
        List<KMNeedyModule> needyModules = FindObjectsOfType<KMNeedyModule>().ToList();
	    PrepareBomb(modules, needyModules, fakeInfo.widgets);

	    fakeInfo.timerModule = _timer;
        fakeInfo.needyModules = needyModules.ToList();
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
        List<KMHighlightable> highlightables = new List<KMHighlightable>(FindObjectsOfType<KMHighlightable>());

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
        List<KMSelectable> selectables = new List<KMSelectable>(FindObjectsOfType<KMSelectable>());

        foreach (KMSelectable selectable in selectables)
        {
	        try
	        {
		        TestSelectable testSelectable = selectable.gameObject.AddComponent<TestSelectable>();
		        testSelectable.Highlight = selectable.Highlight.GetComponent<TestHighlightable>();
	        }
	        catch (Exception ex)
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

    
    

    

    string command = "";
    void OnGUI()
    {
        if (GUILayout.Button("Activate Needy Modules"))
        {
            foreach (KMNeedyModule needyModule in FindObjectsOfType<KMNeedyModule>())
            {
                if (needyModule.OnNeedyActivation != null)
                {
                    needyModule.OnNeedyActivation();
                }
            }
        }

        if (GUILayout.Button("Deactivate Needy Modules"))
        {
            foreach (KMNeedyModule needyModule in FindObjectsOfType<KMNeedyModule>())
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
	    TwitchPlaysID.AntiTrollMode = GUILayout.Toggle(TwitchPlaysID.AntiTrollMode, "Troll Commands Disabled");
	    TwitchPlaysID.AnarchyMode = GUILayout.Toggle(TwitchPlaysID.AnarchyMode, "Anarchy Mode Enabled");

        GUI.SetNextControlName("commandField");
        command = GUILayout.TextField(command);
        if ((GUILayout.Button("Simulate Twitch Command") || Event.current.keyCode == KeyCode.Return) && GUI.GetNameOfFocusedControl() == "commandField" && command != "")
        {
            Debug.Log("Twitch Command: " + command);
            foreach (Transform module in _twitchPlayModules)
            {
	            TwitchPlaysID tpID = module.GetComponentInChildren<TwitchPlaysID>();
	            if (tpID == null) continue;
	            tpID.ProcessCommand(command);
            }

	        if (command.Equals("!cancel", StringComparison.InvariantCultureIgnoreCase))
	        {
		        Canceller.SetCancel();
	        }
			else if (command.Equals("!stop", StringComparison.InvariantCultureIgnoreCase))
	        {
		        Canceller.SetCancel();
		        TwitchPlaysID.TPCoroutineQueue.CancelFutureSubcoroutines();
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

