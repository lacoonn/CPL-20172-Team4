using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

[Serializable]
public class PacketData
{
	public bool hasNewMessage;
	public string newMessage;

	//public List<string> messageLog; // 메세지 기록, 과거부터 현재까지 순서로
	public string typingMessage; // 현재 작성 중인 채팅

	/*public PacketData()
	{
		hasNewMessage = false;
		newMessage = "";

		messageLog = new List<string>();
		typingMessage = "";
	}

	public PacketData(bool _hasNewMessage, string _newMessage, List<string> _messageLog, string _typingMessage)
	{
		hasNewMessage = _hasNewMessage;
		newMessage = _newMessage;

		messageLog = _messageLog;
		typingMessage = _typingMessage;
	}

	public void GetPacketData(ref PacketData packetData)
	{
		packetData.hasNewMessage = hasNewMessage;
		packetData.newMessage = newMessage;
		packetData.messageLog = new List<string>(messageLog);
		packetData.typingMessage = typingMessage;
	}*/
}

