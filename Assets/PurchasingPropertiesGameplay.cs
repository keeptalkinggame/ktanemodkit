using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class PurchasingPropertiesGameplay : MonoBehaviour {
    
    #region Edgework Utility Classes
    private class Battery
    {
        public int numbatteries;
    }
    private class Serial
    {
        public string serial;
    }
    private class Indicator
    {
        public string label;
        public string on;
    }
    private class Port
    {
        public string[] PresentPorts;
    }
    #endregion

    public static readonly int NUM_CARDS = 12; // For determining which card is shown
    private int shownCard;

    private static int numInstances = 0; // For assigning unique debug ID's to each instance of the module
    private int moduleInstanceID;


    public KMSelectable[] Arrows = new KMSelectable[2]; // Left/Right arrows
    public KMSelectable SubmitButton;
    public KMBombModule TheModule; // Refers to the specific instance of PurchasingProperties, handles strikes/solves
    public KMBombInfo TheBomb; // Refers to the entire bomb, handles querying various properties of the bomb

    private int correctProperty;

    // Edgework variables
    private int numPortPlates;
    private int numPorts;
    private bool hasParallel;
    private bool hasEmptyPlate;

    private string serialNumber;

    private int numBatteries;
    private int numBatteryHolders;

    private int numIndicators;
    private int numLitIndicators = 0;
    private bool hasBOB;

    // Runs on per module on module creation
    void Start () {
        moduleInstanceID = numInstances++; // Assign each instance a unique ID for deebugging, increment the num total instances
        Arrows[0].OnInteract += () => { CycleCardDisplay(1); return false; } ; // OnInteract is like a stack, and we are adding CycleCardDisplay to the list of things to call when OnInteract is triggered
        Arrows[1].OnInteract += () => { CycleCardDisplay(-1); return false; } ; // TODO - Add in lambda function on these
        SubmitButton.OnInteract += Submit;
        TheModule.OnActivate += GrabEdgeWork;
    }

    private void GrabEdgeWork()
    {
        // JSON conversions from Flamanis

        // ----- Port -----
        List<string> portList = TheBomb.QueryWidgets(KMBombInfo.QUERYKEY_GET_PORTS, null);
        foreach (string port in portList)
        {
            Port plate = JsonConvert.DeserializeObject<Port>(port);
            foreach (string elem in plate.PresentPorts)
            {
                if (elem == "Parallel") hasParallel = true;
                if (elem == "") hasEmptyPlate = true;
            }
            numPorts += plate.PresentPorts.Length;
        }
        numPortPlates = portList.Count;
        
        // ----- Serial Number -----
        Serial serialNum = JsonConvert.DeserializeObject<Serial>(TheBomb.QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null)[0]);
        serialNumber = serialNum.serial.ToLower();

        // ----- Batteries -----
        List<string> batteryList = TheBomb.QueryWidgets(KMBombInfo.QUERYKEY_GET_BATTERIES, null);
        foreach (string battery in batteryList)
        {
            Battery bat = JsonConvert.DeserializeObject<Battery>(battery);
            numBatteries += bat.numbatteries;
        }
        numBatteryHolders = batteryList.Count;

        // ----- Indicators -----
        List<string> indicatorList = TheBomb.QueryWidgets(KMBombInfo.QUERYKEY_GET_INDICATOR, null);
        foreach (string indicator in indicatorList)
        {
            Indicator ind = JsonConvert.DeserializeObject<Indicator>(indicator);
            if (ind.on == "True" && ind.label == "BOB") hasBOB = true;
            if (ind.on == "True") numLitIndicators++;
        }
        numIndicators = indicatorList.Count;


        // Implement all the logic to determine from the edgwork, which property needs to be purchased
        CalculatePropToPurchase();
    }

    private bool CalculatePropToPurchase()
    {
        //TEMP
        bool condition0 = true;
        bool condition1 = true;
        bool condition2 = true;
        bool condition3 = true;
        bool condition4 = true;
        bool condition5 = true;
        bool condition6 = true;
        //ENDTEMP

        if (condition0) correctProperty = 0;
        else if (condition1) correctProperty = 1;
        else if (condition2) correctProperty = 2;
        else if (condition3) correctProperty = 3;
        else if (condition4) correctProperty = 4;
        else if (condition5) correctProperty = 5;
        else if (condition6) correctProperty = 6;

        return false;
    }

    private bool CycleCardDisplay(int dir)
    {
        shownCard = (shownCard + dir) % NUM_CARDS;
        if (shownCard < 0) shownCard += NUM_CARDS;
        Debug.Log("Module " + moduleInstanceID + " cycled the display in the " + dir + " direction, and now card " + shownCard + " is showing.");
        return false;
    }

    private bool Submit()
    {
        if (shownCard == correctProperty)
        {
            TheModule.HandlePass();
            Debug.Log("Module " + moduleInstanceID + " has been solved with the purchase of property " + shownCard + ".");
        }
        else
        {
            TheModule.HandleStrike();
            Debug.Log("Module " + moduleInstanceID + " has incurred a strike with the attempted purchase of property " + shownCard + ".");
        }

        return false;
    }

    // Update is called once per frame
    void Update () {}
}
