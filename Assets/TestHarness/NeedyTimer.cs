using System.Collections;
using UnityEngine;

public class NeedyTimer : MonoBehaviour
{
	public float TimeRemaining { get; set; }
	public KMAudio Audio;

	private Coroutine _waitAndReset;

	public void Awake()
	{
		State = NeedyState.AwaitingActivation;
		_newState = NeedyState.AwaitingActivation;
	}

	public void StartTimer()
	{
		if (!gameObject.activeInHierarchy || IsStoppedPermanently || IsRunning) return;
		_newState = NeedyState.Running;

		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyActivated, transform);
		TimeRemaining = ParentComponent != null ? ParentComponent.CountdownTime : TotalTime;

		Display.On = true;
		IsWarning = false;

		UpdateSevenSegText();

		if (_waitAndReset == null) return;

		StopCoroutine(_waitAndReset);
		_waitAndReset = null;
	}

	public void StopTimer(NeedyState newState = NeedyState.Cooldown)
	{
		Display.On = false;
		IsWarning = false;
		
		_newState = !gameObject.activeInHierarchy 
			? NeedyState.Terminated 
			: newState;

		UpdateSevenSegText();
	}

	public float GetTimeRemaining()
	{
		return IsRunning ? TimeRemaining : -1f;
	}

	public void SetTimeRemaining(float time)
	{
		if (IsRunning) TimeRemaining = time;
	}

	IEnumerator WaitAndReset()
	{
		yield return new WaitForSeconds(Random.Range(10f, 40f));
		StartTimer();
	}

	private void Update()
	{
		if (IsStoppedPermanently) return;
		if (State != _newState)
		{
			if (_waitAndReset != null)
			{
				StopCoroutine(_waitAndReset);
				_waitAndReset = null;
			}

			switch (_newState)
			{
				case NeedyState.InitialSetup:
					_newState = NeedyState.AwaitingActivation;
					goto default;

				case NeedyState.AwaitingActivation:
					break;

				case NeedyState.Running:
					if (ParentComponent.OnNeedyActivation != null)
						ParentComponent.OnNeedyActivation();
					break;

				case NeedyState.Cooldown:
					_waitAndReset = StartCoroutine(WaitAndReset());
					goto default;

				
				case NeedyState.Terminated:
				case NeedyState.BombComplete:
				default:
					if (ParentComponent.OnNeedyDeactivation != null)
						ParentComponent.OnNeedyDeactivation();
					break;
			}

			State = _newState;
			UpdateSevenSegText();
			Debug.LogFormat("!IsRunning = {0}, TimeRemaining <= 0f = {1}", !IsRunning, TimeRemaining <= 0f);
		}

		if (!IsRunning || TimeRemaining <= 0f) return;

		TimeRemaining -= Time.deltaTime;
		IsWarning = TimeRemaining <= WarnTime && TimeRemaining > 0;

		if (TimeRemaining <= 0f)
		{
			if (ParentComponent.OnTimerExpired != null)
				ParentComponent.OnTimerExpired();
			StopTimer();
		}

		UpdateSevenSegText();
	}

	private void UpdateSevenSegText()
	{
		Display.DisplayValue = (int)Mathf.Round(TimeRemaining);
	}

	public SevenSegDisplay Display;
	public float TotalTime;
	public KMNeedyModule ParentComponent;
	public float WarnTime = 5f;

	public bool IsRunning
	{
		get { return State == NeedyState.Running || _newState == NeedyState.Running; }
	}

	public bool IsStoppedPermanently
	{
		get { return State == NeedyState.Terminated || State == NeedyState.BombComplete; }
	}

	protected bool IsWarning
	{
		get { return _isWarning; }
		set
		{
			if (_isWarning == value) return;

			_isWarning = value;
			if (value)
			{
				if (ParentComponent.WarnAtFiveSeconds)
					_warningRef = Audio.PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.NeedyWarning, transform);
			}
			else
			{
				if (_warningRef != null && _warningRef.StopSound != null)
					_warningRef.StopSound();
				_warningRef = null;
			}
		}
	}

	private KMAudio.KMAudioRef _warningRef;
	private bool _isWarning;
	public NeedyState State { get; private set; }
	private NeedyState _newState;

	public enum NeedyState
	{
		InitialSetup,
		AwaitingActivation,
		Running,
		Cooldown,
		Terminated,
		BombComplete
	}
}
