using UnityEngine;

public class ExampleModule : MonoBehaviour
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

        for(int i = 0; i < buttons.Length; i++)
        {
            string label = i == correctIndex ? "O" : "X";

            TextMesh buttonText = buttons[i].GetComponentInChildren<TextMesh>();
            buttonText.text = label;
            int j = i;
            buttons[i].OnInteract += delegate () { OnPress(j == correctIndex); return false; };
        }
    }

    void OnPress(bool correctButton)
    {
        Debug.Log("Pressed " + correctButton + " button");
        if(correctButton)
        {
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }
    }
}
