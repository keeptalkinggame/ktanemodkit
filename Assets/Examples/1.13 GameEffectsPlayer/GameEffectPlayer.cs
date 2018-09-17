using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameEffectPlayer : MonoBehaviour
{
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMAudio KMAudio;
	public KMSelectable Left;
	public KMSelectable Button;
	public KMSelectable Right;
	public TextMesh Counter;

	private int _index;
	public static List<KMSoundOverride.SoundEffect> Effects =
		Enum.GetValues(typeof(KMSoundOverride.SoundEffect)).Cast<KMSoundOverride.SoundEffect>().ToList();

	private KMAudio.KMAudioRef _audioRef;


	private bool HandlePlay()
	{
		if (_audioRef != null && _audioRef.StopSound != null)
			_audioRef.StopSound.Invoke();
		else
			BombModule.HandlePass();

		_audioRef = KMAudio.HandlePlayGameSoundAtTransformWithRef(Effects[_index], transform);
		return false;
	}

	void Start()
	{
		Left.OnInteract += HandleLeft;
		Button.OnInteract += HandlePlay;
		Right.OnInteract += HandleRight;
		_index = Random.Range(0, Effects.Count);
		Counter.text = Effects[_index].ToString();
	}

	private bool HandleRight()
	{
		_index++;
		_index %= Effects.Count;
		Counter.text = Effects[_index].ToString();
		return false;
	}

	private bool HandleLeft()
	{
		_index += Effects.Count - 1;
		_index %= Effects.Count;
		Counter.text = Effects[_index].ToString();
		return false;
	}
}
