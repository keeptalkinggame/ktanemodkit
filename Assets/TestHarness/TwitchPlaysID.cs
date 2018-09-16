using System.Collections;
using System.Collections.Generic;
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


	public bool Solved;
	public int StrikeCount;

	public static bool AntiTrollMode = true;
	public static bool AnarchyMode;
	public static bool TimeMode;
	public static bool ZenMode;

	private bool HandleStrike()
	{
		if (triedToSolve)
			StopEverything();
		StrikeCount++;
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
		Solved = true;
		return true;
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
	}

	private void Update()
	{
		if (Module == null)
			gameObject.SetActive(false);

		Unsupported.SetActive(TwitchCommandComponent == null && (!Solved || AnarchyMode));
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
			else
			{
				TPCoroutineQueue.AddToQueue(SimulateModule(matches.Groups[1].Value));
			}
		}
	}

	protected void FindTwitchCommandMethod()
	{
		if (Module == null) return;
		Component[] allComponents = Module.gameObject.GetComponentsInChildren<Component>(true);
		foreach (Component component in allComponents)
		{
			System.Type type = component.GetType();
			MethodInfo method = type.GetMethod("ProcessTwitchCommand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null) continue;

			TwitchCommandComponent = component;
			ProcessTwitchCommandMethod = method;
			TwitchForcedSolveMethod = type.GetMethod("TwitchHandleForcedSolve", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

	static readonly Dictionary<Component, HashSet<KMSelectable>> ComponentHelds = new Dictionary<Component, HashSet<KMSelectable>> { };
	IEnumerator SimulateModule(string command)
	{
		if (ProcessTwitchCommandMethod == null) yield break;
		MethodInfo method = ProcessTwitchCommandMethod;
		Component component = TwitchCommandComponent;

		string methodDeclaringTypeFullName = null;
		if (method.DeclaringType != null)
			methodDeclaringTypeFullName = method.DeclaringType.FullName;

		// Simple Command
		if (typeof(IEnumerable<KMSelectable>).IsAssignableFrom(ProcessTwitchCommandMethod.ReturnType))
		{
			IEnumerable<KMSelectable> selectableSequence = null;
			try
			{
				selectableSequence = (IEnumerable<KMSelectable>)method.Invoke(component, new object[] { command });
				if (selectableSequence == null)
				{
					Debug.LogFormat("Twitch Plays handler {0}.{1} reports invalid command (by returning null).", methodDeclaringTypeFullName, method.Name);
					yield break;
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name);
				Debug.LogException(ex);
				yield break;
			}

			int initialStrikes = StrikeCount;
			foreach (KMSelectable selectable in selectableSequence)
			{
				if (Canceller.ShouldCancel)
				{
					Canceller.ResetCancel();
					yield break;
				}
				DoInteractionStart(selectable);
				yield return new WaitForSeconds(0.1f);
				DoInteractionEnd(selectable);

				if ((StrikeCount != initialStrikes || Solved) && !AnarchyMode)
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
				responseCoroutine = (IEnumerator)method.Invoke(component, new object[] { command });
			}
			catch (System.Exception ex)
			{
				Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name);
				Debug.LogException(ex);
				yield break;
			}

			if (responseCoroutine == null)
			{
				Debug.LogFormat("Twitch Plays handler {0}.{1} reports invalid command (by returning null).", methodDeclaringTypeFullName, method.Name);
				yield break;
			}

			if (!ComponentHelds.ContainsKey(component))
				ComponentHelds[component] = new HashSet<KMSelectable>();
			HashSet<KMSelectable> heldSelectables = ComponentHelds[component];

			int initialStrikes = StrikeCount;

			bool forceSolve;
			bool? moved = responseCoroutine.TryMoveNext(out forceSolve, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name));
			if (forceSolve)
			{
				Debug.LogFormat("There was a problem with the solver. Force-solving module");
				SolveModule();
				yield break;
			}
			if (moved.HasValue && !moved.Value)
			{
				Debug.LogFormat("Twitch Plays handler {0}.{1} reports invalid command (by returning empty sequence).", methodDeclaringTypeFullName, method.Name);
				yield break;
			}
			else if (!moved.HasValue)
			{
				yield break;
			}

			string str = responseCoroutine.Current as string;
			if (str != null)
			{
				if (str.StartsWith("sendtochat"))
				{
					Debug.Log("Twitch handler sent: " + str);
					yield break;
				}

				if (str.StartsWith("antitroll") && AntiTrollMode && !AnarchyMode)
				{
					Debug.Log("Twitch handler sent: " + str);
					yield break;
				}
			}

			while (true)
			{
				
				moved = responseCoroutine.TryMoveNext(out forceSolve, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", methodDeclaringTypeFullName, method.Name));
				if (forceSolve)
				{
					Debug.LogFormat("There was a problem with the solver. Force-solving module");
					SolveModule();
					yield break;
				}

				if (moved.HasValue && !moved.Value)
					break;

				object currentObject = responseCoroutine.Current;
				if (currentObject is KMSelectable)
				{
					KMSelectable selectable = (KMSelectable)currentObject;
					if (heldSelectables.Contains(selectable))
					{
						DoInteractionEnd(selectable);
						heldSelectables.Remove(selectable);
						if ((StrikeCount != initialStrikes || Solved) && !AnarchyMode)
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
					foreach (var selectable in (IEnumerable<KMSelectable>)currentObject)
					{
						DoInteractionStart(selectable);
						yield return new WaitForSeconds(.1f);
						DoInteractionEnd(selectable);
					}
				}
				else if (currentObject is string)
				{
					string currentString = (string)currentObject;
					float waitTime;
					Match match = Regex.Match(currentString, "^trywaitcancel ([0-9]+(?:\\.[0-9])?)((?: (?:.|\\n)+)?)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
					if (match.Success && float.TryParse(match.Groups[1].Value, out waitTime))
					{
						yield return new WaitForSecondsWithCancel(waitTime, false, this);
						if (Canceller.ShouldCancel)
						{
							Canceller.ResetCancel();
							yield break;
						}
						continue;
					}

					match = Regex.Match(currentString, "^trycancel$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
					if (match.Success)
					{
						if (Canceller.ShouldCancel)
						{
							Canceller.ResetCancel();
							yield break;
						}

						yield return null;
						continue;
					}

					if (currentString.StartsWith("antitroll"))
					{
						if (AntiTrollMode && !AnarchyMode)
						{
							Debug.Log("Twitch handler sent: " + currentString);
							yield break;
						}
						else
						{
							yield return null;
							continue;
						}
					}

					Debug.Log("Twitch handler sent: " + currentObject);
					yield return currentObject;
				}
				else if (currentObject is Quaternion)
				{
					Module.localRotation = (Quaternion)currentObject;
				}
				else
					yield return currentObject;

				if ((StrikeCount != initialStrikes || Solved) && !AnarchyMode)
					yield break;
			}
		}
	}
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