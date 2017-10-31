using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextBoxToSend : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        // Enter
        if (Input.GetKeyUp(KeyCode.Return))
            GameObject.Find("SendButton").GetComponent<SendListener>().SendtoPipe();
        // KeypadEnter
        else if (Input.GetKeyUp(KeyCode.KeypadEnter))
            GameObject.Find("SendButton").GetComponent<SendListener>().SendtoPipe();
    }
}
