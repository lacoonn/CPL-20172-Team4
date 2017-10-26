using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Pipes;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace CustomMessenger
{
	public partial class Form1 : Form
	{
		//NamedPipeClientStream recevingPipe;

		Thread sendingThread;
		NamedPipeClientStream sendingPipe;
		string sendingPipeName;

		public Form1()
		{
			InitializeComponent();

			sendingPipeName = "c";

			button1.Click += new EventHandler(this.Button1Click);
			button2.Click += new EventHandler(this.Button2Click);

			sendingThread = new Thread(ConnectPipe);
			sendingThread.Start();
		}

		public void ConnectPipe()
		{
			XmlSerializer xmlSerializer;

			try
			{
				//sendingPipe = new NamedPipeServerStream("PcToUnity", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1024, 1024);
				//sendingPipe = new NamedPipeServerStream(sendingPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
				sendingPipe = new NamedPipeClientStream(sendingPipeName);
			}
			catch (Exception e)
			{
				MessageBox.Show("An error has occurred while pipe open.\n" + e.Message);
				return;
			}
			/*IAsyncResult pipeCall = sendingPipe.BeginWaitForConnection(null, null);

			while (!pipeCall.IsCompleted == false)
			{
				label1.Text = "Waiting for connection";
			}
			sendingPipe.EndWaitForConnection(pipeCall);*/
			//sendingPipe.WaitForConnection();
			//while (sendingPipe.IsConnected == false)
			//{

			//}

			while (!sendingPipe.IsConnected)
			{
				//label1.Text = "Waiting";
				sendingPipe.Connect();
			}

			int count = 0;
			while (sendingPipe.IsConnected)
			{
				//label1.Text = "Connecting " + count++;
				try
				{
					/*byte[] dataToSend;
					dataToSend = Encoding.ASCII.GetBytes(textBox1.Text);
					sendingPipe.Write(dataToSend, 0, dataToSend.Length);
					sendingPipe.Flush();
					sendingPipe.WaitForPipeDrain();*/

					//PacketData dataToSend = new PacketData();
					//dataToSend.typingMessage = "hello!";

					//IFormatter formatter = new BinaryFormatter();
					//formatter.Serialize(sendingPipe, dataToSend);

					PacketData dataToSend = new PacketData();
					dataToSend.typingMessage = "this is test data";
					string xmlData = SerializeToXml(dataToSend);
					StreamWriter writer = new StreamWriter(sendingPipe);
					writer.WriteLine(xmlData);
					writer.Flush();


					/*// read data
					int dataReceive = sendingPipe.ReadByte();*/

					// xml test
					//FileStream fileStream = new FileStream("log.xml", FileMode.Create);
					//xmlSerializer.Serialize(fileStream, dataToSend);

					//textBox2.Text = dataToSend.typingMessage;
					//textBox2.Text = dataToSend.ToString();
					//string temp = (count++).ToString();
					//textBox2.Text = temp;
					Thread.Sleep(1000);
				}
				catch (Exception ex)
				{
					if (!ex.Message.StartsWith("Pipe is broken."))
					{
						MessageBox.Show("An error has occurred while seding data. \n" + ex.Message);
						return;
					}
				}
			}
			sendingPipe.Close();
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

		// send button click
		private void Button1Click(object sender, EventArgs e)
		{
			
		}

		private void Button2Click(object sender, EventArgs e)
		{
			if (sendingPipe.IsConnected)
				sendingPipe.Close();
		}

		private void Form1_Closed(object sender, System.EventArgs e)
		{
			//count -= 1;
		}
	}
}
