//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Pathfinding;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System;
//using System.Threading;

//public enum TaskType
//{
//    TaskIdle,
//    TaskFollow, 
//    TaskSnatch,
//}

//public enum RescanState
//{
//    RescanIdle,
//    Rescaning,
//    RescanOK,
//}

//public enum AlongPathState
//{
//    AlongPathIdle,
//    AlongPathing,
//    AlongPathOK,
//}

//public enum NextWaypointState
//{
//    NextWaypointIdle,
//    NextWaypointStart,
//    NextWaypointing,
//    NextWaypointOK,
//    NextWaypointAbort,
//}

//public class RobotControl : MonoBehaviour
//{
//    #region  For debug

//    //zpp 2020.11.9  unityserver 静态标志控制
//    public static bool bRescanPath = false;
//    public static bool bShouldMove = false;
//    //public static bool bAutoClawCatch = false;

//    #endregion

//    #region For Editor
//    /// <summary>
//    /// 在fixed update中更新物体的位置，nRecaculateSpan越大，更新计算越少
//    /// </summary>
//    public int nRecaculateSpan = 5;
//    /// <summary>
//    /// 距目标点的最小阈值，单位米
//    /// </summary>
//    public double dTargetTolerance = 0.6;
//    /// <summary>
//    /// 距下一路点的最小阈值，单位米
//    /// </summary>
//    public double dNextwaypointTolerance = 0.1;
//    public double dClawDistanceTolerance = 0.42;
//    /// <summary>
//    /// 目标角度最小阈值，单位度
//    /// </summary>
//    public double dAheadangleTolerance = 1.0;
//    /// <summary>
//    /// 旋转指定角度仍未到达目标角度时，允许的额外旋转次数，单位次数
//    /// </summary>
//    public int nRotateTimeExtra = 0;
//    /// <summary>
//    /// 判断是否停止的阈值，单位米
//    /// </summary>
//    public double dStayTolerance = 0.005;
//    /// <summary>
//    /// 在行进过程中距目标点应该越来越近，当出现 nMaxOverstepTime * 每秒更新物体次数 越来越远的情况时，重算路径
//    /// </summary>
//    public int nMaxOverstepTime = 2;
//    /// <summary>
//    /// 判断overstep的值，应接近于0，单位米
//    /// </summary>
//    public double dMinValue4Overstep = 0;
//    /// <summary>
//    /// 在行进过程中机器人超过dMaxStaySpan秒未动时，重算路径
//    /// </summary>
//    public float fMaxStaySpan = 1.0f;
//    /// <summary>
//    /// 与机器人距离小于该距离的物体移动时，重算路径，单位米
//    /// </summary>
//    public double dInfluenceDistance = 0.8;
//    /// <summary>
//    /// 机器人一次性旋转时角速度
//    /// </summary>
//    public double dRotateSpeedAtonce = 90;
//    /// <summary>
//    /// 机器人调整旋转时的角速度
//    /// </summary>
//    public double dRotateSpeedModify = 360;
//    /// <summary>
//    /// 机器人同时旋转行走时的速度，这个值会根据距离的远近自动变动
//    /// </summary>
//    public double dMoveSpeed = 0.8;
//    /// <summary>
//    /// 机器人同时旋转行走时的角速度，这个值会根据距离的远近自动变动
//    /// </summary>
//    public double dRotateSpeed = 90;
//    /// <summary>
//    /// 机器人基准步长
//    /// </summary>
//    public double dStardStep = 0.6;
//    public Transform[] transObstacles;
//    public Transform transUser;
//    public Transform transTarget;
//    public Transform transRobot;

//    //zpp 2020.11.7  目标点控制
//    public Transform NowRobotGoal;
//    private int Goal = 1;

//    public string sIPstring = "192.168.2.1";
//    public int nPort = 40923;
//    #endregion

