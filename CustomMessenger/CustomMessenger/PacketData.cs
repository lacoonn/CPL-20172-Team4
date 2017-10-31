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
			public string text;
			public bool isMe;// 로그에서 내가 보낸 메시지인지 저장
		}
		public bool hasNewMessage;
		public string newMessage;

		public List<Message> messageLog; // 메세지 기록, 과거부터 현재까지 순서로
	}
}
