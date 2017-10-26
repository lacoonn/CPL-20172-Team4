using System;
using System.Collections.Generic;
using System.Text;

namespace CustomMessenger
{
	[Serializable]
	public class PacketData
	{
		public bool hasNewMessage;
		public string newMessage;

		public List<string> messageLog; // 메세지 기록, 과거부터 현재까지 순서로
		public string typingMessage; // 현재 작성 중인 채팅
	}
}
