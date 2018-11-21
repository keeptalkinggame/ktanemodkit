using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class TwoFactorWidget : Widget
{
	private static int counter = 1;
	[PrivateWhenPlaying] public int instance;
	public int code;
	private float newcodetime;
	public float timeremaining;

	[PrivateWhenPlaying] public TextMesh TwoFactorTextMesh;
	[PrivateWhenPlaying] public TextMesh TimeRemainingTextMesh;
	[PrivateWhenPlaying] public MeshRenderer TwoFactorDisplay;

	private AudioSource _source;

	private void Awake()
	{
		_source = transform.GetComponent<AudioSource>();
	}

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

	public override void Activate()
	{
		timeremaining = newcodetime;
		TwoFactorTextMesh.text = string.Format("{0,6}.", code);
		base.Activate();
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
		const float fadeToRedTime = 10.0f;

		timeremaining -= Time.fixedDeltaTime;

		if (timeremaining < 10 && timeremaining >= 0)
		{
			var colorChange = timeremaining / fadeToRedTime;

			var redDiff = 127f - (108f * colorChange);
			var greenDiff = 255f * colorChange;
			TwoFactorDisplay.material.color = new Color(redDiff / 255, greenDiff / 255, 0f / 255);
		}

		if (timeremaining < 0)
		{
			timeremaining = newcodetime;
			code = Random.Range(0, 1000000);
			Debug.LogFormat("[Two Factor #{0}] code is now {1,6}.", instance, code);
			_source.PlayOneShot(_source.clip);
			TwoFactorDisplay.material.color = new Color(19f / 255, 255f / 255, 0f / 255);
			TwoFactorTextMesh.text = string.Format("{0,6}.", code);
		}
		
		TimeRemainingTextMesh.text = string.Format("{0,3}", (int)timeremaining);

	}
}