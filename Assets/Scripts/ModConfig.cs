using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using UnityEngine;

#pragma warning disable RCS1128

class ModConfig<T> where T : new()
{
	public ModConfig(string filename, Action<Exception> onRead = null)
	{
		var settingsFolder = Path.Combine(Application.persistentDataPath, "Modsettings");
		// The persistent data path is different in the editor compared to the real game, so the Modsettings folder might not exist.
		if (Application.isEditor && !Directory.Exists(settingsFolder))
			Directory.CreateDirectory(settingsFolder);

		settingsPath = Path.Combine(settingsFolder, filename + ".json");
		OnRead = onRead;
	}

	private readonly string settingsPath;

	/// <summary>Serializes settings the same way it's written to the file. Supports settings that use enums.</summary>
	public static string SerializeSettings(T settings)
	{
		return JsonConvert.SerializeObject(settings, Formatting.Indented, new StringEnumConverter());
	}

	private static readonly object settingsFileLock = new object();

	/// <summary>Whether or not there has been a successful read of the settings file.</summary>
	public bool SuccessfulRead;
	/// <summary>Called every time the settings are read. Parameter is null if the read was successful or an exception if it wasn't.</summary>
	public Action<Exception> OnRead;

	/// <summary>
	/// Reads the settings from the settings file.
	/// If the settings couldn't be read, the default settings will be returned.
	/// </summary>
	public T Read()
	{
		try
		{
			lock (settingsFileLock)
			{
				if (!File.Exists(settingsPath))
				{
					File.WriteAllText(settingsPath, SerializeSettings(new T()));
				}

				T deserialized = JsonConvert.DeserializeObject<T>(File.ReadAllText(settingsPath));
				if (deserialized == null)
					deserialized = new T();

				SuccessfulRead = true;
				if (OnRead != null)
					OnRead.Invoke(null);
				return deserialized;
			}
		}
		catch (Exception e)
		{
			Debug.LogFormat("An exception has occurred while attempting to read the settings from {0}\nDefault settings will be used for the type of {1}.", settingsPath, typeof(T).ToString());
			Debug.LogException(e);

			SuccessfulRead = false;
			if (OnRead != null)
				OnRead.Invoke(e);
			return new T();
		}
	}

	/// <summary>
	/// Writes the settings to the settings file.
	/// To protect the user settings, this does nothing if the last read wasn't successful.
	/// </summary>
	public void Write(T value)
	{
		if (!SuccessfulRead)
			return;

		lock (settingsFileLock)
		{
			try
			{
				File.WriteAllText(settingsPath, SerializeSettings(value));
			}
			catch (Exception e)
			{
				Debug.LogFormat("Failed to write to {0}", settingsPath);
				Debug.LogException(e);
			}
		}
	}

	public override string ToString()
	{
		return SerializeSettings(Read());
	}
}