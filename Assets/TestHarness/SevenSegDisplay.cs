using System;
using UnityEngine;

public class SevenSegDisplay : MonoBehaviour
{
	public int DisplayValue
	{
		get
		{
			return displayValue;
		}
		set
		{
			if (displayValue == value) return;
			displayValue = value;
			UpdateDisplay();
		}
	}

	public bool On
	{
		get
		{
			return on;
		}
		set
		{
			if (on == value) return;
			on = value;
			UpdateDisplay();
		}
	}

	protected void UpdateDisplay()
	{
		if (on)
		{
			string format = "{0:D" + NumDigits + "}";
			DisplayText.text = string.Format(format, displayValue);
		}
		else
		{
			DisplayText.text = string.Empty;
		}
		DisplayActiveBacking.SetActive(on);
		DisplayInactiveBacking.SetActive(!on);
	}

	private void Awake()
	{
		UpdateDisplay();
	}

	public TextMesh DisplayText;
	public int NumDigits;
	public GameObject DisplayActiveBacking;
	public GameObject DisplayInactiveBacking;
	protected int displayValue;
	protected bool on;
}
