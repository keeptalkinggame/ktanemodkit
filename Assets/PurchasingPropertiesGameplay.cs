using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasingPropertiesGameplay : MonoBehaviour {
    private int lol = 12;
    private static int NumModules = 0;
    private int MyModuleID;

    [HideInInspector]
    public KMSelectable[] me;
    public KMBombModule myModule;
    // public KMGameInfo theGame;

	// Use this for initialization
	void Start () {
        MyModuleID = NumModules++;
        me[0].OnInteract += MyMethod; // OnInteract is like a stack, and we are adding myMethod to the list of things to call when the event before the dot occurs
        //theGame.OnLightsChange += someOtherMethod;
    }


    //in some method if there is a strike
    // do
    
        
    //private void someothermethod(bool on)
    //{
    //    throw new notimplementedexception();
    //}

    private bool GrabEdgeWork()
    {
        return false;
    }

    private bool MyMethod()
    {
        if (++lol > 13)
        {
            myModule.HandleStrike();
        }
        Debug.Log("Test log");
        return false;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
