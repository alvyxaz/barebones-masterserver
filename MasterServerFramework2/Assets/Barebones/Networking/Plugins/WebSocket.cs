using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;

namespace Barebones.Networking
{
    public class WebSocket
    {
        private Uri mUrl;
    
        public WebSocket(Uri url)
        {
            mUrl = url;

            string protocol = mUrl.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }


#if !UNITY_EDITOR && (UNITY_WEBGL) 
        private bool SupportsThreads { get { return false; } }
#else
        private bool SupportsThreads { get { return true; } }
#endif

        public void SendString(string str)
        {
            Send(Encoding.UTF8.GetBytes (str));
        }

        public string RecvString()
        {
            byte[] retval = Recv();
            if (retval == null)
                return null;
            return Encoding.UTF8.GetString (retval);
        }

        public bool IsConnecting { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern int SocketCreate (string url);

	[DllImport("__Internal")]
	private static extern int SocketState (int socketInstance);

	[DllImport("__Internal")]
	private static extern void SocketSend (int socketInstance, byte[] ptr, int length);

	[DllImport("__Internal")]
	private static extern void SocketRecv (int socketInstance, byte[] ptr, int length);

	[DllImport("__Internal")]
	private static extern int SocketRecvLength (int socketInstance);

	[DllImport("__Internal")]
	private static extern void SocketClose (int socketInstance);

	[DllImport("__Internal")]
	private static extern int SocketError (int socketInstance, byte[] ptr, int length);

	int m_NativeRef = 0;

	public void Send(byte[] buffer)
	{
		SocketSend (m_NativeRef, buffer, buffer.Length);
	}

	public byte[] Recv()
	{
		int length = SocketRecvLength (m_NativeRef);
		if (length == 0)
			return null;
		byte[] buffer = new byte[length];
		SocketRecv (m_NativeRef, buffer, length);
		return buffer;
	}

	public IEnumerator Connect()
	{
		m_NativeRef = SocketCreate (mUrl.ToString());
        IsConnecting = true;
		while (SocketState(m_NativeRef) == 0)
			yield return 0;

        IsConnecting = false;
	}
 
    public bool IsConnected { get { return SocketState(m_NativeRef) == 1; } }

	public void Close()
	{
		SocketClose(m_NativeRef);
	}

	public string error
	{
		get {
			const int bufsize = 1024;
			byte[] buffer = new byte[bufsize];
			int result = SocketError (m_NativeRef, buffer, bufsize);

			if (result == 0)
				return null;

			return Encoding.UTF8.GetString (buffer);				
		}
	}
#else
        WebSocketSharp.WebSocket m_Socket;
        Queue<byte[]> m_Messages = new Queue<byte[]>();
        bool m_IsConnected = false;
        string m_Error = null;
	    
        public bool IsConnected { get { return m_IsConnected; } }

        public IEnumerator Connect()
        {
	        m_Socket = new WebSocketSharp.WebSocket(mUrl.ToString());
	        m_Socket.OnMessage += (sender, e) =>
            {
                m_Messages.Enqueue(e.RawData);
            };
            m_Socket.OnOpen += (sender, e) =>
            {
                m_IsConnected = true;
            };
            m_Socket.OnError += (sender, e) =>
            {
                m_Error = e.Message;
                Logs.Error(e.Message);
            };
            m_Socket.OnClose += (sender, args) => m_IsConnected = false;

            if (SupportsThreads)
            {
	            //HACK: When restarting in the Unity Editor, send a ping to the destination first to avoid having Connect() hang for 90 seconds.
	            //https://github.com/alvyxaz/barebones-masterserver/pull/142
	            
	            //Note: On Windows Store Apps, a stream socket is used to mimic ping functionality. It will try to open connection to specified ip address with port 80. Also you need to enable InternetClient capability in Package.appxmanifest.
	            //https://docs.unity3d.com/ScriptReference/Ping.html
	            Ping ping = new Ping(mUrl.Host);
	            
	            //The ping send/receive takes about a second, so we wait for it to complete.
	            while (!ping.isDone)
	            {
		            yield return null;
	            }
	            
	            //If the ping succeeded, the time will be 0 or higher, otherwise -1. 
	            if (ping.time >= 0)
	            {   
		            runThread(() =>
		            {
			            m_Socket.Connect();
		            });
	            }
	            else
	            {
		            m_Error = "Barebones Websocket: Could not contact \"" + mUrl.Host + "\" with Ping.";
	            }
            }
            else
            {
                m_Socket.Connect();
            }

            IsConnecting = true;
            while (!m_IsConnected && m_Error == null)
            {
                yield return null;
            }
            IsConnecting = false;
        }

        public void Send(byte[] buffer)
        {
             m_Socket.Send(buffer);
        }

        public byte[] Recv()
        {
            if (m_Messages.Count == 0)
                return null;

            return m_Messages.Dequeue();
        }

        public void Close()
        {
	        if (IsConnected)
	        {
		        if (SupportsThreads)
		        {
			        runThread(() =>
			        {
				        m_Socket.Close();
			        });
		        }
		        else
		        {
			        m_Socket.Close();
		        }
	        }
        }

        public string error
        {
            get {
                return m_Error;
            }
        }

	    void runThread(Action action)
	    {
		    Thread newThread = new Thread(new ThreadStart(() =>
		    {
			    action();
		    }));
		    newThread.Start();
	    }
	    
#endif 
    }
}