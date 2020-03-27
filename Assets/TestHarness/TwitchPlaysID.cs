using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class TwitchPlaysID : MonoBehaviour
{
	private static int counter = 1;
	public int ModuleID;
	public TextMesh IDTextMesh;
	public Transform Module;

	public KMBombModule BombModule;
	public KMNeedyModule NeedyModule;

	public GameObject IDNumber;
	public GameObject Unsupported;
	public static TPCoroutineQueue TPCoroutineQueue;

	public Component TwitchCommandComponent;
	public MethodInfo ProcessTwitchCommandMethod;
	public MethodInfo TwitchForcedSolveMethod;
	public FieldInfo TwitchCancelField;
	public FieldInfo TwitchHelpMessageField;
	public FieldInfo TwitchManualCodeField;
	public FieldInfo TwitchValidCommandsField;
	public FieldInfo TwitchModuleSolveScoreField;
	public FieldInfo TwitchModuleStrikeScoreField;
	public FieldInfo TwitchTimeModeField;
	public FieldInfo TwitchZenModeField;
	public FieldInfo TwitchModeField;
	public FieldInfo TwitchSkipTimeAllowedField;

	public TimerModule TimerModule;
	public FakeBombInfo FakeBombInfo;

	private static List<TwitchPlaysID> TwitchPlaysModules = new List<TwitchPlaysID>();

	public bool Solvable { get { return BombModule != null; } }
	public bool Solved;
	public int StrikeCount;
	public bool TimeSkippingAllowed { get { return GetBool(TwitchSkipTimeAllowedField) ?? false; } }

	public string HelpMessage;
	public string ManualCode;
	public int ModuleSolveScore = 5;
	public int ModuleStrikeScore = -6;

	public List<Transform> ModuleCameras = new List<Transform>();
	private static List<Transform> _moduleCameras;
	private static List<Transform> _moduleCamerasInUse = new List<Transform>();
	private Transform _moduleCamera;

	public static bool AntiTrollMode = true;
	public static bool AnarchyMode;
	public static bool TimeMode;
	public static bool ZenMode;
	

	private bool HandleStrike()
	{
		if (triedToSolve)
			StopEverything();
		StrikeCount++;
		TimerModule.UpdateTimeModeTime(-6, true);
		return true;
	}

	private void StopEverything()
	{
		if (BombModule != null)
		{
			BombModule.HandlePass();
		}

		if (NeedyModule != null)
		{
			NeedyModule.HandlePass();
			NeedyModule.OnNeedyActivation = null;
			NeedyModule.OnNeedyDeactivation = null;
			NeedyModule.OnTimerExpired = null;
		}

		foreach (MonoBehaviour monoBehaviour in Module.GetComponentsInChildren<MonoBehaviour>(true))
		{
			monoBehaviour.StopAllCoroutines();
		}
	}

	private bool HandlePass()
	{
		if (Solved) return true;
		Solved = true;
		TimerModule.UpdateTimeModeTime(5, false);
		if(!AnarchyMode)
			UnviewCamera();
		return true;
	}

	private string GetModuleDisplayName()
	{
		if (BombModule != null)
			return BombModule.ModuleDisplayName;
		if (NeedyModule != null)
			return NeedyModule.ModuleDisplayName;
		return null;
	}

	private bool triedToSolve;
	private IEnumerator ForceSolveModule()
	{
		if (!triedToSolve)
		{
			MethodInfo method = TwitchForcedSolveMethod;
			Component component = TwitchCommandComponent;

			triedToSolve = NeedyModule == null || TwitchForcedSolveMethod == null;
			if (TwitchForcedSolveMethod == null)
			{
				if (BombModule != null)
				{
					BombModule.HandlePass();
				}

				if (NeedyModule != null)
				{
					NeedyModule.HandlePass();
				}
			}
			else
			{
				string methodDeclaringTypeFullName = null;
				if (method.DeclaringType != null)
					methodDeclaringTypeFullName = method.DeclaringType.FullName;

				if (method.ReturnType == typeof(IEnumerator))
				{
					IEnumerator responseCoroutine = null;
					try
					{
						responseCoroutine = (IEnumerator)method.Invoke(component, new object[] {  });
					}
					catch (System.Exception ex)
					{
						Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name);
						Debug.LogException(ex);
						StopEverything();
						yield break;
					}

					bool forceSolve;
					while (responseCoroutine.TryMoveNext(out forceSolve, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name)) ?? false)
						yield return responseCoroutine.Current;
				}
				else
				{
					try
					{
						method.Invoke(component, new object[] { });
					}
					catch (System.Exception ex)
					{
						Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name);
						Debug.LogException(ex);
						StopEverything();
						yield break;
					}
				}
			}
			
		}
		else
		{
			StopEverything();
		}

		yield break;
	}
	
	private void SolveModule()
	{
		TPCoroutineQueue.AddForcedSolve(ForceSolveModule());
	}

	private void Start()
	{
		if (TPCoroutineQueue == null)
			TPCoroutineQueue = new GameObject().AddComponent<TPCoroutineQueue>();

		if (Module == null)
			return;

		ModuleID = counter++;
		IDTextMesh.text = ModuleID.ToString();
		FindTwitchCommandMethod();
		transform.gameObject.SetActive(true);
		
		BombModule = Module.GetComponent<KMBombModule>();
		NeedyModule = Module.GetComponent<KMNeedyModule>();
		if (BombModule != null)
		{
			BombModule.OnPass += HandlePass;
			BombModule.OnStrike += HandleStrike;
		}
		else if (NeedyModule != null)
		{
			NeedyModule.OnStrike += HandleStrike;
		}

		TwitchPlaysModules.Add(this);
		if (_moduleCameras == null) _moduleCameras = ModuleCameras;
	}

	private void Update()
	{
		if (Module == null)
			gameObject.SetActive(false);

		Unsupported.SetActive(ProcessTwitchCommandMethod == null && (!Solved || AnarchyMode));
		IDNumber.SetActive(!Solved || AnarchyMode);
	}

	public void ProcessCommand(string command)
	{

		var matches = Regex.Match(command, string.Format(@"!{0} (.+)", ModuleID));
		if (matches.Success)
		{
			string internalCommand = matches.Groups[1].Value.ToLowerInvariant();

			if (internalCommand.Equals("solve"))
			{
				SolveModule();
			}
			else if (internalCommand.Equals("help") || internalCommand.Equals("manual"))
			{
				string helpMessage = GetString(TwitchHelpMessageField);
				string manualCode = GetString(TwitchManualCodeField);
				if (manualCode == null)
				{
					if (BombModule != null)
						manualCode = BombModule.ModuleDisplayName;
					else if (NeedyModule != null)
						manualCode = NeedyModule.ModuleDisplayName;
					else
						manualCode = "<null>";
				}

				if (helpMessage == null)
					helpMessage = "No help for !{0}. | " + manualCode;
				else
					helpMessage += " | " + manualCode;

				Debug.LogFormat(helpMessage, ModuleID);
			}

			else
			{
				string[] validCommands = GetStrings(TwitchValidCommandsField);
				if(validCommands == null || validCommands.Length == 0 || validCommands.Any(x => Regex.IsMatch(matches.Groups[1].Value, x)))
					TPCoroutineQueue.AddToQueue(SimulateModule(matches.Groups[1].Value));
			}
		}

		if (command.ToLowerInvariant().Equals("!solvebomb"))
		{
			SolveModule();
		}
	}

	protected void SetBool(FieldInfo boolField, bool val)
	{
		if (boolField == null || TwitchCommandComponent == null || boolField.FieldType != typeof(bool)) return;
		if (boolField.IsStatic)
		{
			boolField.SetValue(null, val);
		}
		else
		{
			boolField.SetValue(TwitchCommandComponent, val);
		}
	}

	protected bool? GetBool(FieldInfo boolField)
	{
		bool result = boolField != null && TwitchCommandComponent != null && boolField.FieldType == typeof(bool);
		if (!result) return null;
		if (boolField.IsStatic)
			return (bool) boolField.GetValue(null);
		return (bool) boolField.GetValue(TwitchCommandComponent);
	}

	protected string GetString(FieldInfo stringField)
	{
		bool result = stringField != null && TwitchCommandComponent != null && stringField.FieldType == typeof(string);
		if (!result) return null;
		if (stringField.IsStatic)
			return (string)stringField.GetValue(null);
		return (string)stringField.GetValue(TwitchCommandComponent);
	}

	protected string[] GetStrings(FieldInfo stringField)
	{
		bool result = stringField != null && TwitchCommandComponent != null && stringField.FieldType == typeof(string[]);
		if (!result) return null;
		if (stringField.IsStatic)
			return (string[])stringField.GetValue(null);
		return (string[])stringField.GetValue(TwitchCommandComponent);
	}

	protected int? GetInt(FieldInfo intField)
	{
		bool result = intField != null && TwitchCommandComponent != null && intField.FieldType == typeof(int);
		if (!result) return null;
		if (intField.IsStatic)
			return (int)intField.GetValue(null);
		return (int)intField.GetValue(TwitchCommandComponent);
	}

	protected void FindTwitchCommandMethod()
	{
		if (Module == null) return;
		Component[] allComponents = Module.gameObject.GetComponentsInChildren<Component>(true);
		foreach (Component component in allComponents)
		{
			System.Type type = component.GetType();
			MethodInfo method = type.GetMethod("ProcessTwitchCommand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo forceSolveMethod = type.GetMethod("TwitchHandleForcedSolve", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null && forceSolveMethod == null) continue;

			TwitchCommandComponent = component;
			ProcessTwitchCommandMethod = method;
			TwitchForcedSolveMethod = forceSolveMethod;

			TwitchCancelField = type.GetField("TwitchShouldCancelCommand", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			TwitchModeField = type.GetField("TwitchPlaysActive", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			TwitchTimeModeField = type.GetField("TimeModeActive", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			TwitchZenModeField = type.GetField("ZenModeActive", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			TwitchHelpMessageField = type.GetField("TwitchHelpMessage", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			TwitchManualCodeField = type.GetField("TwitchManualCode", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			TwitchValidCommandsField = type.GetField("TwitchValidCommands", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			TwitchModuleSolveScoreField = type.GetField("TwitchModuleScore", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			TwitchModuleStrikeScoreField = type.GetField("TwitchStrikePenalty", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			TwitchSkipTimeAllowedField = type.GetField("TwitchPlaysSkipTimeAllowed", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			SetBool(TwitchModeField, true);
			SetBool(TwitchTimeModeField, TimeMode);
			SetBool(TwitchZenModeField, ZenMode);

			break;
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


	private bool _zoomed;
	private Rect _originalCameraRect;
	private readonly Rect _zoomCameraLocation = new Rect(0.2738095f, 0.12f, 0.452381f, 0.76f);
	public IEnumerator ZoomCamera(float duration = 1.0f)
	{
		if (_moduleCamera == null) yield break;
		var cameraInstance = _moduleCamera.GetComponentInChildren<Camera>();
		if (cameraInstance == null) yield break;
		_zoomed = true;
		_originalCameraRect = cameraInstance.rect;
		cameraInstance.depth = 100;
		yield return null;
		float initialTime = Time.time;
		while ((Time.time - initialTime) < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			cameraInstance.rect = new Rect(Mathf.Lerp(_originalCameraRect.x, _zoomCameraLocation.x, lerp),
				Mathf.Lerp(_originalCameraRect.y, _zoomCameraLocation.y, lerp),
				Mathf.Lerp(_originalCameraRect.width, _zoomCameraLocation.width, lerp),
				Mathf.Lerp(_originalCameraRect.height, _zoomCameraLocation.height, lerp));

			yield return null;
		}
		cameraInstance.rect = _zoomCameraLocation;
	}

	public IEnumerator UnZoomCamera(float duration = 1.0f)
	{
		if (_moduleCamera == null) yield break;
		if (!_zoomed) yield break;
		var cameraInstance = _moduleCamera.GetComponentInChildren<Camera>();
		yield return null;
		float initialTime = Time.time;
		while ((Time.time - initialTime) < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			cameraInstance.rect = new Rect(Mathf.Lerp(_zoomCameraLocation.x, _originalCameraRect.x, lerp),
				Mathf.Lerp(_zoomCameraLocation.y, _originalCameraRect.y, lerp),
				Mathf.Lerp(_zoomCameraLocation.width, _originalCameraRect.width, lerp),
				Mathf.Lerp(_zoomCameraLocation.height, _originalCameraRect.height, lerp));

			yield return null;
		}
		cameraInstance.rect = _originalCameraRect;
		cameraInstance.depth = 99;
	}

	private void UnviewCamera()
	{
		if (_moduleCamera == null) return;
		_moduleCamerasInUse.Remove(_moduleCamera);
		_moduleCameras.Add(_moduleCamera);
		_moduleCamera.gameObject.SetActive(false);
		_moduleCamera = null;
	}

	private void AttachCameraToModule()
	{
		if (_moduleCamera != null) return;
		Transform t;
		if (_moduleCameras.Any())
		{
			t = _moduleCameras[0];
			_moduleCameras.Remove(t);
			_moduleCamerasInUse.Add(t);
		}
		else if (_moduleCamerasInUse.Any())
		{
			t = _moduleCamerasInUse[0];
			_moduleCamerasInUse.Remove(t);
			_moduleCamerasInUse.Add(t); //Put the camera to back of the line.
		}
		else
		{
			Debug.Log("There are no available cameras to attach to the module");
			return;
		}

		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;
		t.localScale = Vector3.one;
		t.gameObject.SetActive(true);

		var module = TwitchPlaysModules.FirstOrDefault(x => x._moduleCamera == t);
		if (module != null) module._moduleCamera = null;
		_moduleCamera = t;

		if (BombModule != null) t.SetParent(BombModule.transform, false);
		else if (NeedyModule != null) t.SetParent(NeedyModule.transform, false);
		else
		{
			Debug.Log("This should never happen, but apparently this TwitchPlays instance was spawned without a bomb module nor a needy module");
			t.gameObject.SetActive(false);
			_moduleCamerasInUse.Remove(t);
			_moduleCameras.Insert(0, t);
			_moduleCamera = null;
			return;
		}
	}

	private bool _responded;
	private bool _zoom;
	private IEnumerator RespondToCommandCommon(string inputCommand)
	{
		_zoom = false;
		_responded = false;
		inputCommand = inputCommand.Trim();
		if (inputCommand.Equals("unview", StringComparison.InvariantCultureIgnoreCase))
		{
			_responded = true;
			UnviewCamera();
		}
		else
		{
			if (inputCommand.StartsWith("view", StringComparison.InvariantCultureIgnoreCase))
			{
				_responded = true;
			}
			AttachCameraToModule();

			Match match;
			if (inputCommand.RegexMatch(out match, "^zoom(?: ([0-9]+(?:\\.[0-9])?))?$"))
			{
				float delay;
				if (match.Groups.Count == 1 || !float.TryParse(match.Groups[1].Value, out delay))
					delay = 2;
				delay = Math.Max(2, delay);
				_zoom = true;
				yield return null;
				if (delay >= 15)
					yield return "elevator music";
				yield return string.Format("trywaitcancel {0} Your request to hold up the bomb for {0} seconds has been cut short.", delay);
			}

			if (inputCommand.StartsWith("zoom ", StringComparison.InvariantCultureIgnoreCase))
				_zoom = true;
		}

		if (inputCommand.Equals("show", StringComparison.InvariantCultureIgnoreCase))
		{
			yield return "show";
			yield return null;
		}
		else if (inputCommand.Equals("solve"))
		{
			SolveModule();
			_responded = true;
		}
	}

	IEnumerator RespondToCommandInternalSimple(string command)
	{
		if (ProcessTwitchCommandMethod == null) yield break;
		MethodInfo method = ProcessTwitchCommandMethod;
		Component component = TwitchCommandComponent;

		string methodDeclaringTypeFullName = null;
		if (method.DeclaringType != null)
			methodDeclaringTypeFullName = method.DeclaringType.FullName;

		IEnumerable<KMSelectable> selectableSequence = null;
		try
		{
			selectableSequence = (IEnumerable<KMSelectable>)method.Invoke(component, new object[] { command });
			if (selectableSequence == null)
			{
				_responded = true;
				Debug.LogFormat("Twitch Plays handler {0}.{1} reports invalid command (by returning null).", methodDeclaringTypeFullName, method.Name);
				yield break;
			}
		}
		catch (System.Exception ex)
		{
			_responded = true;
			Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name);
			Debug.LogException(ex);
			yield break;
		}

		yield return null;
		yield return "trycancelsequence";
		yield return selectableSequence;
	}

	IEnumerator RespondToCommandInternalComplex(string command)
	{
		if (ProcessTwitchCommandMethod == null) yield break;
		MethodInfo method = ProcessTwitchCommandMethod;
		Component component = TwitchCommandComponent;

		string methodDeclaringTypeFullName = null;
		if (method.DeclaringType != null)
			methodDeclaringTypeFullName = method.DeclaringType.FullName;

		IEnumerator responseCoroutine = null;
		try
		{
			responseCoroutine = (IEnumerator)method.Invoke(component, new object[] { command });
		}
		catch (System.Exception ex)
		{
			_responded = true;
			Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name);
			Debug.LogException(ex);
			yield break;
		}

		if (responseCoroutine == null)
		{
			_responded = true;
			Debug.LogFormat("Twitch Plays handler {0}.{1} reports invalid command (by returning null).", methodDeclaringTypeFullName, method.Name);
			yield break;
		}

		while (responseCoroutine.MoveNext())
			yield return responseCoroutine.Current;
	}

	IEnumerator RespondToTwitchCommand(string command)
	{
		if (ProcessTwitchCommandMethod == null) return null;
		if (typeof(IEnumerable<KMSelectable>).IsAssignableFrom(ProcessTwitchCommandMethod.ReturnType))
			return RespondToCommandInternalSimple(command);
		if (ProcessTwitchCommandMethod.ReturnType == typeof(IEnumerator))
			return RespondToCommandInternalComplex(command);
		return null;
	}

	static readonly Dictionary<Component, HashSet<KMSelectable>> ComponentHelds = new Dictionary<Component, HashSet<KMSelectable>> { };
	
	IEnumerator SimulateModule(string command)
	{
		if (Solved && !AnarchyMode) yield break;

		needQuaternionReset = false;
		frontFace = _heldFrontFace;
		IEnumerator focus = null;
		bool focused = false;
		IEnumerator responseCoroutine = RespondToCommandCommon(command);
		string methodDeclaringTypeFullName = null;
		string methodName = null;
		int initialStrikes = StrikeCount;
		_beforeStrikeCount = StrikeCount;
		bool forceSolve;
		//MethodInfo method = null;
		Component component = this;
		if (TwitchCommandComponent != null)
			component = TwitchCommandComponent;

		if (!ComponentHelds.ContainsKey(component))
			ComponentHelds[component] = new HashSet<KMSelectable>();
		HashSet<KMSelectable> heldSelectables = ComponentHelds[component];

		bool? moved = responseCoroutine.MoveNext();
		if (!(moved ?? false))
		{
			if (_responded) yield break;
			if (ProcessTwitchCommandMethod == null) yield break;

			if (command.StartsWith("zoom ", StringComparison.InvariantCultureIgnoreCase))
				command = command.Substring(4).Trim();
			responseCoroutine = RespondToTwitchCommand(command);

			if (ProcessTwitchCommandMethod.DeclaringType != null)
				methodDeclaringTypeFullName = ProcessTwitchCommandMethod.DeclaringType.FullName;
			methodName = ProcessTwitchCommandMethod.Name;

			if (responseCoroutine == null)
			{
				Debug.LogFormat("Twitch Plays handler {0}.{1} reports invalid command (by returning null).",
					methodDeclaringTypeFullName, methodName);
				yield break;
			}

			moved = responseCoroutine.TryMoveNext(out forceSolve,
				string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.",
					methodDeclaringTypeFullName, methodName));
			if (forceSolve)
			{
				Debug.LogFormat("There was a problem with the solver. Force-solving module");
				SolveModule();
				yield break;
			}

			if (moved.HasValue && !moved.Value)
			{
				Debug.LogFormat("Twitch Plays handler {0}.{1} reports invalid command (by returning empty sequence).",
					methodDeclaringTypeFullName, methodName);
				yield break;
			}
			else if (!moved.HasValue)
			{
				yield break;
			}

			string str = responseCoroutine.Current as string;
			if (str != null && SendToTwitchChat(str, "[YOUR_NICKNAME_COULD_BE_HERE]") == SendToTwitchChatResponse.InstantResponse)
				yield break;
		}

		focus = TestHarness.MoveCamera(Module);
		while (focus.MoveNext())
			yield return focus.Current;
		yield return new WaitForSeconds(0.5f);
		focused = true;

		if (_zoom)
		{
			focus = ZoomCamera();
			while (focus.MoveNext())
				yield return focus.Current;
		}

		
		Quaternion initialModuleQuaternion = Module.localRotation;
		bool tryCancelSequence = false;
		bool multipleStrikes = false;

		while (true)
		{
			moved = responseCoroutine.TryMoveNext(out forceSolve,
				string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, methodName));
			if (forceSolve)
			{
				Debug.LogFormat("There was a problem with the solver. Force-solving module");
				SolveModule();
				yield break;
			}

			if (moved.HasValue && !moved.Value)
				break;

			SetBool(TwitchCancelField, Canceller.ShouldCancel);

			object currentObject = responseCoroutine.Current;
			if (currentObject is KMSelectable)
			{
				KMSelectable selectable = (KMSelectable) currentObject;
				if (heldSelectables.Contains(selectable))
				{
					DoInteractionEnd(selectable);
					heldSelectables.Remove(selectable);
					if ((StrikeCount != initialStrikes || Solved) && !AnarchyMode && !multipleStrikes)
						break;
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
					if (tryCancelSequence && Canceller.ShouldCancel)
					{
						Canceller.ResetCancel();
						break;
					}

					if ((StrikeCount != initialStrikes || Solved) && !AnarchyMode && !multipleStrikes)
						break;

				}
			}
			else if (currentObject is string)
			{
				string currentString = (string) currentObject;
				float waitTime;
				int pointsAwarded;
				Match match;

				if (currentString.Equals("strike", StringComparison.InvariantCultureIgnoreCase))
				{
					Debug.Log("Module has declared that a strike is pending, and might not happen while it is in focus");
				}
				else if (currentString.Equals("solve", StringComparison.InvariantCultureIgnoreCase))
				{
					Debug.Log("Module has declared that a solve is pending, and might not happen while it is in focus");
				}
				else if (currentString.Equals("unsubmittablepenalty", StringComparison.InvariantCultureIgnoreCase))
				{
					Debug.LogFormat("The answer that was submitted to module ID {0} ({1}) could not be submitted.", IDTextMesh.text,
						GetModuleDisplayName());
				}
				else if (currentString.Equals("parseerror"))
				{
					Debug.LogFormat("Bad command");
					break;
				}
				else if (currentString.RegexMatch(out match, "^trycancel((?: (?:.|\\n)+)?)$"))
				{
					if (Canceller.ShouldCancel)
					{
						Canceller.ResetCancel();
						Debug.Log("Twitch handler sent: " + currentString);
						break;
					}

					yield return null;
					continue;
				}
				else if (currentString.RegexMatch(out match, "^trycancelsequence((?: (?:.|\\n)+)?)$"))
				{
					tryCancelSequence = true;
					yield return currentObject;
					continue;
				}

				if (currentString.RegexMatch(out match, "^trywaitcancel ([0-9]+(?:\\.[0-9])?)((?: (?:.|\\n)+)?)$") &&
				    float.TryParse(match.Groups[1].Value, out waitTime))
				{
					yield return new WaitForSecondsWithCancel(waitTime, false, this);
					if (Canceller.ShouldCancel)
					{
						Canceller.ResetCancel();
						Debug.Log("Twitch handler sent: " + currentString);
						break;
					}
				}
				else if (SendToTwitchChat(currentString, "[USER_NICK_NAME_HERE]") != SendToTwitchChatResponse.NotHandled)
				{
					if (AntiTrollMode && !AnarchyMode) break;
					yield return null;
					continue;
				}
				else if (currentString.StartsWith("add strike", StringComparison.InvariantCultureIgnoreCase))
				{
					HandleStrike();
				}
				else if (currentString.Equals("multiple strikes"))
				{
					multipleStrikes = true;
				}
				else if (currentString.Equals("end multiple strikes"))
				{
					multipleStrikes = false;
					if ((StrikeCount != initialStrikes || Solved) && !AnarchyMode)
						break;
				}
				else if (currentString.Equals("autosolve"))
				{
					SolveModule();
					break;
				}
				else if (currentString.RegexMatch(out match, "^(?:detonate|explode)(?: ([0-9.]+))?(?: ((?:.|\\n)+))?$"))
				{
					if (!float.TryParse(match.Groups[1].Value, out waitTime))
					{
						if (string.IsNullOrEmpty(match.Groups[1].Value))
						{
							Debug.LogFormat("Immediate explosion reqeusted by module's twitch handler");
							waitTime = 0.1f;
						}
						else
						{
							Debug.Log("Badly formatted detonate command string: " + currentObject);
							yield return currentObject;
							continue;
						}
					}
					else
					{
						Debug.LogFormat("Delayed explosion reqeusted by module's twitch handler. The bomb will explode in {0} seconds",
							match.Groups[1].Value);
					}

					_delayedExplosionPending = true;
					if (_delayedExplosionCoroutine != null)
						StopCoroutine(_delayedExplosionCoroutine);
					_delayedExplosionCoroutine = StartCoroutine(DelayedModuleBombExplosion(waitTime, match.Groups[2].Value));
				}
				else if (currentString.RegexMatch(out match, "^cancel (detonate|explode|detonation|explosion)$"))
				{
					_delayedExplosionPending = false;
					Debug.LogFormat("Delayed explosion cancelled.");
					if (_delayedExplosionCoroutine != null)
						StopCoroutine(_delayedExplosionCoroutine);
				}
				else if (currentString.RegexMatch(out match, "^(end |toggle )?(?:elevator|hold|waiting) music$"))
				{
					Debug.Log("Twitch handler sent: " + currentObject);
					if (match.Groups.Count > 1 && _elevatorMusicStarted)
					{
						_elevatorMusicStarted = false;
						Debug.LogFormat("Stopping Elevator music");
					}
					else if (!currentString.StartsWith("end ", StringComparison.InvariantCultureIgnoreCase) && !_elevatorMusicStarted)
					{
						Debug.LogFormat("Starting Elevator music");
					}
				}
				else if (currentString.ToLowerInvariant().Equals("hide camera"))
				{
					Debug.Log("Hiding of camera / HUD requested");
				}
				else if (currentString.Equals("cancelled") && Canceller.ShouldCancel)
				{
					Canceller.ResetCancel();
					SetBool(TwitchCancelField, false);
					break;
				}
				else if (currentString.RegexMatch(out match, "^(?:skiptime|settime) ([0-9:.]+)") &&
				         match.Groups[1].Value.TryParseTime(out waitTime))
				{
					Debug.LogFormat("Time skipping requested");



					var skipDenied = TwitchPlaysModules.Where(x => x.Solvable && !x.TimeSkippingAllowed && !x.Solved).ToList();

					if (!skipDenied.Any())
					{
						if ((ZenMode && TimerModule.TimeRemaining < waitTime) ||
						    (!ZenMode && TimerModule.TimeRemaining > waitTime))
						{
							TimerModule.TimeRemaining = waitTime;
							Debug.LogFormat("Skipping of time was allowed. Bomb Timer is now {0}", TimerModule.GetFormattedTime());
						}
						else
						{
							Debug.LogFormat("Skipping of time was not allowed because the requested time to skip to has already gone by.");
						}
					}
					else
					{
						Debug.LogFormat(
							"Skipping of time was not allowed, because there is at least one unsolved module that doesn't allow skipping of time present:");
						Debug.LogFormat(skipDenied
							.Select(x => string.Format("!{0} - ({1})", x.IDTextMesh.text, x.BombModule.ModuleDisplayName)).Join("\n"));
					}
				}
				else if (currentString.RegexMatch(out match, @"^awardpoints (-?\d+)$") && int.TryParse(match.Groups[1].Value, out pointsAwarded))
				{
					Debug.LogFormat("Awarded {0} {1}.", pointsAwarded, pointsAwarded == 1 ? "point" : "points");
				}

				else
				{
					Debug.Log("Unprocessed string: " + currentObject);
				}

				yield return currentObject;
			}
			else if (currentObject is string[])
			{
				string[] currentStrings = (string[]) currentObject;
				if (currentStrings.Length >= 1)
				{
					if (new[] {"detonate", "explode"}.Contains(currentStrings[0].ToLowerInvariant()))
					{
						FakeBombInfo.strikes = FakeBombInfo.numStrikes - 1;
						string moduleDisplayName = GetModuleDisplayName() ?? "Detonate Command in TP Module";
						switch (currentStrings.Length)
						{
							case 3:
								moduleDisplayName = currentStrings[2];
								goto case 2;

							case 2:
								Debug.Log("Detonate command chat message: " + currentStrings[1]);
								goto default;

							default:
								FakeBombInfo.HandleStrike(moduleDisplayName);
								break;
						}
					}
				}
			}
			else if (currentObject is Quaternion)
			{
				RotateBombByLocalQuaternion((Quaternion) currentObject);
			}
			else if (currentObject is Quaternion[])
			{
				Quaternion[] localQuaternions = (Quaternion[]) currentObject;
				if (localQuaternions.Length == 2)
				{
					//Module.parent.parent.localRotation = localQuaternions[0];
					RotateBombByLocalQuaternion(localQuaternions[0]);
					if (_moduleCamera != null) _moduleCamera.localRotation = Quaternion.Euler(frontFace ? -localQuaternions[1].eulerAngles : localQuaternions[1].eulerAngles);
				}
			}
			else
				yield return currentObject;

			if ((StrikeCount != initialStrikes || Solved) && !AnarchyMode && !multipleStrikes)
				break;

			tryCancelSequence = false;
		}

		if (needQuaternionReset)
		{
			focus = TestHarness.MoveCamera(Module);
			while (focus.MoveNext())
				yield return focus.Current;
		}

		if (_zoom)
		{
			focus = UnZoomCamera();
			while (focus.MoveNext())
				yield return focus.Current;
		}

		if (focused)
		{
			focus = TestHarness.MoveCamera(TestHarness.Instance.transform);
			while (focus.MoveNext())
				yield return focus.Current;
			yield return new WaitForSeconds(0.5f);
		}
	}

	bool needQuaternionReset;
	private bool frontFace;
	private bool _heldFrontFace { get { return Module.parent.parent.localEulerAngles.z < 90 || Module.parent.parent.localEulerAngles.z > 270; } }
	protected void RotateBombByLocalQuaternion(Quaternion localQuaternion)
	{
		if (!needQuaternionReset)
		{
			frontFace = _heldFrontFace;
			needQuaternionReset = true;
		}
		float currentZSpin = frontFace ? 0.0f : 180.0f;
		Module.parent.parent.localRotation = Quaternion.Euler(0, 0, currentZSpin) * localQuaternion;
		//Module.parent.parent.localRotation = localQuaternion;
	}

	protected enum SendToTwitchChatResponse
	{
		InstantResponse,
		Handled,
		NotHandled
	}

	protected SendToTwitchChatResponse SendToTwitchChat(string message, string userNickName)
	{
		Match match;
		float messageDelayTime;
		// Within the messages, allow variables:
		// {0} = user’s nickname
		// {1} = Code (module number)
		if (message.RegexMatch(out match, @"^senddelayedmessage ([0-9]+(?:\.[0-9])?) (\S(?:\S|\s)*)$") && float.TryParse(match.Groups[1].Value, out messageDelayTime))
		{
			Debug.LogFormat("Sending delayed message \"{0}\" in {1} seconds", match.Groups[2].Value, match.Groups[1].Value);
			return SendToTwitchChatResponse.InstantResponse;
		}

		if (!message.RegexMatch(out match, @"^(sendtochat|sendtochaterror|strikemessage|antitroll) +(\S(?:\S|\s)*)$")) return SendToTwitchChatResponse.NotHandled;

		var chatMsg = string.Format(match.Groups[2].Value, userNickName, IDTextMesh.text);

		switch (match.Groups[1].Value)
		{
			case "sendtochat":
				Debug.LogFormat("Sending chat message: {0}", chatMsg);
				return SendToTwitchChatResponse.InstantResponse;
			case "antitroll":
				if (!AntiTrollMode || AnarchyMode)
				{
					Debug.Log("Troll command allowed to happen");
					return SendToTwitchChatResponse.Handled;
				}
				Debug.LogFormat("Troll commmand denied, Sending error message to chat: {0}", chatMsg);
				return SendToTwitchChatResponse.InstantResponse;
			case "sendtochaterror":
				Debug.LogFormat("Sending error message to chat: {0}", chatMsg);
				return SendToTwitchChatResponse.InstantResponse;
			case "strikemessage":
				StrikeMessageConflict |= StrikeCount != _beforeStrikeCount && !string.IsNullOrEmpty(StrikeMessage) && !StrikeMessage.Equals(chatMsg);
				StrikeMessage = chatMsg;
				if (StrikeMessageConflict)
				{
					Debug.LogFormat("Strikes happened on the module, and the message changed as to reason for strike, so nothing will be reported");
				}
				else
				{
					Debug.LogFormat("Strike message set to {0}", StrikeMessage);
				}
				return SendToTwitchChatResponse.Handled;
			default:
				return SendToTwitchChatResponse.NotHandled;
		}
	}

	protected IEnumerator DelayedModuleBombExplosion(float delay, string chatMessage)
	{
		yield return new WaitForSeconds(delay);
		if (!_delayedExplosionPending) yield break;
		Debug.LogFormat("Sending the following message to chat: {0}", chatMessage);

		FakeBombInfo.strikes = FakeBombInfo.numStrikes - 1;
		FakeBombInfo.HandleStrike(GetModuleDisplayName() ?? "Detonate Command in TP module");
	}

	private Coroutine _delayedExplosionCoroutine;
	private bool _delayedExplosionPending;
	protected string StrikeMessage;
	private int _beforeStrikeCount;
	protected bool StrikeMessageConflict;
	private bool _elevatorMusicStarted;
}

public static class Canceller
{
	public static void SetCancel()
	{
		ShouldCancel = true;
	}

	public static void ResetCancel()
	{
		ShouldCancel = false;
	}

	public static bool ShouldCancel
	{
		get;
		private set;
	}

	public static bool? TryMoveNext(this IEnumerator iEnumerator, out bool forceSolve, string exceptionReason = null)
	{
		try
		{
			forceSolve = false;
			return iEnumerator.MoveNext();
		}
		catch (System.Exception ex)
		{
			if(exceptionReason != null)
				Debug.Log(exceptionReason);
			Debug.LogException(ex);

			while (!(ex is System.FormatException) && ex.InnerException != null)
				ex = ex.InnerException;

			forceSolve = !(ex is System.FormatException);
			return null;
		}
	}
}


public class TPCoroutineQueue : MonoBehaviour
{
	private void Awake()
	{
		_coroutineQueue = new Queue<IEnumerator>();
		_forceSolveQueue = new Queue<IEnumerator>();
	}

	private void Update()
	{
		if (!_processingForcedSolve && _forceSolveQueue.Count > 0)
		{
			_processingForcedSolve = true;
			_activeForceSolveCoroutine = StartCoroutine(ProcessForcedSolveCoroutine());
		}

		if (Processing || _coroutineQueue.Count <= 0) return;
		Processing = true;
		_activeCoroutine = StartCoroutine(ProcessQueueCoroutine());
	}

	public static void AddForcedSolve(IEnumerator subcoroutine)
	{
		_forceSolveQueue.Enqueue(subcoroutine);
	}

	public void AddToQueue(IEnumerator subcoroutine)
	{
		_coroutineQueue.Enqueue(subcoroutine);
	}

	public void CancelFutureSubcoroutines()
	{
		_coroutineQueue.Clear();
	}

	public void StopQueue()
	{
		if (_activeCoroutine != null)
		{
			StopCoroutine(_activeCoroutine);
			_activeCoroutine = null;
		}

		Processing = false;
	}

	public void StopForcedSolve()
	{
		if (_activeForceSolveCoroutine != null)
		{
			StopCoroutine(_activeForceSolveCoroutine);
			_activeForceSolveCoroutine = null;
		}
		_processingForcedSolve = false;
		_forceSolveQueue.Clear();
	}

	private IEnumerator ProcessQueueCoroutine()
	{

		while (_coroutineQueue.Count > 0)
		{
			IEnumerator coroutine = _coroutineQueue.Dequeue();
			while (coroutine.MoveNext())
			{
				yield return coroutine.Current;
			}
		}

		Processing = false;
		_activeCoroutine = null;
	}

	private IEnumerator ProcessForcedSolveCoroutine()
	{
		while (_forceSolveQueue.Count > 0)
		{
			IEnumerator coroutine = _forceSolveQueue.Dequeue();
			bool result = true;
			while (result)
			{
				try
				{
					result = coroutine.MoveNext();
				}
				catch
				{
					result = false;
				}
				if (!result) continue;

				if (coroutine.Current is bool && ((bool) coroutine.Current))
				{
					_forceSolveQueue.Enqueue(coroutine);
					yield return null;
					result = false;
				}
				else
				{
					yield return coroutine.Current;
				}
			}
		}

		_processingForcedSolve = false;
		_activeForceSolveCoroutine = null;
	}

	public bool Processing { get; private set; }

	private Queue<IEnumerator> _coroutineQueue;
	private Coroutine _activeCoroutine;

	private static Queue<IEnumerator> _forceSolveQueue = null;
	private bool _processingForcedSolve = false;
	private Coroutine _activeForceSolveCoroutine = null;

	public TPCoroutineQueue()
	{
		Processing = false;
	}
}

public class WaitForSecondsWithCancel : CustomYieldInstruction
{
	public WaitForSecondsWithCancel(float seconds, bool resetCancel = true, TwitchPlaysID solver = null)
	{
		_seconds = seconds;
		_startingTime = Time.time;
		_resetCancel = resetCancel;
		_solver = solver;
		_startingStrikes = _solver != null ?  _solver.StrikeCount : 0;
	}

	public override bool keepWaiting
	{
		get
		{
			if (!Canceller.ShouldCancel && ((!(_solver != null && _solver.Solved) && (_solver != null ? _solver.StrikeCount : 0) == _startingStrikes) || TwitchPlaysID.AnarchyMode))
				return (Time.time - _startingTime) < _seconds;

			if (Canceller.ShouldCancel && _resetCancel)
				Canceller.ResetCancel();

			return false;
		}
	}

	private readonly float _seconds = 0.0f;
	private readonly float _startingTime = 0.0f;
	private readonly bool _resetCancel = true;
	private readonly int _startingStrikes = 0;
	private readonly TwitchPlaysID _solver = null;
}