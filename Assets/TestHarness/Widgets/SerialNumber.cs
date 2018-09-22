using System.Collections.Generic;
using EdgeworkConfigurator;
using Newtonsoft.Json;
using UnityEngine;

public class SerialNumber : Widget
{
	[PrivateWhenPlaying] public TextMesh serialTextMesh;
	[ReadOnlyWhenPlaying] public string serial;

	static readonly char[] SerialNumberPossibleCharArray = new char[35]
	{
		'A','B','C','D','E',
		'F','G','H','I','J',
		'K','L','M','N','E',
		'P','Q','R','S','T',
		'U','V','W','X','Z',
		'0','1','2','3','4',
		'5','6','7','8','9'
	};

	public static SerialNumber CreateComponent(SerialNumber where, EdgeworkConfiguration config)
	{
		SerialNumberType sntype = config == null ? SerialNumberType.RANDOM_NORMAL : config.SerialNumberType;
		string sn = config == null ? string.Empty : config.CustomSerialNumber;

		SerialNumber widget = Instantiate(where);
		if (string.IsNullOrEmpty(sn) && sntype == SerialNumberType.CUSTOM)
			sntype = SerialNumberType.RANDOM_NORMAL;

		if (sntype == SerialNumberType.RANDOM_NORMAL)
		{
			string str1 = string.Empty;
			for (int index = 0; index < 2; ++index) str1 = str1 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length)];
			string str2 = str1 + (object)Random.Range(0, 10);
			for (int index = 3; index < 5; ++index) str2 = str2 + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length - 10)];
			widget.serial = str2 + Random.Range(0, 10);
		}
		else if (sntype == SerialNumberType.RANDOM_ANY)
		{
			string res = string.Empty;
			for (int index = 0; index < 6; ++index) res = res + SerialNumberPossibleCharArray[Random.Range(0, SerialNumberPossibleCharArray.Length)];
			widget.serial = res;
		}
		else
		{
			widget.serial = sn;
		}

		widget.serialTextMesh.text = widget.serial;

		Debug.Log("Serial: " + widget.serial);
		return widget;
	}

	public override string GetResult(string key, string data)
	{
		if (key == KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER)
		{
			return JsonConvert.SerializeObject((object)new Dictionary<string, string>()
			{
				{
					"serial", serial
				}
			});
		}
		return null;
	}
}