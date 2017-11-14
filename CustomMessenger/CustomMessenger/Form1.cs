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
using System.Net;
using System.Net.Sockets;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace CustomMessenger
{
	public partial class Form1 : Form
	{
		Thread sendingThread;
		Thread receivingThread;
		Thread messengerClientThread;
		Thread messengerServerThread;
		Thread googleCalendarThread;

		NamedPipeClientStream sendingPipe;
		NamedPipeServerStream receivingPipe;

		Socket server;
		Socket connectedClient;
		Socket client;

		string sendingPipeName;
		string receivingPipeName;

		PacketData packetData;

		bool isSendingPipeConnected;
		bool isReceivingPipeConnected;
		bool isMessengerClientConnected;
		bool isMessengerServerConnected;

		// Google Calendar
		static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
		static string ApplicationName = "Google Calendar API .NET Quickstart";

		public Form1()
		{
			InitializeComponent();

			sendingPipeName = "wtou";
			receivingPipeName = "utow";

			packetData = new PacketData();
			packetData.messageLog = new List<PacketData.Message>();

			try
			{
				//sendingPipe = new NamedPipeClientStream(sendingPipeName);
				//receivingPipe = new NamedPipeServerStream(receivingPipeName);
			}
			catch (Exception e)
			{
				MessageBox.Show("An error has occurred while pipe open.\n" + e.Message);
				return;
			}

			Connect.Click += new EventHandler(this.Button1Click);
			button2.Click += new EventHandler(this.Button2Click);
			button3.Click += new EventHandler(this.Button3Click);
			button4.Click += new EventHandler(this.Button4Click);

			/*sendingThread = new Thread(ConnectSendingPipe);
			sendingThread.Start();
			receivingThread = new Thread(ConnectReceivingPipe);
			receivingThread.Start();*/

			/*messengerClientThread = new Thread(MessengerClient);
			messengerClientThread.Start();
			messengerServerThread = new Thread(MessengerServer);
			messengerServerThread.Start();*/

			googleCalendarThread = new Thread(GoogleCalendar);
			googleCalendarThread.Start();
		}

		public void GoogleCalendar()
		{
			UserCredential credential;

			using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
			{
				string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
				credPath = Path.Combine(credPath, ".credentials/calendar-dotnot-quickstart.json");

				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(credPath, true)).Result;
				WriteTextBox2("Credential file saved to: " + credPath);
			}

			// Create Google Calendar API service.
			var service = new CalendarService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});

			// Define parameters of request.
			EventsResource.ListRequest request = service.Events.List("primary");
			request.TimeMin = DateTime.Now;
			request.ShowDeleted = false;
			request.SingleEvents = true;
			request.MaxResults = 10;
			request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

			// List events.
			Events events = request.Execute();
			WriteTextBox2("Upcoming events >>");
			if (events.Items != null && events.Items.Count > 0)
			{
				foreach (var eventItem in events.Items)
				{
					string when = eventItem.Start.DateTime.ToString();
					if (String.IsNullOrEmpty(when)) // 종일로 설정되어 있을 경우
					{
						when = eventItem.Start.Date;
						DateTime dateTime;
						
						if (DateTime.TryParse(when, out dateTime))
						{
							TimeSpan timeDiff = dateTime - DateTime.Now;
							WriteTextBox2(eventItem.Summary + " // Min diff: " + timeDiff.TotalMinutes);

							//--
							//WriteTextBox2(eventItem.Summary + " // Date: " + dateTime.ToString());
							//--
						}
						else
						{
							WriteTextBox2("Date Error");
						}
					}
					else // 정확한 시간이 설정되어 있을 경우
					{
						DateTime dateTime = eventItem.Start.DateTime.Value;

						TimeSpan timeDiff = dateTime - DateTime.Now;
						WriteTextBox2(eventItem.Summary + " // Min diff: " + timeDiff.TotalMinutes);

						//--
						//WriteTextBox2(eventItem.Summary + " // DateTime " + dateTime.ToString());
						//--
					}
				}
			}
			else
			{
				textBox2.Text +=("No upcoming events found.");
			}
			Console.Read();
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

		public void MessengerServer()
		{
			string myPort = textBox3.Text; // 내 포트 번호
			IPEndPoint ipep = new IPEndPoint(IPAddress.Any, Int32.Parse(myPort));
			server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			server.Bind(ipep);
			label3.Text = "Server:Listen";
			server.Listen(1);
			label3.Text = "Server:Listening";

			connectedClient = server.Accept();
			label3.Text = "Server:Accept";
			isMessengerServerConnected = true;
			while (isMessengerServerConnected)
			{
				IPEndPoint ip = (IPEndPoint)connectedClient.RemoteEndPoint;
				//label3.Text = "Server:" + ip.Address;

				String _buf;
				Byte[] _data = new Byte[1024];
				int result = connectedClient.Receive(_data);
				if (result > 0)
				{
					_buf = Encoding.Default.GetString(_data);
					AddMessageLog(_buf, false);
					label3.Text = "Server:Receve message";
				}
				else
				{
					label3.Text = "Result : " + result.ToString();
				}
			}
			label3.Text = "Server:End";
			connectedClient.Close();
			server.Close();
			isMessengerServerConnected = false;
		}

		public void MessengerClient()
		{
			string connectIP = textBox4.Text;
			string connectPort = textBox5.Text;
			IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connectIP), Int32.Parse(connectPort));
			client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			int count = 1;
			while (isMessengerClientConnected == false)
			{
				try
				{
					Thread.Sleep(1000);
					label4.Text = "Client:trying " + count.ToString();
					count++;
					//label4.Text = IPAddress.Parse(connectIP).ToString();
					//label4.Text = "HERE:" + Int32.Parse(connectPort).ToString();
					client.Connect(ipep);
					isMessengerClientConnected = true;
				}
				catch
				{
					isMessengerClientConnected = false;
				}
			}
			label4.Text = "Client:connect";

			while (isMessengerClientConnected)
			{
				Thread.Sleep(100);
			}

			label4.Text = "Client:End";
			client.Close();
			isMessengerClientConnected = false;
		}

		public void WriteTextBox2(string message)
		{
			textBox2.Text += message;
			textBox2.Text += "\r\n";
		}

		public void AddMessageLog(string message, bool isInternal)
		{
			// 메세지 로그 생성
			PacketData.Message temp;
			temp.text = message;
			temp.isMe = isInternal;
			packetData.messageLog.Add(temp);

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
		}

		// 연결(Connect) 버튼
		public void Button1Click(object sender, EventArgs e)
		{
			messengerClientThread = new Thread(MessengerClient);
			messengerClientThread.Start();
			messengerServerThread = new Thread(MessengerServer);
			messengerServerThread.Start();
		}

		// 종료 버튼
		public void Button2Click(object sender, EventArgs e)
		{
			/*if (sendingPipe.IsConnected)
				sendingPipe.Close();
			if (sendingThread.IsAlive)
				sendingThread.Abort();

			if (receivingPipe.IsConnected)
				receivingPipe.Close();
			if (receivingThread.IsAlive)
				receivingThread.Abort();*/

			if (isMessengerClientConnected)
			{
				isMessengerClientConnected = false;
				client.Close();
				messengerClientThread.Abort();
				
			}
			if (isMessengerServerConnected)
			{
				isMessengerServerConnected = false;
				connectedClient.Close();
				server.Close();
				messengerServerThread.Abort();
			}
		}

		// 전송 버튼
		public void Button3Click(object sender, EventArgs e)
		{
			// 메세지 로그 업데이트
			string currentMessage = textBox1.Text;
			textBox1.Text = "";
			AddMessageLog(currentMessage, true);

			// 상대방에게 데이터 전송
			if (isMessengerClientConnected && isMessengerServerConnected)
			{
				Byte[] _data = new Byte[1024];
				String _buf;
				_buf = currentMessage;
				_data = Encoding.Default.GetBytes(_buf);
				client.Send(_data);
			}

			/*// AR로 데이터 전송
			if (sendingPipe.IsConnected && receivingPipe.IsConnected)
			{
				if (sendingPipe.IsConnected)
				{
					try
					{
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
			}*/
		}

		// 외부 알림 수신 버튼
		public void Button4Click(object sender, EventArgs e)
		{
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
			/*if (sendingPipe.IsConnected)
				sendingPipe.Close();
			if (sendingThread.IsAlive)
				sendingThread.Abort();

			if (receivingPipe.IsConnected)
				receivingPipe.Close();
			if (receivingThread.IsAlive)
				receivingThread.Abort();*/

			if (isMessengerClientConnected)
			{
				isMessengerClientConnected = false;
				client.Close();
				messengerClientThread.Abort();

			}
			if (isMessengerServerConnected)
			{
				isMessengerServerConnected = false;
				connectedClient.Close();
				server.Close();
				messengerServerThread.Abort();
			}
		}
	}
}