//    #region For View
//    public double dTargetDistance = 0;
//    public double dNextPointDistance = 0;
//    public double dAheadAngel = 0f;
//    // 连续离目标点走远的次数
//    public int nOverstepTime = 0;
//    // 行进过程中停止的时间，超过N秒的时间要重算路径
//    public double dStay;
//    private TimeSpan tsStay;
//    #endregion
//    public static Socket tcpClientRobot;
//    private IPAddress ipaddressRobot;
//    private EndPoint pointRobot;
//    private Path pathRobot;
//    private Vector3 vcNextPoint;
//    private int curPathIndex = 0;
//    private Vector3[] vcObstaclesOld;
//    private Vector3 vcUser;
//    //private Transform transUser;
//    // 重算路径相关
//    public RescanState rescanState = RescanState.RescanIdle;
//    // 路径相关
//    public AlongPathState alongpahtState = AlongPathState.AlongPathIdle;
//    // 路点相关
//    public NextWaypointState nextWaypointState = NextWaypointState.NextWaypointIdle;
//    // A*算法seeker
//    private Seeker seeker;
//    // 上一次距路点的距离,在行进过程中应该不断变小,超过N次由小变大要重算路径
//    private double dNextPointDistanceOld = 0;
//    private DateTime dtLastTime;
//    // 进入fixed update时加1，当到达nRecaculateSpan时执行更新物体位置的逻辑，同时重置为0
//    private int nRecaculateTime = 0;
//    private double move_x;
//    private double move_y;
//    private double move_angle;

//    //zpp 2020.11.7  Robot朝向差距角度
//    private double Rorationangle;
//    private bool FirstIntoClaw = true;
//    // Start is called before the first frame update
//    private void Start()
//    {
//        seeker = GetComponent<Seeker>();
//        transTarget = GameObject.Find("Target").GetComponent<Transform>();
//        transRobot = GameObject.Find("RobotCenter").GetComponent<Transform>();
//        InitRobot();
//        vcObstaclesOld = new Vector3[transObstacles.Length];
//        vcUser = new Vector3();
//        nMaxOverstepTime = (int)(nMaxOverstepTime / (Time.fixedDeltaTime * nRecaculateSpan));
//    }

//    private void LateUpdate()
//    {
//        if (bShouldMove)
//        {
//            if (bRescanPath && rescanState != RescanState.Rescaning)
//            {
//                bRescanPath = false;
//                rescanState = RescanState.Rescaning;
//                alongpahtState = AlongPathState.AlongPathIdle;
//                nextWaypointState = NextWaypointState.NextWaypointIdle;
//                StopCoroutine("RobotAhead");
//                StopCoroutine("GotoNextPoint");
//                StopCoroutine("RescanPath");
//                StartCoroutine("RescanPath");
//            }
//            if (RescanState.RescanOK == rescanState)
//            {

//            }
//        }
//    }

//    // Update is called once per frame
//    private void Update()
//    {
//        //zpp 2020.11.7  目标切换公共Transform替换
//        if (Goal == 1)
//        {
//            NowRobotGoal = transTarget;
//        }else if(Goal == 2)
//        {
//            NowRobotGoal = transUser;
//        }

//        // for test
//        if (Input.GetKeyDown(KeyCode.F2))
//        {
//            // 让机器人朝向世界坐标系的-z轴
//            StartCoroutine(RobotAhead(new Vector3(transRobot.position.x, 0, transRobot.position.z - 1)));
//        }
//        if (Input.GetKeyDown(KeyCode.F1))
//        {
//            // SetRobotMove(-0.077512513362223, -0.157031729942777, 0.2, 0.4);
//        }
//        if (Input.GetKeyDown(KeyCode.F3))
//        {
//            SetRobotForward(0.6, 0.1);
//        }
//        if (Input.GetKeyDown(KeyCode.F12))
//        {
//            // 紧急停止
//            SetRobotStop();
//            bShouldMove = false;
//        }
//    }

