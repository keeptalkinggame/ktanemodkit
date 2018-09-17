using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class PortWidget : Widget
{
	public List<string> ports;
	public Transform PortsTransform;
	public TextMesh OtherPortsTextMesh;

	public static PortWidget CreateComponent(PortWidget where, List<string> portNames = null)
	{
		PortWidget widget = Instantiate(where);
		widget.ports = new List<string>();
		string portList = "";
		if (portNames == null)
		{
			if (Random.value > 0.5)
			{
				if (Random.value > 0.5)
				{
					widget.ports.Add("Parallel");
					portList += "Parallel";
				}
				if (Random.value > 0.5)
				{
					widget.ports.Add("Serial");
					if (portList.Length > 0) portList += ", ";
					portList += "Serial";
				}
			}
			else
			{
				if (Random.value > 0.5)
				{
					widget.ports.Add("DVI");
					portList += "DVI";
				}
				if (Random.value > 0.5)
				{
					widget.ports.Add("PS2");
					if (portList.Length > 0) portList += ", ";
					portList += "PS2";
				}
				if (Random.value > 0.5)
				{
					widget.ports.Add("RJ45");
					if (portList.Length > 0) portList += ", ";
					portList += "RJ45";
				}
				if (Random.value > 0.5)
				{
					widget.ports.Add("StereoRCA");
					if (portList.Length > 0) portList += ", ";
					portList += "StereoRCA";
				}
			}
		}
		else
		{
			widget.ports = portNames;
			portList = string.Join(", ", portNames.ToArray());
		}
		if (portList.Length == 0) portList = "Empty plate";

		
		foreach (Transform t in widget.PortsTransform.GetComponentsInChildren<Transform>())
		{
			if (t == widget.PortsTransform) continue;
			t.gameObject.SetActive(false);
		}

		List<string> otherPorts = new List<string>();
		foreach (string port in widget.ports)
		{
			Transform p = widget.PortsTransform.Find(port);
			if (p != null) p.gameObject.SetActive(true);
			else otherPorts.Add(port);
		}
		widget.OtherPortsTextMesh.text = string.Join(", ", otherPorts.ToArray());

		Debug.Log("Added port widget: " + portList);
		return widget;
	}

	public override string GetResult(string key, string data)
	{
		if (key == KMBombInfo.QUERYKEY_GET_PORTS)
		{
			return JsonConvert.SerializeObject((object)new Dictionary<string, List<string>>()
			{
				{
					"presentPorts", ports
				}
			});
		}
		return null;
	}
}