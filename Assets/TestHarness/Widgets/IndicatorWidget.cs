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

	public string Val;
	public bool On;

	public TextMesh IndicatorTextMesh;

	public static IndicatorWidget CreateComponent(IndicatorWidget where, string label = null, IndicatorState state = IndicatorState.RANDOM)
	{
		IndicatorWidget widget = Instantiate(where);

		if (label == null)
		{
			int pos = Random.Range(0, possibleValues.Count);
			widget.Val = possibleValues[pos];
			possibleValues.RemoveAt(pos);
		}
		else
		{
			if (possibleValues.Contains(label))
			{
				widget.Val = label;
				possibleValues.Remove(label);
			}
			else
			{
				widget.Val = "NLL";
			}
		}
		if (state == IndicatorState.RANDOM)
		{
			widget.On = Random.value > 0.4f;
		}
		else
		{
			widget.On = state == IndicatorState.ON ? true : false;
		}

		widget.IndicatorTextMesh.text = (widget.On ? "LIT " : "UNLIT ") + widget.Val;

		Debug.Log("Added indicator widget: " + widget.Val + " is " + (widget.On ? "ON" : "OFF"));
		return widget;
	}

	public override string GetResult(string key, string data)
	{
		if (key == KMBombInfo.QUERYKEY_GET_INDICATOR)
		{
			return JsonConvert.SerializeObject((object)new Dictionary<string, string>()
			{
				{
					"label", Val
				},
				{
					"on", On?bool.TrueString:bool.FalseString
				}
			});
		}
		else return null;
	}
}