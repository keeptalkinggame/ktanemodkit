using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// A simple module that requires the player to push the exactly button 50 times, but only
/// when the timer has a "4" in any position.
/// </summary>
public class ButtonMasherModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMSelectable Button;
    public TextMesh Counter;

    protected int currentCount;

	protected void Start()
    {
        Button.OnInteract += HandlePress;
	}

    protected bool HandlePress()
    {
        KMAudio.PlaySoundAtTransform("tick", this.transform);

        string timerText = BombInfo.GetFormattedTime();

        if (currentCount < 50 && timerText.Contains("4"))
        {
            currentCount++;

            if (currentCount == 50)
            {
                BombModule.HandlePass();
            }
        }
        else
        {
            BombModule.HandleStrike();
        }

        Counter.text = currentCount.ToString();

        return false;
    }

	private IEnumerator ProcessTwitchCommand(string command)
	{
		var split = command.ToLowerInvariant().Split(new []{" "}, StringSplitOptions.RemoveEmptyEntries);
		int count;

		if (command == "troll")
		{
			yield return "antitroll Sorry, I am not going to waste time showing all numbers from 0 to 200";
			count = 0;
			do
			{
				Counter.text = count++.ToString();
				yield return new WaitForSeconds(0.05f * count);
				Counter.text = currentCount.ToString();
				yield return "antitroll Aww man. Why you have to stop me? I was at " + count;
			} while (count < 200);
		}

		if (split.Length != 3 || !split[0].Equals("mash") || !(new[] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"}).Any(x => x.Equals(split[1])) || !int.TryParse(split[2], out count))
			yield break;

		while (count > 0)
		{
			yield return "trycancel";
			if (!BombInfo.GetFormattedTime().Contains(split[1])) continue;
			HandlePress();
			yield return new WaitForSeconds(0.1f);
			count--;
		}
		
	}

	private IEnumerator TwitchHandleForcedSolve()
	{
		return ProcessTwitchCommand("mash 4 " + (50 - currentCount));
	}
}
