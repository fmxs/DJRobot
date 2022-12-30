
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using Pathfinding;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using BalanceBike;

public class CarRobotControl : MonoBehaviour
{
    public enum TaskType
    {
        TaskIdle,
        TaskFollow,
        TaskSnatch,
        TaskMagnetic,
    }

    public enum RescanState
    {
        RescanIdle,
        Rescaning,
        RescanOK,
    }

    public enum AlongPathState
    {
        AlongPathIdle,
        AlongPathing,
        AlongPathOK,
    }

    public enum NextWaypointState
    {
        NextWaypointIdle,
        NextWaypointStart,
        NextWaypointing,
        NextWaypointOK,
        NextWaypointAbort,
    }
    //zpp 2020.11.10 目标切换
    public enum NowRobotGoalPathFinding
    {
        User,
        Target,
        Box
    }
    public enum CarRobotStatus
    {
        Idle,
        Walk,
        Rotation
    }

    #region  For debug

    //zpp 2020.11.9  unityserver 静态标志控制
    public static bool bRescanPath = false;
    //public static bool bAutoClawCatch = false;

    #endregion

    #region For Editor
    /// <summary>
    /// 在fixed update中更新物体的位置，nRecaculateSpan越大，更新计算越少
    /// </summary>
    [Header("更新物体的位置，nRecaculateSpan越大，更新频率越低")]
    public int nRecaculateSpan = 5;
    /// <summary>
    /// 距目标点的最小阈值，单位米
    /// </summary>
    [Header("距目标点的最小阈值，单位米")]
    public double dTargetTolerance = 0.6;
    /// <summary>
    /// 距下一路点的最小阈值，单位米
    /// </summary>
    [Header("距下一路点的最小阈值，单位米")]
    public double dNextwaypointTolerance = 0.1;
    public double dClawDistanceTolerance = 0.42;
    /// <summary>
    /// 目标角度最小阈值，单位度
    /// </summary>
    [Header("目标角度最小阈值，单位度")]
    public double dAheadangleTolerance = 1.0;
    /// <summary>
    /// 旋转指定角度仍未到达目标角度时，允许的额外旋转次数，单位次数
    /// </summary>
    [Header("旋转指定角度仍未到达目标角度时，允许的额外旋转次数，单位次数")]
    public int nRotateTimeExtra = 0;
    /// <summary>
    /// 判断是否停止的阈值，单位米
    /// </summary>
    [Header("判断是否停止的阈值，单位米")]
    public double dStayTolerance = 0.005;
    /// <summary>
    /// 在行进过程中距目标点应该越来越近，当出现 nMaxOverstepTime * 每秒更新物体次数 越来越远的情况时，重算路径
    /// </summary>
    [Header("在行进过程中距目标点应该越来越近，当出现 nMaxOverstepTime * 每秒更新物体次数 越来越远的情况时，重算路径")]
    public int nMaxOverstepTime = 2;
    /// <summary>
    /// 判断overstep的值，应接近于0，单位米
    /// </summary>
    [Header("判断overstep的值，应接近于0，单位米")]
    public double dMinValue4Overstep = 0;
    /// <summary>
    /// 在行进过程中机器人超过dMaxStaySpan秒未动时，重算路径
    /// </summary>
    [Header("在行进过程中机器人超过dMaxStaySpan秒未动时，重算路径")]
    public float fMaxStaySpan = 1.0f;
    /// <summary>
    /// 与机器人距离小于该距离的物体移动时，重算路径，单位米
    /// </summary>
    [Header("与机器人距离小于该距离的物体移动时，重算路径，单位米")]
    public double dInfluenceDistance = 0.8;
    /// <summary>
    /// 机器人一次性旋转时角速度
    /// </summary>
    [Header("机器人一次性旋转时角速度")]
    public double dRotateSpeedAtonce = 90;
    /// <summary>
    /// 机器人调整旋转时的角速度
    /// </summary>
    [Header("机器人调整旋转时的角速度")]
    public double dRotateSpeedModify = 360;
    /// <summary>
    /// 机器人同时旋转行走时的速度，这个值会根据距离的远近自动变动
    /// </summary>
    [Header("机器人同时旋转行走时的速度，这个值会根据距离的远近自动变动")]
    public double dMoveSpeed = 0.8;
    /// <summary>
    /// 机器人同时旋转行走时的角速度，这个值会根据距离的远近自动变动
    /// </summary>
    [Header("机器人同时旋转行走时的角速度，这个值会根据距离的远近自动变动")]
    public double dRotateSpeed = 90;
    /// <summary>
    /// 机器人基准步长
    /// </summary>
    [Header("机器人基准步长")]
    public double dStardStep = 0.6;
    /// <summary>
    /// 障碍物列表
    /// </summary>
    [Header("障碍物列表")]
    public Transform[] transObstacles;
    /// <summary>
    /// 用户，也属于障碍物
    /// </summary>
    [Header("用户，也属于障碍物")]
    public Transform transUser;

