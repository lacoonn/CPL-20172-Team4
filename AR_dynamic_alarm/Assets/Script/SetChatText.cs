using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetChatText : MonoBehaviour {

    public Text chatBox;

    private string messageLog = "";
    private string previous = "";
    private string receiveMessage = "";
    private string sendMessage = "";
    private string receiveTyping = "";
    private string chat = "";

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        chat = messageLog;
        if (!chat.Equals(previous))
        {
            chatBox.text = chat;
            sendMessage = "";
        }
        previous = chat;
    }

    public void setMessageLog(string msg)
    {
        messageLog = msg;
    }

    public void setReceiveMessage(string msg)
    {
        receiveMessage = msg;
    }

    public void setReceiveTyping(string msg)
    {
        receiveTyping = msg;
    }

    public void setSendMessage(string msg)
    {
        sendMessage = msg;
    }

    public string getMessageLog()
    {
        return messageLog;
    }
}
