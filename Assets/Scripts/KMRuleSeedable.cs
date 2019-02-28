using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class KMRuleSeedable : MonoBehaviour
{
    [SerializeField]
    private int _seed = 1;

    public MonoRandom GetRNG()
    {
        if (Application.isEditor)
            return new MonoRandom(_seed);

        GameObject ruleSeedModifierAPIGameObject = GameObject.Find("RuleSeedModifierProperties");
        if (ruleSeedModifierAPIGameObject == null) // Rule Seed Modifer is not installed
            return new MonoRandom(1);

        IDictionary<string, object> ruleSeedModifierAPI = ruleSeedModifierAPIGameObject.GetComponent<IDictionary<string, object>>();
        if (!ruleSeedModifierAPI.ContainsKey("RuleSeed"))
            return new MonoRandom(1);

		//Add the module to the list of supported modules if possible.
	    if (ruleSeedModifierAPI.ContainsKey("AddSupportedModule"))
	    {
		    string key;
		    KMBombModule bombModule = GetComponent<KMBombModule>();
		    KMNeedyModule needyModule = GetComponent<KMNeedyModule>();

		    if (bombModule != null)
			    key = bombModule.ModuleType;
		    else if (needyModule != null)
			    key = needyModule.ModuleType;
		    else
			    key = Regex.Replace(gameObject.name, @"\(Clone\)$", "");

		    ruleSeedModifierAPI["AddSupportedModule"] = key;
	    }

        return new MonoRandom((ruleSeedModifierAPI["RuleSeed"] as int?) ?? 1);
    }
}

public class MonoRandom
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Random" /> class, using a time-dependent default seed value.</summary>
    /// <exception cref="T:System.OverflowException">The seed value derived from the system clock is <see cref="F:System.Int32.MinValue" />, which causes an overflow when its absolute value is calculated. </exception>
    public MonoRandom() : this(Environment.TickCount)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Random" /> class, using the specified seed value.</summary>
    /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence. If a negative number is specified, the absolute value of the number is used. </param>
    /// <exception cref="T:System.OverflowException">
    ///   <paramref name="seed" /> is <see cref="F:System.Int32.MinValue" />, which causes an overflow when its absolute value is calculated. </exception>
    public MonoRandom(int seed)
    {
        Seed = seed;
        var num = 161803398 - Math.Abs(seed);
        _seedArray[55] = num;
        var num2 = 1;
        for (var i = 1; i < 55; i++)
        {
            var num3 = 21 * i % 55;
            _seedArray[num3] = num2;
            num2 = num - num2;
            if (num2 < 0)
            {
                num2 += int.MaxValue;
            }
            num = _seedArray[num3];
        }
        for (var j = 1; j < 5; j++)
        {
            for (var k = 1; k < 56; k++)
            {
                _seedArray[k] -= _seedArray[1 + (k + 30) % 55];
                if (_seedArray[k] < 0)
                {
                    _seedArray[k] += int.MaxValue;
                }
            }
        }
        _inext = 0;
        _inextp = 31;
    }

    /// <summary>Returns a random number between 0.0 and 1.0.</summary>
    /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    protected virtual double Sample()
    {
        if (++_inext >= 56)
        {
            _inext = 1;
        }
        if (++_inextp >= 56)
        {
            _inextp = 1;
        }
        var num = _seedArray[_inext] - _seedArray[_inextp];
        if (num < 0)
        {
            num += int.MaxValue;
        }
        _seedArray[_inext] = num;
        return (double) num * 4.6566128752457969E-10;
    }

    public T ShuffleFisherYates<T>(T list) where T : IList
    {
        // Brings an array into random order using the Fisher-Yates shuffle.
        // This is an inplace algorithm, i.e. the input array is modified.
        var i = list.Count;
        while (i > 1)
        {
            var index = Next(0, i);
            i--;
            var value = list[index];
            list[index] = list[i];
            list[i] = value;
        }
        return list;
    }

    /// <summary>Returns a nonnegative random number.</summary>
    /// <returns>A 32-bit signed integer greater than or equal to zero and less than <see cref="F:System.Int32.MaxValue" />.</returns>
    /// <filterpriority>1</filterpriority>
    public virtual int Next()
    {
        return (int) (Sample() * 2147483647.0);
    }

    /// <summary>Returns a nonnegative random number less than the specified maximum.</summary>
    /// <returns>A 32-bit signed integer greater than or equal to zero, and less than <paramref name="maxValue" />; that is, the range of return values ordinarily includes zero but not <paramref name="maxValue" />. However, if <paramref name="maxValue" /> equals zero, <paramref name="maxValue" /> is returned.</returns>
    /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to zero. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="maxValue" /> is less than zero. </exception>
    /// <filterpriority>1</filterpriority>
    public virtual int Next(int maxValue)
    {
        if (maxValue < 0)
        {
            throw new ArgumentOutOfRangeException("maxValue");
        }
        return (int) (Sample() * (double) maxValue);
    }

    /// <summary>Returns a random number within a specified range.</summary>
    /// <returns>A 32-bit signed integer greater than or equal to <paramref name="minValue" /> and less than <paramref name="maxValue" />; that is, the range of return values includes <paramref name="minValue" /> but not <paramref name="maxValue" />. If <paramref name="minValue" /> equals <paramref name="maxValue" />, <paramref name="minValue" /> is returned.</returns>
    /// <param name="minValue">The inclusive lower bound of the random number returned. </param>
    /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue" /> must be greater than or equal to <paramref name="minValue" />. </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="minValue" /> is greater than <paramref name="maxValue" />. </exception>
    /// <filterpriority>1</filterpriority>
    public virtual int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException("minValue");
        }
        var num = (uint) (maxValue - minValue);
        if (num <= 1u)
        {
            return minValue;
        }
        return (int) ((ulong) ((uint) (Sample() * num)) + (ulong) ((long) minValue));
    }

    /// <summary>Fills the elements of a specified array of bytes with random numbers.</summary>
    /// <param name="buffer">An array of bytes to contain random numbers. </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///   <paramref name="buffer" /> is null. </exception>
    /// <filterpriority>1</filterpriority>
    public virtual void NextBytes(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte) (Sample() * 256.0);
        }
    }

    /// <summary>Returns a random number between 0.0 and 1.0.</summary>
    /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    /// <filterpriority>1</filterpriority>
    public virtual double NextDouble()
    {
        return Sample();
    }

    public int Seed { get; private set; }

    private int _inext;
    private int _inextp;
    private readonly int[] _seedArray = new int[56];
}
