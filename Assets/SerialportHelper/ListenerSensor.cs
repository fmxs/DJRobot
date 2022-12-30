using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Assets.Scripts;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;
/// <summary>
/// 接收传感器的数据
/// </summary>
public class ListenerSensor : MonoBehaviour
{
    //zxl修改
    public static float ZXlrot;
    static float rot = 0, tm = 0, quan = 0;
    public static int isTouched, C2H6O = 0;

    public static SerialPort serialPort = null;
    static bool bChanged = false;
    private static bool stop = false;
    private static bool Listening = false;
    private static bool JustOpen = true;
    private static int INSTRUCTION_LEN = 6;
    private static List<byte> liststr;//在ListByte中读取数据，用于做数据处理
    private static List<byte> ListByte;//存放读取的串口数据
    private static Thread tPort;
    private static bool isStartThread = false;//控制FixedUpdate里面的两个线程是否调用（当准备调用串口的Close方法时设置为false）

    private void Start()
    {

    }

    void Awake()
    {
        ProfileSensor.LoadProfile();
        StartSerial();
    }

    void OnDisable()
    {
        Debug.Log("关闭线程");
        CloseSerial();
    }

    static void StartSerial()
    {
        ListByte = new List<byte>();
        isStartThread = true;
        OpenSerialPort();
        tPort = new Thread(ReceiveData);
        tPort.Priority = ThreadPriority.BelowNormal;
        tPort.Start();
    }

    static void ReceiveData()
    {
        while (tPort.IsAlive && !stop)
        {
            try
            {
                Byte[] buf = new Byte[1];
                int nRead = 0;
                if (serialPort.IsOpen)
                {
                    if (JustOpen)
                    {
                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();
                        ListByte.Clear();
                        JustOpen = false;
                    }
                    Listening = true;
                    nRead = serialPort.Read(buf, 0, 1);
                }
                if (nRead == 0)
                {
                    continue;
                }
                //if (buf[0] != 0x00) //如果是空格 就不加入到数组中
                if(true)
                {
                    ListByte.Add(buf[0]);//将缓冲区的字符加入字节数组中 //帧尾：FDFD

                    if (ListByte.Count >= INSTRUCTION_LEN)
                    {
                        if(ListByte[0] == 0xFF && ListByte[1] == 0xFF && ListByte[ListByte.Count - 1] == 0xFD && ListByte[ListByte.Count - 2] == 0xFD)//如果去掉listbyte[0]或者listbyte[1]的判断 就会接收不到7位的酒精数据 未知其因 2020.9.21
                        {
                            Decode(ListByte.ToArray(), ListByte.Count);
                            ListByte.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
            finally
            {
                Listening = false;
            }
            Thread.Sleep(1);
        }
    }

    static void CloseSerial()//该方法为关闭串口的方法，当程序退出或是离开该页面或是想停止串口时调用。
    {
        stop = true;
        if (null != serialPort)
        {
            if (serialPort.IsOpen)
            {
                while (Listening)
                {
                }
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.Close();
                ListByte.Clear();
            }
            Debug.Log("关闭串口");
        }
        try
        {
            tPort.Abort();
            tPort.Join();
            isStartThread = false;//停止掉FixedUpdate里面的两个线程的调用
        }
        catch {; }
    }

    static bool OpenSerialPort()
    {
        try
        {
            //string PortName = "\\\\?\\" + ProfileSensor.G_PORTNAME;//[1]COM10及以上的串口 用这行语句
            string PortName = ProfileSensor.G_PORTNAME;//[2]COM10以下的串口 用这行语句
            Int32 iBaudRate = Convert.ToInt32(ProfileSensor.G_BAUDRATE);
            Int32 iDateBits = Convert.ToInt32(ProfileSensor.G_DATABITS);
            StopBits stopBits = StopBits.One;
            switch (ProfileSensor.G_STOP)            //停止位
            {
                case "1":
                    stopBits = StopBits.One;
                    break;
                case "1.5":
                    stopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    stopBits = StopBits.Two;
                    break;
                default:
                    Debug.Log("Error：参数不正确!");
                    return false;
            }
            Parity parity = Parity.None;
            switch (ProfileSensor.G_PARITY)
            {
                case "NONE":
                    parity = Parity.None;
                    break;
                case "ODD":
                    parity = Parity.Odd;
                    break;
                case "EVEN":
                    parity = Parity.Even;
                    break;
                case "MARK":
                    parity = Parity.Mark;
                    break;
                case "SPACE":
                    parity = Parity.Space;
                    break;
                default:
                    Debug.Log("Error：参数不正确!");
                    return false;
            }
            serialPort = new SerialPort(PortName, iBaudRate, parity, iDateBits, stopBits);
            //准备就绪              
            serialPort.DtrEnable = true;
            serialPort.RtsEnable = true;
            //设置数据读取超时为1秒
            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            Listening = false;
            JustOpen = true;
            stop = false;
            ListByte.Clear();
            serialPort.Open();
            Debug.Log("串口打开成功");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log("串口打开失败" + ex.ToString());
            return false;
        }
    }

    static bool ShiftToValidCode(byte[] bytes, ref int offset, ref int count)
    {
        int nStart = offset;
        while (count > 0 && bytes[offset] != 0x11)
        {
            offset++;
            count--;
        }
        return true;
    }

    static byte GetChecksum(byte[] data, int start, int len)
    {
        int num = 0;
        for (int i = start; i < start + len; i++)
        {
            num = (num + data[i]) % 0xffff;
        }
        return (byte)num;
    }

    static bool Checksum(byte[] data, int start, int len, int result)
    {
        byte num = GetChecksum(data, start, len);

        if (num == data[result])
            return true;
        else
            return false;
    }

    static Int32 GetInt32(byte[] bytes, int offset)
    {
        byte[] bys = new byte[4];
        Array.Copy(bytes, offset, bys, 0, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bys);
        return BitConverter.ToInt32(bys, 0);
    }

    static Int16 GetInt16(byte[] bytes, int offset)
    {
        byte[] bys = new byte[2];
        Array.Copy(bytes, offset, bys, 0, 2);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bys);
        return BitConverter.ToInt16(bys, 0);
    }

    /// <summary>
    /// 解析数据
    /// </summary>
    /// <param name="bytes">接收到的数据</param>
    /// <returns></returns>
    static bool Decode(byte[] bytes, int count)
    {
        //酒精传感器的标识为01 //下标是3和4的时候为数据 一共2个字节 //数据一共为7个字节
        if (bytes[2] == 0x01)
        {
            //Int32 Wine = bytes[3] * 255 + bytes[4];
            C2H6O = bytes[3] * 256 + bytes[4];
            //C2H6O = Wine;
            Debug.Log("[Listener_Sensor]酒精:" + C2H6O);

            return true;
        }
        //触摸传感器的标识02 //下标是3的时候为数据 //数据一共为6个字节
        else if (bytes[2] == 0x02 && count == 6) 
        {
            if (bytes[3] == 0x11)
                isTouched = 1;
            else
                isTouched = 0;
            Debug.Log("[Listener_Sensor]触摸:" + isTouched);
            return true;
        }
        return false;
    }
}
