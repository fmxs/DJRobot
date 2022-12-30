using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.IO.Ports;

public class DJRobotAction 
{
    private byte serialHead = 0xFF, serialTail = 0xFD;//[1]֡ͷ    [6]֡β      
    private static int DataLen = 2, MODE = 1;//[2]���ݳ���  [3]��������-�˶�

    public static String SetDogForward(SerialPort port, double dist)
    {
        double stepNumber = 0;
        byte[] data = new byte[6];
        data[0] = 0xFF;//[1]֡ͷ
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


        double stepLength = 0.03;// ����3cm
        // ���������ʵ���ߵĹ�Զ������Ҫ����StepLength����ֵ���Ӷ�ʹStepNumber��С
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
        data[0] = 0xFF;//[1]֡ͷ
        data[1] = 0x02;
        data[2] = 0x01;
        data[3] = (byte)LeftRight;
        data[4] = (byte)rotateNumber;
        data[5] = 0xFD;

        double rotateAngle = 18.00;// ת��5��Ϊ90�� ʵ���ϲ�û����ô��ȷ
        rotateNumber = Math.Abs(angle) / rotateAngle;

        if (rotateNumber < 1 || angle == 0)
        {
            port.Write(data, 0, 6);
            return data.ToString();
        }

        if (angle > 0)
        {
            // angleΪ�� ��ת ��ת��"04"
            LeftRight = 4;
        }
        else
        {
            // angleΪ�� ��ת ��ת��"14"
            LeftRight = 20;
        }

        data[3] = (byte)LeftRight;
        data[4] = (byte)rotateNumber;
        port.Write(data, 0, 6);
        return data.ToString();

    }

    public static String SetRobotMove(Socket tcpClientRobot, double x_Distance, double y_Distance, double Angel, double dXYSpeed, double dZSpeed) /*�������ƶ�ָ��*/
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
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";//���ֽ�����ת��Ϊ�ַ���
        }
    }

    public static String SetRobotLateral(Socket tcpClientRobot, double y_Distance, double dSpeed) /*���������Ҳ����ƶ�*/
    {
        string str_yDistance = y_Distance.ToString();
        string strSpeed = dSpeed.ToString();
        string messageaToServer = "chassis move x 0 y " + str_yDistance + " vxy " + strSpeed + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";//���ֽ�����ת��Ϊ�ַ���
        }
    }

    public static String SetRobotForward(Socket tcpClientRobot, double x_Distance, double dSpeed) /*������ǰ�������ƶ� */
    {
        string str_xDistance = x_Distance.ToString();
        string strSpeed = dSpeed.ToString();
        string messageaToServer = "chassis move x " + str_xDistance + " y 0 vxy " + strSpeed + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";//���ֽ�����ת��Ϊ�ַ���
        }
    }

    public static String SetRobotRotate(Socket tcpClientRobot, double angel, double dSpeed) /*��������ת*/
    {
        string str_Angel = angel.ToString();
        string strSpeed = dSpeed.ToString();
        string messageaToServer = "chassis move z " + str_Angel + " vz " + strSpeed + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";//���ֽ�����ת��Ϊ�ַ���
        }
    }

    public static string SetRobotStop(Socket tcpClientRobot) /*������ֹͣ*/
    {
        string messageaToServer = "chassis wheel w2 0 w1 0 w3 0 w4 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";//���ֽ�����ת��Ϊ�ַ���
        }
    }

    public static String StartRobotSDKMode(Socket tcpClientRobot)
    {
        /*   �����˽���sdkģʽ*/
        string messageaToServer = "command;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {

            return "Read timeout;";//���ֽ�����ת��Ϊ�ַ���
        }


    }
    public static String StopRobotSDKMode(Socket tcpClientRobot)/*����SDKģʽ*/
    {
        string messageaToServer = "quit;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Rread timeout;";
        }


    }
    public static String GetMode(Socket tcpClientRobot)/*        �������˶�ģʽ��ȡ*/
    {

        string messageaToServer = "robot mode ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }
    }

    public static String GetRobotBattery(Socket tcpClientRobot) /*    ������ʣ�������ȡ*/
    {

        string messageaToServer = "robot battery ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }


    }
    public static String RobotArmResets(Socket tcpClientRobot)/*��е��λ�û���*/
    {

        string messageaToServer = "robotic_arm recenter;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }

    public static String SetRobotArmForward_Back(Socket tcpClientRobot, double x_Distance)/*���û�е����ǰ�󣬵�λ����*/
    {

        string messageaToServer = "robotic_arm move x " + x_Distance + " y 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String SetRobotArmUp_Down(Socket tcpClientRobot, double y_Distance)/*���û�е�������� ��λ����*/
    {

        string messageaToServer = "robotic_arm move x " + y_Distance + " y 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String GetRobotArmPosition(Socket tcpClientRobot)/*��ȡ��е�۾���λ�� ��λ����*/
    {

        string messageaToServer = "robotic_arm position ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    //robotic_gripper open

    public static String SetRobotGripperOpen(Socket tcpClientRobot, int level_num)/*���û�еצ��  level_num���ȵȼ�*/
    {

        string messageaToServer = "robotic_gripper open " + level_num + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String SetRobotGripperClose(Socket tcpClientRobot, int level_num)/*���û�еצ�ر�  level_num���ȵȼ�*/
    {

        string messageaToServer = "robotic_gripper close " + level_num + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���

        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }

    }
    public static String SetRobotGripperStatus(Socket tcpClientRobot)/*��ȡ��еצ״̬  ����ֵ��0 ��еצ��ȫ�պ�
1 ��еצ��û����ȫ�պϣ�Ҳû����ȫ�ſ�
2 ��еצ��ȫ�ſ�*/
    {

        string messageaToServer = "robotic_gripper status ?;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));

        byte[] data = new byte[1024];
        try
        {
            int length = tcpClientRobot.Receive(data);//�����byte����������������,����ֵlength��ʾ���յ����ݳ���
            return Encoding.UTF8.GetString(data, 0, length);//���ֽ�����ת��Ϊ�ַ���
        }
        catch (SocketException e)
        {
            return "Read timeout;";
        }
    }
}
