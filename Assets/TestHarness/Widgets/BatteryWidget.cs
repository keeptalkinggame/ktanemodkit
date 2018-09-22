using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class BatteryWidget : Widget
{
	[ReadOnlyWhenPlaying] public int batt;
	[PrivateWhenPlaying] public TextMesh BatteryTextMesh;
	[PrivateWhenPlaying] public Transform[] BatteryHolders;
	[PrivateWhenPlaying] public Transform[] BatterySets;

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

		widget.BatteryTextMesh.transform.parent.gameObject.SetActive(false);
		foreach(Transform t in widget.BatteryHolders)
			t.gameObject.SetActive(false);
		foreach (Transform t in widget.BatterySets)
			t.gameObject.SetActive(false);
		foreach(Transform t in widget.BatterySets.SelectMany(x => x.GetComponentsInChildren<MeshRenderer>()).Select(x => x.transform))
			t.localEulerAngles = new Vector3(t.localEulerAngles.x, t.localEulerAngles.y, Random.Range(0, 360f));

		switch (widget.batt)
		{
			case 0:
				widget.BatteryHolders[Random.Range(0, widget.BatteryHolders.Length)].gameObject.SetActive(true);
				break;
			case 1:
			case 2:
			case 3:
			case 4:
				widget.BatteryHolders[widget.batt - 1].gameObject.SetActive(true);
				widget.BatterySets[widget.batt - 1].gameObject.SetActive(true);
				break;
			default:
				widget.BatteryTextMesh.transform.parent.gameObject.SetActive(true);
				widget.BatteryTextMesh.text = widget.batt + " Cells";
				break;
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