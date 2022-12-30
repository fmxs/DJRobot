using UnityEngine;
using System.Collections;
using Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Policy;
using UnityEngine.UI;

public class UnityServer : MonoBehaviour
{

    public enum Pattern
    {
        Idle,
        Task
    }

    #region 控制模式相关
    //Robot模式状态相关
    public Pattern RobotNowPattern = Pattern.Idle;
    //DogRobot模式相关
    public Pattern DogRobotNowPattern = Pattern.Idle;
    //CarRobot模式相关
    public Pattern CarRobotNowPattern = Pattern.Idle;
    //RobotClaw NowRobotClaw = RobotClaw.Idle;
    //Robot任务限制
    private bool isFristInto = true;
    ////测试任务开启
    //public bool isRobotTask = false, isDogTask = false, isCarTask = false;
    //public bool isClawWorking = false,isClawEnd = false; 
    public static bool RobotShouldWork;

    #endregion

    #region 跨脚本引用
    //跨脚本
    public RobotControl robotcontrol;
    private Hololens hololens;
    private DogRobotControl dogrobotcontrol;
    private CarRobotControl carrobotcontrol;
    #endregion

    #region Web端
    //Tracker们的位置
    public Transform[] trackerposition;
    //传输判断使用的计数器
    private int Tasktime = 0;
    //服务器接收机器人状态 1 Start 3 END
    private int Robotstatus;
    //模式状态ID
    //private string RobotId="0";
    public string RobotId = "0", RobotTaskstart = "0", DogRobotTaskstart = "0", CarRobotTaskstart = "0";
    //电量获取
    public string Electricityquantity = "100";
    //灯泡开关控制
    public string LightStatus="0";
    private Text text;
    //Web端服务器地址
    public static string[] url =
        { "http://47.93.213.106:9000/unity/experId",
          "http://47.93.213.106:9000/unity/robotData",
          "http://47.93.213.106:9000/unity/personData",
          "http://47.93.213.106:9000/unity/trackerData",
          "http://47.93.213.106:9000/unity/clawData",
          "http://47.93.213.106:9000/unity/setElectricity"

           };
    //登录界面参数
    private string[] Imagineurl = {
        "http://121.37.133.191:9000/unity/login"
        ,"http://121.37.133.191:9000/unity/experId"
        ,"http://121.37.133.191:9000/unity/getUserExperienceAnalyse"

    };
    public string m_info = "Nothing";
    private string Username;
    private string Password;
    //Model评级
    private string Level = "";
    //Model得分
    private int Score = 100;
    //Model扣分项
    private string ModelData="";
    #endregion

    //zpp 2020.11.27  动态刷新寻路width、depth、nodesize限制标志位
    private bool isNewAstarMap = true;

    //zpp 2020.12.3 地图3个机器人扫描控制队列
    public Queue queue=new Queue();

    //zpp 修改物体标签使用
    public GameObject[] RobotsObject;
    void Start()
    {
        robotcontrol = GameObject.Find("Robot").GetComponent<RobotControl>();
        hololens = GameObject.Find("Server").GetComponent<Hololens>();
        //dogrobotcontrol = GameObject.Find("DogRobot").GetComponent<DogRobotControl>();
        //carrobotcontrol = GameObject.Find("CarRobot").GetComponent<CarRobotControl>();
        text = GameObject.Find("Text").GetComponent<Text>();
        //InvokeRepeating("TaskControl", 2f, 2f);
        //InvokeRepeating("SendElectricity", 10f, 10f);
    }

    //寻路结束;  

    #region Update
    // Update is called once per frame  
    void Update()//模式状态切换
    {
        RobotSwitch();

        FLPLightControl();
    }

    void LateUpdate()
    { 

        RobotRscanControl();
    
    }
    #endregion