    public Transform key;

    //zpp 2020.11.7  目标点控制
    public Transform NowRobotGoal;
    //private int Goal = 1;

    //public string sIPstring = "192.168.2.1";
    //public int nPort = 40923;
    #endregion

    #region For View
    public double dTargetDistance = 0;
    public double dNextPointDistance = 0;
    public double dAheadAngel = 0f;
    // 连续离目标点走远的次数
    public int nOverstepTime = 0;
    // 行进过程中停止的时间，超过N秒的时间要重算路径
    public double dStay;
    private TimeSpan tsStay;
    #endregion

    public static Socket tcpClientRobot;
    private Transform transTarget;
    public Transform transRobot;
    private IPAddress ipaddressRobot;
    private EndPoint pointRobot;
    private Path pathRobot;
    private Vector3 vcNextPoint;
    private int curPathIndex = 0;
    private Vector3[] vcObstaclesOld;
    private Vector3 vcUser;
    // 任务类型
    public TaskType taskType = TaskType.TaskIdle;
    // 重算路径相关
    public RescanState rescanState = RescanState.RescanIdle;
    // 路径相关
    public AlongPathState alongpahtState = AlongPathState.AlongPathIdle;
    // 路点相关
    public NextWaypointState nextWaypointState = NextWaypointState.NextWaypointIdle;
    //zpp 2020.11.11 寻路目标相关
    public NowRobotGoalPathFinding NowGoal = NowRobotGoalPathFinding.User;
    //zpp 2020.11.22  车机器人相关
    public CarRobotStatus NowCarRobotStatus = CarRobotStatus.Idle;
    public CarRobotStatus OldCarRobotStatus = CarRobotStatus.Idle;
    // A*算法seeker
    public Seeker seeker;
    // 上一次距路点的距离,在行进过程中应该不断变小,超过N次由小变大要重算路径
    private double dNextPointDistanceOld = 0;
    private DateTime dtLastTime;
    // 进入fixed update时加1，当到达nRecaculateSpan时执行更新物体位置的逻辑，同时重置为0
    private int nRecaculateTime = 0;
    private double move_x;
    private double move_y;
    private double move_angle;

    //zpp 2020.11.7  Robot朝向差距角度
    private double Rotationangle;
    private bool FirstIntoClaw = true, SecondIntoClaw = false, ClawEnding = true;

    ////zpp 2020.11.10  自动夹持和结束
    //private bool AutoClaw = true;

    //zpp 2020.11.18  跨脚本
    private UnityServer unityserver;
    private Hololens hololens;
    //zpp 2020.11.18  判断强行停止标志位
    private bool isStop = false;
    // Start is called before the first frame update
    //zpp 2020.11.22  车串口相关
    SerialPort serialPort = null;
    CarListener listener = new CarListener();
    //zpp 2020.11.24  车状态计时器
    public float CarStausChangeTime=0;
    //zpp 2020.11.24  车旋转最大次数
    private int Carrotationcount=0;
    private void Awake()
    {
        listener.Awake();//唤醒串口；
        //Thread.Sleep(3000);
        //if (listener.getlisteningstate())
        serialPort = listener.getserialPort();
        BalanceBike.BalanceBike.setSpeed_level("Hight");

    }
    private void Start()
    {
        
        seeker = GetComponent<Seeker>();
        transTarget = GameObject.Find("Target").GetComponent<Transform>();
        transRobot = GameObject.Find("CarRobotCenter").GetComponent<Transform>();
        unityserver = GameObject.Find("Server").GetComponent<UnityServer>();
        hololens = GameObject.Find("Server").GetComponent<Hololens>();
        vcObstaclesOld = new Vector3[transObstacles.Length];
        vcUser = new Vector3();
        nMaxOverstepTime = (int)(nMaxOverstepTime / (Time.fixedDeltaTime * nRecaculateSpan));
        
    }

