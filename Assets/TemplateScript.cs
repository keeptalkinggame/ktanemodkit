using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rnd = UnityEngine.Random;
using KModkit;
public class TemplateScript: MonoBehaviour {
    public KMBombModule module;
    public KMAudio sound;
    int moduleId;
    static int moduleIdCounter = 1;
    bool solved;
	void Awake () {
        moduleId = moduleIdCounter++;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
