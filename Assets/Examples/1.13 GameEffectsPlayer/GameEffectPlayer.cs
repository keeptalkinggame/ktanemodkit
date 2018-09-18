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
	public KMSelectable Stop;
	public KMSelectable Right;
	public TextMesh Counter;

	private int _index;
	public static List<KMSoundOverride.SoundEffect> Effects =
		Enum.GetValues(typeof(KMSoundOverride.SoundEffect)).Cast<KMSoundOverride.SoundEffect>().ToList();

	private KMAudio.KMAudioRef _audioRef;
	private List<KMAudio.KMAudioRef> _audioRefs = new List<KMAudio.KMAudioRef>();


	private bool HandlePlay()
	{
		BombModule.HandlePass();
		_audioRef = KMAudio.HandlePlayGameSoundAtTransformWithRef(Effects[_index], transform);
		if (_audioRef != null && _audioRef.StopSound != null)
			_audioRefs.Add(_audioRef);
		return false;
	}

	void Start()
	{
		Left.OnInteract += HandleLeft;
		Button.OnInteract += HandlePlay;
		Right.OnInteract += HandleRight;
		Stop.OnInteract += HandleStop;
		_index = Random.Range(0, Effects.Count);
		Counter.text = Effects[_index].ToString();
	}

	private bool HandleStop()
	{
		while (_audioRefs.Count > 0)
		{
			try
			{
				if (_audioRefs[0] != null && _audioRefs[0].StopSound != null)
					_audioRefs[0].StopSound();
			}
			catch { /**/ }

			_audioRefs.Remove(_audioRefs[0]);
		}
		return false;
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