    private void LateUpdate()
    {
        switch (taskType)
        {
            case TaskType.TaskFollow:
                {
                    if (bRescanPath && rescanState != RescanState.Rescaning)
                    {
                        bRescanPath = false;
                        rescanState = RescanState.Rescaning;
                        alongpahtState = AlongPathState.AlongPathIdle;
                        nextWaypointState = NextWaypointState.NextWaypointIdle;
                        //StopCoroutine("RobotAhead");
                        StopCoroutine("GotoNextPoint");
                        StopCoroutine("RescanPath");
                        StartCoroutine("RescanPath");
                    }
                    if (RescanState.RescanOK == rescanState)
                    {

                    }
                }
                break;
        }

    }

    private void Update()
    {
        
        //zpp 2020.11.11  目标切换公共Transform替换
        if (NowGoal == NowRobotGoalPathFinding.Target)
        {
            NowRobotGoal = transTarget;//目标1
        }
        else if (NowGoal == NowRobotGoalPathFinding.User)
        {
            NowRobotGoal = transUser;//目标2
        }
        else if (NowGoal == NowRobotGoalPathFinding.Box)
        {
            NowRobotGoal = key.transform;//目标3
        }

        //2020.11.21 Dog恢复行动控制 改车使用
        if (unityserver.CarRobotNowPattern == UnityServer.Pattern.Task)
        {
            dTargetDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z));
            if (dTargetDistance >= dTargetTolerance && taskType == TaskType.TaskIdle)
            {
                taskType = TaskType.TaskFollow;
                bRescanPath = true;
               
            }
            isStop = false;
        }
        else if (unityserver.CarRobotNowPattern == UnityServer.Pattern.Idle && isStop == false)
        {
            isStop = true;
            //DJRobotAction.SetDogForward(Listener.serialPort, 0);
            //BalanceBike.MoveToBeforeandAfter(0, serialPort);
            BalanceBike.BalanceBike.Stop(serialPort);
            taskType = TaskType.TaskIdle;
        }
        //if (unityserver.CarRobotNowPattern == UnityServer.Pattern.Task)
        //{
        //    taskType = TaskType.TaskFollow;
        //    bRescanPath = true;
        //    unityserver.CarRobotNowPattern = UnityServer.Pattern.Tasking;
        //}
    }
    public void OnPathComplete(Path p)
    {
        Debug.Log("OnPathComplete error = " + p.error);
        rescanState = RescanState.RescanIdle;
        if (!p.error)
        {
            pathRobot = p;
            curPathIndex = 0;
            rescanState = RescanState.RescanOK;
            if (pathRobot.vectorPath.Count > 1)
            {
                alongpahtState = AlongPathState.AlongPathing;
                curPathIndex = 1;
                vcNextPoint = pathRobot.vectorPath[curPathIndex];
                // 控制机器人向下一路点移动
                StartCoroutine("GotoNextPoint");
            }
        }

    }



    private double GetRobotRotate(Vector3 target) //获取机器人与目标点的角度
    {
        Vector3 vcRobot2Target = new Vector3(target.x, 0, target.z) - new Vector3(transRobot.position.x, 0, transRobot.position.z);
        double f = Vector3.Angle(transRobot.forward, vcRobot2Target);
        Vector3 normal = Vector3.Cross(transRobot.forward, vcRobot2Target);
        f *= Mathf.Sign(Vector3.Dot(normal, transRobot.up));
        return f;
    }

    IEnumerator RescanPath()
    {
        // DJRobotAction.SetRobotStop(tcpClientRobot); // stop the robot
        // DJRobotAction.SetRobotSpeed(tcpClientRobot, 0.2);
        yield return new WaitForSeconds(0.1f);
        if (!unityserver.queue.Contains("CarRobot"))
            unityserver.queue.Enqueue("CarRobot");
        //AstarPath.active.Scan();
        //seeker.StartPath(transRobot.position, new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z), OnPathComplete);
    }

    IEnumerator SafeRobotStop()
    {
        yield return new WaitForSeconds(0.1f);
        //DJRobotAction.SetRobotStop(tcpClientRobot); // stop the robot
        //DJRobotAction.SetDogForward(Listener.serialPort, 0);
        BalanceBike.BalanceBike.Stop(serialPort);
        yield return new WaitForSeconds(0.5f);
    }



    IEnumerator GotoNextPoint() //向下一点运动
    {
        nextWaypointState = NextWaypointState.NextWaypointStart;
        // 旋转机器人
        //yield return StartCoroutine("RobotAhead", vcNextPoint);
        //Listener.ByteData[0] = 0x00;
        countangle(vcNextPoint);
        //if ((Rotationangle > 2f || Rotationangle < -2f)&&NowCarRobotStatus ==CarRobotStatus.Idle&&(OldCarRobotStatus==CarRobotStatus.Idle||OldCarRobotStatus==CarRobotStatus.Walk))
        if ((Rotationangle > 5f || Rotationangle < -5f) && NowCarRobotStatus == CarRobotStatus.Idle &&Carrotationcount<=1)
        {
            Carrotationcount++;
            NowCarRobotStatus = CarRobotStatus.Rotation;
            OldCarRobotStatus = CarRobotStatus.Rotation;
            Debug.Log("开始旋转");
            //DJRobotAction.SetDogRotate(Listener.serialPort, Rotationangle);//旋转角度
            CarStausChangeTime = BalanceBike.BalanceBike.RotateInplace(Rotationangle, serialPort);
            //BalanceBike.BalanceBike.RotateInplace(Rotationangle, serialPort);
            yield return new WaitForSeconds(CarStausChangeTime);
            
        }

        // 移动机器人
        nextWaypointState = NextWaypointState.NextWaypointing;
        dNextPointDistanceOld = dNextPointDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(vcNextPoint.x, 0, vcNextPoint.z));
        nOverstepTime = 0;
        tsStay = TimeSpan.Zero;
        dStay = tsStay.TotalSeconds;
        dtLastTime = DateTime.MinValue;
        BackupObstaclesAndUserPos();

        // 直接向前走
        // DJRobotAction.SetRobotForward(tcpClientRobot, dNextPointDistance, 0.4);
        //if (NowCarRobotStatus == CarRobotStatus.Idle && (OldCarRobotStatus == CarRobotStatus.Rotation||OldCarRobotStatus==CarRobotStatus.Idle))
        //{
        if (NowCarRobotStatus == CarRobotStatus.Idle || Carrotationcount>=2) {
            Carrotationcount = 0;
            Debug.Log("开始行走");
            NowCarRobotStatus = CarRobotStatus.Walk;
            OldCarRobotStatus = CarRobotStatus.Walk;
            CarStausChangeTime=BalanceBike.BalanceBike.MoveToBeforeandAfter(dNextPointDistance*100, serialPort);
            //float time= BalanceBike.BalanceBike.MoveToBeforeandAfter(dNextPointDistance, serialPort);
            yield return new WaitForSeconds(CarStausChangeTime);
            
        }
        yield return new WaitForSeconds(0.1f);

    }

    /// <summary>
    ///  备份障碍物和用户的位置信息，只要x和z
    /// </summary>
    private void BackupObstaclesAndUserPos()
    {
        vcUser.x = transUser.position.x;
        vcUser.y = 0;
        vcUser.z = transUser.position.z;

        for (int i = 0; i < transObstacles.Length; i++)
        {
            vcObstaclesOld[i].x = transObstacles[i].position.x;
            vcObstaclesOld[i].y = 0;
            vcObstaclesOld[i].z = transObstacles[i].position.z;
        }
    }

    /// <summary>
    /// 判定是否因为障碍物或用户的移动而需要重算路径
    /// 如果有移动会重新备份障碍物或用户的位置
    /// </summary>
    /// <returns>如果有移动且移动的物体距机器人距离大于阈值时（可能对机器人有影响）返回true，否则返回false</returns>
    private bool IsRescan4ObstaclesAndUser()
    {
        bool bRescan = false;
        bool bMoved = false;
        double d = Vector3.Distance(vcUser, new Vector3(transUser.position.x, 0, transUser.position.z));
        if (d > dStayTolerance)
        {
            bMoved = true;
            d = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(transUser.position.x, 0, transUser.position.z));
            if (d < dInfluenceDistance)
                bRescan = true;
        }
        else
        {
            for (int i = 0; i < transObstacles.Length; i++)
            {
                d = Vector3.Distance(vcObstaclesOld[i], new Vector3(transObstacles[i].position.x, 0, transObstacles[i].position.z));
                if (d > dStayTolerance)
                {
                    bMoved = true;
                    d = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(transObstacles[i].position.x, 0, transObstacles[i].position.z));
                    if (d < dInfluenceDistance)
                    {
                        bRescan = true;
                        break;
                    }
                }
            }
        }
        if (bMoved)
            BackupObstaclesAndUserPos();
        return bRescan;
    }

    private void AlongPathing()
    {
        dTargetDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z));
        if (dTargetDistance < dTargetTolerance)
        {
            alongpahtState = AlongPathState.AlongPathIdle;
            nextWaypointState = NextWaypointState.NextWaypointIdle;
            // 发机器人停止指令
            //DJRobotAction.SetRobotStop(tcpClientRobot);
            //DJRobotAction.SetDogForward(Listener.serialPort, 0);
            BalanceBike.BalanceBike.Stop(serialPort);
            //unityserver.Taskstart = "0";
            unityserver.TaskMessage("2", "0");
            //hololens.SendRobotTaskStatus(2);
            taskType = TaskType.TaskIdle;

            return;
        }
        switch (nextWaypointState)
        {
            case NextWaypointState.NextWaypointing:
                dNextPointDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(vcNextPoint.x, 0, vcNextPoint.z));
                // is arrived
                if (dNextPointDistance < dNextwaypointTolerance)
                {
                    BalanceBike.BalanceBike.Stop(serialPort);
                    nextWaypointState = NextWaypointState.NextWaypointOK;
                    Debug.Log("1");
                    bRescanPath = true;
                    break;
                }
                // is overstep  dMinValue4Overstep
                if (dNextPointDistanceOld - dNextPointDistance < 0)
                {
                    nOverstepTime++;
                }
                else
                {
                    nOverstepTime = 0;
                }
                if (nOverstepTime > nMaxOverstepTime) // 判定机器人离目标越来越远
                {
                    nextWaypointState = NextWaypointState.NextWaypointAbort;
                    Debug.Log("2");
                    bRescanPath = true;
                    break;
                }
                // is stay
                if (Math.Abs(dNextPointDistance - dNextPointDistanceOld) < dStayTolerance)
                {
                    if (dtLastTime == DateTime.MinValue)
                    {
                        dtLastTime = DateTime.Now;
                        tsStay = TimeSpan.Zero;
                        dStay = tsStay.TotalSeconds;
                    }
                    else
                    {
                        tsStay = DateTime.Now - dtLastTime;
                        dStay = tsStay.TotalSeconds;
                    }
                }
                else
                {
                    dtLastTime = DateTime.MinValue;
                    tsStay = TimeSpan.Zero;
                }
                if ((tsStay.TotalSeconds > fMaxStaySpan) && NowCarRobotStatus == CarRobotStatus.Idle) // 判定机器人停滞 或者行走完毕
                {
                    nextWaypointState = NextWaypointState.NextWaypointAbort;
                    Debug.Log("3");
                    bRescanPath = true;
                    break;
                }
                // is objects moved
                if (IsRescan4ObstaclesAndUser())
                {
                    nextWaypointState = NextWaypointState.NextWaypointAbort;
                    Debug.Log("4");
                    bRescanPath = true;
                    break;
                }
                dNextPointDistanceOld = dNextPointDistance;
                break;
        }

    }
    private void FixedUpdate()
    {
        if (CarStausChangeTime > 0)
        {
            CarStausChangeTime -= 0.01f;
        }
        else if (CarStausChangeTime <= 0 )
        {
            BalanceBike.BalanceBike.Stop(serialPort);
            NowCarRobotStatus = CarRobotStatus.Idle;
        }
            nRecaculateTime++;
        if (nRecaculateTime >= nRecaculateSpan)
            nRecaculateTime = 0;
        else
            return;

        switch (taskType)
        {
            case TaskType.TaskFollow:
                {
                    switch (alongpahtState)
                    {
                        case AlongPathState.AlongPathing:
                            AlongPathing();
                            break;
                    }
                }
                break;
            case TaskType.TaskSnatch:
                {

                }
                break;
            case TaskType.TaskMagnetic:
                {

                }
                break;
        }

    }
    void countangle(Vector3 NowNextPoint)
    {
        Transform trackerangle = transRobot;
        Quaternion raw_rotation = trackerangle.rotation;
        double myangle = trackerangle.eulerAngles.y;//自己的角度
        trackerangle.LookAt(NowNextPoint);
        double lookatangle = trackerangle.eulerAngles.y;//看向目标时的角度
        trackerangle.rotation = raw_rotation;
        Rotationangle = myangle - lookatangle; //需要旋转的角度
        if (Rotationangle > 360)
            Rotationangle -= 360;
        if (Rotationangle < -360)
            Rotationangle = 360 + Rotationangle;
        if (Rotationangle > 180)
            Rotationangle = -(360 - Rotationangle);
        if (Rotationangle < -180)
            Rotationangle = 360 + Rotationangle;
        Rotationangle = -Rotationangle;
    } //计算角度

    private void OnDestroy()
    {
        listener.OnDisable();
        
    }
}

