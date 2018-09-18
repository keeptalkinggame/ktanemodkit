using UnityEngine;

public class NeedyTimer : MonoBehaviour
{
	public float TimeRemaining { get; set; }

	protected void Awake()
	{
		Reset();
	}

	public void Reset()
	{
		TimeRemaining = TotalTime;
		UpdateSevenSegText();
		isWarning = false;
	}

	public void StartTimer()
	{
		Display.On = true;
		isRunning = true;
		isWarning = false;

		if (ParentComponent.OnNeedyActivation != null)
			ParentComponent.OnNeedyActivation();
	}

	public void StopTimer()
	{
		Display.On = false;
		isRunning = false;
		isWarning = false;

		if (ParentComponent.OnNeedyDeactivation != null)
			ParentComponent.OnNeedyDeactivation();
	}

	private void Update()
	{
		if (!isRunning || TimeRemaining <= 0f) return;

		TimeRemaining -= Time.deltaTime;
		if (TimeRemaining <= WarnTime && !isWarning)
		{
			isWarning = true;
			if (OnTimerWarn != null)
			{
				OnTimerWarn();
			}
		}
		if (TimeRemaining <= 0f)
		{
			if (OnTimerExpire != null)
			{
				OnTimerExpire();
			}
			StopTimer();
		}
		if (TimeRemaining > WarnTime && isWarning)
		{
			isWarning = false;
			if (OnTimerWarnOff != null)
			{
				OnTimerWarnOff();
			}
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

	public bool isRunning { get; private set; }
	protected bool isWarning;

	public delegate void NeedyTimerExpireEvent();
	public delegate void NeedyTimerWarnEvent();
	public delegate void NeedyTimerWarnOffEvent();
}
