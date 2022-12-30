using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
public class MyClient : MonoBehaviour
{
    public string connectIP = "192.168.1.104";
    public int connectPort = 4000;
    public string sendText;
    private void Start()
    {
        Client();
    }
    /// <summary>
    ///创建客户端
    /// </summary>
    void Client()
    {
        //直接调用我们自己写的方法，传进ip和端口号，就可以连接服务器了
        //（服务器ip，端口号）23546随便写的端口号，但要与服务器创建时一致
        //127.0.0.1因为是我自己建的服务器，直接写了本地地址
        CreateClient.GetInstance().InitClient(connectIP, connectPort);

        //假设下面是我们要发送的数据
        string text = "Come from Client";
        //调用我们自己写的发送消息方法，直接发送过去就行了
        CreateClient.GetInstance().ClientSend(text);
        Debug.Log("[Client]发送成功" + text);
    }
    void OnGUI()
    {
        if (GUI.Button(new Rect(180, 40, 100, 20), "发送字符串"))
            CreateClient.GetInstance().ClientSend(sendText);
    }
    public class CreateClient
    {
        //同服务器一样写成单例，以方便在需要创建客户端时调用
        private static CreateClient _instance;
        private CreateClient() { }
        public static CreateClient GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CreateClient();
            }
            return _instance;
        }
        //创建客户端Socket对象
        private Socket clientSocket;
        /// <summary>
        /// 客户端创建方法
        /// </summary>
        /// <param name="ip">服务器IP</param>
        /// <param name="port">端口号</param>
        public void InitClient(string ip, int port)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            Debug.Log("[Client]创建成功");
        }
        //由于我们需要发送数据，所有跟服务器一样
        //声明一个byte的数组用于数据发送
        private byte[] clientbuffer = new byte[1024];
        /// <summary>
        /// 客户端发送消息方法
        /// </summary>
        /// <param name="msg">客户端要发送的数据，可以在外面调用这个方法发送数据</param>
        public void ClientSend(string msg)
        {
            //这里就用到了System.Text中的转换,一般采用UTF8的编码
            clientbuffer = UTF8Encoding.UTF8.GetBytes(msg);
            //开始发送消息
            //参数（发送的byte[]数据，偏移量，消息长度，SocketFlags服务，回调方法，当前状态）
            clientSocket.BeginSend(clientbuffer, 0, clientbuffer.Length, SocketFlags.None, clientAySend, clientSocket);
        }
        /// <summary>
        /// 回调方法
        /// </summary>
        private void clientAySend(System.IAsyncResult ar)
        {
            clientSocket = ar.AsyncState as Socket;
            //在回调中结束发送,返回一个int
            //count:发送的数据的字节数
            int count = clientSocket.EndSend(ar);
        }
    }
}
