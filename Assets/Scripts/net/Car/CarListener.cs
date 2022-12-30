using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
//using Assets.Scripts;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;
/// <summary>
/// 自行车工程中的listener 现用于平衡车的串口数据的收发
/// </summary>
/// 

namespace BalanceBike
{
    public class CarListener : MonoBehaviour
    {
        //zxl修改
        public static float ZXlrot;
        public static int isTouched, C2H6O = 0;
        public static bool isReceivedData = false;
        public static bool isDogRespond = false;
        public static SerialPort serialPort = null;

        private static bool stop = false;
        private static bool Listening = false;
        private static bool JustOpen = true;
        private static int INSTRUCTION_LEN = 1;//?
        private static List<byte> ListByte;//存放读取的串口数据
        private static Thread tPort;
        //private static bool isStartThread = false;//控制FixedUpdate里面的两个线程是否调用（当准备调用串口的Close方法时设置为false）

        //zpp 读取一个字节的串口数据
        public static Byte[] ByteData;
        //public static DogRobotControl dogrobotcontrol;
        public void Awake()
        {
            //Profile.LoadProfile();
            StartSerial();
        }
        private void Start()
        {
            //dogrobotcontrol = GameObject.Find("DogRobot").GetComponent<DogRobotControl>();
        }
        public bool getlisteningstate()
        {
            return Listening;
        }
        public void OnDisable()
        {
            //Debug.Log("关闭线程");
            byte[] buff = new byte[] { 0xff, 0xfe, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00 };
            serialPort.Write(buff,0,10);
            CloseSerial();
        }

        public SerialPort getserialPort()
        {
        //while(true)/*)等待串口打开*/
        //{
        //    if (Listening)

        //}
            return serialPort;

        }
        static void StartSerial()
        {
            ListByte = new List<byte>();
            //isStartThread = true;
            if (!OpenSerialPort()) return;
            tPort = new Thread(ReceiveData);
            tPort.Priority = ThreadPriority.BelowNormal;//设置更低的优先级
            tPort.Start();
        }

        static void ReceiveData()
        {
            while (tPort.IsAlive && !stop)//机器狗只会在向它发送数据后 才会回传一次数据
            {
                try
                {
                    Byte[] buf = new Byte[4];
                    ByteData = new Byte[4];
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
                    ByteData[0] = buf[0];
                    //ByteData = System.Text.Encoding.ASCII.GetBytes(buf.ToString());
                    if (ByteData[0] == 49 || ByteData[0] == 50)
                    {
                        //dogrobotcontrol.NowDogRobotStatus = DogRobotControl.DogRobotStatus.Idle;
                        ByteData[0] = 48;
                    }
                    isDogRespond = true;


                }
                catch (Exception ex)
                {
                    //Debug.Log(ex);
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
                //Debug.Log("关闭串口");
            }
            try
            {
                tPort.Abort();
                tPort.Join();
                //isStartThread = false;//停止掉FixedUpdate里面的两个线程的调用
            }
            catch {; }
        }

        static bool OpenSerialPort()
        {
            if (Listening)
                return true;
            try
            {
                //string PortName = "\\\\?\\" + Profile.G_PORTNAME; //>10
                string PortName = "COM6";//Profile.G_PORTNAME;//串口号
                Int32 iBaudRate = 115200;//Convert.ToInt32(Profile.G_BAUDRATE);//波特率
                Int32 iDateBits = 8;// Convert.ToInt32(Profile.G_DATABITS);//数据位
                StopBits stopBits = StopBits.One;//停止位1
                /* switch (Profile.G_STOP)            //停止位
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
                         //Debug.Log("Error：参数不正确!");
                         return false;
                 }*/
                Parity parity = Parity.None;//校验位
                /* switch (Profile.G_PARITY)
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
                         //Debug.Log("Error：参数不正确!");
                         return false;
                 }*/
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

        static bool Decode(byte[] bytes, ref int offset, ref int count)
        {
            for (int i = 0; i < 1; i++)//?
            {
                //Debug.Log(bytes[i]);
            }
            isDogRespond = true;
            return false;
        }
}


}
