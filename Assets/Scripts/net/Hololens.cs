using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Hololens : MonoBehaviour
{

    #region 参数
    List<Socket> ClientProxSocketList = new List<Socket>();
    //接收从HoloLens发送的数据
    public string HololensString;
    //接收从Model发送的数据
    public string ModelString;
    //接收从Kinect发送的数据
    public string KinectString;
    //发往HoloLens的数据
    public string editString = ""; //编辑框文字
    //定义服务器的IP和端口，端口与服务器对应
    public string IPAddress_Server = "192.168.31.240";//可以是局域网或互联网ip
    public string Port_Server = "5000";
    public bool trackersend = false,Controlsend=true;
    //zpp 2020.12.26 整理从模型中传来的数据
    public Queue ModelDataqueue = new Queue();
    //zpp 2020.12.26 真正的模型数据
    public Queue RealModelDataqueue = new Queue();

    #endregion

    //跨脚本调用
    private UnityServer unityserver;
    private Kinect kinect;

    //机器人电量Power
    private string RobotPower;
    // Start is called before the first frame update
    void Start()
    {
        unityserver = GameObject.Find("Server").GetComponent<UnityServer>();
        kinect = GameObject.Find("Server").GetComponent<Kinect>();
        ServerStart();
        //InvokeRepeating("SendDateEdit", 1f, 1f);
        InvokeRepeating("SendTo_HololensRobotPower",15f,15f);

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        AnalysisDate();
        //SendTrackerPostion();
    }

    #region 解析数据
    void AnalysisDate()
    {
        #region 读取控制机器人的开关
        //HoloLens发来的数据
        if (HololensString.Contains("HO") && HololensString.Contains("RC") && HololensString.Contains("NS"))
        {
            string temp = HololensString;
            HololensString = "";
            string RobotId = temp.Substring(4, 1);//id
            string Taskstart = temp.Substring(5, 1);//status
            unityserver.TaskMessage(RobotId, Taskstart);

        }
        #endregion

        #region 读取灯状态控制
        //if (HololensString.Contains("HO") && HololensString.Contains("LC") && HololensString.Contains("NS"))
        //{
        //    string temp = HololensString;
        //    HololensString = "";
        //    string LightStatus = temp.Substring(4, 1);//status
        //    unityserver.LightControl(LightStatus);
        //}
        #endregion

        #region 读取账号密码
        //读取账号密码
        if (HololensString.Contains("HOUS")&&HololensString.Contains("NS"))
        {
            char[] temp = HololensString.ToCharArray();
            int templength = temp.Length;
            HololensString = "";
            string username="";
            int i = 4;
            while (i<=templength-3)
            {
                username += temp[i];
                i++;
            }
            unityserver.Hololens_User(username);
            Debug.Log("Username:" + username);
        }
        if (HololensString.Contains("HOPW") && HololensString.Contains("NS"))
        {
            char[] temp = HololensString.ToCharArray();
            int templength = temp.Length;
            HololensString = "";
            string password = "";
            int i = 4;
            while (i <= templength - 3)
            {
                password += temp[i];
                i++;
            }
            unityserver.Hololens_PW(password);
            Debug.Log("Password:" + password);
        }
        #endregion

        #region 读取控制Kinect开关
        if (HololensString.Contains("HOKN"))
        {
            HololensString = "";
            kinect.KinectStartStatus = 1;
            //kinect.SendToKinect = 1;
            Debug.Log("KinectStart");
        }
        #endregion

        #region 读取 Model 发来的数据
        //Model发来的数据
        if (ModelString.Contains("Model"))
        {
            char[] temp= ModelString.ToCharArray();
            ModelString = "";
            int i = 0;
            while (i<temp.Length)
            {
                if (temp[i] >= 48 && temp[i] <= 57)
                    if(!ModelDataqueue.Contains(temp[i]))
                        ModelDataqueue.Enqueue(temp[i]);
                i++;
            }
            if(RealModelDataqueue.Count==0)
                RealModelDataqueue = ModelDataqueue;
            else
            {
                RealModelDataqueue = ModelDataqueue;
                StartCoroutine(CountModelData());
                RealModelDataqueue.Clear();
                ModelDataqueue.Clear();
            }
        }

        #endregion

        #region 读取 Kinect 发来的数据
        if (KinectString.Contains("Open"))
        {
            KinectString = "";
            //SendTo_HololensKinectStart();
            UnitySend_Message("UNKS1NS");
            kinect.SendToKinect = 1;
            Debug.Log("HOLOLENS kinectStart");
        }
        else if (KinectString.Contains("Close"))
        {
            KinectString = "";
            kinect.ModelMessageStatus = 1;
            UnitySend_Message("KinectStart");
        }
        #endregion

        #region 读取RobotHand初始化指令
        if (HololensString.Contains("HORH"))
        {
            HololensString = "";
            unityserver.robotcontrol.RobotHandInit();
        }
        #endregion
    }

    #endregion

    #region 发送数据

    #region 发送Tracker数据
    //public void SendTrackerPostion() //发送Tracker数据
    //{
    //    //向 tracker 发送数据
    //    editString = "";
    //    for (int i = 0; i < unityserver.trackerposition.Length; i++)
    //    {
    //        string x = unityserver.trackerposition[i].position.x.ToString();
    //        string y = unityserver.trackerposition[i].position.y.ToString();
    //        string z = unityserver.trackerposition[i].position.z.ToString();
    //        editString += "HO" + "TK" + "0x3" + i + "X" + x + "Y" + y + "Z" + z + "LS";
    //        SendMsg();

    //    }
    //}
    #endregion

    //#region 发送登录信息回馈
    ////public void SendTo_HololenSignMessage()//发送登录信息回馈
    ////{
    ////    editString = "Sign"+unityserver.m_info;
    ////    SendMsg();
    ////}
    //#endregion

    //#region 给Model 和 Kinect 发送信号
    ////public void SendModelMessage() //给Model 一个开始信号
    ////{
    ////    editString = "ModelStart";
    ////    SendMsg();
    ////}

    ////public void SendKinectMessage() //给Kinect 一个开始信号
    ////{
    ////    editString = "KinectStart";
    ////    SendMsg();
    ////}
    //#endregion

    //#region 告诉HoloLens Kinect打开了
    ////public void SendTo_HololensKinectStart()
    ////{
    ////    editString = "UNKS1NS";
    ////    SendMsg();
    ////}
    //#endregion

    #region 发送Web和HoloLens 分数 AND 获取Level AND 获取扣分项
    IEnumerator CountModelData()
    {
        
        //发送数据
        string tempstring = "";
        while (RealModelDataqueue.Count > 0)
        {
            tempstring += RealModelDataqueue.Dequeue();
        }
        kinect.ModelData = tempstring;
        char[] tempchar = tempstring.ToCharArray();
        kinect.ModelScore = 100;
        for (int i = 0; i < tempchar.Length; i++)
        {
            //例子
            switch (tempchar[i])
            {
                case '1':
                    kinect.ModelScore -= 20;
                    break;
                case '2':
                    kinect.ModelScore -= 10;
                    break;
                case '3':
                    kinect.ModelScore -= 20;
                    break;
                case '4':
                    kinect.ModelScore -= 10;
                    break;
                case '5':
                    kinect.ModelScore -= 10;
                    break;
                case '6':
                    kinect.ModelScore -= 10;
                    break;
                default:
                    break;

            }
        }
        string level= unityserver.Kinect_ModelScore(kinect.ModelScore);
        unityserver.Kinect_ModelData(kinect.ModelData);
        //editString = "UNSC" + kinect.ModelScore + "NS" + level;
        //SendMsg();
        UnitySend_Message("UNSC" + kinect.ModelScore + "NS" + level);
        yield return new WaitForSeconds(0.2f);
        //editString = "UNMD"+ kinect.ModelData + "NS";
        //SendMsg();
        UnitySend_Message("UNMD" + kinect.ModelData + "NS");

    }  //数据计算和发送至HoloLens

    #endregion

    #region Unity服务器广播消息
    public void UnitySend_Message(string temp)
    {
        editString = temp;
        SendMsg();
    }
    #endregion

    #region 回传机器人状态
    //public void SendRobotTaskStatus(int RobotId)//发送机器人状态回馈
    //{
    //    //editString = "";
    //    editString = "HOET" + RobotId + "NS";
    //    SendMsg();
    //}
    #endregion

    #region 发送电量
    void SendTo_HololensRobotPower()
    {
        string temp = "RobotPower"+unityserver.robotcontrol.Electricity();//获取电量
        UnitySend_Message(temp);
    }

    #endregion

    #region 程序中止时
    private void OnDestroy()
    {
        UnitySend_Message("ModelDataStop");
    }

    #endregion

    #endregion

    #region hololens 网络连接
    private void ServerStart()
    {
        //1 创建Socket对象
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //2 绑定端口ip
        socket.Bind(new IPEndPoint(IPAddress.Parse(IPAddress_Server), int.Parse(Port_Server)));
        //3 开启侦听
        socket.Listen(10);//链接等待队列：同时来了100个链接请求，队列里放10个等待链接客户端，其他返回错误信息
        //4 开始接受客户端的链接
        ThreadPool.QueueUserWorkItem(new WaitCallback(this.AcceptClientConnect), socket);
    }

    public void AcceptClientConnect(object socket)
    {
        var serverSocket = socket as Socket;//强制类型转换  

        this.AppendTextToConsole("服务器端开始接受客户端的链接");

        while (true)//不断的接收
        {
            var proxSocket = serverSocket.Accept();//会阻塞当前线程，因此必须放入异步线程池中
            this.AppendTextToConsole(string.Format("客户端:{0}链接上了", proxSocket.RemoteEndPoint.ToString()));
            ClientProxSocketList.Add(proxSocket);//使方法体外部也可以访问到方法体内部的数据

            //不停接收当前链接的客户端发送来的消息
            //不能因为接收一个客户端消息阻塞整个线程，启用线程池
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ReceiveData), proxSocket);
        }
    }

    //接收客户端消息
    public void ReceiveData(object socket)//接收消息
    {
        var proxSocket = socket as Socket;
        byte[] data = new byte[1024 * 1024];
        while (true)
        {
            int len = 0;
            try
            {
                len = proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
            }
            catch (Exception)
            {
                //异常退出,在阻塞线程时与服务器连接中断或断电等等
                AppendTextToConsole(string.Format("接收到客户端:{0}非正常退出", proxSocket.RemoteEndPoint.ToString()));
                ClientProxSocketList.Remove(proxSocket);
                StopConnect(proxSocket);
                return;
            }


            if (len <= 0)
            {
                //客户端正常退出
                AppendTextToConsole(string.Format("接收到客户端:{0}正常退出", proxSocket.RemoteEndPoint.ToString()));
                ClientProxSocketList.Remove(proxSocket);

                StopConnect(proxSocket);
                return;//让方法结束。终结当前接收客户端数据的异步线程
            }

            //把接收到的数据放到文本框上
            string str = Encoding.Default.GetString(data, 0, len);
            AppendTextToConsole(string.Format("接收到客户端:{0}的消息是:{1}", proxSocket.RemoteEndPoint.ToString(), str));
            string temp = string.Format(str);
            if (temp.Contains("Model"))
                ModelString = temp;
            else if (temp.Contains("Kinect"))
                KinectString = temp;
            else
                HololensString = temp;
        }
    }
    private void StopConnect(Socket proxSocket)  //关闭连接
    {
        try
        {
            if (proxSocket.Connected)
            {
                proxSocket.Shutdown(SocketShutdown.Both);
                proxSocket.Close(100);//100秒后没有正常关闭则强行关闭
            }
        }
        catch (Exception)
        {

        }
    }
    //发送字符串
    private void SendMsg() //发送消息
    {
        foreach (var proxSocket in ClientProxSocketList)
        {
            if (proxSocket.Connected)
            {
                //原始的字符串转换成的字节数组
                byte[] data = Encoding.Default.GetBytes(editString);
                proxSocket.Send(data, 0, data.Length, SocketFlags.None);
                Debug.Log("服务器已发送" + editString);
            }
        }
    }

    //在控制台打印要显示的内容
    public void AppendTextToConsole(string txt)  //打印消息
    {
        Debug.Log(string.Format("{0}", txt));
    }

    #endregion
}
