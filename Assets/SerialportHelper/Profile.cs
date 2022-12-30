using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    class Profile
    {
        private static IniFile _file;//内置了一个对象

        public static string G_PORTNAME = "COM1";//给ini文件赋新值，并且影响界面下拉框的显示
        public static string G_BAUDRATE = "1200";//给ini文件赋新值，并且影响界面下拉框的显示
        public static string G_DATABITS = "8";
        public static string G_STOP = "1";
        public static string G_PARITY = "NONE";
        public static string G_INTERVAL = "25";
        public static string G_ETRACKERID = "0";

        public static void LoadProfile()//从文件中读取配置信息并赋给声明的静态变量
        {
            string strPath = Application.dataPath;
            _file = new IniFile(Application.streamingAssetsPath + "\\SerialPort.ini");
            Debug.Log(_file.FileName);
            G_BAUDRATE = _file.ReadString("CONFIG", "BaudRate", "115200");    //读数据，下同
            G_DATABITS = _file.ReadString("CONFIG", "DataBits", "8");
            G_STOP = _file.ReadString("CONFIG", "StopBits", "1");
            G_PARITY = _file.ReadString("CONFIG", "Parity", "NONE");
            G_PORTNAME = _file.ReadString("CONFIG", "PortName", "COM7");// 这里改一下即可
            G_INTERVAL = _file.ReadString("CONFIG", "Interval", "25");
            G_ETRACKERID = _file.ReadString("CONFIG", "ExtinguisherTrackerID", "3");
        }

        public static void SaveProfile()
        {
            string strPath = Application.streamingAssetsPath + "\\";
            _file = new IniFile(strPath + "SerialPort.ini");
            _file.WriteString("CONFIG", "BaudRate", G_BAUDRATE);            //写数据，下同
            _file.WriteString("CONFIG", "DataBits", G_DATABITS);
            _file.WriteString("CONFIG", "StopBits", G_STOP);
            _file.WriteString("CONFIG", "Parity", G_PARITY);
            _file.WriteString("CONFIG", "PortName", G_PORTNAME);
            _file.WriteString("CONFIG", "Interval", G_INTERVAL);
            _file.WriteString("CONFIG", "ExtinguisherTrackerID", G_ETRACKERID);
            
        }

        
    }
}
