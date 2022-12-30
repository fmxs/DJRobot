using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;


public class RobotControl : MonoBehaviour
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
        Key
    }

    //zpp 2020.11.11 机械爪状态
    public enum ClawStatus
    {
        Idle,
        Work,   //抓取
        Working,
        End,     //放开
        Ending
    }
    #region  For debug

    //zpp 2020.11.9  unityserver 静态标志控制
    public static bool bRescanPath = false;
    //public static bool bAutoClawCatch = false;

    //2020.11.10 紧急停止标志
    public  bool bStop = false;
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

    public string sIPstring = "192.168.31.115";
    public int nPort = 40923;
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
    public NowRobotGoalPathFinding NowGoal = NowRobotGoalPathFinding.Target;
    //zpp 2020.11.11 自动爪子控制相关
    public ClawStatus NowClawStatus = ClawStatus.Idle;
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

    //zpp 2020.11.10  自动夹持和结束
    private bool AutoClaw = true;
    //zpp 2020.11.11 unityserver联动
    private UnityServer unityserver;
    private Hololens hololens;
    // Start is called before the first frame update

    private void Start()
    {
        seeker = GetComponent<Seeker>();
        transTarget = GameObject.Find("Target").GetComponent<Transform>();
        transRobot = GameObject.Find("RobotCenter").GetComponent<Transform>();
        unityserver = GameObject.Find("Server").GetComponent<UnityServer>();
        hololens = GameObject.Find("Server").GetComponent<Hololens>();
        InitRobot();
        vcObstaclesOld = new Vector3[transObstacles.Length];
        vcUser = new Vector3();
        nMaxOverstepTime = (int)(nMaxOverstepTime / (Time.fixedDeltaTime * nRecaculateSpan));
    }

    private void LateUpdate()
    {
        switch(taskType)
        {
            case TaskType.TaskFollow:
                {
                    if (bRescanPath && rescanState != RescanState.Rescaning)
                    {
                        bRescanPath = false;
                        rescanState = RescanState.Rescaning;
                        alongpahtState = AlongPathState.AlongPathIdle;
                        nextWaypointState = NextWaypointState.NextWaypointIdle;

                        //if (NowRobotChoose == RobotChoose.DJ)
                        //    StopCoroutine("RobotAhead");

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
            NowRobotGoal = transTarget;//夹持物体1
        }
        else if (NowGoal == NowRobotGoalPathFinding.User)
        {
            NowRobotGoal = transUser;//用户
        }
        else if (NowGoal == NowRobotGoalPathFinding.Key)
        {
            NowRobotGoal = key.transform;//铁质钥匙
        }


        //2020.11.10 添加紧急停止情况
        if (Input.GetKeyDown(KeyCode.F12) || bStop)
        {
            alongpahtState = AlongPathState.AlongPathIdle;
            nextWaypointState = NextWaypointState.NextWaypointIdle;
            // 发机器人停止指令
            DJRobotAction.SetRobotStop(tcpClientRobot);
            taskType = TaskType.TaskIdle;
        }
        if(unityserver.RobotNowPattern==UnityServer.Pattern.Task && taskType==TaskType.TaskIdle && bStop==false)//？？？ 用它传参数就恢复寻路，但是其他方法不行
        {
            unityserver.TaskMessage("0","1");
        }
        
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

        // for (int index = 0; index < path.vectorPath.Count; index++)
        // {
        //     Debug.Log("path.vectorPath[" + index + "]=" + path.vectorPath[index]);

        //}
    }

    private void InitRobot() //command进入连接
    {
        tcpClientRobot = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ipaddressRobot = IPAddress.Parse(sIPstring);//IPAddress.Parse可以把string类型的ip地址转化为ipAddress型
        pointRobot = new IPEndPoint(ipaddressRobot, nPort);//通过ip地址和端口号定位要连接的服务器端
        tcpClientRobot.Connect(pointRobot);//建立连接
        tcpClientRobot.ReceiveTimeout = 10000;
        tcpClientRobot.SendTimeout = 10000;
        string messageToServer = "command;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageToServer));//向服务器端发送消息
        byte[] data = new byte[1000];
        int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
        string message = Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串
        Debug.Log("RobotReturn of command" + message);
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
        if (!unityserver.queue.Contains("Robot"))
            unityserver.queue.Enqueue("Robot");
        //AstarPath.active.Scan();
        //seeker.StartPath(transRobot.position, new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z), OnPathComplete);
    }

    IEnumerator SafeRobotStop()
    {
        yield return new WaitForSeconds(0.1f);
        DJRobotAction.SetRobotStop(tcpClientRobot); // stop the robot
        yield return new WaitForSeconds(0.5f);
    }

    //IEnumerator RobotAhead(Vector3 targetPoint)
    //{
    //    dAheadAngel = GetRobotRotate(targetPoint);//获取角度
    //    int nTime = nRotateTimeExtra; // 调整旋转次数
    //    DJRobotAction.SetRobotRotate(tcpClientRobot, dAheadAngel, dRotateSpeedAtonce);
    //    yield return new WaitForSeconds(2.0f);
    //    dAheadAngel = GetRobotRotate(targetPoint);//获取角度
    //    while (Math.Abs(dAheadAngel) > dAheadangleTolerance && nTime > 0)
    //    {
    //        DJRobotAction.SetRobotRotate(tcpClientRobot, Math.Sign(dAheadAngel) * Math.Max(2.5, Math.Abs(dAheadAngel / 1.2)), dRotateSpeedModify); //math.sign获取正负，旋转
    //        nTime--; //旋转次数减一
    //        yield return new WaitForSeconds(0.5f); //延时s
    //        dAheadAngel = GetRobotRotate(targetPoint);
    //    }
    //}

    IEnumerator GotoNextPoint() //向下一点运动
    {
        nextWaypointState = NextWaypointState.NextWaypointStart;
        // 旋转机器人
        //yield return StartCoroutine("RobotAhead", vcNextPoint);
        //yield return new WaitForSeconds(2.0f);
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

        // 同步转向与向前走
        move_angle = GetRobotRotate(vcNextPoint);
        move_x = 1.0 * (dNextPointDistance * Math.Cos(move_angle * Math.PI / 180));
        move_y = 1.0 * (dNextPointDistance * Math.Sin(move_angle * Math.PI / 180));
        double dRealRotateSpeed = dRotateSpeed * dNextPointDistance / dStardStep;
        double dRealMoveSpeed = dMoveSpeed * dNextPointDistance / dStardStep;
        DJRobotAction.SetRobotMove(tcpClientRobot, move_x, move_y, move_angle, dRealMoveSpeed, dRealRotateSpeed);
        yield return new WaitForSeconds(0.2f);

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
            // 2020.11.21 发各种机器人停止指令
            DJRobotAction.SetRobotStop(tcpClientRobot);
            taskType = TaskType.TaskIdle;

            //zpp 2020.11.10  自动夹持和放开检测
            if ((NowGoal == NowRobotGoalPathFinding.Target || NowGoal == NowRobotGoalPathFinding.Key) && AutoClaw)
            {
                if (NowClawStatus == ClawStatus.Working) ;

                else
                {
                    NowClawStatus = ClawStatus.Work;
                    AutoClaw = false;//自动夹持设为false
                }

            }
            else if (NowGoal == NowRobotGoalPathFinding.User)
            {
                if (NowClawStatus == ClawStatus.Ending) ;

                else
                    NowClawStatus = ClawStatus.End;
            }

            return;
        }
        switch (nextWaypointState)
        {
            case NextWaypointState.NextWaypointing:
                dNextPointDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(vcNextPoint.x, 0, vcNextPoint.z));
                // is arrived
                if (dNextPointDistance < dNextwaypointTolerance)
                {
                    nextWaypointState = NextWaypointState.NextWaypointOK;
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
                if (tsStay.TotalSeconds > fMaxStaySpan) // 判定机器人停滞
                {
                    nextWaypointState = NextWaypointState.NextWaypointAbort;
                    bRescanPath = true;
                    break;
                }
                // is objects moved
                if (IsRescan4ObstaclesAndUser())
                {
                    nextWaypointState = NextWaypointState.NextWaypointAbort;
                    bRescanPath = true;
                    break;
                }
                dNextPointDistanceOld = dNextPointDistance;
                break;
        }

    }
    private void FixedUpdate()
    {
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
        //zpp 2020.11.10  自动夹持
        if (NowClawStatus == ClawStatus.Work)
        {
            alongpahtState = AlongPathState.AlongPathIdle;
            nextWaypointState = NextWaypointState.NextWaypointIdle;
            taskType = TaskType.TaskIdle;
            //发机器人停止指令
            DJRobotAction.SetRobotStop(tcpClientRobot);
            //进入自动抓取流程
            NowClawStatus = ClawStatus.Working;

            StartCoroutine(StartCrawling());
            return;
        }
        //zpp 2020.11.10  自动放开夹持
        else if (NowClawStatus == ClawStatus.End && AutoClaw == false)
        {

            alongpahtState = AlongPathState.AlongPathIdle;
            nextWaypointState = NextWaypointState.NextWaypointIdle;
            taskType = TaskType.TaskIdle;
            //发机器人停止指令
            DJRobotAction.SetRobotStop(tcpClientRobot);
            NowClawStatus = ClawStatus.Ending;
            //结束夹持
            StartCoroutine(EndCrawling(tcpClientRobot));
            return;

        }


    }

    private void OnDestroy()
    {
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
        tcpClientRobot.Close();
    }

    //zpp 2020.11.7  机械手臂控制
    #region 机械手臂控制 

    void countangle()
    {
        Transform trackerangle = transRobot;
        Quaternion raw_rotation = trackerangle.rotation;
        double myangle = trackerangle.eulerAngles.y;//自己的角度
        trackerangle.LookAt(NowRobotGoal);
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

    IEnumerator RobotAhead(Vector3 targetPoint)
    {
        dAheadAngel = GetRobotRotate(targetPoint);//获取角度
        int nTime = nRotateTimeExtra; // 调整旋转次数
        DJRobotAction.SetRobotRotate(tcpClientRobot, dAheadAngel, dRotateSpeedAtonce);
        yield return new WaitForSeconds(1.0f);
        dAheadAngel = GetRobotRotate(targetPoint);//获取角度
        DJRobotAction.SetRobotRotate(tcpClientRobot, dAheadAngel, dRotateSpeedAtonce);
        yield return new WaitForSeconds(1.0f);
        dAheadAngel = GetRobotRotate(targetPoint);//获取角度
        while (Math.Abs(dAheadAngel) > dAheadangleTolerance && nTime > 0)
        {
            DJRobotAction.SetRobotRotate(tcpClientRobot, Math.Sign(dAheadAngel) * Math.Min(2.0, Math.Max(6, Math.Abs(dAheadAngel / 1.2))), dRotateSpeedModify); //math.sign获取正负，旋转
            nTime--; //旋转次数减一
            yield return new WaitForSeconds(0.5f); //延时s
            dAheadAngel = GetRobotRotate(targetPoint);
        }
    }

    IEnumerator StartCrawling() //开始抓取,方式一，用于抓取水瓶
    {
        yield return new WaitForSeconds(1f);
        countangle();
        //UnityEngine.Debug.Log("length:"+length);
        if (Rotationangle > 1f || Rotationangle < -1f)
        {
            StartCoroutine(RobotAhead(new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z)));
            //string messageaToServer = "chassis move vz 180 z " + Rotationangle + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
            //tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
            //                                                                 //UnityEngine.Debug.Log("旋转角度：" + messageaToServer);


            //UnityEngine.Debug.Log("接收到服务器端的消息：" + message_angel);

        }
        yield return new WaitForSeconds(1.5f);


        ////机器手臂控制逻辑代码

        //tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper open 1;")); //张开机械爪

        //yield return new WaitForSeconds(2f);

        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper open 1;")); //张开机械爪

        yield return new WaitForSeconds(1.8f);

        //tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm recenter;")); //机械复位

        //yield return new WaitForSeconds(0.8f);

        ///开始抓取
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;"));  //向上

        yield return new WaitForSeconds(0.8f);

        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 200 y 0;")); //向前

        yield return new WaitForSeconds(0.8f);
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y -200;")); //向下


        StartCoroutine(PositionCorrection(tcpClientRobot));//位置矫正
        yield return new WaitForSeconds(2f);

        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper close 3;"));

        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper close 3;")); //开始抓取

        yield return new WaitForSeconds(1.5f);
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;")); //向上

        yield return new WaitForSeconds(0.5f);
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x -80 y 0;")); //向后收回

        yield return new WaitForSeconds(0.8f);


        //切换目标User
        NowGoal = NowRobotGoalPathFinding.User;
        //重新计算路径寻路
        taskType = TaskType.TaskFollow;
        bRescanPath = true;
 
        ///////////////////////////////抓取完成
    }
    IEnumerator EndCrawling(Socket tcpclientRost)  //结束抓取，将抓取的物品
    {
        
        countangle();

        if (Rotationangle > 1f || Rotationangle < -1f)
        {

            string messageaToServer = "chassis move vz 180 z " + Rotationangle + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
            tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
        }
        yield return new WaitForSeconds(1.5f);

        countangle();

        if (Rotationangle > 1f || Rotationangle < -1f)
        {

            string messageaToServer = "chassis move vz 180 z " + Rotationangle + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
            tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
        }

        yield return new WaitForSeconds(0.5f);
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 60 0;")); //向
                                                             //Thread.Sleep(500);
        yield return new WaitForSeconds(0.5f);
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 120;")); //向上

        yield return new WaitForSeconds(1.5f);

        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_gripper open 1;")); //张开机械爪

        yield return new WaitForSeconds(1.8f);
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm recenter;")); //机械复位

        yield return new WaitForSeconds(0.6f);
        AutoClaw = true;

        unityserver.TaskMessage("0", "0");
        //hololens.SendRobotTaskStatus(0);
        ////恢复寻路
        //taskType = TaskType.TaskFollow;
        //bRescanPath = true;
        //切换回Target目标点
        NowGoal = NowRobotGoalPathFinding.Target;
        //bStop = true;//任务结束停止
        //unityserver.Taskstart = "0";
        
    }
    IEnumerator PositionCorrection(Socket tcpclientRost)  //位置矫正
    {
        double length = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z));
        if (length >= 0.01 || length <= -0.01)
        {
            //调整位置
            length = length - 0.34;
            string len_ = length.ToString().Substring(0, 5);
            tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis move x " + len_ + " y 0;")); //
            //Thread.Sleep(550);
            yield return new WaitForSeconds(0.55f);
            tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis speed x " + "0" + ";"));
            tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis wheel w2 0 w1 0 w3 0 w4 0 ;"));   //向服务器端发送停止指令
            //Thread.Sleep(500);
            yield return new WaitForSeconds(0.5f);
            UnityEngine.Debug.Log("juli:" + len_);
            length = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z));
            length = length - 0.34;
            len_ = length.ToString().Substring(0, 5);
            if (length >= 0.01 || length <= -0.01)
            {
                //调整位置
                tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis move x " + len_ + " y 0;")); //
                yield return new WaitForSeconds(0.55f);
                //Thread.Sleep(550);
                tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis speed x " + "0" + ";"));
                tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis wheel w2 0 w1 0 w3 0 w4 0 ;"));   //向服务器端发送停止指令
                //Thread.Sleep(500);
                yield return new WaitForSeconds(0.5f);

            }
            
        }

    }


    //IEnumerator StartCrawling_Way2() //开始抓取,方式二，用于抓取钥匙串
    //{
    //    yield return new WaitForSeconds(1f);
    //    countangle(NowRobotGoal.position);
    //    //UnityEngine.Debug.Log("length:"+length);
    //    if (Rotationangle > 1f || Rotationangle < -1f)
    //    {

    //        //UnityEngine.Debug.Log("偏转{0}" + angel);
    //        //PIDText.text += "偏转:" + angel;
    //        string messageaToServer = "chassis move vz 180 z " + Rotationangle + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
    //        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
    //    }
    //    yield return new WaitForSeconds(1.5f);
    //    countangle(NowRobotGoal.position);
    //    //UnityEngine.Debug.Log("length:"+length);
    //    if (Rotationangle > 1f || Rotationangle < -1f)
    //    {

    //        //UnityEngine.Debug.Log("偏转{0}" + angel);
    //        //PIDText.text += "偏转:" + angel;
    //        string messageaToServer = "chassis move vz 180 z " + Rotationangle + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
    //        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
    //    }
    //    yield return new WaitForSeconds(0.5f);
    //    //tcpClientRobot.Send(Encoding.UTF8.GetBytes("command;"));
    //    //length = length - 0.46;
    //    //string len_ = length.ToString().Substring(0, 5);


    //    ////机器手臂控制逻辑代码

    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper open 1;")); //张开机械爪
    //    yield return new WaitForSeconds(0.1f);
    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm recenter;")); //机械复位
    //    yield return new WaitForSeconds(0.8f);

    //    StartCoroutine(PositionCorrection(tcpClientRobot));
    //    yield return new WaitForSeconds(2f);
    //    //PositionCorrection(tcpClientRobot); //位置矫正
    //    //yield return new WaitForSeconds(0.1f);
    //    ///开始抓取
    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;"));  //向上
    //    yield return new WaitForSeconds(0.8f);

    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 200 y 0;")); //向前
    //    yield return new WaitForSeconds(0.8f);

    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y -200;")); //向下
    //    yield return new WaitForSeconds(0.8f);
    //    //UnityEngine.Debug.Log("机械臂进入2");

    //    /*    tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis wheel w2 50 w1 50 w3 50 w4 50;"));//走2cm
    //        Thread.Sleep(500);

    //        tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis wheel w2 0 w1 0 w3 0 w4 0;")); //停止
    //        Thread.Sleep(2000);
    //        */




    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper close 1;"));
    //    // Thread.Sleep(1000);
    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper close 1;")); //开始抓取
    //    yield return new WaitForSeconds(1f);

    //    tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;")); //向上
    //    yield return new WaitForSeconds(0.5f);

    //    //tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_arm move x -80 y 0;")); //向后收回
    //    //Thread.Sleep(800);
    //    //UnityEngine.Debug.Log("机械臂进入3");

    //    //切换目标user
    //    //Goal = 2;
    //    NowGoal = NowRobotGoalPathFinding.User;
    //    //重新计算路径寻路
    //    taskType = TaskType.TaskFollow;
    //    bRescanPath = true;
    //    FirstIntoClaw = true;  //启动第一种夹持的代码
    //    SecondIntoClaw = false; //结束第二种夹持的代码
    //    //tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
    //    ///////////////////////////////抓取完成
    //}
    #endregion

    //zpp 2020.11.7  Robot电量获取服务器调用回传
    #region 电量获取
    public string Electricity()
    {
        string messageToServer = "robot battery ?;";
        //UnityEngine.Debug.Log("向服务器端发送消息：" + messageToServer);//
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageToServer));//向服务器端发送消息

        byte[] data = new byte[1000];
        int length_1 = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
        string message_1 = Encoding.UTF8.GetString(data, 0, length_1);//把字节数组转化为字符串
        if (message_1.Contains("ok") || message_1.Contains("fail") || message_1.Contains("error"))
            message_1 = "";
        char[] temp1 = message_1.ToCharArray();
        string temp2 = "";
        for (int i = 0; i < temp1.Length; i++)
        {
            if (temp1[i] >= '0' && temp1[i] <= '9')
            {
                temp2 += temp1[i].ToString();
            }
        }
        return temp2;
    }

    #endregion

    #region 机器人手臂初始化
    public void RobotHandInit()
    {

    }

    #endregion

    ////zpp 2020.11.7  Robot Test模式测试代码
    //#region Robot Test模式
    //public string RobotControlTest(string status)
    //{
    //    string len_ = "0.15";
    //    //前
    //    if (status == "1")
    //    {
    //        tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x " + len_ + " y 0;"));
    //    }
    //    //后
    //    else if (status == "2")
    //    {
    //        tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x -" + len_ + " y 0;"));
    //    }
    //    //左
    //    else if (status == "3")
    //    {
    //        tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x " + "0" + " y " + len_ + ";"));
    //    }
    //    //右
    //    else if (status == "4")
    //    {
    //        tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x " + "0" + " y -" + len_ + ";"));
    //    }
    //    return "OK";
    //}
    //#endregion
}
