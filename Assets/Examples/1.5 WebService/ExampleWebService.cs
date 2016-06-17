using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System;

public class ExampleWebService : MonoBehaviour
{
    KMBombInfo bombInfo;
    KMGameCommands gameCommands;
    string modules;
    string solvableModules;
    string solvedModules;

    bool shouldStartMission = false;
    string missionId;
    string seed;

    Thread workerThread;

    void Awake()
    {
        bombInfo = GetComponent<KMBombInfo>();
        gameCommands = GetComponent<KMGameCommands>();
        // Create the thread object. This does not start the thread.
        Worker workerObject = new Worker(this);
        workerThread = new Thread(workerObject.DoWork);
        // Start the worker thread.
        workerThread.Start(this);
    }

    void Update()
    {
        if(shouldStartMission)
        {
            shouldStartMission = false;
            gameCommands.StartMission(missionId, seed);
        }
    }

    void OnDestroy()
    {
        workerThread.Abort();
    }

    // This example requires the System and System.Net namespaces.
    public void SimpleListenerExample(string[] prefixes)
    {
        // Create a listener.
        HttpListener listener = new HttpListener();
        // Add the prefixes.
        foreach (string s in prefixes)
        {
            listener.Prefixes.Add(s);
        }
        listener.Start();
        while(true)
        {
            // Note: The GetContext method blocks while waiting for a request. 
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = "";

            if(request.Url.OriginalString.Contains("bombInfo"))
            {
                responseString = GetBombInfo();
            }

            if(request.Url.OriginalString.Contains("startMission"))
            {
                string missionId = request.QueryString.Get("missionId");
                string seed = request.QueryString.Get("seed");
                responseString = StartMission(missionId, seed);
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
        }
    }

    protected string StartMission(string missionId, string seed)
    {
        this.missionId = missionId;
        this.seed = seed;
        shouldStartMission = true;

        return missionId + " " + seed;
    }

    protected string GetBombInfo()
    {
        string time = bombInfo.GetFormattedTime();
        int strikes = bombInfo.GetStrikes();
        modules = GetListAsHTML(bombInfo.GetModuleNames());
        solvableModules = GetListAsHTML(bombInfo.GetSolvableModuleNames());
        solvedModules = GetListAsHTML(bombInfo.GetSolvedModuleNames());


        string responseString = string.Format(
            "<HTML><BODY>"
            + "<span>Time: {0}</span><br>"
            + "<span>Strikes: {1}</span><br>"
            + "<span>Modules: {2}</span><br>"
            + "<span>Solvable Modules: {3}</span><br>"
            + "<span>Solved Modules: {4}</span>"
            + "</BODY></HTML>", time, strikes, modules, solvableModules, solvedModules);

        return responseString;
    }

    protected string GetListAsHTML(List<string> list)
    {
        string listString = "";

        foreach(string s in list)
        {
            listString += s + ", ";
        }

        return listString;
    }

    public class Worker
    {
        ExampleWebService service;

        public Worker(ExampleWebService s)
        {
            service = s;
        }

        // This method will be called when the thread is started. 
        public void DoWork()
        {
            service.SimpleListenerExample(new string[] { "http://localhost:8085/" });
        }
    }
}