using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Pipes;
using System.Threading;

namespace CustomMessenger
{
	public partial class Form1 : Form
	{
		NamedPipeClientStream recevingPipe;

		Thread sendingThread;
		NamedPipeServerStream sendingPipe;

		int dataCount = 0;

		public Form1()
		{
			InitializeComponent();

			sendingThread = new Thread(ConnectPipe);
			sendingThread.Start();
		}

		public void ConnectPipe()
		{
			try
			{
				sendingPipe = new NamedPipeServerStream("PcToUnity", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1024, 1024);
			}
			catch (Exception e)
			{
				MessageBox.Show("An error has occurred while pipe open.\n" + e.Message);
			}
			IAsyncResult pipeCall = sendingPipe.BeginWaitForConnection(null, null);

			while (!pipeCall.IsCompleted == false)
			{
				label1.Text = "Waiting for connection";
			}
			sendingPipe.EndWaitForConnection(pipeCall);

			while (sendingPipe.IsConnected)
			{
				label1.Text = "Connecting";
				try
				{
					byte[] data;
					data = Encoding.ASCII.GetBytes(textBox1.Text + "\n" + dataCount);
					sendingPipe.Write(data, 0, data.Length);
					sendingPipe.Flush();
					sendingPipe.WaitForPipeDrain();
					dataCount += 1;
				}
				catch (Exception ex)
				{
					if (!ex.Message.StartsWith("Pipe is broken."))
					{
						MessageBox.Show("An error has occurred while seding data. \n" + ex.Message);
					}
				}
			}
			sendingPipe.Close();
		}
	}
}
