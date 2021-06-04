using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Linq;

/// <summary>A class to add and access shared modded APIs for things like exploding the bomb.</summary>
static class ModdedAPI
{
	private static IDictionary<string, object> sharedAPI;
	private static IDictionary<string, object> API
	{
		get
		{
			EnsureSharedAPI();

			return sharedAPI;
		}
	}

	private static Type propertyType;

	/// <summary>
	/// Adds a new property with a getter and setter. The getter or setter can be null.
	/// If at anytime you can't handle a property, disable the property using <see cref="SetEnabled(object, bool)"/>.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="get">The getter of the property.</param>
	/// <param name="set">The setter of the property.</param>
	/// <returns>An object that represents the property. Used in <see cref="SetEnabled(object, bool)"/>.</returns>
	public static object AddProperty(string name, Func<object> get, Action<object> set)
	{
		var sharedAPIType = API.GetType();
		var addPropertyMethod = sharedAPIType.GetMethod("AddProperty", BindingFlags.Public | BindingFlags.Instance);
		return addPropertyMethod.Invoke(sharedAPI, new object[] { name, get, set });
	}

	/// <summary>Sets whether or not a property is enabled. This allows other mods to handle the property instead.</summary>
	/// <param name="property">The property object from the <see cref="AddProperty(string, Func{object}, Action{object})"/> method.</param>
	/// <param name="enabled">Whether or not the property should be enabled or disabled.</param>
	public static void SetEnabled(object property, bool enabled)
	{
		propertyType.GetField("Enabled").SetValue(property, enabled);
	}

	/// <summary>Gets a value from a property.</summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="name">The name of the property.</param>
	/// <param name="value">The variable to put the value into.</param>
	/// <returns>Whether or not a value was read succesfully.</returns>
	public static bool TryGetAs<T>(string name, out T value)
	{
#pragma warning disable IDE0018, IDE0038
		object objectValue;
		if (API.TryGetValue(name, out objectValue) && objectValue is T)
#pragma warning restore
		{
			value = (T) objectValue;
			return true;
		}

#pragma warning disable IDE0034, IDE0034WithoutSuggestion, RCS1244
		value = default(T);
#pragma warning restore
		return false;
	}

	/// <summary>Sets a value to a property.</summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="value">The value to put into the property.</param>
	/// <returns>Whether or not a value was written sucessfully.</returns>
	public static bool TrySet(string name, object value)
	{
		if (!API.ContainsKey(name))
			return false;

		API[name] = value;
		return true;
	}

	private static void EnsureSharedAPI()
	{
		if (sharedAPI != null)
			return;

#pragma warning disable RCS1128
		var apiObject = GameObject.Find("ModdedAPI_Info");
#pragma warning restore
		if (apiObject == null)
		{
			apiObject = new GameObject("ModdedAPI_Info", typeof(ModdedAPIBehaviour));
			UnityEngine.Object.DontDestroyOnLoad(apiObject);
		}

		sharedAPI = apiObject.GetComponent<IDictionary<string, object>>();
		propertyType = sharedAPI.GetType().GetNestedType("Property");
	}
}

// This class originally came from Multiple Bombs, written by Lupo511. Modified a bit from the original.
public class ModdedAPIBehaviour : MonoBehaviour, IDictionary<string, object>
{
	public class Property
	{
		private readonly Func<object> _getDelegate;

		private readonly Action<object> _setDelegate;

		public bool Enabled = true;

		public Property(Func<object> get, Action<object> set)
		{
			_getDelegate = get;
			_setDelegate = set;
		}

		public object Get()
		{
			return _getDelegate();
		}

		public bool CanSet()
		{
			return _setDelegate != null;
		}

		public void Set(object value)
		{
			_setDelegate(value);
		}
	}

	private readonly Dictionary<string, List<Property>> _properties;

	public ModdedAPIBehaviour()
	{
		_properties = new Dictionary<string, List<Property>>();
	}

	public Property AddProperty(string name, Func<object> get, Action<object> set)
	{
#pragma warning disable IDE0018
		List<Property> subproperties;
#pragma warning restore
		if (!_properties.TryGetValue(name, out subproperties))
		{
			subproperties = new List<Property>();
			_properties.Add(name, subproperties);
		}

		var property = new Property(get, set);
		subproperties.Add(property);
		return property;
	}

	public object this[string key]
	{
#pragma warning disable IDE0027
		get { return GetEnabledProperty(key).Get(); }
#pragma warning restore
		set
		{
			if (!_properties.ContainsKey(key))
			{
				throw new Exception("You can't add items to this Dictionary.");
			}
			Property property = GetEnabledProperty(key);
			if (!property.CanSet())
			{
				throw new Exception("The key \"" + key + "\" cannot be set (it is read-only).");
			}
			property.Set(value);
		}
	}

#pragma warning disable IDE0025
	public int Count
	{
		get
		{
			return _properties.Count;
		}
	}

	public bool IsReadOnly
	{
		get
		{
			return false;
		}
	}

	public ICollection<string> Keys
	{
		get
		{
			return _properties.Keys.ToList();
		}
	}

	public ICollection<object> Values
	{
		get { throw new NotSupportedException("The Values property is not supported in this Dictionary."); }
	}
#pragma warning restore

	public void Add(KeyValuePair<string, object> item)
	{
		throw new NotSupportedException("You can't add items to this Dictionary.");
	}

	public void Add(string key, object value)
	{
		throw new NotSupportedException("You can't add items to this Dictionary.");
	}

	public void Clear()
	{
		throw new NotSupportedException("You can't clear this Dictionary.");
	}

	public bool Contains(KeyValuePair<string, object> item)
	{
		throw new NotSupportedException("The Contains method is not supported in this Dictionary.");
	}

	public bool ContainsKey(string key)
	{
		return _properties.ContainsKey(key);
	}

	public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new NotSupportedException("The CopyTo method is not supported in this Dictionary.");
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		throw new NotSupportedException("The GetEnumerator method is not supported in this Dictionary.");
	}

	public bool Remove(KeyValuePair<string, object> item)
	{
		throw new NotSupportedException("The Remove method is not supported in this Dictionary.");
	}

	public bool Remove(string key)
	{
		throw new NotSupportedException("The Remove method is not supported in this Dictionary.");
	}

	public bool TryGetValue(string key, out object value)
	{
		bool result;
		try
		{
			value = GetEnabledProperty(key).Get();
			result = true;
		}
		catch
		{
			value = null;
			result = false;
		}
		return result;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotSupportedException("The GetEnumerator method is not supported in this Dictionary.");
	}

	private Property GetEnabledProperty(string name)
	{
		return _properties[name].Find(property => property.Enabled);
	}
}