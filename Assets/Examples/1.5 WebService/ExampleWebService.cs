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
    string bombState;

    Thread workerThread;
    Worker workerObject;
    Queue<Action> actions;

    void Awake()
    {
        actions = new Queue<Action>();
        bombInfo = GetComponent<KMBombInfo>();
        bombInfo.OnBombExploded += OnBombExplodes;
        bombInfo.OnBombSolved += OnBombDefused;
        gameCommands = GetComponent<KMGameCommands>();
        bombState = "NA";
        // Create the thread object. This does not start the thread.
        workerObject = new Worker(this);
        workerThread = new Thread(workerObject.DoWork);
        // Start the worker thread.
        workerThread.Start(this);
    }

    void Update()
    {
        if(actions.Count > 0)
        {
            Action action = actions.Dequeue();
            action();
        }
    }

    void OnDestroy()
    {
        workerThread.Abort();
        workerObject.Stop();
    }

    // This example requires the System and System.Net namespaces.
    public void SimpleListenerExample(HttpListener listener)
    {
        while (true)
        {
            // Note: The GetContext method blocks while waiting for a request. 
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = "";

            if (request.Url.OriginalString.Contains("bombInfo"))
            {
                responseString = GetBombInfo();
            }

            if (request.Url.OriginalString.Contains("startMission"))
            {
                string missionId = request.QueryString.Get("missionId");
                string seed = request.QueryString.Get("seed");
                responseString = StartMission(missionId, seed);
            }

            if (request.Url.OriginalString.Contains("causeStrike"))
            {
                string reason = request.QueryString.Get("reason");
                responseString = CauseStrike(reason);
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
        actions.Enqueue(delegate () { gameCommands.StartMission(missionId, seed); });

        return missionId + " " + seed;
    }

    protected string CauseStrike(string reason)
    {
        actions.Enqueue(delegate () { gameCommands.CauseStrike(reason); });

        return reason;
    }

    protected string GetBombInfo()
    {
        if(bombInfo.IsBombPresent())
        {
            if(bombState == "NA")
            {
                bombState = "Active";
            }
        }
        else if(bombState == "Active")
        {
            bombState = "NA";
        }

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
            + "<span>Solved Modules: {4}</span><br>"
            + "<span>State: {5}</span><br>"
            + "</BODY></HTML>", time, strikes, modules, solvableModules, solvedModules, bombState);

        return responseString;
    }

    protected void OnBombExplodes()
    {
        bombState = "Exploded";
    }

    protected void OnBombDefused()
    {
        bombState = "Defused";
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
        HttpListener listener;

        public Worker(ExampleWebService s)
        {
            service = s;
        }

        // This method will be called when the thread is started. 
        public void DoWork()
        {
            // Create a listener.
            listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in new string[] { "http://localhost:8085/" })
            {
                listener.Prefixes.Add(s);
            }
            listener.Start();

            service.SimpleListenerExample(listener);
        }

        public void Stop()
        {
            listener.Stop();
        }
    }
}