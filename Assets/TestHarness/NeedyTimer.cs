using UnityEngine;

public class NeedyTimer : MonoBehaviour
{
	public float TimeRemaining { get; set; }

	public void Reset()
	{
		TimeRemaining = ParentComponent != null ? ParentComponent.CountdownTime : TotalTime;
		UpdateSevenSegText();
		IsWarning = false;
	}

	public void StartTimer(bool reset=false)
	{
		if (reset) Reset();

		Display.On = true;
		IsRunning = true;
		IsWarning = false;

		if (ParentComponent.OnNeedyActivation != null)
			ParentComponent.OnNeedyActivation();
	}

	public void StopTimer()
	{
		Display.On = false;
		IsRunning = false;
		IsWarning = false;

		if (ParentComponent.OnNeedyDeactivation != null)
			ParentComponent.OnNeedyDeactivation();
	}

	private void Update()
	{
		if (!IsRunning || TimeRemaining <= 0f) return;

		TimeRemaining -= Time.deltaTime;
		IsWarning = TimeRemaining <= WarnTime && TimeRemaining > 0;

		if (TimeRemaining <= 0f)
		{
			if (OnTimerExpire != null)
				OnTimerExpire();
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

	public NeedyTimerExpireEvent OnTimerExpire;
	public NeedyTimerWarnEvent OnTimerWarn;
	public NeedyTimerWarnOffEvent OnTimerWarnOff;

	public bool IsRunning { get; private set; }
	protected bool IsWarning
	{
		get { return _isWarning; }
		set
		{
			if (_isWarning == value) return;

			_isWarning = value;
			if (value && OnTimerWarn != null)
				OnTimerWarn();

			if (!value && OnTimerWarnOff != null)
				OnTimerWarnOff();
		}
	}

	private bool _isWarning;

	public delegate void NeedyTimerExpireEvent();
	public delegate void NeedyTimerWarnEvent();
	public delegate void NeedyTimerWarnOffEvent();
}