    #region Web端通信 暂无使用
    //IEnumerator RobotWeb()
    //{
    //    WWWForm form = new WWWForm();
    //    form.AddField("status", Robotstatus);
    //    form.AddField("robotX", trackerposition[0].position.x.ToString());
    //    form.AddField("robotY", trackerposition[0].position.y.ToString());
    //    form.AddField("robotZ", trackerposition[0].position.z.ToString());
    //    WWW getData = new WWW(url[1], form);
    //    yield return getData;
    //    if (getData.error != null)
    //    {
    //        //ShowText.text = getData.error;
    //    }
    //    else
    //    {
    //        //ShowText.text = getData.text;
    //    }

    //} //大疆机器人
    //IEnumerator PlayerWeb()
    //{
    //    WWWForm form = new WWWForm();
    //    form.AddField("personX", trackerposition[1].position.x.ToString());
    //    form.AddField("personY", trackerposition[1].position.y.ToString());
    //    form.AddField("personZ", trackerposition[1].position.z.ToString());
    //    WWW getData = new WWW(url[2], form);
    //    yield return getData;
    //    if (getData.error != null)
    //    {
    //        //ShowText.text = getData.error;
    //    }
    //    else
    //    {
    //        //ShowText.text = getData.text;
    //    }
    //} //用户
    //IEnumerator TrackerWeb(Transform tracker, int i)
    //{
    //    //1号tracker
    //    WWWForm form = new WWWForm();
    //    form.AddField("trackerId", i.ToString());
    //    form.AddField("trackerX", tracker.position.x.ToString());
    //    form.AddField("trackerY", tracker.position.y.ToString());
    //    form.AddField("trackerZ", tracker.position.z.ToString());
    //    WWW getData = new WWW(url[3], form);
    //    yield return getData;
    //    if (getData.error != null)
    //    {
    //        //ShowText.text = getData.error;
    //    }
    //    else
    //    {
    //        //ShowText.text = getData.text;
    //    }
    //} //tracker
    //IEnumerator ClawWeb(int claw)
    //{
    //    WWWForm form = new WWWForm();
    //    form.AddField("status", claw);
    //    WWW getData = new WWW(url[4], form);
    //    yield return getData;
    //    if (getData.error != null)
    //    {
    //        //ShowText.text = getData.error;
    //    }
    //    else
    //    {
    //        //ShowText.text = getData.text;
    //    }
    //} //爪子
    //IEnumerator TaskIDSend()  //任务ID
    //{
    //    WWWForm form = new WWWForm();
    //    form.AddField("experId", RobotId.ToString());
    //    WWW getData = new WWW(url[0], form);
    //    yield return getData;
    //    if (getData.error != null)
    //    {
    //        //ShowText.text = getData.error;
    //    }
    //    else
    //    {
    //        //ShowText.text = getData.text;
    //    }
    //}
    //void SendElectricity()
    //{
    //    StartCoroutine(setElectricity());
    //}
    //IEnumerator setElectricity()  //发送电量
    //{
    //    string temp2 = robotcontrol.Electricity();//获取电量
    //    if (temp2 != "")
    //        Electricityquantity = temp2;
    //    //1号tracker
    //    WWWForm form = new WWWForm();
    //    form.AddField("electricity", Electricityquantity);
    //    WWW getData = new WWW(url[5], form);
    //    yield return getData;
    //    if (getData.error != null)
    //    {
    //        //ShowText.text = getData.error;
    //    }
    //    else
    //    {
    //        //ShowText.text = getData.text;
    //    }
    //}

    //void web()
    //{
    //    StartCoroutine(RobotWeb());
    //    StartCoroutine(PlayerWeb());
    //    for (int i = 2; i < trackerposition.Length; i++)
    //    {
    //        StartCoroutine(TrackerWeb(trackerposition[i], i));
    //    }
    //}
    #endregion

    #region Web任务界面  暂无使用
    //void TaskControl()
    //{
    //    if (Tasktime == 0 && RobotNowPattern == Pattern.Task)
    //    {
    //        Tasktime = 1;
    //        Robotstatus = 1;
    //        StartCoroutine(TaskIDSend());
    //        web();
    //    }
    //    else if (RobotNowPattern == Pattern.Task)
    //    {
    //        web();
    //    }
    //    else if (Tasktime > 0 && RobotNowPattern != Pattern.Task)
    //    {
    //        Robotstatus = 3;
    //        StartCoroutine(RobotWeb());
    //        Tasktime = 0;
    //    }

