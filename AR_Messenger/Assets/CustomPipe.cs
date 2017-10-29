using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.IO;
using System.Threading;

public class CustomPipe : MonoBehaviour
{
	public string receivingPipeName;
	NamedPipeServerStream receivingPipe;

	string receivingStatus;
    string receivingData;

    string sendingStatus;
    string sendingData;

	Thread thread;

    // Use this for initialization
    void Start()
    {
		receivingPipe = new NamedPipeServerStream(receivingPipeName);
		receivingData = "Initial data";
		SetReceivingStatus("Not connected");

		thread = new Thread(WaitForConnect);
		thread.Start();
	}

	private void FixedUpdate()
	{
		if (receivingPipe.IsConnected)
		{
			if (thread.IsAlive == false)
			{
				thread = new Thread(WaitForConnect);
				thread.Start();
			}
		}
		else
		{
			Debug.Log("Not connected");
		}
	}

	void OnGUI()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), receivingStatus);
        GUI.Label(new Rect(0, 20, Screen.width, Screen.height), receivingData);
    }

	private void OnApplicationQuit()
	{
		if (receivingPipe.IsConnected)
			receivingPipe.Close();
	}

	public static PacketData DeserializeFromXML(string xmlData)
	{
		PacketData data = null;

		StringReader stringReader = null;

		XmlSerializer deserializer = new XmlSerializer(typeof(PacketData));
		stringReader = new StringReader(xmlData);
		data = (PacketData)deserializer.Deserialize(stringReader);

		return data;
	}

    private void ReadData()
    {	
		if (receivingPipe.IsConnected)
		{
			StreamReader reader = new StreamReader(receivingPipe);
			string xmlData;
			xmlData = reader.ReadLine();
			if (xmlData != null)
			{
				Debug.Log(xmlData);
				PacketData packetData = DeserializeFromXML(xmlData);

				receivingData = "";
				foreach (string tempString in packetData.messageLog)
				{
					receivingData += tempString;
					receivingData += "\n";
				}
				receivingData += "TYPING ==> ";
				receivingData += packetData.typingMessage;
			}
		}
    }

	private void WaitForConnect()
	{
		Debug.Log("Wait start");
		receivingPipe.WaitForConnection();
		Debug.Log("Wait end");
		if (receivingPipe.IsConnected)
			SetReceivingStatus("connected");

		while (receivingPipe.IsConnected)
		{
			ReadData();
		}

		return;
	}

    private void SetReceivingStatus(string status)
    {
        receivingStatus = "Receiving Status : " + status;
    }
}

