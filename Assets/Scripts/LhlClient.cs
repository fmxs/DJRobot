using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System;
using UnityEngine;
using UnityEngine.UI;

public class LhlClient : MonoBehaviour
{
    public string connectIP = "192.168.1.104";
    public int connectPort = 5000;
    public InputField InputField;

    void Start()
    {
        Connection();
    }
    private void ac(string connectIP, int connectPort)
    {
        this.connectIP = connectIP;
        this.connectPort = connectPort;
        
        //Connection();
        //while (true)
        //{
        //    if (socket == null)
        //        return;
        //    if (socket.Poll(0, SelectMode.SelectRead))
        //    {
        //        byte[] readBuff = new byte[1024];
        //        int count = socket.Receive(readBuff);
        //        string recvStr = System.Text.Encoding.Default.GetString(readBuff, 0, count);
        //    }
        //}
    }
    void Connection()
    {
       
        Socket socket;
        try
        {
            //socket  连接模式
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            //Connect  进行链接
            socket.Connect(connectIP, connectPort);
           
            string iPRemote = socket.RemoteEndPoint.ToString();
           
            Debug.Log("Client : 连接服务器" + iPRemote + "成功");
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

    }

    //void OnGUI()
    //{
    //    if (GUI.Button(new Rect(180, 40, 100, 20), "发送字符串"))
    //        Send();
    //}
    //public void Send()
    //{
    //    string sendStr = InputField.text;                   //编辑发送数据
    //    byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
    //    socket.Send(sendBytes);
    //}
}