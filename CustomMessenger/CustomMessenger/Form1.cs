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
		static List<string> eventIdList = new List<string>();

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
			FormClosing += Form1_Closing;

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
			int loopCount = 0;
			while (true)
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

					//WriteTextBox2("Credential file saved to: " + credPath);
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
				//WriteTextBox2("Upcoming events >>");
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
								// 이벤트 시간 파싱을 성공했을 경우(여기에 필요한 행위가 들어감)
								//WriteTextBox2(eventItem.Id + " _ " + eventItem.Summary + " // Min diff: " + timeDiff.TotalMinutes);

								// eventItem.OriginalStartTime = 반복 이벤트

								if (0 <= timeDiff.TotalMinutes && timeDiff.TotalMinutes <= 60)
								{
									bool isItemExist = false;
									foreach (string eventIdItem in eventIdList)
									{
										if(eventItem.Id.Equals(eventIdItem))
										{
											isItemExist = true;
											break;
										}
									}
									if (isItemExist)
									{
										// Pass
									}
									else
									{
										eventIdList.Add(eventItem.Id);
										SendCalendarAlarmToAr(eventItem.Summary, when); // 캘린더 알림 AR로 전송
										MessageBox.Show(eventItem.Summary + when);
									}
								}
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
							// 이벤트 시간 파싱을 성공했을 경우(여기에 필요한 행위가 들어감)
							//WriteTextBox2(eventItem.Id + " _ " + eventItem.Summary + " // Min diff: " + timeDiff.TotalMinutes);

							// eventItem.OriginalStartTime = 반복 이벤트

							if (0 <= timeDiff.TotalMinutes && timeDiff.TotalMinutes <= 60)
							{
								bool isItemExist = false;
								foreach (string eventIdItem in eventIdList)
								{
									if (eventItem.Id.Equals(eventIdItem))
									{
										isItemExist = true;
										break;
									}
								}
								if (isItemExist)
								{
									// Pass
								}
								else
								{
									eventIdList.Add(eventItem.Id);
									SendCalendarAlarmToAr(eventItem.Summary, when); // 캘린더 알림 AR로 전송
									MessageBox.Show(eventItem.Summary + when);
								}
							}
						}
					}
				}
				else
				{
					MessageBox.Show("No upcoming events found.");
				}

				loopCount++;

				// 저장된 이벤트 id 리스트를 검사해서 쓸모없어진 것들을 제거
				if (loopCount >= 60) // 1시간에 1번씩 검사
				{
					if (events.Items != null && events.Items.Count > 0)
					{
						foreach (string idItem in eventIdList)
						{
							bool isIdExist = false;
							foreach (var eventItem in events.Items)
							{
								if (idItem.Equals(eventItem.Id))
								{
									isIdExist = true;
									break;
								}
							}
							// id가 더 이상 존재하지 않으면 삭제
							if (!isIdExist)
							{
								eventIdList.Remove(idItem);
							}
						}
					}
					loopCount = 0;
				}

				Thread.Sleep(60000); // 1분에 한번씩 캘린더 일정을 검사
			}
		}

		// 캘린더에서 임박한 알림을 Ar로 전송
		public void SendCalendarAlarmToAr(string eventSummary, string when)
		{
			MessageBox.Show(eventSummary);
			try
			{
				// PacketData에 캘린더 알림 추가
				packetData.hasNewCalendarAlarm = true;
				packetData.newCalendarAlarm.summary = eventSummary;
				packetData.newCalendarAlarm.time = when;
				// 데이터 전송
				string xmlData = SerializeToXml(packetData);
				StreamWriter writer = new StreamWriter(sendingPipe);
				writer.WriteLine(xmlData);
				writer.Flush();
				// PacketData에서 캘린더 알림 삭제
				packetData.hasNewCalendarAlarm = false;
				packetData.newCalendarAlarm.summary = null;
				packetData.newCalendarAlarm.time = null;
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

		// 텍스트박스2(메세지 로그)에 string 추가
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

		private void Form1_Closing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.WindowsShutDown)
			{
				return;
			}
			else
			{
				try
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
					if (googleCalendarThread.IsAlive)
					{
						googleCalendarThread.Abort();
					}
					MessageBox.Show("프로그램을 정상적으로 종료하셨습니다. \n");
				}
				catch (Exception ex)
				{
					MessageBox.Show("프로그램이 비정상적으로 종료되었습니다. \n" + ex.Message);
				}
			}
		}
	}
}
