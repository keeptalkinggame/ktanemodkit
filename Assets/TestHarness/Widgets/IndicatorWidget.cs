using System.Collections.Generic;
using EdgeworkConfigurator;
using Newtonsoft.Json;
using UnityEngine;

public class IndicatorWidget : Widget
{
	static List<string> possibleValues = new List<string>()
	{
		"SND","CLR","CAR",
		"IND","FRQ","SIG",
		"NSA","MSA","TRN",
		"BOB","FRK"
	};

	public string val;
	public bool on;

	public static IndicatorWidget CreateComponent(IndicatorWidget where, string label = null, IndicatorState state = IndicatorState.RANDOM)
	{
		IndicatorWidget widget = Instantiate(where);

		if (label == null)
		{
			int pos = Random.Range(0, possibleValues.Count);
			widget.val = possibleValues[pos];
			possibleValues.RemoveAt(pos);
		}
		else
		{
			if (possibleValues.Contains(label))
			{
				widget.val = label;
				possibleValues.Remove(label);
			}
			else
			{
				widget.val = "NLL";
			}
		}
		if (state == IndicatorState.RANDOM)
		{
			widget.on = Random.value > 0.4f;
		}
		else
		{
			widget.on = state == IndicatorState.ON ? true : false;
		}

		Debug.Log("Added indicator widget: " + widget.val + " is " + (widget.on ? "ON" : "OFF"));
		return widget;
	}

	public override string GetResult(string key, string data)
	{
		if (key == KMBombInfo.QUERYKEY_GET_INDICATOR)
		{
			return JsonConvert.SerializeObject((object)new Dictionary<string, string>()
			{
				{
					"label", val
				},
				{
					"on", on?bool.TrueString:bool.FalseString
				}
			});
		}
		else return null;
	}
}