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
		NamedPipeClientStream sendingPipe;
		string sendingPipeName;
		PacketData dataToSend;

		public Form1()
		{
			InitializeComponent();

			sendingPipeName = "hello";

			dataToSend = new PacketData();
			dataToSend.messageLog = new List<PacketData.Message>();

			button1.Click += new EventHandler(this.Button1Click);
			button2.Click += new EventHandler(this.Button2Click);
			button3.Click += new EventHandler(this.Button3Click);
			button4.Click += new EventHandler(this.Button4Click);

			sendingThread = new Thread(ConnectPipe);
			sendingThread.Start();
		}

		public void ConnectPipe()
		{
			try
			{
				sendingPipe = new NamedPipeClientStream(sendingPipeName);
			}
			catch (Exception e)
			{
				MessageBox.Show("An error has occurred while pipe open.\n" + e.Message);
				return;
			}

			while (!sendingPipe.IsConnected)
			{
				label1.Text = "Waiting";
				sendingPipe.Connect();
			}

			/*int count = 0;
			while (sendingPipe.IsConnected)
			{
				label1.Text = "Connecting " + count++;
				count = count % 10000;
				try
				{
					textBox2.Text = "";
					foreach (PacketData.Message tempMessage in dataToSend.messageLog)
					{
						if (tempMessage.isMe)
							textBox2.Text += tempMessage.text;
						else
							textBox2.Text += "\t" + tempMessage.text;
						textBox2.Text += "\r\n";
					}
					string xmlData = SerializeToXml(dataToSend);
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
				Thread.Sleep(100);
			}*/
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
		public void Button1Click(object sender, EventArgs e)
		{
			
		}

		public void Button2Click(object sender, EventArgs e)
		{
			if (sendingPipe.IsConnected)
				sendingPipe.Close();
			if (sendingThread.IsAlive)
				sendingThread.Abort();
			label2.Text = "Closed";
		}

		public void Button3Click(object sender, EventArgs e)
		{
			// 전송 버튼
			PacketData.Message temp;
			temp.text = textBox1.Text;
			temp.isMe = true;
			dataToSend.messageLog.Add(temp);
			textBox1.Text = "";

			if (sendingPipe.IsConnected)
			{
				try
				{
					// 로그 텍스트박스 업데이트
					textBox2.Text = "";
					foreach (PacketData.Message tempMessage in dataToSend.messageLog)
					{
						if (tempMessage.isMe)
							textBox2.Text += tempMessage.text;
						else
							textBox2.Text += "\t" + tempMessage.text;
						textBox2.Text += "\r\n";
					}
					// 데이터 전송
					string xmlData = SerializeToXml(dataToSend);
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
			dataToSend.messageLog.Add(temp);

			if (sendingPipe.IsConnected)
			{
				try
				{
					// 로그 텍스트박스 업데이트
					textBox2.Text = "";
					foreach (PacketData.Message tempMessage in dataToSend.messageLog)
					{
						if (tempMessage.isMe)
							textBox2.Text += tempMessage.text;
						else
							textBox2.Text += "\t" + tempMessage.text;
						textBox2.Text += "\r\n";
					}
					// 데이터 전송
					string xmlData = SerializeToXml(dataToSend);
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
		}
	}
}
