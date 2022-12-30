//using System;
//using UnityEngine;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
///// <summary>
///// Define the struct of PID
///// </summary>
//#region PIDStruct
//public struct Pidx
//{
//    public double exp_x;
//    public double now_x;
//    public double last_x;
//    public double err_x;
//    public double sum_x;
//    public double speed_x;
//};

//public struct Pidy
//{
//    public double exp_y;
//    public double now_y;
//    public double last_y;
//    public double err_y;
//    public double sum_y;
//    public double speed_y;
//};
//#endregion

//public class pid_control : MonoBehaviour
//{
//    public Transform Tracker, Target;
//    public float angle;//The angle between tracker and target
//    public float[] SaveVec = new float[5];//保存Transform.position的值
//    private int flag;
//    private int rotateAngle = 3;
//    #region PID
//    private Socket tcpClientRobot;
//    private IPAddress ipaddressRobot;
//    private EndPoint pointRobot;
//    private string x_speed_string;
//    private string y_speed_string;
//    private string message_zero;//停止指令
//    private string message2ToServer;//控制速度指令
//    private byte[] position = new byte[1000];
//    private string angel_string, messageaToServer;
//    private string RobotRotateString = "chassis move vz 360 z ";
//    #endregion
//    private void Awake()
//    {
//        #region 建立socket连接
//        //----------------------------建立socket连接---------------------------------------------
//        tcpClientRobot = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//        ipaddressRobot = IPAddress.Parse("192.168.2.1");//IPAddress.Parse可以把string类型的ip地址转化为ipAddress型
//        pointRobot = new IPEndPoint(ipaddressRobot, 40923);//通过ip地址和端口号定位要连接的服务器端
//        try
//        {
//            tcpClientRobot.Connect(pointRobot);//建立连接
//        }
//        catch (Exception ex)
//        {
//            Debug.Log(ex);
//        }
//        #endregion
//    }

//    void Start()
//    {
//        SaveVec[0] = Tracker.position.z;//Tracker在Z轴上的初始值
//        SaveVec[1] = Tracker.position.x;//Tracker在X轴上的初始值
//                                        //Quaternion RotateY = Quaternion.FromToRotation(Tracker.position, Vector3.forward);
//                                        //angle = RotateY.y * (RotateY.w / Math.Abs(RotateY.w));

//        RobotControl();
//    }

//    void RobotControl()
//    {
//        #region PID参数
//        //-------------------------PID参数设定与初始化 ----------------------------

//        Pidx pidx;
//        pidx.exp_x = 0.6;
//        pidx.now_x = 0;
//        pidx.err_x = 0;
//        pidx.last_x = 0;
//        pidx.sum_x = 0;
//        pidx.speed_x = 0;

//        Pidy pidy;
//        pidy.exp_y = 0.6;
//        pidy.now_y = 0;
//        pidy.err_y = 0;
//        pidy.last_y = 0;
//        pidy.sum_y = 0;
//        pidy.speed_y = 0;

//        double KP, KI, KD;  //手动调试输入KP,KI,KD
//        KP = 0.82;
//        KI = 0.012;
//        KD = 0.13;
//        #endregion
//        //--------进入连接---------
//        string messageToServer = "command;";
//        UnityEngine.Debug.Log("向服务器端发送消息：" + messageToServer);//
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageToServer));//向服务器端发送消息

//        byte[] data = new byte[1000];
//        int length_1 = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
//        string message_1 = Encoding.UTF8.GetString(data, 0, length_1);//把字节数组转化为字符串
//        UnityEngine.Debug.Log("接收到服务器端的消息：" + message_1);
//        //-------------------对齐坐标轴------------------
//        #region xjy修改
//        //angle = GetRobotRotate(Tracker, Tracker.position, Target.position);
//        //flag = GetRotationLeftOrRight(Tracker, Target);
//        //if (flag <= 0)//左转
//        //{
//        //    while (angle > 2f)
//        //    {
//        //        angel_string = Convert.ToString(rotateAngle);
//        //        tcpClientRobot.Send(Encoding.UTF8.GetBytes(RobotRotateString + angel_string + ";"));   //向服务器端发送旋转指令
//        //        System.Threading.Thread.Sleep(500);
//        //        angle -= rotateAngle;
//        //    }
//        //}
//        //else//右转
//        //{
//        //    while (angle > 2f)
//        //    {
//        //        angel_string = Convert.ToString(rotateAngle);
//        //        tcpClientRobot.Send(Encoding.UTF8.GetBytes(RobotRotateString + angel_string + ";"));   //向服务器端发送旋转指令
//        //        System.Threading.Thread.Sleep(500);
//        //        angle -= rotateAngle;
//        //    }
//        //}
//        #endregion