//    public void OnPathComplete(Path p)
//    {
//        Debug.Log("OnPathComplete error = " + p.error);
//        rescanState = RescanState.RescanIdle;
//        if (!p.error)
//        {
//            pathRobot = p;
//            curPathIndex = 0;
//            rescanState = RescanState.RescanOK;
//            if (pathRobot.vectorPath.Count > 1)
//            {
//                alongpahtState = AlongPathState.AlongPathing;
//                curPathIndex = 1;
//                vcNextPoint = pathRobot.vectorPath[curPathIndex];
//                // 控制机器人向下一路点移动
//                StartCoroutine("GotoNextPoint");
//            }
//        }

//        // for (int index = 0; index < path.vectorPath.Count; index++)
//        // {
//        //     Debug.Log("path.vectorPath[" + index + "]=" + path.vectorPath[index]);

//        //}
//    }

//    private void InitRobot() //command进入连接
//    {
//        tcpClientRobot = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//        ipaddressRobot = IPAddress.Parse(sIPstring);//IPAddress.Parse可以把string类型的ip地址转化为ipAddress型
//        pointRobot = new IPEndPoint(ipaddressRobot, nPort);//通过ip地址和端口号定位要连接的服务器端
//        tcpClientRobot.Connect(pointRobot);//建立连接
//        string messageToServer = "command;";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageToServer));//向服务器端发送消息
//        byte[] data = new byte[1000];
//        int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
//        string message = Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串
//        Debug.Log("RobotReturn of command" + message);
//    }


//    private double GetRobotRotate(Vector3 target) //获取机器人与目标点的角度
//    {
//        Vector3 vcRobot2Target = new Vector3(target.x, 0, target.z) - new Vector3(transRobot.position.x, 0, transRobot.position.z);
//        double f = Vector3.Angle(transRobot.forward, vcRobot2Target);
//        Vector3 normal = Vector3.Cross(transRobot.forward, vcRobot2Target);
//        f *= Mathf.Sign(Vector3.Dot(normal, transRobot.up));
//        return f;
//    }

//    IEnumerator RescanPath()
//    {
//        // SetRobotStop(); // stop the robot
//        // SetRobotSpeed(0.2);
//        yield return new WaitForSeconds(0.1f);
//        AstarPath.active.Scan();
//        seeker.StartPath(transRobot.position, new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z), OnPathComplete);
//    }

//    IEnumerator SafeRobotStop()
//    {
//        yield return new WaitForSeconds(0.1f);
//        SetRobotStop(); // stop the robot
//        yield return new WaitForSeconds(0.5f);
//    }

//    IEnumerator RobotAhead(Vector3 targetPoint)
//    {
//        dAheadAngel = GetRobotRotate(targetPoint);//获取角度
//        int nTime = nRotateTimeExtra; // 调整旋转次数
//        SetRobotRotate(dAheadAngel, dRotateSpeedAtonce);
//        yield return new WaitForSeconds(2.0f);
//        dAheadAngel = GetRobotRotate(targetPoint);//获取角度
//        while (Math.Abs(dAheadAngel) > dAheadangleTolerance && nTime > 0)
//        {
//            SetRobotRotate(Math.Sign(dAheadAngel) * Math.Max(2.5, Math.Abs(dAheadAngel / 1.2)), dRotateSpeedModify); //math.sign获取正负，旋转
//            nTime--; //旋转次数减一
//            yield return new WaitForSeconds(0.5f); //延时s
//            dAheadAngel = GetRobotRotate(targetPoint);
//        }
//    }

//    IEnumerator GotoNextPoint() //向下一点运动
//    {
//        nextWaypointState = NextWaypointState.NextWaypointStart;
//        // 旋转机器人
//        //yield return StartCoroutine("RobotAhead", vcNextPoint);
//        //yield return new WaitForSeconds(2.0f);
//        // 移动机器人
//        nextWaypointState = NextWaypointState.NextWaypointing;
//        dNextPointDistanceOld = dNextPointDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(vcNextPoint.x, 0, vcNextPoint.z));
//        nOverstepTime = 0;
//        tsStay = TimeSpan.Zero;
//        dStay = tsStay.TotalSeconds;
//        dtLastTime = DateTime.MinValue;
//        BackupObstaclesAndUserPos();

