using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;

public class Kinect : MonoBehaviour
{
    public int KinectStartStatus = 0;
    public int ModelMessageStatus = 0;
    public int SendToKinect = 0;
    public int ModelScore = 100;
    public string ModelData = "";
    private Hololens hololens;
    public Process Modelexe,Kinectexe;
    // Start is called before the first frame update
    void Start()
    {
        hololens = GameObject.Find("Server").GetComponent<Hololens>();
        Thread thread = new Thread(ModelExe);
        thread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (KinectStartStatus == 1)
        {
            OnClickKinect();
            KinectStartStatus = 0;
        }
        if (ModelMessageStatus == 1)
        {
            //hololens.SendModelMessage();
            hololens.UnitySend_Message("ModelStart");
            ModelMessageStatus = 0;
        }
        if (SendToKinect == 1)
        {
            //hololens.SendKinectMessage();
            hololens.UnitySend_Message("KinectStart");
            SendToKinect = 0;
        }
    }
    public void OnClickKinect()
    {
        Thread thread = new Thread(KinectExe);
        thread.Start();
    }
    void KinectExe()
    {
        Kinectexe = Process.Start("G:/Asserts/kinect(1)/kinect/x64/Debug/kinect.exe");
        Kinectexe.WaitForExit();
    }
    void ModelExe()
    {
        Modelexe = Process.Start("G:/Asserts/calling/py_socket/dist/client.exe");
    }

    private void OnDestroy()
    {
        Modelexe.Kill();                       //杀死所有的进程
        Modelexe.Dispose();               //释放所有的资源
        Modelexe.Close();                  //关闭exe程序
    }
}
