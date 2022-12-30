using System.Text;
using System.Collections;
using UnityEngine;

public class SendHEXData : MonoBehaviour
{
    private byte serialHead = 0xFF, serialTail = 0xFD;//[1]帧头    [6]帧尾      
    private static int  DataLen = 2, MODE = 1;//[2]数据长度  [3]数据类型-运动
    private int count = 0, n=0;


    void Start()
    {
        InvokeRepeating("DataSend", 1f, 3f);
    }

    private void DataSend()
    {
        if (count != 3)
            StartCoroutine(Timer(0.2f));
        else
            if(ListenerSensor.isTouched == 1)
                StartCoroutine(Timer(0.2f));
    }

    /// <summary>
    /// 发送数据后延时一段时间
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    IEnumerator Timer(float time)
    {
        yield return new WaitForSeconds(time);
        byte[] data = new byte[7];
        data[0] = 0xFF;//[1]帧头
        data[1] = (byte)DataLen;
        data[2] = (byte)MODE;
        data[3] = (byte)DataSet();
        data[4] = (byte)CalFootStep();
        data[5] = 0xFD;

        Listener.serialPort.Write(data, 0, 6);//FF02011303FD
        count++;
    }
    #region 数据的计算
    /// <summary>
    /// [5]运动模式的设定
    /// 0x13小跑前进 0x03小跑后退 0x14左转弯 0x04右转弯
    /// </summary>
    /// <returns></returns>
    /// 这里需要根据点与点的相对位置来确定旋转的方向与前进步数
    private int DataSet()
    {
        if (count != 3)
        {
            return 19;//19D = 13H
        }
        else
        {
            if (ListenerSensor.isTouched == 1)
            {
                return 20;//左转  14H = 20D
            }
            return 0;
        }
    }

    /// <summary>
    /// [6]计算要前进的步数或旋转步数
    /// </summary>
    /// <returns></returns>
    private int CalFootStep()
    {    
        if (count != 3)
            return 6;//03D = 03H
        else
        {
            if (ListenerSensor.isTouched == 1)
            {
                Debug.Log("rotation");
                return 7;//左转  
            }
            return 0;
        }
    }
    #endregion

}