//        // 直接向前走
//        // SetRobotForward(dNextPointDistance, 0.4);

//        // 同步转向与向前走
//        move_angle = GetRobotRotate(vcNextPoint);
//        move_x = 1.0 * (dNextPointDistance * Math.Cos(move_angle * Math.PI / 180));
//        move_y = 1.0 * (dNextPointDistance * Math.Sin(move_angle * Math.PI / 180));
//        double dRealRotateSpeed = dRotateSpeed * dNextPointDistance / dStardStep;
//        double dRealMoveSpeed = dMoveSpeed * dNextPointDistance / dStardStep;
//        SetRobotMove(move_x, move_y, move_angle, dRealMoveSpeed, dRealRotateSpeed);
//        yield return new WaitForSeconds(0.2f);
//    }

//    /// <summary>
//    ///  备份障碍物和用户的位置信息，只要x和z
//    /// </summary>
//    private void BackupObstaclesAndUserPos()
//    {
//        vcUser.x = transUser.position.x;
//        vcUser.y = 0;
//        vcUser.z = transUser.position.z;

//        for (int i = 0; i < transObstacles.Length; i++)
//        {
//            vcObstaclesOld[i].x = transObstacles[i].position.x;
//            vcObstaclesOld[i].y = 0;
//            vcObstaclesOld[i].z = transObstacles[i].position.z;
//        }
//    }

//    /// <summary>
//    /// 判定是否因为障碍物或用户的移动而需要重算路径
//    /// 如果有移动会重新备份障碍物或用户的位置
//    /// </summary>
//    /// <returns>如果有移动且移动的物体距机器人距离大于阈值时（可能对机器人有影响）返回true，否则返回false</returns>
//    private bool IsRescan4ObstaclesAndUser()
//    {
//        bool bRescan = false;
//        bool bMoved = false;
//        double d = Vector3.Distance(vcUser, new Vector3(transUser.position.x, 0, transUser.position.z));
//        if (d > dStayTolerance)
//        {
//            bMoved = true;
//            d = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(transUser.position.x, 0, transUser.position.z));
//            if (d < dInfluenceDistance)
//                bRescan = true;
//        }
//        else
//        {
//            for (int i = 0; i < transObstacles.Length; i++)
//            {
//                d = Vector3.Distance(vcObstaclesOld[i], new Vector3(transObstacles[i].position.x, 0, transObstacles[i].position.z));
//                if (d > dStayTolerance)
//                {
//                    bMoved = true;
//                    d = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(transObstacles[i].position.x, 0, transObstacles[i].position.z));
//                    if (d < dInfluenceDistance)
//                    {
//                        bRescan = true;
//                        break;
//                    }
//                }
//            }
//        }
//        if (bMoved)
//            BackupObstaclesAndUserPos();
//        return bRescan;
//    }

