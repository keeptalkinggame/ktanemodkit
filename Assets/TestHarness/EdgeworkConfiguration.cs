using System.Collections.Generic;
using UnityEngine;

namespace EdgeworkConfigurator
{
    public class EdgeworkConfiguration : ScriptableObject
    {
        public SerialNumberType SerialNumberType;
        public string CustomSerialNumber;

        public List<THWidget> Widgets;
    }

    public enum SerialNumberType
    {
        RANDOM_NORMAL,
        RANDOM_ANY,
        CUSTOM
    }
}