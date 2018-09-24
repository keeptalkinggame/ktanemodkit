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

	private float easeInOutQuad(float time, float start, float end, float duration)
	{
		time /= duration / 2;
		if (time < 1)
			return (end - start) / 2 * time * time + start;
		time--;
		return -(end - start) / 2 * (time * (time - 2) - 1) + start;
	}

	private float getRotateRate(float targetTime, float rate)
	{
		return rate * (Time.deltaTime / targetTime);
	}

	IEnumerator OriginalRotate()
	{
		yield return null;
		//Bomb rotation requires Quaternion.Euler(x,0,0) * Quaternion.Euler(0,y,0) * Quaternion.Euler(0,0,z)	
		//Camera view rotation requires Quaternion.Euler(x,y,z)	
		//The new format for this requires returning a Quaterion[] array containing the above two specifcations in that order.	
		bool frontFace = transform.parent.parent.localEulerAngles.z > 315 || transform.parent.parent.localEulerAngles.z < 45;  //eulerAngles.z = 0 on front face, 180 on back face.	
		const int angle = 60;
		for (float i = 0; i <= angle; i += getRotateRate(0.5f, 150))
		{
			yield return frontFace
				? new[] { Quaternion.Euler(easeInOutQuad(i, 0, angle, angle), 0, 0), Quaternion.Euler(easeInOutQuad(i, 0, angle, angle), 0, 0) }
				: new[] { Quaternion.Euler(easeInOutQuad(i, 0, -angle, angle), 0, 0), Quaternion.Euler(easeInOutQuad(i, 0, -angle, angle), 0, 0) };
			yield return null;
		}
		for (float i = 0; i <= 360; i += getRotateRate(10, 750))
		{
			yield return frontFace
				? new[] { Quaternion.Euler(angle, 0, 0) * Quaternion.Euler(0, easeInOutQuad(i, 0, 360, 360), 0), Quaternion.Euler(angle, easeInOutQuad(i, 0, 360, 360), 0) }
				: new[] { Quaternion.Euler(-angle, 0, 0) * Quaternion.Euler(0, easeInOutQuad(i, 0, -360, 360), 0), Quaternion.Euler(-angle, easeInOutQuad(i, 0, -360, 360), 0) };
			yield return null;
		}
		for (float i = 0; i <= angle; i += getRotateRate(0.5f, 150))
		{
			yield return frontFace
				? new[] { Quaternion.Euler(easeInOutQuad(i, angle, 0, angle), 0, 0), Quaternion.Euler(easeInOutQuad(i, angle, 0, angle), 0, 0) }
				: new[] { Quaternion.Euler(easeInOutQuad(i, -angle, 0, angle), 0, 0), Quaternion.Euler(easeInOutQuad(i, -angle, 0, angle), 0, 0) };
			yield return null;
		}
		yield return Quaternion.Euler(0, 0, 0);
	}

#pragma warning disable 0414
	private string TwitchHelpMessage = "Solve the module with !{0} mash 4 50.  The first number is the time that has to appear in the time, the second number is the number of times to mash the button";

#pragma warning disable 0649
	private bool TwitchShouldCancelCommand;
	private bool TwitchPlaysSkipTimeAllowed = true;
#pragma warning restore 0649
#pragma warning restore 0414
	private IEnumerator ProcessTwitchCommand(string command)
	{
		var split = command.ToLowerInvariant().Split(new []{" "}, StringSplitOptions.RemoveEmptyEntries);
		int count;

		if (command.StartsWith("command |") || command.StartsWith("command|"))
		{
			yield return null;
			foreach (string c in command.Split('|').Skip(1))
				yield return c;
			yield break;
		}

		if (command == "detonate")
		{
			yield return null;
			yield return "detonate";
		}

		if (command == "rotate")
		{
			yield return null;
			IEnumerator rotate = OriginalRotate();
			while (rotate.MoveNext())
				yield return rotate.Current;
		}

		if (command == "troll")
		{
			yield return "antitroll Sorry, I am not going to waste time showing all numbers from 0 to 200";
			yield return "elevator music";
			count = 0;
			do
			{
				Counter.text = count++.ToString();
				yield return new WaitForSeconds(0.01f * count);
				Counter.text = currentCount.ToString();
				yield return "antitroll Aww man. Why you have to stop me? I was at " + count;
			} while (count < 200 && !TwitchShouldCancelCommand);

			if (TwitchShouldCancelCommand)
			{
				yield return "sendtochat Aww man, counting to 200 was cut short. I was at " + count;
				yield return "cancelled";
				yield break;
			}
		}

		if (split.Length < 3 || !split[0].Equals("mash") || !(new[] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"}).Any(x => x.Equals(split[1])) || !int.TryParse(split[2], out count))
			yield break;

		yield return null;

		if (split.Length >= 4 && split[3].Equals("ms"))
			yield return "multiple strikes";

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