//    private void AlongPathing()
//    {
//        dTargetDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z));
//        if (dTargetDistance < dTargetTolerance)
//        {
//            alongpahtState = AlongPathState.AlongPathIdle;
//            nextWaypointState = NextWaypointState.NextWaypointIdle;
//            // 发机器人停止指令
//            SetRobotStop();
//            bShouldMove = false;
//            return;
//        }
//        switch (nextWaypointState)
//        {
//            case NextWaypointState.NextWaypointing:
//                dNextPointDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(vcNextPoint.x, 0, vcNextPoint.z));
//                // is arrived
//                if (dNextPointDistance < dNextwaypointTolerance)
//                {
//                    nextWaypointState = NextWaypointState.NextWaypointOK;
//                    bRescanPath = true;
//                    break;
//                }
//                // is overstep  dMinValue4Overstep
//                if (dNextPointDistanceOld - dNextPointDistance < 0)
//                {
//                    nOverstepTime++;
//                }
//                else
//                {
//                    nOverstepTime = 0;
//                }
//                if (nOverstepTime > nMaxOverstepTime) // 判定机器人离目标越来越远
//                {
//                    nextWaypointState = NextWaypointState.NextWaypointAbort;
//                    bRescanPath = true;
//                    break;
//                }
//                // is stay
//                if (Math.Abs(dNextPointDistance - dNextPointDistanceOld) < dStayTolerance)
//                {
//                    if (dtLastTime == DateTime.MinValue)
//                    {
//                        dtLastTime = DateTime.Now;
//                        tsStay = TimeSpan.Zero;
//                        dStay = tsStay.TotalSeconds;
//                    }
//                    else
//                    {
//                        tsStay = DateTime.Now - dtLastTime;
//                        dStay = tsStay.TotalSeconds;
//                    }
//                }
//                else
//                {
//                    dtLastTime = DateTime.MinValue;
//                    tsStay = TimeSpan.Zero;
//                }
//                if (tsStay.TotalSeconds > fMaxStaySpan) // 判定机器人停滞
//                {
//                    nextWaypointState = NextWaypointState.NextWaypointAbort;
//                    bRescanPath = true;
//                    break;
//                }
//                // is objects moved
//                if (IsRescan4ObstaclesAndUser())
//                {
//                    nextWaypointState = NextWaypointState.NextWaypointAbort;
//                    bRescanPath = true;
//                    break;
//                }
//                dNextPointDistanceOld = dNextPointDistance;
//                break;
//        }

//    }
//    private void FixedUpdate()
//    {
//        nRecaculateTime++;
//        if (nRecaculateTime >= nRecaculateSpan)
//            nRecaculateTime = 0;
//        else
//            return;

//        if (bShouldMove)
//        {
//            switch (alongpahtState)
//            {
//                case AlongPathState.AlongPathing:
//                    AlongPathing();
//                    break;
//            }
//        }
//        /*
//        if (unityserver.isClawWorking)
//        {
//            alongpahtState = AlongPathState.AlongPathIdle;
//            nextWaypointState = NextWaypointState.NextWaypointIdle;
//            bShouldMove = false;
//            //发机器人停止指令
//            SetRobotStop();
//            //进入自动抓取流程
//            if (FirstIntoClaw)
//            {
//                StartCoroutine(StartCrawling());
//                FirstIntoClaw = false;
//                unityserver.isClawWorking = false;;
//            }
//            //StartCrawling_Way2();


//            //return;
//        }
//        if (unityserver.isClawEnd)
//        {

//            alongpahtState = AlongPathState.AlongPathIdle;
//            nextWaypointState = NextWaypointState.NextWaypointIdle;
//            bShouldMove = false;
//            //发机器人停止指令
//            SetRobotStop();
//            //结束夹持
//            EndCrawling(tcpClientRobot);
//            //切换回Target目标点
//            Goal = 1;
//            unityserver.isClawEnd = false;
//            bShouldMove = true;
//            bRescanPath = true;
//            return;

//        }
//        */

//        //if (dTargetDistance < dClawDistanceTolerance)
//        //{

//        //}
//        //    //zpp 2020.11.9  开始夹持及其恢复寻路
//        //    if (unityserver.isClawWorking)
//        //{
//        //    if (dTargetDistance < dClawDistanceTolerance)
//        //    {

//        //        alongpahtState = AlongPathState.AlongPathIdle;
//        //        nextWaypointState = NextWaypointState.NextWaypointIdle;
//        //        bShouldMove = false;
//        //        //发机器人停止指令
//        //        SetRobotStop();
//        //        //进入自动抓取流程

//        //        StartCoroutine(StartCrawling());
//        //        //StartCrawling_Way2();


//        //        return;
//        //    }

//        //}
//        //zpp 2020.11.9  结束夹持
//        //if (unityserver.isClawEnd)
//        //{

//        //    alongpahtState = AlongPathState.AlongPathIdle;
//        //    nextWaypointState = NextWaypointState.NextWaypointIdle;
//        //    bShouldMove = false;
//        //    //发机器人停止指令
//        //    SetRobotStop();
//        //    //结束夹持
//        //    EndCrawling(tcpClientRobot);
//        //    //切换回Target目标点
//        //    Goal = 1;
//        //    unityserver.isClawEnd = false;
//        //    return;

