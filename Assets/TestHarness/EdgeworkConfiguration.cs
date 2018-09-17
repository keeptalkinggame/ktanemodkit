using System.Collections.Generic;
using UnityEngine;

namespace EdgeworkConfigurator
{
    public class EdgeworkConfiguration : ScriptableObject
    {
        public SerialNumberType SerialNumberType;
        public string CustomSerialNumber;
	    [Range(30,999)] public int TwoFactorResetTime = 30;

        public List<THWidget> Widgets;
    }

    public enum SerialNumberType
    {
        RANDOM_NORMAL,
        RANDOM_ANY,
        CUSTOM
    }
}