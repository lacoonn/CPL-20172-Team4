using UnityEngine;
using System.Collections;
using System.IO.Pipes;
using System.Threading;

public class pipe : MonoBehaviour
{
	NamedPipeClientStream myPipe = new NamedPipeClientStream("MyPipe");
	string myString = "Application Started.";
	public bool isEnable = false;
	Object aSyncObj;
	byte[] buffer;
	Thread readThread;

	void Start()
	{
		myString = "Not Connected. Set isEnabled to true to start a connection.";
		myPipe.ReadMode = PipeTransmissionMode.Message;
		readThread = new Thread(new ThreadStart(ReadData));
	}

	void Update()
	{
		if (isEnable)
		{
			if (IsConnected())
			{
				if (!readThread.IsAlive)
				{
					readThread = new Thread(new ThreadStart(ReadData));
					readThread.Start();
				}
			}
			else
			{
				if (!IsInvoking("Connect"))
				{
					Invoke("Connect", 1f);
				}
			}
		}
	}

	private void Connect()
	{
		myPipe.Connect(1);
		buffer = new byte[1024];
		if (IsConnected())
		{
			myString = "Pipeline Connected!";
		}
	}

	public void StopPipe()
	{
		isEnable = false;
		myPipe.Close();
	}

	private void ReadData()
	{
		while (IsConnected() && isEnable)
		{
			buffer = new byte[1024];

			int bytesRead = myPipe.Read(buffer, 0, 1024);
			if (bytesRead > 0)
			{
				string tmpString = System.Text.Encoding.ASCII.GetString(buffer).Trim();
				if (myString != tmpString)
				{
					myString = tmpString;
				}
			}
		}
	}

	void OnGUI()
	{
		GUI.Label(new Rect(0, 0, Screen.width, Screen.height), myString);
	}

	void OnApplicationQuit()
	{
		myPipe.Close();
	}

	public bool IsConnected()
	{
		return myPipe.IsConnected;
	}


}