//        //}

//    }

//    private void SetRobotSpeed(double dSpeed)
//    {
//        byte[] data = new byte[1000];
//        string strSpeed = dSpeed.ToString();
//        string messageaToServer = "chassis speed x " + strSpeed + " z 0;";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void SetRobotStop()
//    {
//        byte[] data = new byte[1000];
//        string messageaToServer = "chassis wheel w2 0 w1 0 w3 0 w4 0 ;";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void SetRobotRotate(double dRotate, double dSpeed) //控制旋转
//    {
//        byte[] data = new byte[1000];
//        string angel_string = Convert.ToString(dRotate);
//        string rotatespeed = Convert.ToString(dSpeed);
//        string messageaToServer = "chassis move vz " + rotatespeed + " z " + angel_string + ";";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void SetRobotMove(double dX, double dY, double dZ, double dXYSpeed, double dZSpeed)
//    {
//        byte[] data = new byte[1000];
//        string strDX = dX.ToString();
//        string strDY = dY.ToString();
//        string strDZ = dZ.ToString();
//        string strXYSpeed = dXYSpeed.ToString();
//        string strZSpeed = dZSpeed.ToString();
//        string messageaToServer = "chassis move x " + strDX + " y " + strDY + " z " + strDZ + " vxy " + strXYSpeed + " vz " + strZSpeed + ";";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void SetRobotForward(double x_Distance, double dSpeed)
//    {
//        byte[] data = new byte[1000];
//        string str_xDistance = x_Distance.ToString();
//        string strSpeed = dSpeed.ToString();
//        string messageaToServer = "chassis move x " + str_xDistance + " y 0 vxy " + strSpeed + ";";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void OnDestroy()
//    {
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
//    }

//    //zpp 2020.11.7  机械手臂控制
//    #region 机械手臂控制 

//    void countangle()
//    {
//        Transform trackerangle = transRobot;
//        Quaternion raw_rotation = trackerangle.rotation;
//        double myangle = trackerangle.eulerAngles.y;//自己的角度
//        trackerangle.LookAt(vcNextPoint);
//        double lookatangle = trackerangle.eulerAngles.y;//看向目标时的角度
//        trackerangle.rotation = raw_rotation;
//        Rorationangle = myangle - lookatangle; //需要旋转的角度
//        if (Rorationangle > 360)
//            Rorationangle -= 360;
//        if (Rorationangle < -360)
//            Rorationangle = 360 + Rorationangle;
//        if (Rorationangle > 180)
//            Rorationangle = -(360 - Rorationangle);
//        if (Rorationangle < -180)
//            Rorationangle = 360 + Rorationangle;
//        Rorationangle = -Rorationangle;
//    } //计算角度
//    void StartCrawling_Way2(Socket tcpclientRost) //开始抓取,方式二，用于抓取钥匙串
//    {
//        countangle();
//        //UnityEngine.Debug.Log("length:"+length);
//        if (Rorationangle > 1f || Rorationangle < -1f)
//        {

//            //UnityEngine.Debug.Log("偏转{0}" + angel);
//            //PIDText.text += "偏转:" + angel;
//            string messageaToServer = "chassis move vz 180 z " + Rorationangle + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
//                                                                             //UnityEngine.Debug.Log("旋转角度：" + messageaToServer);
//                                                                             //UnityEngine.Debug.Log("接收到服务器端的消息：" + message_angel);

//        }
//        Thread.Sleep(1500);
//        //tcpClientRobot.Send(Encoding.UTF8.GetBytes("command;"));
//        //length = length - 0.46;
//        //string len_ = length.ToString().Substring(0, 5);


//        ////机器手臂控制逻辑代码

//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_gripper open 1;")); //张开机械爪
//        Thread.Sleep(100);
//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_arm recenter;")); //机械复位
//        Thread.Sleep(800);

