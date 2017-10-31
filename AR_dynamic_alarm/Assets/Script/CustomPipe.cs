using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Threading;

public class CustomPipe : MonoBehaviour
{
    public SetAlramListener alramListener;
    public SetChatText setChatText;
    Thread receivingThread;
	Thread sendingThread;

	NamedPipeServerStream receivingPipe;
	NamedPipeClientStream sendingPipe;

	public string receivingPipeName;
	public string sendingPipeName;

	PacketData packetData;

	string receivingStatus;
	string sendingStatus;

	string messageLogString;


    // Use this for initialization
    void Start()
    {
		packetData = new PacketData();
		packetData.messageLog = new List<PacketData.Message>();

		SetReceivingStatus("Not connected");
		SetSendingStatus("Not connected");

		receivingPipe = new NamedPipeServerStream(receivingPipeName);
		sendingPipe = new NamedPipeClientStream(sendingPipeName);

		receivingThread = new Thread(ConnectReceivingPipe);
		receivingThread.Start();
		sendingThread = new Thread(ConnectSendingPipe);
		sendingThread.Start();
	}

	private void FixedUpdate()
	{
		if (receivingPipe.IsConnected)
		{
			if (receivingThread.IsAlive == false)
			{
				receivingThread = new Thread(ConnectReceivingPipe);
				receivingThread.Start();
				Debug.Log("Receiving thread restart");
			}
		}

		if (sendingPipe.IsConnected)
		{

		}
		else
		{
			if (sendingThread.IsAlive == false)
			{
				sendingThread = new Thread(ConnectSendingPipe);
				sendingThread.Start();
				Debug.Log("Sending thread restart");
			}
		}
	}

	void OnGUI()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), receivingStatus);
		GUI.Label(new Rect(0, 20, Screen.width, Screen.height), sendingStatus);
		GUI.Label(new Rect(0, 40, Screen.width, Screen.height), messageLogString);
    }

	private void OnApplicationQuit()
	{
		if (receivingPipe.IsConnected)
		{
			receivingPipe.Close();
			Debug.Log("Pipe closed in Quit Event");
		}
		if (receivingThread.IsAlive)
		{
			receivingThread.Abort();
			Debug.Log("Thread closed in Quit Event");
		}
		if (sendingPipe.IsConnected)
		{
			sendingPipe.Close();
			Debug.Log("Pipe closed in Quit Event");
		}
		if (sendingThread.IsAlive)
		{
			sendingThread.Abort();
			Debug.Log("Thread closed in Quit Event");
		}
		Debug.Log("End Quit Event");
	}

	private void ConnectReceivingPipe()
	{
		Debug.Log("Wait start");
		receivingPipe.WaitForConnection();
		Debug.Log("Wait end");
		if (receivingPipe.IsConnected)
			SetReceivingStatus("connected");

		while (receivingPipe.IsConnected)
		{
			StreamReader reader = new StreamReader(receivingPipe);
			string xmlData;
			xmlData = reader.ReadLine();
			if (xmlData != null)
			{
				Debug.Log("Read data");
				packetData = DeserializeFromXML(xmlData);

				messageLogString = "";
				foreach (PacketData.Message tempMessage in packetData.messageLog)
				{
                    if (tempMessage.isMe)
                    {
                        messageLogString += tempMessage.text;
                    }
                    else
                    {
                        messageLogString += "\t" + tempMessage.text;
                    }
					messageLogString += "\n";

                    alramListener.Alram();
                    setChatText.setMessageLog(messageLogString);
				}
			}
		}

		return;
	}

	private void ConnectSendingPipe()
	{
		while (!sendingPipe.IsConnected)
		{
			SetSendingStatus("Waiting");
			sendingPipe.Connect();
		}
		SetSendingStatus("Connected");
	}

	public void TouchSendButton(string msg)
	{
		// 전송 버튼
		PacketData.Message temp;
		temp.text = msg;
		temp.isMe = true;
		packetData.messageLog.Add(temp);

		if (sendingPipe.IsConnected)
		{
			try
			{
				// 로그 텍스트박스 업데이트
				messageLogString = "";
				foreach (PacketData.Message tempMessage in packetData.messageLog)
				{
					if (tempMessage.isMe)
						messageLogString += tempMessage.text;
					else
						messageLogString += "\t" + tempMessage.text;
					messageLogString += "\r\n";

                    alramListener.Alram();
                    setChatText.setMessageLog(messageLogString);
                }
				// 데이터 전송
				string xmlData = SerializeToXml(packetData);
				StreamWriter writer = new StreamWriter(sendingPipe);
				writer.WriteLine(xmlData);
				writer.Flush();

				Debug.Log("Send message from Unity");
			}
			catch (Exception ex)
			{
				if (!ex.Message.StartsWith("Pipe is broken."))
				{
					Debug.Log("An error has occurred while seding data. \n" + ex.Message);
					return;
				}
			}
		}
	}

    private void SetReceivingStatus(string status)
    {
        receivingStatus = "Receiving Status : " + status;
    }

	private void SetSendingStatus(string status)
	{
		sendingStatus = "Sending Status : " + status;
	}

	public static string SerializeToXml(PacketData data)
	{
		string xmlData = null;

		StringWriter stringWriter = null;

		XmlSerializer serializer = new XmlSerializer(data.GetType());
		stringWriter = new StringWriter();
		serializer.Serialize(stringWriter, data);

		xmlData = stringWriter.ToString().Replace(Environment.NewLine, " ");

		stringWriter.Close();

		return xmlData;
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
}

