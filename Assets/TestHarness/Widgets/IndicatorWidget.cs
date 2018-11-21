using System.Collections.Generic;
using System.Linq;
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

	[ReadOnlyWhenPlaying] public string Val;
	[ReadOnlyWhenPlaying] public bool On;

	[PrivateWhenPlaying] public TextMesh IndicatorTextMesh;
	[PrivateWhenPlaying] public Transform LightOnTransform;
	[PrivateWhenPlaying] public Transform LightOffTransform;

	public static IndicatorWidget CreateComponent(IndicatorWidget where, string label = null, IndicatorState state = IndicatorState.RANDOM)
	{
		IndicatorWidget widget = Instantiate(where);

		if (label == null)
		{
			if (possibleValues.Any())
			{
				int pos = Random.Range(0, possibleValues.Count);
				widget.Val = possibleValues[pos];
				possibleValues.RemoveAt(pos);
			}
			else
			{
				widget.Val = "NLL";
			}
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

		widget.LightOnTransform.gameObject.SetActive(widget.On);
		widget.LightOffTransform.gameObject.SetActive(!widget.On);

		widget.IndicatorTextMesh.text = widget.Val;

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