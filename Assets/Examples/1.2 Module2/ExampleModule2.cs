using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ExampleModule2 : MonoBehaviour
{
    public KMSelectable[] buttons;
    KMAudio.KMAudioRef audioRef;
    int correctIndex;

    void Start()
    {
        Init();
    }

    void Init()
    {
        correctIndex = Random.Range(0, 4);
        GetComponent<KMBombModule>().OnActivate += OnActivate;
        GetComponent<KMSelectable>().OnCancel += OnCancel;
        GetComponent<KMSelectable>().OnLeft += OnLeft;
        GetComponent<KMSelectable>().OnLeft += OnRight;
        GetComponent<KMSelectable>().OnSelect += OnSelect;
        GetComponent<KMSelectable>().OnDeselect += OnDeselect;
        GetComponent<KMSelectable>().OnHighlight += OnHighlight;

        for (int i = 0; i < buttons.Length; i++)
        {
            string label = i == correctIndex ? "A" : "B";

            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = label;
            int j = i;
            buttons[i].OnInteract += delegate () { Debug.Log("Press #" + j); OnPress(j == correctIndex); return false; };
            buttons[i].OnInteractEnded += OnRelease;
        }
    }

    private void OnDeselect()
    {
        Debug.Log("ExampleModule2 OnDeselect.");
    }

    private void OnLeft()
    {
        Debug.Log("ExampleModule2 OnLeft.");
    }

    private void OnRight()
    {
        Debug.Log("ExampleModule2 OnRight.");
    }

    private void OnSelect()
    {
        Debug.Log("ExampleModule2 OnSelect.");
    }

    private void OnHighlight()
    {
        Debug.Log("ExampleModule2 OnHighlight.");
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

    bool OnCancel()
    {
        Debug.Log("ExampleModule2 cancel.");

        return true;
    }

    //On pressing button a looped sound will play
    void OnPress(bool correctButton)
    {
        Debug.Log("Pressed " + correctButton + " button");

        if (correctButton)
        {
            audioRef = GetComponent<KMAudio>().PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.AlarmClockBeep, transform);
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            audioRef = GetComponent<KMAudio>().PlaySoundAtTransformWithRef("doublebeep125", transform);
        }
    }

    //On releasing a button a looped sound will stop
    void OnRelease()
    {
        Debug.Log("OnInteractEnded Released");
        if(audioRef != null && audioRef.StopSound != null)
        {
            audioRef.StopSound();
        }
    }
}
