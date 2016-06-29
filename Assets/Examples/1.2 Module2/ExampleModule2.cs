using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ExampleModule2 : MonoBehaviour
{
    public KMSelectable[] buttons;

    int correctIndex;

    void Start()
    {
        Init();
    }

    void Init()
    {
        correctIndex = Random.Range(0, 4);
        GetComponent<KMBombModule>().OnActivate += OnActivate;
        
        for (int i = 0; i < buttons.Length; i++)
        {
            string label = i == correctIndex ? "A" : "B";

            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = label;
            int j = i;
            buttons[i].OnInteract += delegate () { Debug.Log("Press #" + j); OnPress(j == correctIndex); return false; };
        }
    }

    void OnActivate()
    {
        foreach (string query in new List<string> { KMBombInfo.QUERYKEY_GET_BATTERIES, KMBombInfo.QUERYKEY_GET_INDICATOR, KMBombInfo.QUERYKEY_GET_PORTS, KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, "example"})
        {
            List<string> queryResponse = GetComponent<KMBombInfo>().QueryWidgets(query, null);

            if (queryResponse.Count > 0)
            {
                Debug.Log(queryResponse[0]);
            }
        }

        int batteryCount = 0;
        List<string> responses = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_BATTERIES, null);
        foreach (string response in responses)
        {
            Dictionary<string, int> responseDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(response);
            batteryCount += responseDict["numbatteries"];
        }

        Debug.Log("Battery count: " + batteryCount);
    }

    void OnPress(bool correctButton)
    {
        Debug.Log("Pressed " + correctButton + " button");
        if (correctButton)
        {
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }
    }
}
