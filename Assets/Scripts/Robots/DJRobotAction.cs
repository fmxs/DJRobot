using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.IO.Ports;

public class DJRobotAction 
{
    private byte serialHead = 0xFF, serialTail = 0xFD;//[1]帧头    [6]帧尾      
    private static int DataLen = 2, MODE = 1;//[2]数据长度  [3]数据类型-运动

    public static String SetDogForward(SerialPort port, double dist)
    {
        double stepNumber = 0;
        byte[] data = new byte[6];
        data[0] = 0xFF;//[1]帧头
        data[1] = 0x02;
        data[2] = 0x01;
        data[3] = 0x13;
        data[4] = (byte)stepNumber;
        data[5] = 0xFD;

        if (dist == 0)
        {
            stepNumber = 0;
            port.Write(data, 0, 6);
            return data.ToString();
        }


        double stepLength = 0.03;// 步长3cm
        // 如果机器狗实际走的过远，则需要增大StepLength的数值，从而使StepNumber减小
        stepNumber = dist / stepLength;

        if (stepNumber > 6)
        {
            stepNumber = 6;
        }
        data[4] = (byte)stepNumber;
        port.Write(data, 0, 6);//FF02011303FD

        return data.ToString();
    }
    public static String SetDogRotate(SerialPort port, double angle)
    {
        double rotateNumber = 0;
        int LeftRight = 20;
        byte[] data = new byte[6];
        data[0] = 0xFF;//[1]帧头
        data[1] = 0x02;
        data[2] = 0x01;
        data[3] = (byte)LeftRight;
        data[4] = (byte)rotateNumber;
        data[5] = 0xFD;

        double rotateAngle = 18.00;// 转弯5步为90度 实际上并没有这么精确
        rotateNumber = Math.Abs(angle) / rotateAngle;

        if (rotateNumber < 1 || angle == 0)
        {
            port.Write(data, 0, 6);
            return data.ToString();
        }

        if (angle > 0)
        {
            // angle为正 右转 右转是"04"
            LeftRight = 4;
        }
        else
        {
            // angle为负 左转 左转是"14"
            LeftRight = 20;
        }

        data[3] = (byte)LeftRight;
        data[4] = (byte)rotateNumber;
        port.Write(data, 0, 6);
        return data.ToString();

    }

    public static String SetRobotMove(Socket tcpClientRobot, double x_Distance, double y_Distance, double Angel, double dXYSpeed, double dZSpeed) /*机器人移动指令*/
    {
        string str_xDistance = x_Distance.ToString();
        string str_yDistance = y_Distance.ToString();
        string str_Angel = Angel.ToString();
        string strXYSpeed = dXYSpeed.ToString();
        string strZSpeed = dZSpeed.ToString();
        string messageaToServer = "chassis move x " + str_xDistance + " y " + str_yDistance + " z " + str_Angel + " vxy " + strXYSpeed + " vz " + strZSpeed + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";//把字节数组转化为字符串
        }
    }

    public static String SetRobotLateral(Socket tcpClientRobot, double y_Distance, double dSpeed) /*机器人左右侧向移动*/
    {
        string str_yDistance = y_Distance.ToString();
        string strSpeed = dSpeed.ToString();
        string messageaToServer = "chassis move x 0 y " + str_yDistance + " vxy " + strSpeed + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";//把字节数组转化为字符串
        }
    }

    public static String SetRobotForward(Socket tcpClientRobot, double x_Distance, double dSpeed) /*机器人前后正向移动 */
    {
        string str_xDistance = x_Distance.ToString();
        string strSpeed = dSpeed.ToString();
        string messageaToServer = "chassis move x " + str_xDistance + " y 0 vxy " + strSpeed + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";//把字节数组转化为字符串
        }
    }

    public static String SetRobotRotate(Socket tcpClientRobot, double angel, double dSpeed) /*机器人旋转*/
    {
        string str_Angel = angel.ToString();
        string strSpeed = dSpeed.ToString();
        string messageaToServer = "chassis move z " + str_Angel + " vz " + strSpeed + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";//把字节数组转化为字符串
        }
    }

    public static string SetRobotStop(Socket tcpClientRobot) /*机器人停止*/
    {
        string messageaToServer = "chassis wheel w2 0 w1 0 w3 0 w4 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";//把字节数组转化为字符串
        }
    }

    public static String StartRobotSDKMode(Socket tcpClientRobot)
    {
        /*   机器人进入sdk模式*/
        string messageaToServer = "command;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {

            return "Read timeout;";//把字节数组转化为字符串
        }


    }
    public static String StopRobotSDKMode(Socket tcpClientRobot)/*结束SDK模式*/
    {
        string messageaToServer = "quit;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Rread timeout;";
        }


    }
    public static String GetMode(Socket tcpClientRobot)/*        机器人运动模式获取*/
    {

        string messageaToServer = "robot mode ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }
    }

    public static String GetRobotBattery(Socket tcpClientRobot) /*    机器人剩余电量获取*/
    {

        string messageaToServer = "robot battery ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }


    }
    public static String RobotArmResets(Socket tcpClientRobot)/*机械臂位置回中*/
    {

        string messageaToServer = "robotic_arm recenter;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }

    public static String SetRobotArmForward_Back(Socket tcpClientRobot, double x_Distance)/*设置机械臂向前后，单位厘米*/
    {

        string messageaToServer = "robotic_arm move x " + x_Distance + " y 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String SetRobotArmUp_Down(Socket tcpClientRobot, double y_Distance)/*设置机械臂向上下 单位厘米*/
    {

        string messageaToServer = "robotic_arm move x " + y_Distance + " y 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String GetRobotArmPosition(Socket tcpClientRobot)/*获取机械臂绝对位置 单位厘米*/
    {

        string messageaToServer = "robotic_arm position ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    //robotic_gripper open

    public static String SetRobotGripperOpen(Socket tcpClientRobot, int level_num)/*设置机械爪打开  level_num力度等级*/
    {

        string messageaToServer = "robotic_gripper open " + level_num + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String SetRobotGripperClose(Socket tcpClientRobot, int level_num)/*设置机械爪关闭  level_num力度等级*/
    {

        string messageaToServer = "robotic_gripper close " + level_num + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String SetRobotGripperStatus(Socket tcpClientRobot)/*获取机械爪状态  返回值：0 机械爪完全闭合
1 机械爪既没有完全闭合，也没有完全张开
2 机械爪完全张开*/
    {

        string messageaToServer = "robotic_gripper status ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
            return Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串
        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }
    }
}
