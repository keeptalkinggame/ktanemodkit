using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ExampleWidget : MonoBehaviour
{
    public string WIDGET_QUERY_KEY = "example";
    public TextMesh NumberText;
    int number;

    void Awake()
    {
        GetComponent<KMWidget>().OnQueryRequest += GetQueryResponse;
        GetComponent<KMWidget>().OnWidgetActivate += Activate;
        number = Random.Range(0, 1000);
    }

    //This happens when the bomb turns on, don't turn on any lights or unlit shaders until activate
    public void Activate()
    {
        NumberText.text = "" + number;
    }

    public string GetQueryResponse(string queryKey, string queryInfo)
    {
        if(queryKey == WIDGET_QUERY_KEY)
        {
            Dictionary<string, int> response = new Dictionary<string, int>();
            response.Add("numbertext", number);
            string responseStr = JsonConvert.SerializeObject(response);
            return responseStr;
        }

        return "";
    }
}
