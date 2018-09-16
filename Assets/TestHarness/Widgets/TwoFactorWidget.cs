using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class TwoFactorWidget : Widget
{
	private static int counter = 1;
	public int instance;
	public int code;
	private float newcodetime;
	public float timeremaining;

	public static TwoFactorWidget CreateComponent(TwoFactorWidget where, float newcode = 30)
	{
		TwoFactorWidget widget = Instantiate(where);
		widget.instance = counter++;

		if (newcode < 30)
			newcode = 30;
		if (newcode > 999)
			newcode = 999;

		widget.newcodetime = newcode;
		widget.timeremaining = newcode;
		widget.code = Random.Range(0, 1000000);

		Debug.LogFormat("Added Two factor widget #{0}: {1,6}.", widget.instance, widget.code);
		return widget;
	}

	public override string GetResult(string key, string data)
	{
		if (key == "twofactor")
		{
			return JsonConvert.SerializeObject((object)new Dictionary<string, int>()
			{
				{
					"twofactor_key", code
				}
			});
		}
		else return null;
	}

	private void FixedUpdate()
	{
		timeremaining -= Time.fixedDeltaTime;
		if (timeremaining < 0)
		{
			timeremaining = newcodetime;
			code = Random.Range(0, 1000000);
			Debug.LogFormat("[Two Factor #{0}] code is now {1,6}.", instance, code);
		}
	}
}