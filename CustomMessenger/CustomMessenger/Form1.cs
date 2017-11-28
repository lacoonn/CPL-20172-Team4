using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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
		// AR 인터프로세스 통신 스레드
		Thread sendingThread;
		Thread receivingThread;
		// 메신저 스레드
		Thread messengerClientThread;
		Thread messengerServerThread;
		// 구글 캘린더 스레드
		Thread googleCalendarThread;

		// AR 프로세스 통신 소켓
		Socket serverAR;
		Socket connectedClientAR;
		Socket clientAR;
		bool isArClientConnected;
		bool isArServerConnected;

		// 메신저 통신 소켓
		Socket server;
		Socket connectedClient;
		Socket client;
		bool isMessengerClientConnected;
		bool isMessengerServerConnected;

		// AR 통신 포트 번호
		string winPort;
		string arPort;

		// 전송 데이터
		PacketData packetData;

		string myName = "석문주";
		string yourName = "김홍재";

		// Google Calendar
		static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
		static string ApplicationName = "Google Calendar API .NET Quickstart";
		static List<string> eventIdList = new List<string>();

		// 폼 초기화
		public Form1()
		{
			InitializeComponent();

			// AR 통신 포트 설정
			winPort = "8282";
			arPort = "8283";

			packetData = new PacketData();
			packetData.messageLog = new List<PacketData.Message>();

			Connect.Click += new EventHandler(this.Button1Click);
			button2.Click += new EventHandler(this.Button2Click);
			button3.Click += new EventHandler(this.Button3Click);
			button4.Click += new EventHandler(this.Button4Click);
			FormClosing += Form1_Closing;

			// 구글 캘린더 스레드 실행
			googleCalendarThread = new Thread(GoogleCalendar);
			googleCalendarThread.Start();
		}

		// 구글 캘린더 스레드 함수
		public void GoogleCalendar()
		{
			while (!isArClientConnected || !isArServerConnected)
			{
				Thread.Sleep(1000); // 캘린더 스레드는 AR 프로그램의 연결을 기다린다.
			}

			int loopCount = 0;

			while (true)
			{
				// 구글 캘린더 계정 인증
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

				// 구글 캘린더 API 서비스 생성
				var service = new CalendarService(new BaseClientService.Initializer()
				{
					HttpClientInitializer = credential,
					ApplicationName = ApplicationName,
				});

				// 리퀘스트 생성
				EventsResource.ListRequest request = service.Events.List("primary");
				request.TimeMin = DateTime.Now;
				request.ShowDeleted = false;
				request.SingleEvents = true;
				request.MaxResults = 10;
				request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

				// 이벤트 수신
				Events events = request.Execute();
				//WriteTextBox2("Upcoming events >>");
				if (events.Items != null && events.Items.Count > 0) // 이벤트(일정)이 있을 경우
				{
					foreach (var eventItem in events.Items)
					{
						string when = eventItem.Start.DateTime.ToString();
						if (String.IsNullOrEmpty(when)) // 일정 시간이 종일로 설정되어 있을 경우
						{
							when = eventItem.Start.Date;
							DateTime dateTime;

							// 일정 시간 파싱
							if (DateTime.TryParse(when, out dateTime))
							{
								TimeSpan timeDiff = dateTime - DateTime.Now;
								//WriteTextBox2(eventItem.Id + " _ " + eventItem.Summary + " // Min diff: " + timeDiff.TotalMinutes);

								// 남은 시간이 1시간 이내일 경우
								if (0 <= timeDiff.TotalMinutes && timeDiff.TotalMinutes <= 60)
								{
									bool isItemExist = false;
									foreach (string eventIdItem in eventIdList) // 이미 전송한 알림인지 확인
									{
										if(eventItem.Id.Equals(eventIdItem))
										{
											isItemExist = true;
											break;
										}
									}
									if (isItemExist) // 이미 전송한 알림이라면
									{
										// Pass
									}
									else // 아직 전송하지 않은 알림이라면
									{
										eventIdList.Add(eventItem.Id); // 전송한 알림으로 등록하고
										SendCalendarAlarmToAr(eventItem.Summary, when); // 캘린더 알림 AR로 전송
									}
								}
							}
							else
							{
								WriteTextBox2("Date Error");
							}
						}
						else // 일정이 정확한 시간이 설정되어 있을 경우
						{
							DateTime dateTime = eventItem.Start.DateTime.Value;
							TimeSpan timeDiff = dateTime - DateTime.Now;
							//WriteTextBox2(eventItem.Id + " _ " + eventItem.Summary + " // Min diff: " + timeDiff.TotalMinutes);

							// 남은 시간이 1시간 이내일 경우
							if (0 <= timeDiff.TotalMinutes && timeDiff.TotalMinutes <= 60)
							{
								bool isItemExist = false;
								foreach (string eventIdItem in eventIdList) // 이미 전송한 알림인지 확인
								{
									if (eventItem.Id.Equals(eventIdItem))
									{
										isItemExist = true;
										break;
									}
								}
								if (isItemExist) // 이미 전송한 알림이라면
								{
									// Pass
								}
								else // 아직 전송하지 않은 알림이라면
								{
									eventIdList.Add(eventItem.Id); // 전송한 알림으로 등록하고
									SendCalendarAlarmToAr(eventItem.Summary, when); // 캘린더 알림 AR로 전송
								}
							}
						}
					}
				}
				else // 일정이 없을 경우
				{
					//MessageBox.Show("No upcoming events found.");
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
			try
			{
				// PacketData에 캘린더 알림 추가
				packetData.hasNewCalendarAlarm = true;
				packetData.newCalendarAlarm.summary = eventSummary;
				packetData.newCalendarAlarm.time = when;
				// 데이터 전송
				NetworkStream networkStream = new NetworkStream(clientAR);
				string xmlData = SerializeToXml(packetData);
				StreamWriter writer = new StreamWriter(networkStream);
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

		// 메신저간 통신에 사용되는 스레드 서버(수신) 함수
		public void MessengerServer()
		{
			string myPort = textBox3.Text; // 내 포트 번호
			IPEndPoint ipep = new IPEndPoint(IPAddress.Any, Int32.Parse(myPort));
			server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			server.Bind(ipep);
			label3.Text = "Server:Listen";
			server.Listen(1);
			label3.Text = "Server:Listening";

			// 상대 메신저의 클라이언트가 접속
			connectedClient = server.Accept();
			label3.Text = "Server:Accept";
			isMessengerServerConnected = true;
			// 상대 메신저의 클라이언트가 접속해 있는 동안
			while (isMessengerServerConnected)
			{
				// 상대방 IP
				//IPEndPoint ip = (IPEndPoint)connectedClient.RemoteEndPoint;
				//WriteTextBox2("Server:" + ip.Address);

				// 스트림 생성
				string _buf = "";
				NetworkStream network = new NetworkStream(connectedClient);
				StreamReader reader = new StreamReader(network);

				// 스트림에서 모든 라인을 수신
				while (true)
				{
					label3.Text = "Server:Reading";
					
					_buf += reader.ReadLine();
					if (reader.Peek() >= 0)
						_buf += "\r\n";
					else
						break;
				}

				// 수신한 메세지를 로그에 추가하고 화면 업데이트
				AddMessageLog(_buf, false);
				UpdateTextBox2();

				label3.Text = "Server:Receve message";

				// AR로 데이터 전송
				if (isArServerConnected && isArClientConnected)
				{
					if (isArClientConnected)
					{
						try
						{
							// 데이터를 XML화해서 스트림으로 데이터 전송
							NetworkStream networkStream = new NetworkStream(clientAR);
							string xmlData = SerializeToXml(packetData);
							StreamWriter writer = new StreamWriter(networkStream);
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
			}
			// 종료
			label3.Text = "Server:End";
			connectedClient.Close();
			server.Close();
			isMessengerServerConnected = false;
		}

		// 메신저간 통신에 사용되는 클라이언트(통신) 스레드 함수
		public void MessengerClient()
		{
			// 상대 메신저 서버에 연결
			string connectIP = textBox4.Text;
			//string connectIP = "127.0.0.1";
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
					client.Connect(ipep);
					isMessengerClientConnected = true;
				}
				catch
				{
					isMessengerClientConnected = false;
				}
			}
			label4.Text = "Client:connect";

			// 연결 성공 시 대기
			while (isMessengerClientConnected)
			{
				Thread.Sleep(100);
			}

			// 종료
			label4.Text = "Client:End";
			client.Close();
			isMessengerClientConnected = false;
		}

		// AR 프로세스와 통신에 사용되는 서버(수신) 스레드 함수
		public void ConnectReceivingPipe()
		{
			// AR 프로그램 클라이언트 연결 대기
			string myPort = winPort; // 내 포트 번호
			IPEndPoint ipep = new IPEndPoint(IPAddress.Any, Int32.Parse(myPort));
			serverAR = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			serverAR.Bind(ipep);
			label1.Text = "Server:Listen";
			serverAR.Listen(1);
			label1.Text = "Server:Listening";

			// AR 프로그램 클라이언트 연결
			connectedClientAR = serverAR.Accept();
			label1.Text = "Server:Accept";
			isArServerConnected = true;
			while (isArServerConnected)
			{
				// 스트림을 생성하고 데이터 수신을 대기
				label1.Text = "Wait data";
				NetworkStream networkStream = new NetworkStream(connectedClientAR);
				StreamReader reader = new StreamReader(networkStream);
				string xmlData = "";
				while (true)
				{
					xmlData = reader.ReadLine();
					if (reader.Peek() >= 0)
						xmlData += "\r\n";
					else
						break;
				}
				if (!xmlData.Equals(""))
				{
					// 수신된 xml 데이터를 파싱해서 통신 데이터와 화면을 업데이트
					label1.Text = "Receive data";
					packetData = DeserializeFromXML(xmlData);
					UpdateTextBox2();

					// 상대방에게 데이터 전송
					if (isMessengerClientConnected && isMessengerServerConnected)
					{
						// 스트림을 생성
						NetworkStream network = new NetworkStream(client);
						StreamWriter writer = new StreamWriter(network);
						// 마지막 메세지를 전송
						string currentMessage = packetData.messageLog[packetData.messageLog.Count - 1].text;
						writer.WriteLine(currentMessage.ToCharArray(), 0, currentMessage.Length);
						writer.Flush();
					}
				}
			}

			// 종료
			label1.Text = "Server:End";
			connectedClientAR.Close();
			serverAR.Close();
			isArServerConnected = false;
		}

		// AR 프로세스와 통신에 사용되는 클라이언트(통신) 스레드 함수
		public void ConnectSendingPipe()
		{
			// AR 프로세스의 서버에 연결 시도
			string connectIP = "127.0.0.1";
			string connectPort = arPort;
			IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connectIP), Int32.Parse(connectPort));
			clientAR = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			int count = 1;
			while (isArClientConnected == false)
			{
				try
				{
					Thread.Sleep(1000);
					label2.Text = "Client:trying " + count.ToString();
					count++;
					//label4.Text = IPAddress.Parse(connectIP).ToString();
					//label4.Text = "HERE:" + Int32.Parse(connectPort).ToString();
					clientAR.Connect(ipep);
					isArClientConnected = true;
				}
				catch
				{
					isArClientConnected = false;
				}
			}
			label2.Text = "Client:connect";

			// 연결에 성공하면 대기
			while (isArClientConnected)
			{
				Thread.Sleep(100);
			}

			// 종료
			label2.Text = "Client:End";
			clientAR.Close();
			isArClientConnected = false;
		}

		//PacketData 클래스를 xml화하는 함수
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

		//xml 데이터를 PacketData화하는 함수
		public static PacketData DeserializeFromXML(string xmlData)
		{
			PacketData data = null;

			StringReader stringReader = null;

			XmlSerializer deserializer = new XmlSerializer(typeof(PacketData));
			stringReader = new StringReader(xmlData);
			data = (PacketData)deserializer.Deserialize(stringReader);

			return data;
		}

		// PacketData의 메세지 로그에 메세지 추가
		public void AddMessageLog(string message, bool isInternal)
		{
			PacketData.Message temp;
			temp.text = message;
			temp.isMe = isInternal;
			packetData.messageLog.Add(temp);
		}

		// 텍스트박스2(메세지 로그 본문)을 현재 메세지 로그로 업데이트하는 함수
		public void UpdateTextBox2()
		{
			textBox2.Text = "";
			foreach (PacketData.Message tempMessage in packetData.messageLog)
			{
				if (tempMessage.isMe)
					WriteTextBox2(myName + " : " + tempMessage.text);
				else
					WriteTextBox2(yourName + " : " + tempMessage.text);
			}
		}

		// 텍스트박스2(메세지 로그)에 string 추가하는 함수
		public void WriteTextBox2(string message)
		{
			textBox2.Text += message;
			textBox2.Text += "\r\n";
		}

		// 연결(Connect) 버튼
		public void Button1Click(object sender, EventArgs e)
		{
			// 메신저 스레드들을 생성
			messengerClientThread = new Thread(MessengerClient);
			messengerClientThread.Start();
			messengerServerThread = new Thread(MessengerServer);
			messengerServerThread.Start();
		}

		// 종료 버튼
		public void Button2Click(object sender, EventArgs e)
		{
			// 연결된 모든 소켓, 스레드들을 종료
			try
			{
				if (isArClientConnected)
				{
					isArClientConnected = false;
					clientAR.Close();
					sendingThread.Abort();
				}

				if (isArServerConnected)
				{
					isArServerConnected = false;
					connectedClientAR.Close();
					serverAR.Close();
					receivingThread.Abort();
				}

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

		// 전송 버튼
		public void Button3Click(object sender, EventArgs e)
		{
			// 현재 메세지를 로그에 추가하고 화면을 업데이트
			string currentMessage = textBox1.Text;
			textBox1.Text = "";
			AddMessageLog(currentMessage, true);
			UpdateTextBox2();

			// 상대방에게 데이터 전송
			if (isMessengerClientConnected && isMessengerServerConnected)
			{
				NetworkStream networkStream = new NetworkStream(client);
				StreamWriter writer = new StreamWriter(networkStream);
				writer.WriteLine(currentMessage.ToCharArray(), 0, currentMessage.Length);
				writer.Flush();
			}

			// AR로 데이터 전송
			if (isArServerConnected && isArClientConnected)
			{
				if (isArClientConnected)
				{
					try
					{
						NetworkStream networkStream = new NetworkStream(clientAR);
						string xmlData = SerializeToXml(packetData);
						StreamWriter writer = new StreamWriter(networkStream);
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
		}

		// 외부 알림 수신 버튼(이제 사용하지 않음)
		public void Button4Click(object sender, EventArgs e)
		{
			PacketData.Message temp;
			temp.text = "This is external message";
			temp.isMe = false;
			packetData.messageLog.Add(temp);

			if (isArClientConnected)
			{
				try
				{
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
					NetworkStream networkStream = new NetworkStream(clientAR);
					string xmlData = SerializeToXml(packetData);
					StreamWriter writer = new StreamWriter(networkStream);
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

		// 프로그램 종료 시
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
					if (isArClientConnected)
					{
						isArClientConnected = false;
						clientAR.Close();
						sendingThread.Abort();
					}

					if (isArServerConnected)
					{
						isArServerConnected = false;
						connectedClientAR.Close();
						serverAR.Close();
						receivingThread.Abort();
					}

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

		// AR 연결 버튼
		private void button1_Click(object sender, EventArgs e)
		{
			// AR 통신 스레드 생성
			sendingThread = new Thread(ConnectSendingPipe);
			sendingThread.Start();
			receivingThread = new Thread(ConnectReceivingPipe);
			receivingThread.Start();
		}
	}
}
