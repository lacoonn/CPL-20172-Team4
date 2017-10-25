using UnityEngine;
using System.Collections;
using System.IO.Pipes;
using System.Threading;

public class CustomPipe : MonoBehaviour
{
    NamedPipeClientStream receivingPipe = new NamedPipeClientStream("PcToUnity");
    //NamedPipeServerStream sendingPipe = new NamedPipeServerStream("ArToPc");

    string receivingStatus;
    string receivingData;

    string sendingStatus;
    string sendingData;

    byte[] receivingBuffer = new byte[1024];
    byte[] sendingBuffer = new byte[1024];

    // Use this for initialization
    void Start()
    {
        receivingPipe.ReadMode = PipeTransmissionMode.Message;
        SetReceivingStatus("Not connected");
    }
	
    // Update is called once per frame
    void Update()
    {
        if (receivingPipe.IsConnected)
        {
            ReadData();
        }
        else
        {
            if (IsInvoking("ConnectPipe") == false)
            {
                Invoke("ConnectPipe", 1f);
            }
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), receivingStatus);
        GUI.Label(new Rect(0, 10, Screen.width, Screen.height), receivingData);
        //GUI.Label(new Rect(10, 0, Screen.width, Screen.height), receivingStatus);
        //GUI.Label(new Rect(10, -10, Screen.width, Screen.height), receivingData);
    }

    void OnApplicationQuit()
    {
        receivingPipe.Close();
    }

    private void ConnectPipe()
    {
        receivingPipe.Connect(1);
        if (receivingPipe.IsConnected)
        {
            receivingStatus = "Receiving pipe connected!";
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
                receivingData = System.Text.Encoding.ASCII.GetString(receivingBuffer).Trim();
                //Debug.Log(receivingData);
            }
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

