using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class BatteryWidget : Widget
{
	public int batt;

	public static BatteryWidget CreateComponent(BatteryWidget where, int battCount = -1)
	{
		BatteryWidget widget = Instantiate(where);
		if (battCount == -1)
		{
			widget.batt = Random.Range(1, 3);
		}
		else
		{
			widget.batt = battCount;
		}

		Debug.Log("Added battery widget: " + widget.batt);
		return widget;
	}

	public override string GetResult(string key, string data)
	{
		if (key == KMBombInfo.QUERYKEY_GET_BATTERIES)
		{
			return JsonConvert.SerializeObject((object)new Dictionary<string, int>()
			{
				{
					"numbatteries", batt
				}
			});
		}
		else return null;
	}
}