//        PositionCorrection(tcpclientRost); //位置矫正
//        ///开始抓取
//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;"));  //向上
//        Thread.Sleep(800);

//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_arm move x 200 y 0;")); //向前
//        Thread.Sleep(800);

//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y -200;")); //向下
//        Thread.Sleep(800);
//        //UnityEngine.Debug.Log("机械臂进入2");

//        /*    tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis wheel w2 50 w1 50 w3 50 w4 50;"));//走2cm
//            Thread.Sleep(500);

//            tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis wheel w2 0 w1 0 w3 0 w4 0;")); //停止
//            Thread.Sleep(2000);
//            */




//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_gripper close 1;"));
//        // Thread.Sleep(1000);
//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_gripper close 1;")); //开始抓取
//        Thread.Sleep(3000);

//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;")); //向上
//        Thread.Sleep(500);

//        //tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_arm move x -80 y 0;")); //向后收回
//        //Thread.Sleep(800);
//        //UnityEngine.Debug.Log("机械臂进入3");

//        //tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
//        ///////////////////////////////抓取完成
//    }

//    IEnumerator StartCrawling() //开始抓取,方式一，用于抓取水瓶
//    {
//        countangle();
//        //UnityEngine.Debug.Log("length:"+length);
//        if (Rorationangle > 1f || Rorationangle < -1f)
//        {

//            //UnityEngine.Debug.Log("偏转{0}" + angel);
//            //PIDText.text += "偏转:" + angel;
//            string messageaToServer = "chassis move vz 180 z " + Rorationangle + ";";   //Console.WriteLine("向服务器端发送消息：" + messageToServer);
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));   //向服务器端发送旋转指令
//                                                                             //UnityEngine.Debug.Log("旋转角度：" + messageaToServer);


//            //UnityEngine.Debug.Log("接收到服务器端的消息：" + message_angel);

//        }
//        UnityEngine.Debug.Log("1");
//        yield return new WaitForSeconds(1.5f);
//        //Thread.Sleep(1500);
//        //tcpClientRobot.Send(Encoding.UTF8.GetBytes("command;"));
//        //length = length - 0.46;
//        //string len_ = length.ToString().Substring(0, 5);


//        ////机器手臂控制逻辑代码

//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper open 1;")); //张开机械爪
//        ////Thread.Sleep(1000);
//        UnityEngine.Debug.Log("2");
//        yield return new WaitForSeconds(2f);

//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm recenter;")); //机械复位
//        //Thread.Sleep(1000);
//        UnityEngine.Debug.Log("3");
//        yield return new WaitForSeconds(2f);

//        ///开始抓取
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;"));  //向上
//        //Thread.Sleep(800);
//        UnityEngine.Debug.Log("4");
//        yield return new WaitForSeconds(2f);

//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 200 y 0;")); //向前
//        //Thread.Sleep(800);
//        UnityEngine.Debug.Log("5");
//        yield return new WaitForSeconds(2f);
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y -200;")); //向下

//        //UnityEngine.Debug.Log("机械臂进入2");

//        /*    tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis wheel w2 50 w1 50 w3 50 w4 50;"));//走2cm
//            Thread.Sleep(500);

//            tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis wheel w2 0 w1 0 w3 0 w4 0;")); //停止
//            Thread.Sleep(2000);
//            */

//        UnityEngine.Debug.Log("6");
//        PositionCorrection(tcpClientRobot); //位置矫正


//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper close 1;"));
//        // Thread.Sleep(1000);
//        UnityEngine.Debug.Log("7");
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_gripper close 1;")); //开始抓取
//        //Thread.Sleep(3000);
//        yield return new WaitForSeconds(5f);
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x 0 y 100;")); //向上
//        //Thread.Sleep(500);
//        UnityEngine.Debug.Log("8");
//        yield return new WaitForSeconds(1f);
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("robotic_arm move x -80 y 0;")); //向后收回
//        //Thread.Sleep(800);
//        UnityEngine.Debug.Log("9");
//        yield return new WaitForSeconds(1.5f);
//        UnityEngine.Debug.Log("10");

        
//        //切换目标User
//        Goal = 2;
//        //重新计算路径寻路
//        bShouldMove = true;
//        bRescanPath = true;
//        FirstIntoClaw = true;

