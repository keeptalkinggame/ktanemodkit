using UnityEngine;

public class TimerModule : MonoBehaviour
{
	public TextMesh TimerText;
	public TextMesh StrikesText;
	public float TimeRemaining = 600f;
	public int StrikeCount = 0;
	public bool TimerRunning = false;
	public bool ExplodedToTime = false;

	private void Update()
	{
		if (TimerRunning)
		{
			switch (StrikeCount)
			{
				case 0:
					TimeRemaining -= Time.deltaTime;
					break;
				case 1:
					TimeRemaining -= Time.deltaTime * 1.25f;
					break;
				case 2:
					TimeRemaining -= Time.deltaTime * 1.5f;
					break;
				case 3:
					TimeRemaining -= Time.deltaTime * 1.75f;
					break;
				default:
					TimeRemaining -= Time.deltaTime * 2.0f;
					break;
			}

			if (TimeRemaining < 0)
			{
				TimeRemaining = 0;
				TimerRunning = false;
				ExplodedToTime = true;
			}
		}

		TimerText.text = GetFormattedTime();
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
}