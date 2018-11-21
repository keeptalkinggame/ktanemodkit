using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
		string format = "{0:D" + NumDigits + "}";
		DisplayText.text = @on ? string.Format(format, displayValue) : string.Empty;
		int digitLength = Mathf.Max(NumDigits, DisplayText.text.Length);
		BackgroundText.text = string.Format(format, Enumerable.Range(0, digitLength).Select(x => "8").Join(""));

		float textScale = (0.04f / digitLength * 2);
		DisplayText.transform.localPosition = new Vector3(textScale, DisplayText.transform.localPosition.y, DisplayText.transform.localPosition.z);
		BackgroundText.transform.localPosition = new Vector3(textScale, BackgroundText.transform.localPosition.y, BackgroundText.transform.localPosition.z);

		DisplayText.transform.localScale = new Vector3(textScale, textScale, 1.5f);
		BackgroundText.transform.localScale = new Vector3(textScale, textScale, 1.5f);

		DisplayActiveBacking.SetActive(on);
		DisplayInactiveBacking.SetActive(!on);
	}

	private void Awake()
	{
		UpdateDisplay();
	}

	public TextMesh DisplayText;
	public TextMesh BackgroundText;
	public int NumDigits;
	public GameObject DisplayActiveBacking;
	public GameObject DisplayInactiveBacking;
	protected int displayValue;
	protected bool on;
}
