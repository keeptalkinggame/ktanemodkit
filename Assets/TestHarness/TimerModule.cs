using UnityEngine;

public class TimerModule : MonoBehaviour
{
	public KMAudio Audio;
	public TextMesh TimerText;
	public TextMesh StrikesText;
	public float TimeRemaining = 600f;
	public int StrikeCount = 0;
	public bool TimerRunning = false;
	public bool ExplodedToTime = false;

	public static TimerModule Instance;
	public bool ZenMode;
	public bool TimeMode;

	public float TimeModeMultiplier = 8.0f;
	public float TimeModeCappedMultiplier { get { return Mathf.Min(TimeModeMultiplier, TimeModeMultiplierUpperCap); } }

	public float TimeModeMultiplierUpperCap = 10.0f;
	public float TimeModeMultiplierBoostRate = 0.1f;
	public float TimeModeMinimumTimeGained = 20.0f;

	public float TimeModeMultiplierLowerCap = 1.0f;
	public float TimeModeMultiplierPenaltyRate = 1.5f;
	public float TimeModeStrikePenaltyMultiplier = 0.20f;
	public float TimeModeMinimumTimeLost = 15.0f;


	protected bool EmergencyLighsRunning
	{
		get { return _emergencyLightsRunning; }
		set
		{
			if (value == _emergencyLightsRunning) return;
			_emergencyLightsRunning = value;
			if (value && OnStartEmergencyLights != null) OnStartEmergencyLights();
			if (!value && OnStopEmergencyLights != null) OnStopEmergencyLights();
		}
	}
	private bool _emergencyLightsRunning;
	
	public delegate void StartEmergencyLights();
	public delegate void StopEmergencyLights();

	public StartEmergencyLights OnStartEmergencyLights;
	public StopEmergencyLights OnStopEmergencyLights;
	

	private void Start()
	{
		if (ZenMode) TimeRemaining = 1.0f;
	}

	private int previousTime;
	private void Update()
	{
		if (TimerRunning)
		{
			float multiplier = ZenMode ? -1.0f : 1.0f;
			KMSoundOverride.SoundEffect beepEffect;
			switch (StrikeCount)
			{
				case 0:
					beepEffect = KMSoundOverride.SoundEffect.NormalTimerBeep;
					multiplier *= 1.0f;
					break;
				case 1:
					beepEffect = KMSoundOverride.SoundEffect.FastTimerBeep;
					multiplier *= 1.25f;
					break;
				case 2:
					beepEffect = KMSoundOverride.SoundEffect.FastestTimerBeep;
					multiplier *= 1.5f;
					break;
				case 3:
					beepEffect = KMSoundOverride.SoundEffect.FastestTimerBeep;
					multiplier *= 1.75f;
					break;
				default:
					beepEffect = KMSoundOverride.SoundEffect.FastestTimerBeep;
					multiplier *= 2.0f;
					break;
			}

			TimeRemaining -= Time.deltaTime * multiplier;
			EmergencyLighsRunning = TimeRemaining <= 60.0f && TimeRemaining >= 0.0f;

			if (TimeRemaining < 0)
			{
				TimeRemaining = 0;
				TimerRunning = false;
				ExplodedToTime = true;
			}
			else if (previousTime != (int)TimeRemaining)
			{
				previousTime = (int) TimeRemaining;
				Audio.HandlePlayGameSoundAtTransform(beepEffect, transform);
			}
		}

		TimerText.text = GetFormattedTime();
		if(StrikesText != null)
			StrikesText.text = StrikeCount.ToString();
	}

	public string GetFormattedTime()
	{
		string time = "";
		if (TimeRemaining < 60)
		{
			if (TimeRemaining < 10) time += "0";
			time += (int)TimeRemaining;
			time += ".";
			int s = ((int)(TimeRemaining * 100)) % 100;
			if (s < 10) time += "0";
			time += s;
		}
		else
		{
			if (TimeRemaining < 600) time += "0";
			time += (int)TimeRemaining / 60;
			time += ":";
			int s = (int)TimeRemaining % 60;
			if (s < 10) time += "0";
			time += s;
		}
		return time;
	}

	public void UpdateTimeModeTime(int moduleScore, bool strike)
	{
		if (!TimeMode) return;
		if (strike)
		{
			float timelost = TimeRemaining * TimeModeStrikePenaltyMultiplier;
			if (timelost < TimeModeMinimumTimeLost) timelost = TimeModeMinimumTimeLost;
			TimeRemaining -= timelost;

			TimeModeMultiplier -= TimeModeMultiplierPenaltyRate;
			if (TimeModeMultiplier < TimeModeMultiplierLowerCap)
				TimeModeMultiplier = TimeModeMultiplierLowerCap;

			Debug.LogFormat("Time Mode Strike: {0:0.0} seconds {1}. Multiplier is now {2:0.0}", timelost, timelost > 0 ? "lost" : "gained", TimeModeCappedMultiplier);
		}
		else
		{
			float timeGained = moduleScore * TimeModeCappedMultiplier;
			if (timeGained < TimeModeMinimumTimeGained) timeGained = TimeModeMinimumTimeGained;
			TimeRemaining += timeGained;

			TimeModeMultiplier += TimeModeMultiplierBoostRate;

			Debug.LogFormat("Time Mode Solve: {0:0.0} seconds {1}. Multiplier is now {2:0.0}", timeGained, timeGained > 0 ? "gained" : "lost", TimeModeCappedMultiplier);
		}

		StrikeCount = 0;	//Strikes always zero in time mode. Bomb still explodes though if the strike limit is exactly 1.
	}
}