//        //tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
//        ///////////////////////////////抓取完成
//    }

//    public static void EndCrawling(Socket tcpclientRost)  //结束抓取，将抓取的物品
//    {

//        tcpclientRost.Send(Encoding.UTF8.GetBytes("robotic_gripper open 1;")); //张开机械爪
//        Thread.Sleep(2000);
//    }
//    void PositionCorrection(Socket tcpclientRost)  //位置矫正
//    {
//        double length=Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(NowRobotGoal.position.x, 0, NowRobotGoal.position.z));
//        if (length >= 0.01 || length <= -0.01)
//        {
//            //调整位置
//            length = length - 0.42;
//            string len_ = length.ToString().Substring(0, 5);
//            tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis move x " + len_ + " y 0;")); //
//            Thread.Sleep(550);
//            tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis speed x " + "0" + ";"));
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis wheel w2 0 w1 0 w3 0 w4 0 ;"));   //向服务器端发送停止指令
//            Thread.Sleep(500);
//            UnityEngine.Debug.Log("juli:" + len_);
//            length = length - 0.42;
//            len_ = length.ToString().Substring(0, 5);
//            if (length >= 0.01 || length <= -0.01)
//            {
//                //调整位置
//                tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis move x " + len_ + " y 0;")); //
//                Thread.Sleep(550);
//                tcpclientRost.Send(Encoding.UTF8.GetBytes("chassis speed x " + "0" + ";"));
//                tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis wheel w2 0 w1 0 w3 0 w4 0 ;"));   //向服务器端发送停止指令
//                Thread.Sleep(500);

//            }
//        }

//    }
//    #endregion

//    //zpp 2020.11.7  Robot电量获取服务器调用回传
//    #region 电量获取
//    public static void Electricity()
//    {
//        string messageToServer = "robot battery ?;";
//        //UnityEngine.Debug.Log("向服务器端发送消息：" + messageToServer);//
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageToServer));//向服务器端发送消息

//        byte[] data = new byte[1000];
//        int length_1 = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
//        string message_1 = Encoding.UTF8.GetString(data, 0, length_1);//把字节数组转化为字符串
//        if (message_1.Contains("ok") || message_1.Contains("fail") || message_1.Contains("error"))
//            message_1 = "";
//        char[] temp1 = message_1.ToCharArray();
//        string temp2 = "";
//        for (int i = 0; i < temp1.Length; i++)
//        {
//            if (temp1[i] >= '0' && temp1[i] <= '9')
//            {
//                temp2 += temp1[i].ToString();
//            }
//        }
//        if(temp2!="")
//            unityserver.Electricityquantity=temp2;
//    }

//    #endregion

//    //zpp 2020.11.7  Robot Test模式测试代码
//    #region Robot Test模式
//    public static void RobotControlTest()
//    {
//        string len_ = "0.15";
//        //前
//        if (Hololens.RobotControlWalk == "1")
//        {
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x " + len_ + " y 0;")); 
//            Hololens.RobotControlWalk = "0";
//        }
//        //后
//        else if (Hololens.RobotControlWalk == "2")
//        {
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x -" + len_ + " y 0;")); 
//            Hololens.RobotControlWalk = "0";
//        }
//        //左
//        else if (Hololens.RobotControlWalk == "3")
//        {
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x " + "0" + " y " + len_ + ";")); 
//            Hololens.RobotControlWalk = "0";
//        }
//        //右
//        else if (Hololens.RobotControlWalk == "4")
//        {
//            tcpClientRobot.Send(Encoding.UTF8.GetBytes("chassis move x " + "0" + " y -" + len_ + ";")); 
//            Hololens.RobotControlWalk = "0";
//        }
//    }
//    #endregion
//}
