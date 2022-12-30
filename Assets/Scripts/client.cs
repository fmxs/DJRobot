using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

public class client : MonoBehaviour
{
    public Socket m_socket;
    IPEndPoint m_endPoint;
    private SocketAsyncEventArgs m_connectSAEA;
    private SocketAsyncEventArgs m_sendSAEA;
    public static string ip = "192.168.1.115";
    public int port = 4000;

    private void Start()
    {
        Client();
    }
    public void Client()
    {
        m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress iPAddress = IPAddress.Parse(ip);
        m_endPoint = new IPEndPoint(iPAddress, port);
        m_connectSAEA = new SocketAsyncEventArgs { RemoteEndPoint = m_endPoint };
        m_connectSAEA.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectedCompleted);
        m_socket.ConnectAsync(m_connectSAEA);
    }

    private void OnConnectedCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success) return;
        Socket socket = sender as Socket;
        string iPRemote = socket.RemoteEndPoint.ToString();

        Debug.Log("Client : 连接服务器" + iPRemote + "成功");

        SocketAsyncEventArgs receiveSAEA = new SocketAsyncEventArgs();
        byte[] receiveBuffer = new byte[1024 * 1024 * 16];
        receiveSAEA.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
        receiveSAEA.Completed += OnReceiveCompleted;
        receiveSAEA.RemoteEndPoint = m_endPoint;
        socket.ReceiveAsync(receiveSAEA);
    }
    private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
    {

        if (e.SocketError == SocketError.OperationAborted) return;

        Socket socket = sender as Socket;

        if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
        {
            string ipAdress = socket.RemoteEndPoint.ToString();
            int lengthBuffer = e.BytesTransferred;
            byte[] receiveBuffer = e.Buffer;

            //向服务器端发送消息
            Send("成功收到消息");

            socket.ReceiveAsync(e);
        }
        else if (e.SocketError == SocketError.ConnectionReset && e.BytesTransferred == 0)
        {
            Debug.Log("Client: 服务器断开连接 ");
        }
        else
        {
            return;
        }
    }
        #region 发送
    public void Send(string msg)
        {
            byte[] sendBuffer = Encoding.Default.GetBytes(msg);
            if (m_sendSAEA == null)
            {
                m_sendSAEA = new SocketAsyncEventArgs();
                m_sendSAEA.Completed += OnSendCompleted;
            }

            m_sendSAEA.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            if (m_socket != null)
            {
                m_socket.SendAsync(m_sendSAEA);
            }
     }

    private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success) return;
            Socket socket = sender as Socket;
            byte[] sendBuffer = e.Buffer;

            string sendMsg = Encoding.Default.GetString(sendBuffer);

            Debug.Log("Client : Send message" + sendMsg + "to Serer" + socket.RemoteEndPoint.ToString());
     }
        #endregion
        #region 断开连接
    public void DisConnect()
        {
            if (m_socket != null)
            {
                try
                {
                    m_socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException )
                {
                }
                finally
                {
                    m_socket.Close();
                }
            }
     }
        #endregion

}