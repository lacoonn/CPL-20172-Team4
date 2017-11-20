using System;
using System.Collections.Generic;
using System.Text;

namespace CustomMessenger
{
	[Serializable]
	public class PacketData
	{
		public struct Message
		{
			public string text; // 로그에서 메시지 string
			public bool isMe; // 로그에서 이 메시지가 내가 보낸 메시지인지 여부
		}
		public bool hasNewMessage;
		public string newMessage;

		public List<Message> messageLog; // 메세지 기록, 과거부터 현재까지 순서로

		public bool hasNewCalendarAlarm; // 새로운 캘린더 알림을 포함하고 있는지 여부
		public string newCalendarAlarm; // hasNewCalendarAlarm이 true라면 이 string은 캘린더 이벤트의 Summary이다.
	}
}