    //}
    #endregion

    #region Hololens2参数回传
    public void TaskMessage(string RobotId,string Taskstart) //? 反正控制不给力用它传参数就对了
    {
        this.RobotId = RobotId;
        if (RobotId == "0")
            RobotTaskstart = Taskstart;
        else if (RobotId == "1")
            DogRobotTaskstart = Taskstart;
        else if (RobotId == "2")
            CarRobotTaskstart = Taskstart;
    }
    public void LightControl(string Lightstatus)
    {
        this.LightStatus = Lightstatus;
    }
    #endregion

    #region A*算法地图设置刷新
    
    /// <summary>
	/// 修改A*算法地图参数
	/// </summary>
    void AStarNewMap(int width, int depth, float nodeSize)
    {
        AstarData data = AstarPath.active.data;
        //GridGraph gg = data.AddGraph(typeof(GridGraph)) as GridGraph;
        GridGraph gg=data.gridGraph;
        gg.SetDimensions(width,depth,nodeSize);
        //int width = 60;
        //int depth = 90;
        //float nodeSize = 0.2f;
        //gg.center = new Vector3(10, 0, 0);
        //// Updates internal size from the above values
        //gg.SetDimensions(width, depth, nodeSize);
        // Scans all graphs
        AstarPath.active.Scan();
    }
    #endregion

    #region 动态修改物体的Layer
    ///<summary>
    ///动态修改物体的Layer
    /// </summary>
    private void toChangePlayerLayer(GameObject obj, int layerValue)
    {
        Transform[] transArray = obj.GetComponentsInChildren<Transform>();
        foreach (Transform trans in transArray)
        {
            
            trans.gameObject.layer = layerValue;
        }
    }
    #endregion

    #region  Robot寻路各自刷新控制
    /// <summary>
    /// 各个Robot控制刷新
    /// </summary>
    void RobotRscanControl()
    {
        while (queue.Count > 0)
        {
            //object temp = queue.Dequeue();
            string temp = Convert.ToString(queue.Dequeue());
            if (temp == "Robot" && RobotNowPattern==Pattern.Task)
            {
                AStarNewMap(40, 60, 0.17f);
                toChangePlayerLayer(RobotsObject[0], 0);
                toChangePlayerLayer(RobotsObject[1], 8);
                toChangePlayerLayer(RobotsObject[2], 8);
                AstarPath.active.Scan();
                robotcontrol.seeker.StartPath(robotcontrol.transRobot.position, new Vector3(robotcontrol.NowRobotGoal.position.x, 0, robotcontrol.NowRobotGoal.position.z), robotcontrol.OnPathComplete);
            }
            if (temp == "DogRobot" && DogRobotNowPattern == Pattern.Task)
            {
                AStarNewMap(40, 60, 0.2f);
                toChangePlayerLayer(RobotsObject[0], 8);
                toChangePlayerLayer(RobotsObject[1], 0);
                toChangePlayerLayer(RobotsObject[2], 8);
                AstarPath.active.Scan();
                dogrobotcontrol.seeker.StartPath(dogrobotcontrol.transRobot.position, new Vector3(dogrobotcontrol.NowRobotGoal.position.x, 0, dogrobotcontrol.NowRobotGoal.position.z), dogrobotcontrol.OnPathComplete);
            }
            if (temp == "CarRobot" && CarRobotNowPattern == Pattern.Task)
            {
                AStarNewMap(30, 30, 0.5f);
                toChangePlayerLayer(RobotsObject[0], 8);
                toChangePlayerLayer(RobotsObject[1], 8);
                toChangePlayerLayer(RobotsObject[2], 0);
                AstarPath.active.Scan();
                carrobotcontrol.seeker.StartPath(carrobotcontrol.transRobot.position, new Vector3(carrobotcontrol.NowRobotGoal.position.x, 0, carrobotcontrol.NowRobotGoal.position.z), carrobotcontrol.OnPathComplete);
            }
            else;
        }
    }

