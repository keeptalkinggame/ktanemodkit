namespace EdgeworkConfigurator
{
    [System.Serializable]
    public class THWidget
    {
        public WidgetType Type;
        public int Count = 1;

        // Port Plates
        public PortPlateType PortPlateType;
        public bool DVIPort;
        public bool ParallelPort;
        public bool PS2Port;
        public bool RJ45Port;
        public bool SerialPort;
        public bool StereoRCAPort;
	    public bool ComponentVideoPort;
	    public bool CompositeVideoPort;
	    public bool HDMIPort;
	    public bool VGAPort;
	    public bool USBPort;
	    public bool ACPort;
	    public bool PCMCIAPort;
        public string[] CustomPorts;

        // Batteries
        public BatteryType BatteryType;
        public int BatteryCount;
        public int MinBatteries;
        public int MaxBatteries;

        // Indicators
        public IndicatorLabel IndicatorLabel;
        public string CustomLabel;
        public IndicatorState IndicatorState;

        // Custom
        public string CustomQueryKey;
        public string CustomData;
    }

    public enum WidgetType
    {
        BATTERY,
        PORT_PLATE,
        INDICATOR,
		TWOFACTOR,
        RANDOM,
        CUSTOM
    }

    public enum PortPlateType {
        CUSTOM,
        RANDOM_NORMAL,
        RANDOM_ANY
    }

    public enum BatteryType
    {
        EMPTY = 0,
        ONE = 1,
        TWO = 2,
        THREE = 3,
        FOUR = 4,
        D = 1,
        AA = 2,
        RANDOM = 10,
        CUSTOM = 20
    }

    public enum IndicatorLabel
    {
        SND,
        CLR,
        CAR,
        IND,
        FRQ,
        SIG,
        NSA,
        MSA,
        TRN,
        BOB,
        FRK,
        NLL,
        RANDOM,
        CUSTOM
    }

    public enum IndicatorState
    {
        ON,
        OFF,
        RANDOM
    }
}