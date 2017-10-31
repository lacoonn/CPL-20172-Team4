using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SendListener : MonoBehaviour {
    
    public InputField inputFieldRef;
    string myString;
    string oldString;

    public void SendtoPipe()
    {
        GameObject.Find("Administrator").
            GetComponent<CustomPipe>().TouchSendButton(inputFieldRef.text);
        
        inputFieldRef.text = "";
    }
}