    #endregion

    #region 各个机器人开关控制位
    /// <summary>
    /// 各个机器人开关控制位
    /// </summary>
    void RobotSwitch()
    {
        if (RobotId == "0")
        {
            if (RobotTaskstart == "1")
            {
                if (RobotNowPattern == Pattern.Idle)
                {
                    RobotNowPattern = Pattern.Task;
                    robotcontrol.taskType = RobotControl.TaskType.TaskIdle;
                    robotcontrol.bStop = true;
                }
                if (RobotNowPattern == Pattern.Task && isFristInto)
                {
                    robotcontrol.bStop = false;
                    RobotControl.bRescanPath = true;
                    robotcontrol.taskType = RobotControl.TaskType.TaskFollow;
                    isFristInto = false;
                }
            }
            else
            {
                RobotNowPattern = Pattern.Idle;
                isFristInto = true;
            }
        }
        else if (RobotId == "1")
        {

            if (DogRobotTaskstart == "1")
                DogRobotNowPattern = Pattern.Task;
            else
                DogRobotNowPattern = Pattern.Idle;
        }
        else if (RobotId == "2")
        {
            if (CarRobotTaskstart == "1")
                CarRobotNowPattern = Pattern.Task;
            else
                CarRobotNowPattern = Pattern.Idle;
        }
        else
        { 
            RobotNowPattern = Pattern.Idle;
            DogRobotNowPattern = Pattern.Idle;
            CarRobotNowPattern = Pattern.Idle;
        }
    }
    #endregion

    #region 飞利浦LED灯泡控制
    /// <summary>
    /// 飞利浦LED灯泡控制
    /// </summary>
    void FLPLightControl()
    {
        if (LightStatus == "1")
        {
            if (text.text != "开灯")
                text.text = "开灯";
        }
        else
        {
            if (text.text != "关灯")
                text.text = "关灯";
        }
    }

    #endregion

    #region Web用户登录

    IEnumerator StartWeb()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", Username);
        form.AddField("password", Password);
        WWW getData = new WWW(Imagineurl[0], form);
        yield return getData;
        if (getData.error != null)
        {
            m_info = getData.error;
        }
        else
        {
            m_info = getData.text;
        }
        //hololens.SendTo_HololenSignMessage();
        hololens.UnitySend_Message("Sign" + m_info);
        StartCoroutine(Send_ExperId());
    }
    public void Hololens_User(string Username)//用户名、密码
    {
        this.Username = Username;
    }
    public void Hololens_PW(string password)//用户名、密码
    {
        this.Password = password;
        StartCoroutine(StartWeb());
    }

    #endregion

    #region Web实验id发送
    IEnumerator Send_ExperId()
    {
        WWWForm form = new WWWForm();
        form.AddField("experId", 1);
        WWW getData = new WWW(Imagineurl[1], form);
        yield return getData;
        if (getData.error != null)
        {
           // m_info = getData.error;
        }
        else
        {
           // m_info = getData.text;
        }
    }

    #endregion


    #region 发送健康相关数据
    public string Kinect_ModelScore(int Score)
    {
        this.Score = Score;
        if (Score >= 80)
            Level = "A";
        else if (Score >= 60)
            Level = "B";
        else
            Level = "C";
        return Level;
    }
    public void Kinect_ModelData(string ModelData)
    {
        this.ModelData = ModelData;
        StartCoroutine(Send_HealthyScore());
    }
    IEnumerator Send_HealthyScore()
    {
        WWWForm form = new WWWForm();
        form.AddField("Level", Level);  //String A,B,C
        form.AddField("Score", Score);  //int
        char[] temp = ModelData.ToCharArray();
        Debug.Log("temp"+temp.ToString());
        for (int i = 0; i < temp.Length; i++)
        {
            form.AddField("TestResult" + i, temp[i].ToString());  //string
        }
        WWW getData = new WWW(Imagineurl[2], form);
        yield return getData;
        if (getData.error != null)
        {
            m_info = getData.error;
        }
        else
        {
            m_info = getData.text;
        }
    }
    #endregion

}