//        if (angle > 2f)
//        {
//            angel_string = Convert.ToString(angle);
//            messageaToServer = "chassis move vz 180 z " + angel_string + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
//            System.Threading.Thread.Sleep(500);
//            UnityEngine.Debug.Log("旋转角度：" + angle);
//            //angle = Vector3.Angle(Tracker.transform.forward, Vector3.forward);//重新获得该物体Z轴和世界坐标Z轴的夹角
//            //Quaternion qua = Quaternion.FromToRotation(Tracker.position, Vector3.forward);
//            //angle = qua.eulerAngles.y;
//        }
//        //-------------------------------------
//        while (pidx.exp_x - pidx.err_x > 0.1 || pidx.exp_x - pidx.err_x < -0.1 || pidy.exp_y - pidy.err_y > 0.1 || pidy.exp_y - pidy.err_y < -0.1)
//        {
//            System.Threading.Thread.Sleep(300); //每次循环结束等待0.3秒
//            //UnityEngine.Debug.Log("等待结束");
//            //------------x速度计算-------------
//            pidx.last_x = pidx.err_x;
//            pidx.err_x = pidx.exp_x - pidx.now_x;
//            pidx.sum_x += pidx.err_x;
//            pidx.speed_x = KP * pidx.err_x + KI * pidx.sum_x + KD * (pidx.last_x - pidx.err_x);
//            if (pidx.speed_x > 3.5)
//                pidx.speed_x = 3.5;
//            else if (pidx.speed_x < 3.5)
//                pidx.speed_x = -3.5;
//            //------------y速度计算-------------
//            pidy.last_y = pidy.err_y;
//            pidy.err_y = pidy.exp_y - pidy.now_y;
//            pidy.sum_y += pidy.err_y;
//            pidy.speed_y = KP * pidy.err_y + KI * pidy.sum_y + KD * (pidy.last_y - pidy.err_y);
//            if (pidy.speed_y > 3.5)
//                pidy.speed_y = 3.5;
//            else if (pidy.speed_y < 3.5)
//                pidy.speed_y = -3.5;
//            //限制速度，防止超过机器人速度上限
//            //if (pidy.speed_y > 2.0)
//            //    pidy.speed_y = 2.0;
//            //else if (pidy.speed_y < 2.0)
//            //    pidy.speed_y = -2.0;
//            //x_speed_string = Convert.ToString(pidx.speed_x * 500);
//            //y_speed_string = Convert.ToString(pidy.speed_y * 500);

//            //message2ToServer = "chassis wheel x " + x_speed_string + " y " + y_speed_string + ";";
//            //tcpClientRobot.Send(Encoding.UTF8.GetBytes(message2ToServer));   //向服务器端发送消息
//            //UnityEngine.Debug.Log("向服务器端发送消息：" + message2ToServer);//

//            x_speed_string = Convert.ToString(pidx.speed_x);
//            y_speed_string = Convert.ToString(pidy.speed_y);
//            message2ToServer = "chassis speed x " + x_speed_string + " y " + y_speed_string + ";";
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes(message2ToServer));   //向服务器端发送消息
//            //UnityEngine.Debug.Log("向服务器端发送消息：" + message2ToServer);//

//            byte[] data_2 = new byte[1000];
//            int length_2 = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
//            string message_2 = Encoding.UTF8.GetString(data_2, 0, length_2);//把字节数组转化为字符串
//            //UnityEngine.Debug.Log("接收到服务器端的消息：" + message_2);

//            pidx.now_x = Tracker.position.z - SaveVec[0];  //调用小车x轴方向上距起始点的位置
//            pidy.now_y = Tracker.position.x - SaveVec[1];   //调用小车y轴方向上距起始点的位置
//            UnityEngine.Debug.Log("now_x =" + pidx.now_x);
//            UnityEngine.Debug.Log("now_y =" + pidy.now_y);

//        }
//        //UnityEngine.Debug.Log("退出循环");
//        message_zero = "chassis wheel w2 0 w1 0 w3 0 w4 0 ;";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(message_zero));   //向服务器端发送停止指令
//        //UnityEngine.Debug.Log("向服务器端发送消息：" + message_zero);//

//        byte[] data_zero = new byte[1000];
//        int length_zero = tcpClientRobot.Receive(data_zero);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
//        string message_zero_1 = Encoding.UTF8.GetString(data_zero, 0, length_zero);//把字节数组转化为字符串
//        //UnityEngine.Debug.Log("接收到服务器端的消息：" + message_zero_1);

//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
//        UnityEngine.Debug.Log("退出");
//        byte[] data_quit = new byte[1000];
//        int length_quit = tcpClientRobot.Receive(data_quit);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
//        string message_quit_1 = Encoding.UTF8.GetString(data_quit, 0, length_quit);//把字节数组转化为字符串
//        //UnityEngine.Debug.Log("接收到服务器端的消息：" + message_quit_1);
//        //isend = true;
//    }


//    private float GetRobotRotate(Transform self, Vector3 selfPos, Vector3 targetPos)
//    {
//        Vector3 dir = new Vector3(targetPos.x, 0, targetPos.z) - new Vector3(selfPos.x, 0, selfPos.z);//需要行走的距离
//        float angle = Vector3.Angle(dir, self.forward);//需要旋转的角度
//        return angle;
//    }

//    /// <summary>
//    /// 判断一个点在自身的哪一侧
//    /// </summary>
//    /// <param name="self"></param>
//    /// <param name="target"></param>
//    /// <returns></returns>
//    private int GetRotationLeftOrRight(Transform self, Transform target)
//    {
//        float flag = Vector3.Dot(self.right, target.position - self.position);//返回值为正时,目标在自己的右方,反之在左方
//        return flag <= 0 ? 1 : -1;
//    }
//}
