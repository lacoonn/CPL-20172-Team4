using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.IO;

public class CustomPipe : MonoBehaviour
{
	public string receivingPipeName;
	NamedPipeClientStream receivingPipe;

    string receivingStatus;
    PacketData receivingData;

    string sendingStatus;
    string sendingData;

	byte[] receivingBuffer = new byte[1024];
	//byte[] sendingBuffer = new byte[1024];

	XmlSerializer xmlSerializer;

    // Use this for initialization
    void Start()
    {
		xmlSerializer = new XmlSerializer(typeof(PacketData));
		receivingPipe = new NamedPipeClientStream(receivingPipeName);
		receivingData = new PacketData();
		receivingData.typingMessage = "Initial!";
		SetReceivingStatus("Not connected");
	}

	private void FixedUpdate()
	{
		if (receivingPipe.IsConnected)
		{
			ReadData();
		}
		else
		{
			if (!IsInvoking("ConnectPipe"))
			{
				Invoke("ConnectPipe", 1f);
			}
		}
	}

	void OnGUI()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), receivingStatus);
        GUI.Label(new Rect(0, 20, Screen.width, Screen.height), receivingData.typingMessage);
        //GUI.Label(new Rect(10, 0, Screen.width, Screen.height), receivingStatus);
        //GUI.Label(new Rect(10, 10, Screen.width, Screen.height), receivingData);
    }

	private void OnApplicationQuit()
	{
		receivingPipe.Close();
		//receivingThread.Abort();
	}

	private void ConnectPipe()
    {
        receivingPipe.Connect(1);
        if (receivingPipe.IsConnected)
        {
            SetReceivingStatus("Receiving pipe connected!");
        }
    }

    /*private void ReadData()
    {
        while (receivingPipe.IsConnected)
        {
            int bytesRead = receivingPipe.Read(receivingBuffer, 0, 1024);
            if (bytesRead > 0)
            {
                receivingData = System.Text.Encoding.ASCII.GetString(receivingBuffer).Trim();
            }
        }
    }*/

    private void ReadData()
    {	
		if (receivingPipe.IsConnected)
		{
			int bytesRead = receivingPipe.Read(receivingBuffer, 0, 1024);
            if (bytesRead > 0)
            {
                receivingData.typingMessage = System.Text.Encoding.ASCII.GetString(receivingBuffer).Trim();
            }

			//ss.WaitForConnection();
			//receivingPipe.WaitForPipeDrain();

			//IFormatter formatter = new BinaryFormatter();
			//receivingData = (PacketData)formatter.Deserialize(receivingPipe);

			/*Debug.Log("Read Data One Loop");
			Debug.Log("1");
			if (receivingPipe.CanRead)
			{
				PacketData packetData = (PacketData)xmlSerializer.Deserialize(receivingPipe);
				Debug.Log("2");
				receivingData.typingMessage = packetData.typingMessage;
				Debug.Log("TypingMessage : " + packetData.typingMessage);
				receivingPipe.Flush();
			}*/



			// xml test
			/*FileStream fileStream = new FileStream("log.xml", FileMode.Create);
			PacketData dataToSave = new PacketData();
			dataToSave.typingMessage = "hello!";
			xmlSerializer.Serialize(fileStream, dataToSave);*/

		}
    }

    private void StopReceivingPipe()
    {
        Debug.Log("HERE!?");
        receivingPipe.Close();
        Debug.Log("HERE2!?");
    }

    private void SetReceivingStatus(string status)
    {
        receivingStatus = "Receiving Status : " + status;
    }
}

