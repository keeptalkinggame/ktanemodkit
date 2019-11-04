using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
// ReSharper disable UnusedMember.Global

public static class GeneralExtensions
{
    public static bool EqualsAny(this object obj, params object[] targets)
    {
        return targets.Contains(obj);
    }

    public static bool InRange(this int num, int min, int max)
    {
        return min <= num && num <= max;
    }

    public static string FormatTime(this float seconds)
    {
        bool addMilliseconds = seconds < 60;
        int[] timeLengths = { 86400, 3600, 60, 1 };
        List<int> timeParts = new List<int>();

        if (seconds < 1)
        {
            timeParts.Add(0);
        }
        else
        {
            foreach (int timeLength in timeLengths)
            {
                int time = (int) (seconds / timeLength);
                if (time > 0 || timeParts.Count > 0)
                {
                    timeParts.Add(time);
                    seconds -= time * timeLength;
                }
            }
        }

        string formatedTime = string.Join(":", timeParts.Select((time, i) => timeParts.Count > 2 && i == 0 ? time.ToString() : time.ToString("00")).ToArray());
        if (addMilliseconds) formatedTime += ((int) (seconds * 100)).ToString(@"\.00");

        return formatedTime;
    }

    public static string Join<T>(this IEnumerable<T> values, string separator = " ")
    {
        StringBuilder stringBuilder = new StringBuilder();
        IEnumerator<T> enumerator = values.GetEnumerator();
        if (enumerator.MoveNext()) stringBuilder.Append(enumerator.Current); else return "";

        while (enumerator.MoveNext()) stringBuilder.Append(separator).Append(enumerator.Current);

        return stringBuilder.ToString();
    }

    //String wrapping code from http://www.java2s.com/Code/CSharp/Data-Types/ForcesthestringtowordwrapsothateachlinedoesntexceedthemaxLineLength.htm
    public static string Wrap(this string str, int maxLength)
    {
        return Wrap(str, maxLength, "");
    }

    public static string Wrap(this string str, int maxLength, string prefix)
    {
        if (string.IsNullOrEmpty(str)) return "";
        if (maxLength <= 0) return prefix + str;

        var lines = new List<string>();

        // breaking the string into lines makes it easier to process.
        foreach (string line in str.Split("\n".ToCharArray()))
        {
            var remainingLine = line.Trim();
            do
            {
                var newLine = GetLine(remainingLine, maxLength - prefix.Length);
                lines.Add(newLine);
                remainingLine = remainingLine.Substring(newLine.Length).Trim();
                // Keep iterating as int as we've got words remaining 
                // in the line.
            } while (remainingLine.Length > 0);
        }

        return string.Join("\n" + prefix, lines.ToArray());
    }

    private static string GetLine(string str, int maxLength)
    {
        // The string is less than the max length so just return it.
        if (str.Length <= maxLength) return str;

        // Search backwords in the string for a whitespace char
        // starting with the char one after the maximum length
        // (if the next char is a whitespace, the last word fits).
        for (int i = maxLength; i >= 0; i--)
        {
            if (char.IsWhiteSpace(str[i]))
                return str.Substring(0, i).TrimEnd();
        }

        // No whitespace chars, just break the word at the maxlength.
        return str.Substring(0, maxLength);
    }

    public static int? TryParseInt(this string number)
    {
        int i;
        return int.TryParse(number, out i) ? (int?) i : null;
    }

    public static bool ContainsIgnoreCase(this string str, string value)
    {
        return str.ToLowerInvariant().Contains(value.ToLowerInvariant());
    }

    public static bool EqualsIgnoreCase(this string str, string value)
    {
        return str.Equals(value, StringComparison.InvariantCultureIgnoreCase);
    }

    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
    {
        return source.Skip(Math.Max(0, source.Count() - N));
    }

    public static bool RegexMatch(this string str, params string[] patterns)
    {
        Match _;
        return str.RegexMatch(out _, patterns);
    }

