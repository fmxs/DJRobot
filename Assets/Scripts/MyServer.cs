using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class MyServer : MonoBehaviour
{
    //咱们这里写个委托用来把数据传出来
    //不理解的童鞋可以把这行为理解为一种把服务器消息传出去的方式
    public delegate void ServerCallBack(byte[] contex);
    
    private void Start()
    {
        Server();
    }
    public class CreateServer
    {
        public int connectPort = 4000;
        //写成单例，以方便在需要创建服务器时调用
        private static CreateServer _instance;
        private CreateServer() { }
        public static CreateServer GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CreateServer();
            }
            return _instance;
        }

        private Socket serverSocket;//创建服务器Socket对象
        private ServerCallBack serverCallBack;//传出消息的委托对象
        public void InitServer(ServerCallBack CallBack)
        {
            //将委托对象传输进来        
            serverCallBack = CallBack;
            //创建Sock，有两种重载，我用了三个参数的重载，分别是三个枚举（IP地址族，双向读写流的传输协议，Tcp传输控制协议）        
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //绑定网络节点,设置所有ip都可以连        
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, connectPort));
            //设置监听端口,可以同时连接1000个客户端        
            serverSocket.Listen(1000);
            //接受客户端的请求,下面的ServerAccept()是一个回调方法,        
            //用来服务器接受请求，看好是接受请求，不是接收数据        
            serverSocket.BeginAccept(ServerAccept, serverSocket);
            Debug.Log(serverSocket + "开始接受请求");
        }
    

    //在接受客户端请求后，需要回调用来接收数据方法
    //所以我们在这里建一个临时存储数据的数组,
        private byte[] serverbuffer = new byte[1024];
        /// <summary>
        /// 接受请求的回调方法
        /// </summary>
        void ServerAccept(System.IAsyncResult ar)
        {
            try{
                //获取接受请求的套接字
                serverSocket = ar.AsyncState as Socket;
                //在回调里结束接受请求
                Socket workingSocket = serverSocket.EndAccept(ar);
                //接受完请求后,开始接收消息,看好是接收
                //BeginReceive(用接收消息的数组,第0个字节开始接收，接受的字节数，Socket标识符，接收消息之后的回调接受数据的方法)
                workingSocket.BeginReceive(serverbuffer, 0, serverbuffer.Length, SocketFlags.None, ServerReceive, workingSocket);
                Debug.Log(serverSocket + "开始接受消息");
                //下面写了一个尾递归
                //当接受完一个客户端的连接请求后，继续接受其他客户端的连接请求
                workingSocket.BeginAccept(ServerAccept, serverSocket);
            }
            catch (Exception ex){
                Debug.Log(ex);
            }
        }

        /// <summary>
        /// 接收数据的回调方法
        /// </summary>
        /// <param name="ar"></param>
        void ServerReceive(System.IAsyncResult ar)
        {
            //获取接收消息的套接字
            serverSocket = ar.AsyncState as Socket;
            //结束接受,这里返回接受数据字节数
            int count = serverSocket.EndReceive(ar);
            //如果有数据，接收消息
            if (count > 0)
            {
                //将数据通过委托，传送到外界
                serverCallBack(serverbuffer);
            }
            //下面再写一个尾递归
            //服务器成功连接到客户端后，继续接收当前客户端发来的消息
            serverSocket.BeginReceive(serverbuffer, 0, serverbuffer.Length, SocketFlags.None, ServerReceive, serverSocket);
        }
    }
    /// <summary>
     /// 创建服务器
     /// </summary>
    void Server()
    {
        //后台运行
        Application.runInBackground = true;

        CreateServer.GetInstance().InitServer(Print);
    }
    //public delegate void ServerCallBack(byte[] contex);这是上面的委托
    /// <summary>
    /// 下面是定义一个跟委托类型相同方法
    /// </summary>
    /// <param name="msg"></param>
    void Print(byte[] msg)
    {

    }
}
