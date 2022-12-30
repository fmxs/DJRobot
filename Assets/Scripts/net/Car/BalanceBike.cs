using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Ports;
using UnityEngine;
using System.Collections;

namespace BalanceBike
{


    public class BalanceBike : MonoBehaviour
{
        private static byte[] buff = new byte[] { 0xff, 0xfe, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00,0x00, 0x00 };

        private static string Speed_level = "Middle";
        private static double line_speed = 10.22; //默认线速度，中速，10.22厘米每秒
        private static double angle_speed = 19.25; //默认角速度，中速下，19.25度每秒；
      

        private static void setSynchronousSpeed()/*同步速度*/
        {

            /*
             * 该方法功能为了同步小车的实际速度与速度值
             * 使用者不必考虑
             * 
             */

            if (Speed_level == "Hight")
            {
                buff[2] = buff[3] = 0x18;
                line_speed = 13.84;
                angle_speed = 25.46;


            }
            else if (Speed_level == "Lower")
            {
                buff[2] = buff[3] = 0x0C;
                line_speed = 6.94;    
                angle_speed = 12.99;
            }
            else //中等-默认  Middle
            {
                buff[2] = buff[3] = 0x12;
                line_speed = 10.22;
                angle_speed = 19.25;
            }
        }
        public static void setSpeed_level(String level) /*Hight=0x18,Middle=0x12,Lower=0x0C*/
        {

            /*
             * 设置速度水平
             * 字符串类型，该设置对于整个类有效
             * 默认为中等速度：Middle；
             * 低速模式：Lower
             * 高速模式：Hight
             * 
            */
            if (level != "Hight" && level != "Lower"&& level != "Middle")
            
            {
              /*  System.Diagnostics.Debug.WriteLine("设置了：Middle");*/
                Speed_level = "Middle";
            }
            else
            {
                /*System.Diagnostics.Debug.WriteLine("设置了："+level);*/
                Speed_level = level;
            }
          
        }

        public static float MoveToBeforeandAfter(double x_Diance, SerialPort Balanceport)/*控制小车前后距离，单位为CM*/
        {
            /*
             * 需提供SerialPort对象参数；
             * 正值表示前进
             * 负值表示后退
             * 调用该方法会阻塞，阻塞时间取决于x_Diance数值
             * 最大参数值300CM，超过不报错
             */
            setSynchronousSpeed();
            if (x_Diance<0)
             {
                buff[4] = 0x00;
                buff[5] = 0x00;//后退
                x_Diance = -1.0 * x_Diance;
            }
            else
            {
                buff[4] = 0x01;
                buff[5] = 0x01;//前进
            }
            Balanceport.Write(buff, 0, 10);
            Balanceport.Write(buff, 0, 10);

            if (x_Diance > 300)
                x_Diance = 300;
            return ((float)(x_Diance / line_speed));
            Debug.Log("开始停止");
            //Thread.Sleep((int)((x_Diance/ line_speed) * 1000));
            Debug.Log("结束停止");
            //CarRobotControl.NowCarRobotStatus = CarRobotControl.CarRobotStatus.Idle;
            ///*System.Diagnostics.Debug.WriteLine("x_Diance:"+ x_Diance + " line_speed:"+ line_speed + " (x_Diance/ line_speed):"+ (x_Diance / line_speed));
            //System.Diagnostics.Debug.WriteLine("前进睡眠时间："+ (int)((x_Diance / line_speed) * 1000));*/
            //buff[2] = buff[3] = 0x00;
            //Balanceport.Write(buff, 0, 10);
            //Balanceport.Write(buff, 0, 10);//停止前进


        }

        public static float RotateInplace(double angle, SerialPort Balanceport)/*控制小车原地旋转，单位为度  》0表示右转 《0左转*/
        {
            /*
             * 需提供SerialPort对象参数；
             * 单位为度，大于0表示右转  小于0表示左转
             * 调用该方法会阻塞，阻塞时间取决于angle数值
             * 最大720度，超过该值不报错；
             */
            setSynchronousSpeed();
           
            if (angle < 0)
            {
                buff[4] = 0x00;
                buff[5] = 0x01;//左转
                angle = -1.0*angle;
            }
            else
            {
                buff[4] = 0x01;
                buff[5] = 0x00;//右转
            }

            Balanceport.Write(buff, 0, 10);
            Balanceport.Write(buff, 0, 10);

            if (angle > 720)
                angle = 720;
            ////////////////////
            //Thread.Sleep((int)((angle / angle_speed) * 1000));
            return ((float)(angle / angle_speed));
            /* System.Diagnostics.Debug.WriteLine("旋转睡眠时间：" + (int)((angle / angle_speed) * 1000));*/
            ///////////////////
            //CarRobotControl.NowCarRobotStatus=CarRobotControl.CarRobotStatus.Idle;
            //buff[2] = buff[3] = 0x00;
            //Balanceport.Write(buff, 0, 10);//停止旋转
            //Balanceport.Write(buff, 0, 10);//停止旋转
        }


        public static void LowerSpeedStop(SerialPort Balanceport)
        {
            //CarRobotControl.NowCarRobotStatus = CarRobotControl.CarRobotStatus.Idle;
            buff[2] = buff[3] = 0x02;
            Balanceport.Write(buff, 0, 10);//停止旋转
            Balanceport.Write(buff, 0, 10);//停止旋转
        }
        public static void Stop(SerialPort Balanceport)
        {
            //CarRobotControl.NowCarRobotStatus = CarRobotControl.CarRobotStatus.Idle;
            buff[2] = buff[3] = 0x00;
            Balanceport.Write(buff, 0, 10);//停止旋转
            Balanceport.Write(buff, 0, 10);//停止旋转
        }
    }
}
