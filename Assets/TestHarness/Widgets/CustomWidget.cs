using UnityEngine;

public class CustomWidget : Widget
{

	[ReadOnlyWhenPlaying] public string key;
	[ReadOnlyWhenPlaying] public string data;

	public static CustomWidget CreateComponent(CustomWidget where, string queryKey, string dataString)
	{
		CustomWidget widget = Instantiate(where);
		widget.key = queryKey;
		widget.data = dataString;

		Debug.Log("Added custom widget (" + widget.key + "): " + widget.data);
		return widget;
	}

	public override string GetResult(string query, string passedData)
	{
		if (query == key)
		{
			return data;
		}
		else
		{
			return null;
		}
	}
}