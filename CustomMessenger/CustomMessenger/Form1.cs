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
using System.Xml.Serialization;
using System.IO;

namespace CustomMessenger
{
	public partial class Form1 : Form
	{
		Thread sendingThread;
		Thread receivingThread;

		NamedPipeClientStream sendingPipe;
		NamedPipeServerStream receivingPipe;

		string sendingPipeName;
		string receivingPipeName;

		PacketData packetData;

		public Form1()
		{
			InitializeComponent();

			sendingPipeName = "wtou";
			receivingPipeName = "utow";

			packetData = new PacketData();
			packetData.messageLog = new List<PacketData.Message>();

			try
			{
				sendingPipe = new NamedPipeClientStream(sendingPipeName);
				receivingPipe = new NamedPipeServerStream(receivingPipeName);
			}
			catch (Exception e)
			{
				MessageBox.Show("An error has occurred while pipe open.\n" + e.Message);
				return;
			}

			button1.Click += new EventHandler(this.Button1Click);
			button2.Click += new EventHandler(this.Button2Click);
			button3.Click += new EventHandler(this.Button3Click);
			button4.Click += new EventHandler(this.Button4Click);

			sendingThread = new Thread(ConnectSendingPipe);
			sendingThread.Start();
			receivingThread = new Thread(ConnectReceivingPipe);
			receivingThread.Start();
		}

		public void ConnectSendingPipe()
		{
			label1.Text = "Not connected";
			while (!sendingPipe.IsConnected)
			{
				label1.Text = "Waiting";
				sendingPipe.Connect();
			}
		}

		public void ConnectReceivingPipe()
		{
			label2.Text = "Not connected";
			receivingPipe.WaitForConnection();

			while (receivingPipe.IsConnected)
			{
				label2.Text = "Wait data";
				StreamReader reader = new StreamReader(receivingPipe);
				string xmlData;
				xmlData = reader.ReadLine();
				if (xmlData != null)
				{
					label2.Text = "Receive data";
					packetData = DeserializeFromXML(xmlData);

					textBox2.Text = "";
					foreach (PacketData.Message tempMessage in packetData.messageLog)
					{
						if (tempMessage.isMe)
							textBox2.Text += tempMessage.text;
						else
							textBox2.Text += "\t" + tempMessage.text;
						textBox2.Text += "\r\n";
					}
				}
			}
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

		// send button click
		public void Button1Click(object sender, EventArgs e)
		{
			
		}

		public void Button2Click(object sender, EventArgs e)
		{
			if (sendingPipe.IsConnected)
				sendingPipe.Close();
			if (sendingThread.IsAlive)
				sendingThread.Abort();

			if (receivingPipe.IsConnected)
				receivingPipe.Close();
			if (receivingThread.IsAlive)
				receivingThread.Abort();
			label2.Text = "Closed";
		}

		public void Button3Click(object sender, EventArgs e)
		{
			// 전송 버튼
			PacketData.Message temp;
			temp.text = textBox1.Text;
			temp.isMe = true;
			packetData.messageLog.Add(temp);
			textBox1.Text = "";

			if (sendingPipe.IsConnected)
			{
				try
				{
					// 로그 텍스트박스 업데이트
					textBox2.Text = "";
					foreach (PacketData.Message tempMessage in packetData.messageLog)
					{
						if (tempMessage.isMe)
							textBox2.Text += tempMessage.text;
						else
							textBox2.Text += "\t" + tempMessage.text;
						textBox2.Text += "\r\n";
					}
					// 데이터 전송
					string xmlData = SerializeToXml(packetData);
					StreamWriter writer = new StreamWriter(sendingPipe);
					writer.WriteLine(xmlData);
					writer.Flush();
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
		}

		public void Button4Click(object sender, EventArgs e)
		{
			// 외부 알림 수신 버튼
			PacketData.Message temp;
			temp.text = "This is external message";
			temp.isMe = false;
			packetData.messageLog.Add(temp);

			if (sendingPipe.IsConnected)
			{
				try
				{
					// 로그 텍스트박스 업데이트
					textBox2.Text = "";
					foreach (PacketData.Message tempMessage in packetData.messageLog)
					{
						if (tempMessage.isMe)
							textBox2.Text += tempMessage.text;
						else
							textBox2.Text += "\t" + tempMessage.text;
						textBox2.Text += "\r\n";
					}
					// 데이터 전송
					string xmlData = SerializeToXml(packetData);
					StreamWriter writer = new StreamWriter(sendingPipe);
					writer.WriteLine(xmlData);
					writer.Flush();
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
		}

		private void Form1_Closed(object sender, System.EventArgs e)
		{
			if (sendingPipe.IsConnected)
				sendingPipe.Close();
			if (sendingThread.IsAlive)
				sendingThread.Abort();

			if (receivingPipe.IsConnected)
				receivingPipe.Close();
			if (receivingThread.IsAlive)
				receivingThread.Abort();
		}
	}
}
