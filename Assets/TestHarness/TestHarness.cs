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

	public KMAudio Audio;

	public delegate void LightsOn();
    public LightsOn ActivateLights;
	public TimerModule TimerModule;

	public SerialNumber SerialNumberWidget;
	public BatteryWidget BatteryWidget;
	public PortWidget PortWidget;
	public IndicatorWidget IndicatorWidget;
	public TwoFactorWidget TwoFactorWidget;
	public CustomWidget CustomWidget;

	void FixedUpdate()
    {
        if (solved || exploded) return;
	    if (TimerModule == null) return;
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

	            TimerModule.TimerRunning = true;
            }
        }
        else
        {
	        timeLeft = TimerModule.TimeRemaining;
	        if (TimerModule.ExplodedToTime)
	        {
		        OnBombExploded("Time Ran Out!");
				
	        }
        }
    }

	public int numStrikes = 3;

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
	public List<NeedyTimer> needyModuleTimers { get { return needyModules.Select(x => x.GetComponentInChildren<NeedyTimer>()).ToList(); } }

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

	public void OnBombExploded(string reason)
	{
		if (exploded) return;
		Debug.LogFormat("KABOOM! - Cause of Explosion: {0}", string.IsNullOrEmpty(reason) ? "Unknown" : reason);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BombExplode, transform);
		TimerModule.TimerRunning = false;
		exploded = true;

		foreach (NeedyTimer timer in needyModuleTimers)
		{
			timer.StopTimer(NeedyTimer.NeedyState.Terminated);
		}
	}

	public void HandleStrike(string reason = null)
    {
	    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);

		if (!string.IsNullOrEmpty(reason))
			Debug.Log("Strike: " + reason);

		strikes++;
	    TimerModule.StrikeCount++;
        Debug.Log(strikes + "/" + numStrikes);
        if (strikes == numStrikes)
        {
	        OnBombExploded(reason);
			if (Detonate != null) Detonate();
        }

	    if (TimerModule.TimeMode)
		    strikes--;

	    if (TimerModule.ZenMode)
		    numStrikes++;
    }

    public delegate void OnDetonate();
    public OnDetonate Detonate;

	public delegate void OnSolved();
    public OnSolved HandleSolved;

    public void Solved()
    {
		if (solved || exploded) return;
	    Debug.Log("Bomb defused!");
	    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BombDefused, transform);
	    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.GameOverFanfare, transform);
	    TimerModule.TimerRunning = false;

		solved = true;
        if (HandleSolved != null) HandleSolved();

	    foreach (NeedyTimer timer in needyModuleTimers)
		    timer.StopTimer(NeedyTimer.NeedyState.BombComplete);
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

	public void OnLightsChange(bool state)
	{
		if (OnLights != null) OnLights(state);
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
								widgets.Add(TwoFactorWidget.CreateComponent(TwoFactorWidget, config.TwoFactorResetTime));
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

public enum TwitchPlaysMode
{
	NormalMode,
	TimeMode,
	ZenMode
}

public class TestHarness : MonoBehaviour
{
	public StatusLight StatusLightPrefab;
	public TimerModule TimerModulePrefab;
	public TimerModule StrikelessTimerModulePrefab;
	public Transform ModuleCoverPrefab;
	public KMModuleBacking ModuleFoamBacking;
	public TwitchPlaysID TwitchIDPrefab;
	public NeedyTimer NeedyTimerPrefab;

	public SerialNumber SerialNumberWidget;
	public BatteryWidget BatteryWidget;
	public PortWidget PortWidget;
	public IndicatorWidget IndicatorWidget;
	public TwoFactorWidget TwoFactorWidget;
	public CustomWidget CustomWidget;

	private FakeBombInfo fakeInfo;

    public GameObject HighlightPrefab;
	TestSelectable currentModule;
    TestSelectable currentSelectable;
    TestSelectableArea currentSelectableArea;

    bool gamepadEnabled = false;
    TestSelectable lastSelected;

    AudioSource audioSource;
	[Range(0.0f, 1.0f)] public float AudioVolume = 0.25f;
    public List<AudioClip> AudioClips;
	public Dictionary<KMSoundOverride.SoundEffect, AudioSource> GameSoundEffectSources = new Dictionary<KMSoundOverride.SoundEffect, AudioSource>();
	public Dictionary<KMSoundOverride.SoundEffect, List<AudioClip>> GameSoundEffects = new Dictionary<KMSoundOverride.SoundEffect, List<AudioClip>>();

	public int StrikeCount = 3;
	public int TimeLimit = 600;
	[Range(1,22)] public int BombSize = 1;
	public bool FrontFaceOnly = false;

    public EdgeworkConfiguration EdgeworkConfiguration;
	[ReadOnlyWhenPlaying] public bool TwitchPlaysActive = true;
	[ReadOnlyWhenPlaying] public TwitchPlaysMode TwitchPlaysMode = TwitchPlaysMode.NormalMode;

	private bool _interacting;

	public float turnSpeed = 128.0f;      // Speed of camera turning when mouse moves in along an axis
	public float panSpeed = 4.0f;       // Speed of the camera when being panned
	public float zoomSpeed = 16.0f;      // Speed of the camera going back and forth

	private Vector3 mouseOrigin;    // Position of cursor when mouse dragging starts
	private bool isPanning;     // Is the camera being panned?
	private bool isRotating;    // Is the camera being rotated?
	private float mouseDownTIme;

	private Transform _camera;
	private Transform _bomb;

	private TimerModule _timer;
	private readonly List<Transform> _twitchPlayModules = new List<Transform>();

	public static TestHarness Instance;

	private List<KMBombModule> Modules;
	private List<KMNeedyModule> NeedyModules;

	void Awake()
	{
		Instance = this;

	    _camera = Camera.main.transform;
	    _camera.localPosition = new Vector3(0, 0.7f, 0);
	    _camera.localEulerAngles = new Vector3(90, 0, 0);
	    _camera.localScale = Vector3.one;
	    Camera.main.nearClipPlane = 0.01f;
	    Camera.main.farClipPlane = 3.0f;

		PrepareLights();

        fakeInfo = gameObject.AddComponent<FakeBombInfo>();
		fakeInfo.numStrikes = StrikeCount;
		fakeInfo.timeLeft = TimeLimit;
	    fakeInfo.SerialNumberWidget = SerialNumberWidget;
	    fakeInfo.BatteryWidget = BatteryWidget;
	    fakeInfo.PortWidget = PortWidget;
	    fakeInfo.IndicatorWidget = IndicatorWidget;
	    fakeInfo.TwoFactorWidget = TwoFactorWidget;
	    fakeInfo.CustomWidget = CustomWidget;
	    fakeInfo.Audio = GetComponent<KMAudio>();

		fakeInfo.SetupEdgework(EdgeworkConfiguration);

        fakeInfo.ActivateLights += delegate()
        {
            TurnLightsOn();
            fakeInfo.OnLightsOn();
	        PlaySoundEffectHandler(KMSoundOverride.SoundEffect.Switch, transform);
        };
        TurnLightsOff();

		Modules = FindObjectsOfType<KMBombModule>().ToList();
		NeedyModules = FindObjectsOfType<KMNeedyModule>().ToList();
		var allModules = Modules.ToArray().Concat<Component>(NeedyModules.ToArray());
		foreach (Component moduleComponent in allModules)
			Handlers(moduleComponent.GetComponent<KMBombInfo>());

        ReplaceBombInfo();
        AddHighlightables();
        AddSelectables();
    }

	void LogErrorAtTransform(Transform obj, string unassigned)
	{
		var str = new List<string>();
		while (obj != null)
		{
			str.Add(obj.gameObject.name);
			obj = obj.parent;
		}
		Debug.LogErrorFormat("There is an unassigned {0} on the following object: {1}", unassigned, string.Join(" > ", str.ToArray()));
	}

	Component LogReplaceBombInfoError(FieldInfo f, MonoBehaviour s)
	{
		Component component = (Component)f.GetValue(s);
		if (component == null)
		{
			var obj = s.transform;
			LogErrorAtTransform(obj,string.Format("component of type {0}", f.FieldType.Name));
		}
		return component;
	}

	void ReplaceBombInfo()
    {
		HashSet<Component> components = new HashSet<Component>();
        MonoBehaviour[] scripts = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour s in scripts)
        {
            IEnumerable<FieldInfo> fields = s.GetType().GetFields();
            foreach (FieldInfo f in fields)
            {
                if (f.FieldType == typeof(KMGameInfo))
                {
                    KMGameInfo component = (KMGameInfo)LogReplaceBombInfoError(f, s);
					if (component == null || !components.Add(component)) continue;

					component.OnLightsChange += new KMGameInfo.KMLightsChangeDelegate(fakeInfo.OnLightsChange);
                    //component.OnAlarmClockChange += new KMGameInfo.KMAlarmClockChangeDelegate(fakeInfo.OnAlarm);
                    continue;
                }
                if (f.FieldType == typeof(KMGameCommands))
                {
                    KMGameCommands component = (KMGameCommands)LogReplaceBombInfoError(f, s);
					if (component == null || !components.Add(component)) continue;

					component.OnCauseStrike += new KMGameCommands.KMCauseStrikeDelegate(fakeInfo.HandleStrike);
                    continue;
                }
            }
        }
    }

	WidgetZone CreateWidgetArea(Vector3 rot, Vector3 pos, Vector3 scale, Transform parent, List<GameObject> areaList, string name)
	{
		Transform widgetBase = new GameObject().transform;
		widgetBase.name = name;
		widgetBase.localPosition = pos;
		widgetBase.localEulerAngles = new Vector3(rot.x, rot.y, 0);
		widgetBase.SetParent(parent, false);

		Transform widgetInner = new GameObject().transform;
		widgetInner.name = "Widget Area";
		widgetInner.localEulerAngles = new Vector3(rot.z, 0, 0);
		widgetInner.localScale = scale;
		widgetInner.SetParent(widgetBase, false);
		areaList.Add(widgetInner.gameObject);

		return WidgetZone.CreateZone(widgetInner.gameObject);
	}

	void PrepareTwitchPlaysModule(Transform module)
	{
		if (!TwitchPlaysActive) return;
		_twitchPlayModules.Add(module);

		KMStatusLightParent statusLight = module.GetComponentInChildren<KMStatusLightParent>();
		TwitchPlaysID tpID = Instantiate(TwitchIDPrefab);
		tpID.FakeBombInfo = fakeInfo;
		tpID.TimerModule = fakeInfo.TimerModule;
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

	Transform PrepareModuleAnchor(List<List<Transform>> anchors, Transform module, ref int timerFace, Transform parent, bool prepareTwitchPlays = true)
	{
		module.localPosition = Vector3.zero;
		module.localRotation = Quaternion.identity;
		module.localScale = Vector3.one;
		Transform anchor = anchors[timerFace].FirstOrDefault();
		while (anchor == null && anchors.Count > 0)
		{
			anchors.Remove(anchors[timerFace]);
			timerFace = Random.Range(0, anchors.Count);
			anchor = anchors[timerFace].FirstOrDefault();
		}
		if (anchor == null) return null;

		anchors[timerFace].Remove(anchor);
		module.SetParent(anchor, false);

		if(parent != null) module.SetParent(parent, true);
		if(prepareTwitchPlays) PrepareTwitchPlaysModule(module);

		return anchor;
	}

	void PrepareBomb(List<KMBombModule> bombModules, List<KMNeedyModule> needyModules, ref List<Widget> widgets)
	{
		List<Transform> timerSideModules = new List<Transform>();
		List<Transform> modules = new List<Transform>();
		List<List<Transform>> anchors = new List<List<Transform>>();
		List<List<Transform>> timerAnchors = new List<List<Transform>>();
		List<List<Transform>> emptyAnchors = new List<List<Transform>>();
		List<WidgetZone> widgetZones = new List<WidgetZone>();

		timerSideModules.AddRange(bombModules.Where(x => x.RequiresTimerVisibility).Select(x => x.transform));
		timerSideModules.AddRange(needyModules.Where(x => x.RequiresTimerVisibility).Select(x => x.transform));

		modules.AddRange(bombModules.Where(x => !x.RequiresTimerVisibility).Select(x => x.transform));
		modules.AddRange(needyModules.Where(x => !x.RequiresTimerVisibility).Select(x => x.transform));

		KMBomb[] bombs = FindObjectsOfType<KMBomb>().OrderBy(x => Random.value).ToArray();
		KMBomb bomb;
		if (bombs.Any())
		{
			bomb = bombs[0];
			foreach (KMBomb kmbomb in bombs.Skip(1))
			{
				kmbomb.gameObject.SetActive(false);
			}

			bomb.transform.localPosition = Vector3.zero;
			bomb.transform.localRotation = Quaternion.identity;
			bomb.transform.localScale = Vector3.one;

			_bomb = bomb.transform;
			foreach (KMBombFace face in bomb.Faces)
			{
				if (FrontFaceOnly && anchors.Any())
				{
					emptyAnchors.Add(face.Anchors);
					continue;
				}
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
					module = timerSideModules[Random.Range(0, timerSideModules.Count)];
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
				DestroyImmediate(module.gameObject);
			}

			widgetZones.AddRange(bomb.WidgetAreas.Select(WidgetZone.CreateZone));

		}
		else
		{
			int square = BombSize;
			while ((square * square * (FrontFaceOnly ? 1 : 2)) < (modules.Count + timerSideModules.Count + 1))
				square++;
			float squaresize = 0.2f * square;

			Transform bombTransform = new GameObject("Bomb").transform;
			_bomb = bombTransform;

			Transform visualTransform = new GameObject("Visual Transform").transform;
			visualTransform.SetParent(bombTransform, false);

			Transform highlightTransform = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			highlightTransform.name = "Highlight";
			KMHighlightable highlightable = highlightTransform.gameObject.AddComponent<KMHighlightable>();
			DestroyImmediate(highlightTransform.GetComponent<MeshRenderer>());
			highlightTransform.localPosition = Vector3.zero;
			highlightTransform.localRotation = Quaternion.identity;
			highlightTransform.localScale = new Vector3(squaresize + 0.02f, 0.2f, squaresize + 0.02f);
			highlightTransform.SetParent(visualTransform, false);

			Rigidbody rigidBody = bombTransform.gameObject.AddComponent<Rigidbody>();
			rigidBody.isKinematic = true;
			rigidBody.useGravity = false;
			bomb = bombTransform.gameObject.AddComponent<KMBomb>();
			bomb.Faces = new List<KMBombFace>();
			bomb.WidgetAreas = new List<GameObject>();
			bomb.visualTransform = visualTransform;
			bomb.Scale = (5f / 3) / square;

			KMSelectable bombSelectable = bombTransform.gameObject.AddComponent<KMSelectable>();
			bombSelectable.Children = new KMSelectable[2];
			bombSelectable.Highlight = highlightable;
			
			bombTransform = visualTransform;

			Transform bombFaces = new GameObject("Bomb Faces").transform;
			bombFaces.SetParent(bombTransform);

			Transform bottom = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			DestroyImmediate(bottom.GetComponent<BoxCollider>());
			bottom.localScale = new Vector3(squaresize, 0.005f, squaresize);
			bottom.SetParent(bombFaces, true);

			Transform walls = new GameObject("Walls").transform;
			walls.SetParent(bombTransform);

			Transform wall = new GameObject("Right Wall").transform;
			wall.localPosition = new Vector3((squaresize / 2), 0, 0);
			wall.localEulerAngles = new Vector3(0f, -90f, 0f);
			wall.localScale = new Vector3(squaresize, 0.2f, 0.005f);
			wall.SetParent(walls, false);
			Transform wallCube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			wallCube.SetParent(wall, false);
			DestroyImmediate(wallCube.GetComponent<BoxCollider>());

			wall = new GameObject("Left Wall").transform;
			wall.localPosition = new Vector3((-squaresize / 2), 0, 0);
			wall.localEulerAngles = new Vector3(0f, -90f, 0f);
			wall.localScale = new Vector3(squaresize, 0.2f, 0.005f);
			wall.SetParent(walls, false);
			wallCube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			wallCube.SetParent(wall, false);
			DestroyImmediate(wallCube.GetComponent<BoxCollider>());

			wall = new GameObject("Top Wall").transform;
			wall.localPosition = new Vector3(0, 0, (squaresize / 2));
			wall.localEulerAngles = new Vector3(0f, -180f, 0f);
			wall.localScale = new Vector3(squaresize, 0.2f, 0.005f);
			wall.SetParent(walls, false);
			wallCube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			wallCube.transform.SetParent(wall, false);
			DestroyImmediate(wallCube.GetComponent<BoxCollider>());

			wall = new GameObject("Bottom Wall").transform;
			wall.localPosition = new Vector3(0, 0, (-squaresize / 2));
			wall.localEulerAngles = new Vector3(0f, 0f, 0f);
			wall.localScale = new Vector3(squaresize, 0.2f, 0.005f);
			wall.SetParent(walls, false);
			wallCube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			wallCube.transform.SetParent(wall, false);
			DestroyImmediate(wallCube.GetComponent<BoxCollider>());

			for (int bombFace = 0; bombFace < 2; bombFace++)
			{
				Transform bombFaceTransform = new GameObject().transform;
				KMBombFace kmbombFace = bombFaceTransform.gameObject.AddComponent<KMBombFace>();
				kmbombFace.Anchors = new List<Transform>();
				kmbombFace.TimerAnchors = new List<Transform>();
				kmbombFace.Backings = new List<KMModuleBacking>();

				KMSelectable bombFaceSelectable = bombFaceTransform.gameObject.AddComponent<KMSelectable>();
				bombFaceSelectable.Parent = bombSelectable;
				bombFaceSelectable.IsPassThrough = true;
				bombSelectable.Children[bombFace] = bombFaceSelectable;
				bombSelectable.ChildRowLength = square;

				bomb.Faces.Add(kmbombFace);

				bombFaceTransform.name = bombFace == 0 ? "Bottom Face" : "Top Face";


				anchors.Add(new List<Transform>());
				timerAnchors.Add(new List<Transform>());
				emptyAnchors.Add(new List<Transform>());


				for (float i = (squaresize / 2) - 0.1f; i > -squaresize / 2; i -= 0.2f)
				{
					for (float j = (-squaresize / 2) + 0.1f; j < squaresize / 2; j += 0.2f)
					{
						Transform anchor = new GameObject("Anchor").transform;
						kmbombFace.Anchors.Add(anchor);
						anchor.localPosition = new Vector3(j, -0.02f, i);
						anchor.SetParent(bombFaceTransform, true);
						if (!FrontFaceOnly || bombFace == 0)
						{
							anchors[bombFace].Add(anchor);
							timerAnchors[bombFace].Add(anchor);
						}
						else
						{
							emptyAnchors[bombFace].Add(anchor);
						}

						KMModuleBacking backing = Instantiate(ModuleFoamBacking);
						kmbombFace.Backings.Add(backing);
						backing.gameObject.SetActive(true);
						backing.transform.localPosition = Vector3.zero;
						backing.transform.localRotation = Quaternion.identity;
						backing.transform.localScale = Vector3.one;
						backing.transform.SetParent(anchor, false);
					}
				}

				bombFaceTransform.localPosition = new Vector3(0, (bombFace * -0.2f) + 0.1f, 0);
				bombFaceTransform.localEulerAngles = new Vector3(0, 0, (bombFace * 180f));
				bombFaceTransform.SetParent(bombFaces, true);
			}

			Transform widgetAreas = new GameObject("Widget Areas").transform;
			widgetAreas.SetParent(bombTransform);

			widgetZones.Add(CreateWidgetArea(new Vector3(0, 0, -90), new Vector3(0, 0, (-squaresize / 2) - 0.0175f), new Vector3(squaresize, 0.03f, 0.17f), widgetAreas, bomb.WidgetAreas, "Bottom Widget Area"));
			widgetZones.Add(CreateWidgetArea(new Vector3(-90, 90, 0), new Vector3((-squaresize / 2) - 0.0175f, 0, 0), new Vector3(squaresize, 0.03f, 0.17f), widgetAreas, bomb.WidgetAreas, "Lef Widget Area"));
			widgetZones.Add(CreateWidgetArea(new Vector3(-90, -90, 0), new Vector3((squaresize / 2) + 0.0175f, 0, 0), new Vector3(squaresize, 0.03f, 0.17f), widgetAreas, bomb.WidgetAreas, "Right Widget Area"));
			widgetZones.Add(CreateWidgetArea(new Vector3(0, -180, -90), new Vector3(0, 0, (squaresize / 2) + 0.0175f), new Vector3(squaresize, 0.03f, 0.17f), widgetAreas, bomb.WidgetAreas, "Top Widget Area"));

			var dupebomb = Instantiate(_bomb);
			dupebomb.gameObject.SetActive(false);
			dupebomb.transform.name = string.Format("{0}x{0} Square Bomb", square);
		}

		for (int i = 0; i < anchors.Count; i++)
			anchors[i] = anchors[i].OrderBy(x => Random.value).ToList();
		for (int i = 0; i < timerAnchors.Count; i++)
			timerAnchors[i] = timerAnchors[i].OrderBy(x => Random.value).ToList();

		int timerFace = Random.Range(0, timerAnchors.Count);
		_timer = Instantiate(fakeInfo.numStrikes == 1 ? StrikelessTimerModulePrefab : TimerModulePrefab);
		_timer.TimeRemaining = TimeLimit;
		_timer.gameObject.SetActive(true);

		Transform timerAnchor = PrepareModuleAnchor(timerAnchors, _timer.transform, ref timerFace, _bomb, false);
		anchors[timerFace].Remove(timerAnchor);

		_timer.OnStartEmergencyLights += delegate 
		{
			_emergencyLightsActivated = true;
			StartCoroutine(CycleEmergencyLights());
		};

		_timer.OnStopEmergencyLights += delegate { _emergencyLightsActivated = false; };

		fakeInfo.TimerModule = _timer;

		if (TwitchPlaysMode == TwitchPlaysMode.TimeMode)
		{
			_timer.TimeMode = true;
			TwitchPlaysID.TimeMode = true;
		}
		else if (TwitchPlaysMode == TwitchPlaysMode.ZenMode)
		{
			_timer.ZenMode = true;
			TwitchPlaysID.ZenMode = true;
		}

		Transform modulesTransform = new GameObject("Modules").transform;
		modulesTransform.SetParent(_bomb, true);
		foreach (Transform module in timerSideModules)
		{
			Transform anchor = PrepareModuleAnchor(anchors, module, ref timerFace, modulesTransform);

			if (anchor != null) continue;
			bombModules.Remove(transform.GetComponent<KMBombModule>());
			needyModules.Remove(transform.GetComponent<KMNeedyModule>());
		}

		foreach (Transform module in modules)
		{
			timerFace = Random.Range(0, anchors.Count);
			Transform anchor = PrepareModuleAnchor(anchors, module, ref timerFace, modulesTransform);

			if (anchor != null) continue;
			bombModules.Remove(transform.GetComponent<KMBombModule>());
			needyModules.Remove(transform.GetComponent<KMNeedyModule>());
		}

		while (anchors.Sum(x => x.Count) > 0)
		{
			Transform cover = Instantiate(ModuleCoverPrefab);
			cover.gameObject.SetActive(true);
			PrepareModuleAnchor(anchors, cover, ref timerFace, null, false);
		}

		while (emptyAnchors.Sum(x => x.Count) > 0)
		{
			Transform cover = Instantiate(ModuleCoverPrefab);
			cover.gameObject.SetActive(true);
			PrepareModuleAnchor(emptyAnchors, cover, ref timerFace, null, false);
		}

		Transform widgetsTransform = new GameObject("Widgets").transform;
		widgetsTransform.SetParent(_bomb);

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
				widget.transform.parent = widgetsTransform;
				continue;
			}
			if (i == 0)
				continue;

			widgets.Remove(widget);
			Destroy(widget.gameObject);
			i--;
		}

		bomb.transform.localScale = Vector3.one * bomb.Scale;
	}

	StatusLight CreateStatusLight(Transform module)
	{
		KMStatusLightParent statuslightparent = module.GetComponentInChildren<KMStatusLightParent>();
		if (statuslightparent == null) return null;
		var statuslight = Instantiate<StatusLight>(StatusLightPrefab);
		statuslight.transform.SetParent(statuslightparent.transform, false);
		statuslight.transform.localPosition = Vector3.zero;
		statuslight.transform.localScale = Vector3.one;
		statuslight.transform.localRotation = Quaternion.identity;
		statuslight.SetInActive();
		return statuslight;
	}

    void Handlers(KMBombInfo component)
    {
        component.TimeHandler += new KMBombInfo.GetTimeHandler(fakeInfo.GetTime);
        component.FormattedTimeHandler += new KMBombInfo.GetFormattedTimeHandler(fakeInfo.GetFormattedTime);
        component.StrikesHandler += new KMBombInfo.GetStrikesHandler(fakeInfo.GetStrikes);
        component.ModuleNamesHandler += new KMBombInfo.GetModuleNamesHandler(fakeInfo.GetModuleNames);
        component.SolvableModuleNamesHandler += new KMBombInfo.GetSolvableModuleNamesHandler(fakeInfo.GetSolvableModuleNames);
        component.SolvedModuleNamesHandler += new KMBombInfo.GetSolvedModuleNamesHandler(fakeInfo.GetSolvedModuleNames);
        component.WidgetQueryResponsesHandler += new KMBombInfo.GetWidgetQueryResponsesHandler(fakeInfo.GetWidgetQueryResponses);
        component.IsBombPresentHandler += new KMBombInfo.KMIsBombPresent(fakeInfo.IsBombPresent);
    }

    void UpdateRoot(TestSelectable currentSelectable)
    {
        currentSelectable.Children = new TestSelectable[Modules.Count + NeedyModules.Count];
        currentSelectable.ChildRowLength = currentSelectable.Children.Length;
        var allModules = Modules.ToArray().Concat<Component>(NeedyModules.ToArray()).ToList();
        for (int i = 0; i < allModules.Count; i++)
        {
            TestSelectable testSelectable = allModules[i].GetComponent<TestSelectable>();
            currentSelectable.Children[i] = testSelectable;
            testSelectable.Parent = currentSelectable;
            testSelectable.x = i;
        }
    }


    void Start()
    {
        currentSelectable = GetComponent<TestSelectable>();

		List<KMBombModule> modules = Modules;
        List<KMNeedyModule> needyModules = NeedyModules;
	    PrepareBomb(modules, needyModules, ref fakeInfo.widgets);

	    fakeInfo.TimerModule = _timer;
        fakeInfo.needyModules = needyModules.ToList();
        UpdateRoot(GetComponent<TestSelectable>());
        for (int i = 0; i < modules.Count; i++)
        {
            KMBombModule mod = modules[i];
	        StatusLight statuslight = CreateStatusLight(mod.transform);

            fakeInfo.modules.Add(new KeyValuePair<KMBombModule, bool>(modules[i], false));
            modules[i].OnPass = delegate ()
            {
	            KeyValuePair<KMBombModule, bool> kvp = fakeInfo.modules.First(t => t.Key.Equals(mod));
	            if (kvp.Value) return false;

				Debug.Log("Module Passed");
				if(statuslight != null) statuslight.SetPass();

                fakeInfo.modules.Remove(kvp);
                fakeInfo.modules.Add(new KeyValuePair<KMBombModule, bool>(mod, true));

                if (fakeInfo.modules.All(x => x.Value)) fakeInfo.Solved();
                return false;
            };

	        int j = i;
            modules[i].OnStrike = delegate ()
            {
                Debug.Log("Strike");
				if(statuslight != null) statuslight.FlashStrike();
                fakeInfo.HandleStrike(modules[j].ModuleDisplayName);
                return false;
            };
        }

        for (int i = 0; i < needyModules.Count; i++)
        {
	        KMNeedyModule needyModule = needyModules[i];

	        StatusLight statusLight = CreateStatusLight(needyModule.transform);
			NeedyTimer needyTimer = Instantiate(NeedyTimerPrefab);
	        needyTimer.transform.localPosition = Vector3.zero;
	        needyTimer.transform.localRotation = Quaternion.identity;
	        needyTimer.transform.localScale = Vector3.one;
	        needyTimer.ParentComponent = needyModule;

	        needyModule.OnPass = delegate ()
            {
                Debug.Log("Module Passed");
	            needyTimer.StopTimer();
                return false;
            };
	        needyModule.OnStrike = delegate ()
            {
                Debug.Log("Strike");
	            if (statusLight != null) statusLight.FlashStrike();
                fakeInfo.HandleStrike(needyModule.ModuleDisplayName);
                return false;
            };

	        needyTimer.TotalTime = needyModule.CountdownTime;
	        needyModule.GetNeedyTimeRemainingHandler += needyTimer.GetTimeRemaining;
	        needyModule.SetNeedyTimeRemainingHandler += needyTimer.SetTimeRemaining;

	        needyTimer.transform.SetParent(needyTimer.ParentComponent.transform, false);
	        needyTimer.transform.gameObject.SetActive(true);

        }

        currentSelectable.ActivateChildSelectableAreas();

	    Transform audioSouceTransforms = new GameObject().transform;
	    audioSouceTransforms.name = "Audio Sources";
	    audioSouceTransforms.parent = transform;

        audioSource = new GameObject().AddComponent<AudioSource>();
	    audioSource.transform.name = "PlaySoundHandler";
	    audioSource.transform.parent = audioSouceTransforms;
        KMAudio[] kmAudios = FindObjectsOfType<KMAudio>();
        foreach (KMAudio kmAudio in kmAudios)
        {
            kmAudio.HandlePlaySoundAtTransform += PlaySoundHandler;
			kmAudio.HandlePlaySoundAtTransformWithRef += PlaySoundHandler;
	        kmAudio.HandlePlayGameSoundAtTransform += (effect,t) => PlaySoundEffectHandler(effect,t);
			kmAudio.HandlePlayGameSoundAtTransformWithRef += PlaySoundEffectHandler;
        }


	    KMSoundOverride[] kmSoundOverrides = FindObjectsOfType<KMSoundOverride>();
		foreach (KMSoundOverride.SoundEffect effect in Enum.GetValues(typeof(KMSoundOverride.SoundEffect)))
	    {
			AudioSource effectAudioSource = new GameObject().AddComponent<AudioSource>();
		    effectAudioSource.transform.name = "SoundEffect." + effect;
		    effectAudioSource.transform.parent = audioSouceTransforms;
		    effectAudioSource.loop = effect == KMSoundOverride.SoundEffect.NeedyWarning ||
		                             effect == KMSoundOverride.SoundEffect.AlarmClockBeep;

			GameSoundEffectSources[effect] = effectAudioSource;
		    GameSoundEffects[effect] = new List<AudioClip>();

		    foreach (var kmSoundOverride in kmSoundOverrides.Where(x => x.OverrideEffect == effect))
		    {
			    if (kmSoundOverride.AudioClip != null)
				    GameSoundEffects[effect].Add(kmSoundOverride.AudioClip);
			    GameSoundEffects[effect].AddRange(kmSoundOverride.AdditionalVariants.Where(x => x != null));
			}
	    }
    }

	protected void PlaySoundHandler(string clipName, Transform t)
    {
        AudioClip clip = AudioClips == null ? null : AudioClips.FirstOrDefault(a => a.name == clipName);

        if (clip != null)
        {
	        audioSource.volume = AudioVolume;
			audioSource.loop = false;
			audioSource.transform.position = t.position;
            audioSource.PlayOneShot(clip);
        }
        else
            Debug.Log("Audio clip not found: " + clipName);
    }

	private KMAudio.KMAudioRef PlaySoundHandler(string clipName, Transform t, bool loop)
	{
		KMAudio.KMAudioRef audioRef = new KMAudio.KMAudioRef {StopSound = () => { }};

		AudioClip clip = AudioClips == null ? null : AudioClips.FirstOrDefault(a => a.name == clipName);

		if (clip != null)
		{
			audioSource.volume = AudioVolume;
			audioSource.transform.position = t.position;
			audioSource.loop = loop;
			audioSource.clip = clip;
			audioSource.Play();
			audioRef.StopSound = () => { audioSource.Stop(); };
		}
		else
			Debug.Log("Audio clip not found: " + clipName);

		return audioRef;
	}

	private KMAudio.KMAudioRef PlaySoundEffectHandler(KMSoundOverride.SoundEffect effect, Transform t)
	{
		KMAudio.KMAudioRef audioRef = new KMAudio.KMAudioRef();
		List<AudioClip> clips;
		AudioSource source;
		if (!GameSoundEffects.TryGetValue(effect, out clips) || !GameSoundEffectSources.TryGetValue(effect, out source) || clips.Count == 0)
		{
			audioRef.StopSound = () => { };
			return audioRef;
		}

		AudioClip clip = clips[Random.Range(0, clips.Count)];
		if (clip != null)
		{
			source.volume = AudioVolume;
			source.transform.position = t.position;
			if (source.loop)
			{
				source.clip = clip;
				source.Play();
			}
			else
			{
				source.PlayOneShot(clip);
			}
			audioRef.StopSound = () => { if(source.loop) source.Stop(); };
		}

		return audioRef;
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
		if (Mathf.Abs(mouseWheel) > 0.000000001f)
		{
			Camera.main.fieldOfView += (-mouseWheel * zoomSpeed);
		}

		if (!gamepadEnabled)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction);
            RaycastHit hit;
            int layerMask = 1 << 11;
            bool rayCastHitSomething = Physics.Raycast(ray, out hit, 1000, layerMask);

			if (rayCastHitSomething && !_interacting) {
                TestSelectableArea hitArea = hit.collider.GetComponent<TestSelectableArea>();
                if (hitArea != null)
                {
                    if (currentSelectableArea != hitArea)
                    {
                        if (currentSelectableArea != null)
                        {
                            currentSelectableArea.Selectable.Deselect();
	                        currentSelectableArea = null;
						}

	                    if (hitArea.transform.eulerAngles.z > 270.0f || hitArea.transform.eulerAngles.z < 90.0f)
	                    {
		                    PlaySoundEffectHandler(KMSoundOverride.SoundEffect.SelectionTick, hitArea.Selectable.transform);
		                    hitArea.Selectable.Select();
		                    currentSelectableArea = hitArea;
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
            }
            else if (!_interacting)
            {
                if (currentSelectableArea != null)
                {
                    currentSelectableArea.Selectable.Deselect();
                    currentSelectableArea = null;
                }
            }

	        if (Input.GetMouseButtonDown(0))
	        {
		        _interacting = true;
		        Interact();
	        }

	        if (Input.GetMouseButtonUp(0))
	        {
		        _interacting = false;
		        InteractEnded();
	        }
	        if (Input.GetMouseButtonDown(1))
	        {
		        mouseDownTIme = 0;
	        }

	        if (Input.GetMouseButtonUp(1) && mouseDownTIme < 0.25f)
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
	    TestSelectable root = GetComponent<TestSelectable>();
		TestSelectable selectable = lastSelected.GetNearestSelectable(direction);
        if (selectable)
        {
            lastSelected = selectable;
            currentSelectable.LastSelectedChild = lastSelected;
	        if (root.Children.Contains(selectable))
		        MoveCamera(selectable);
        }
    }

	public static IEnumerator MoveCamera(Transform destination)
	{
		const float moveTime = 0.125f;
		float startTime = Time.time;

		float front = destination.eulerAngles.z < 90 || destination.eulerAngles.z > 270 ? 0 : 180;
		float back = destination.eulerAngles.z < 90 || destination.eulerAngles.z > 270 ? 180 : 0;

		Vector3 bombOrigin = Instance._bomb.localEulerAngles;
		Vector3 cameraOrigin = Instance._camera.localPosition;

		Vector3 bombDestination = new Vector3(0, 0, (bombOrigin.z >= 270.01f || bombOrigin.z <= 90f) ? front : back);
		Instance._bomb.localEulerAngles = bombDestination;

		Vector3 cameraDestination = new Vector3(destination.position.x, Instance._camera.localPosition.y, destination.position.z);
		Instance._bomb.localEulerAngles = bombOrigin;
		
		while ((Time.time - startTime) < moveTime)
		{
			Instance._bomb.rotation = Quaternion.Lerp(Quaternion.Euler(bombOrigin), Quaternion.Euler(bombDestination), (Time.time - startTime) / moveTime);
			Instance._camera.localPosition = Vector3.Lerp(cameraOrigin, cameraDestination, (Time.time - startTime) / moveTime);
			yield return null;
		}

		Instance._bomb.localEulerAngles = bombDestination;
		Instance._camera.localPosition = cameraDestination;
	}

	void MoveCamera(TestSelectable selectable)
	{
		if (selectable.GetComponent<TestHarness>() != null)
		{
			StartCoroutine(MoveCamera(selectable.transform));
		}
		else if (selectable.Parent != null && selectable.Parent.GetComponent<TestHarness>() != null)
		{
			StartCoroutine(MoveCamera(selectable.transform));
		}
	}

	void Interact()
	{
		TestSelectable root = GetComponent<TestSelectable>();

        if (currentSelectableArea != null && currentSelectableArea.Selectable.Interact())
        {
	        MoveCamera(currentSelectableArea.Selectable);
            currentSelectable.DeactivateChildSelectableAreas(currentSelectableArea.Selectable);
            currentSelectable = currentSelectableArea.Selectable;
	        if (root.Children.Contains(currentSelectable))
		        currentModule = currentSelectable;

	        GetComponent<TestSelectable>().ActivateChildSelectableAreas();
	        if (currentModule != null) currentModule.SelectableArea.DeactivateSelectableArea();

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
	        MoveCamera(currentSelectable.Parent);
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
	        TestHighlightable highlight = highlightable.GetComponent<TestHighlightable>();
	        if (highlight != null) continue;

			highlight = highlightable.gameObject.AddComponent<TestHighlightable>();

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
		        TestSelectable testSelectable = selectable.GetComponent<TestSelectable>();
		        if (testSelectable != null) continue;
		        testSelectable = selectable.gameObject.AddComponent<TestSelectable>();

				selectable.OnUpdateChildren += select => {
					AddHighlightables();
					AddSelectables();

					if (currentSelectable == testSelectable)
					{
						currentSelectable.ActivateChildSelectableAreas();

						if (select != null)
							select.GetComponent<TestSelectable>().Select();
					}

				};

				if (selectable.transform.name.Equals("Bottom Face") || selectable.transform.name.Equals("Top Face"))
					continue;
				else if (selectable.Highlight == null)
				{
					LogErrorAtTransform(selectable.transform, "KMSelectable.Highlight");
				}
				else
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
            if (selectable.Children == null) continue;
            testSelectable.Children = new TestSelectable[selectable.Children.Length];
            for (int i = 0; i < selectable.Children.Length; i++)
            {
                if (selectable.Children[i] != null)
                {
                    testSelectable.Children[i] = selectable.Children[i].GetComponent<TestSelectable>();
                }
            }
        }
        UpdateRoot(GetComponent<TestSelectable>());
    }

    
    

    

    string command = "";
    void OnGUI()
    {
        if (GUILayout.Button("Activate Needy Modules"))
        {
	        foreach (NeedyTimer needyModule in fakeInfo.needyModuleTimers)
	        {
		        needyModule.StartTimer();
	        }
        }

        if (GUILayout.Button("Deactivate Needy Modules"))
        {
            foreach (NeedyTimer needyModule in fakeInfo.needyModuleTimers)
            {
	            needyModule.StopTimer(NeedyTimer.NeedyState.InitialSetup);
            }
        }

        if (GUILayout.Button("Lights On"))
        {
            TurnLightsOn();
            fakeInfo.OnLightsOn();
	        PlaySoundEffectHandler(KMSoundOverride.SoundEffect.LightBuzzShort, transform);
        }

        if (GUILayout.Button("Lights Off"))
        {
            TurnLightsOff();
            fakeInfo.OnLightsOff();
	        PlaySoundEffectHandler(KMSoundOverride.SoundEffect.LightBuzz, transform);
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

	    if (TwitchPlaysActive)
	    {
		    GUILayout.Space(10);
		    TwitchPlaysID.AntiTrollMode = GUILayout.Toggle(TwitchPlaysID.AntiTrollMode, "Troll Commands Disabled");
		    TwitchPlaysID.AnarchyMode = GUILayout.Toggle(TwitchPlaysID.AnarchyMode, "Anarchy Mode Enabled");

		    GUI.SetNextControlName("commandField");
		    command = GUILayout.TextField(command);
		    if ((GUILayout.Button("Simulate Twitch Command") || Event.current.keyCode == KeyCode.Return) &&
		        GUI.GetNameOfFocusedControl() == "commandField" && command != "")
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
    }

	private Light _emergencyLight;
	private bool _emegerncyLightsOn;
	private bool _emergencyLightsActivated;
	public IEnumerator CycleEmergencyLights()
	{
		while (_emergencyLightsActivated)
		{
			TurnOnEmergencyLights();
			yield return new WaitForSeconds(1.0f);
			TurnOffEmergencyLights();
			yield return new WaitForSeconds(Mathf.Lerp(0.75f, 2.0f, fakeInfo.timeLeft / 60.0f));
		}
	}

	void TurnOnEmergencyLights()
	{
		_emegerncyLightsOn = true;
		UpdateAmbientIntensity();
		PlaySoundEffectHandler(KMSoundOverride.SoundEffect.EmergencyAlarm, transform);
	}

	void TurnOffEmergencyLights()
	{
		_emegerncyLightsOn = false;
		UpdateAmbientIntensity();
	}

	private bool lightsOn;
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

	    o = new GameObject("Emergency Light");
	    o.transform.localPosition = new Vector3(0, 3, 0);
	    o.transform.localRotation = Quaternion.Euler(new Vector3(130, -30, 0));
	    _emergencyLight = o.AddComponent<Light>();
	    _emergencyLight.type = LightType.Directional;
	    _emergencyLight.color = Color.red;
	    _emergencyLight.enabled = false;
    }

	public void UpdateAmbientIntensity()
	{
		RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
		RenderSettings.ambientIntensity = lightsOn && !_emegerncyLightsOn ? 1f : 0.1f;
		DynamicGI.UpdateEnvironment();
		testLight.enabled = lightsOn && !_emegerncyLightsOn;
		_emergencyLight.enabled = _emegerncyLightsOn;
	}

    public void TurnLightsOn()
    {
	    lightsOn = true;
	    UpdateAmbientIntensity();
    }

    public void TurnLightsOff()
    {
	    lightsOn = false;
		UpdateAmbientIntensity();
	}
}