    public static bool RegexMatch(this string str, out Match match, params string[] patterns)
    {
        if (patterns == null) throw new ArgumentNullException("patterns");
        match = null;
        foreach (string pattern in patterns)
        {
            try
            {
                Regex r = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                match = r.Match(str);
                if (match.Success)
                    return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        return false;
    }

    public static double TotalSeconds(this DateTime datetime)
    {
        return TimeSpan.FromTicks(datetime.Ticks).TotalSeconds;
    }

    public static bool TryEquals(this string str, string value)
    {
        if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value);
        if (str == null && value == null) return true;
        if (str == string.Empty && value == string.Empty) return true;
        return false;
    }

    public static bool TryEquals(this string str, string value, StringComparison comparisonType)
    {
        if (!string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(value)) return str.Equals(value, comparisonType);
        if (str == null && value == null) return true;
        if (str == string.Empty && value == string.Empty) return true;
        return false;
    }

    /// <summary>
    ///     Adds an element to a List&lt;V&gt; stored in the current IDictionary&lt;K, List&lt;V&gt;&gt;. If the specified
    ///     key does not exist in the current IDictionary, a new List is created.</summary>
    /// <typeparam name="K">
    ///     Type of the key of the IDictionary.</typeparam>
    /// <typeparam name="V">
    ///     Type of the values in the Lists.</typeparam>
    /// <param name="dic">
    ///     IDictionary to operate on.</param>
    /// <param name="key">
    ///     Key at which the list is located in the IDictionary.</param>
    /// <param name="value">
    ///     Value to add to the List located at the specified Key.</param>
    public static void AddSafe<K, V>(this IDictionary<K, List<V>> dic, K key, V value)
    {
        if (dic == null)
            throw new ArgumentNullException("dic");
        if (key == null)
            throw new ArgumentNullException("key", "Null values cannot be used for keys in dictionaries.");
        if (!dic.ContainsKey(key))
            dic[key] = new List<V>();
        dic[key].Add(value);
    }

    /// <summary>
    ///     Brings the elements of the given list into a random order.</summary>
    /// <typeparam name="T">
    ///     Type of elements in the list.</typeparam>
    /// <param name="list">
    ///     List to shuffle.</param>
    /// <returns>
    ///     The list operated on.</returns>
    public static T Shuffle<T>(this T list) where T : IList
    {
        if (list == null)
            throw new ArgumentNullException("list");
        for (int j = list.Count; j >= 1; j--)
        {
            int item = UnityEngine.Random.Range(0, j);
            if (item < j - 1)
            {
                var t = list[item];
                list[item] = list[j - 1];
                list[j - 1] = t;
            }
        }
        return list;
    }

    /// <summary>
    ///     Returns a random element from the specified collection.</summary>
    /// <typeparam name="T">
    ///     The type of the elements in the collection.</typeparam>
    /// <param name="src">
    ///     The collection to pick from.</param>
    /// <param name="rnd">
    ///     Optionally, a random number generator to use.</param>
    /// <returns>
    ///     The element randomly picked.</returns>
    /// <remarks>
    ///     This method enumerates the entire input sequence into an array.</remarks>
    public static T PickRandom<T>(this IEnumerable<T> src)
    {
        var list = (src as IList<T>) ?? src.ToArray();
        if (list.Count == 0)
            throw new InvalidOperationException("Cannot pick an element from an empty set.");
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static bool TryParseTime(this string timeString, out float time)
    {
        float _;
        int[] multiplier = new int[] { 0, 1, 60, 3600, 86400 };

        string[] split = timeString.Split(new[] { ':' }, StringSplitOptions.None);
        float[] splitFloat = split.Where(x => float.TryParse(x, out _)).Select(float.Parse).ToArray();

        if (split.Length != splitFloat.Length)
        {
            time = 0;
            return false;
        }

        time = splitFloat.Select((t, i) => t * multiplier[split.Length - i]).Sum();
        return true;
    }

    public static Color Color(int red, int green, int blue, int alpha = 255)
    {
        return new Color(red / 255f, green / 255f, blue / 255f, alpha / 255f);
    }

    public static Color Color(this string colorString)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(colorString, out color)) return color;
        ColorUtility.TryParseHtmlString("#" + colorString, out color);
        return color;
    }

    [StringFormatMethod("message")]
    public static void AppendLineFormat(this StringBuilder builder, string message, params object[] args)
    {
        builder.AppendLine(string.Format(message, args));
    }

    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        var i = 0;
        foreach (var elem in source)
        {
            if (predicate(elem))
                return i;
            i++;
        }
        return -1;
    }
}
