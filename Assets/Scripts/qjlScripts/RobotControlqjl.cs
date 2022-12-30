//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Pathfinding;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System;

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

//public class RobotControlqjl : MonoBehaviour
//{
//    #region  For debug
//    public bool bRescanPath = false;
//    public bool bShouldMove = false;
//    public bool bAutoClawCatch = false;
//    #endregion

//    #region For Editor
//    /// <summary>
//    /// 距目标点的最小阈值，单位米
//    /// </summary>
//    public double dTargetTolerance = 0.03;
//    /// <summary>
//    /// 距下一路点的最小阈值，单位米
//    /// </summary>
//    public double dNextwaypointTolerance = 0.03;
//    public double dClawDistanceTolerance = 0.42;
//    /// <summary>
//    /// 目标角度最小阈值，单位度
//    /// </summary>
//    public double dAheadangleTolerance = 0.5;
//    /// <summary>
//    /// 旋转指定角度仍未到达目标角度时，允许的额外旋转次数，单位次数
//    /// </summary>
//    public int nRotateTimeExtra = 10;
//    /// <summary>
//    /// 判断是否停止的阈值，单位米
//    /// </summary>
//    public double dStayTolerance = 0.005;
//    /// <summary>
//    /// 在行进过程中距目标点应该越来越近，当出现nMaxOverstepTime次越来越远的情况时，重算路径
//    /// </summary>
//    public int nMaxOverstepTime = 3;
//    /// <summary>
//    /// 判断overstep的值，应接近于0，单位米
//    /// </summary>
//    public double dMinValue4Overstep = 0.001;
//    /// <summary>
//    /// 在行进过程中机器人超过dMaxStaySpan秒未动时，重算路径
//    /// </summary>
//    public float fMaxStaySpan = 5.0f;
//    /// <summary>
//    /// 与机器人距离小于该距离的物体移动时，重算路径，单位米
//    /// </summary>
//    public double dInfluenceDistance = 0.8;
//    public Transform[] transObstacles;
//    public Transform transUser;
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
//    private Socket tcpClientRobot;
//    private IPAddress ipaddressRobot;
//    private EndPoint pointRobot;
//    //
//    private Path pathRobot;
//    private Transform transTarget;
//    private Transform transRobot;
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
//    private double move_x;
//    private double move_y;
//    private double move_angle;
//    // Start is called before the first frame update
//    private void Start()
//    {
//        seeker = GetComponent<Seeker>();
//        transTarget = GameObject.Find("Target").GetComponent<Transform>();
//        transRobot = GameObject.Find("Robot").GetComponent<Transform>();
//        InitRobot();
//        vcObstaclesOld = new Vector3[transObstacles.Length];
//        vcUser = new Vector3();
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
//                StopCoroutine("GotoNextPoint");
//                SetRobotStop(); // stop the robot
//                AstarPath.active.Scan();
//                seeker.StartPath(transRobot.position, new Vector3(transTarget.position.x, 0, transTarget.position.z), OnPathComplete);
//            }
//            if (RescanState.RescanOK == rescanState)
//            {

//            }
//        }
//    }

//    // Update is called once per frame
//    private void Update()
//    {
//        // for test
//        if (Input.GetKeyDown(KeyCode.F1))
//        {
//            // 让机器人朝向世界坐标系的-z轴
//            StartCoroutine(RobotAhead(new Vector3(transRobot.position.x, 0, transRobot.position.z - 1)));
//        }
//        if (Input.GetKeyDown(KeyCode.F2))
//        {
//            SetRobotMove(-0.077512513362223, -0.157031729942777, 0.2, 0.4);
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

//    #region xjy修改2020.11.3
//    public Transform Tracker, Target;//Tracker和目标点
//    private int rotateAngle = 1;//每次旋转值
//    private float MAX_ROTATION = 2f;//给定的阈值
//    private string angel_string;//将rotateAngle变量保存为字符型
//    private string RobotRotateString = "chassis move vz 360 z ";//声明机器人速度的字符指令

//    /// <summary>
//    /// Get the rotation value of robot
//    /// </summary>
//    /// <param name="self"></param>
//    /// <param name="selfPos"></param>
//    /// <param name="targetPos"></param>
//    /// <returns></returns>
//    private float GetRobotRotate(Transform self, Vector3 selfPos, Vector3 targetPos)
//    {
//        Vector3 dir = new Vector3(targetPos.x, 0, targetPos.z) - new Vector3(selfPos.x, 0, selfPos.z);//需要行走的距离
//        float angle = Vector3.Angle(dir, self.forward);//需要旋转的角度
//        return angle;
//    }

//    /// <summary>
//    /// Judge target on the left or right
//    /// </summary>
//    /// <param name="self"></param>
//    /// <param name="target"></param>
//    /// <returns>返回值负左正右</returns>
//    private int GetRotationLeftOrRight(Transform self, Transform target)
//    {
//        float flag = Vector3.Dot(self.right, target.position - self.position);//返回值为正时,目标在自己的右方,反之在左方
//        return flag <= 0 ? 1 : -1;
//    }

//    /// <summary>
//    /// Rotate the robot one float by one float
//    /// </summary>
//    private void RotationOneByOne()
//    {
//        float angle = GetRobotRotate(Tracker, Tracker.position, Target.position);
//        int flag = GetRotationLeftOrRight(Tracker, Target);
//        angel_string = Convert.ToString(rotateAngle);
//        if (flag <= 0)//当flag<=0 说明目标在自己的左边 所以左转
//        {
//            while (angle > MAX_ROTATION)
//            {
//                tcpClientRobot.Send(Encoding.UTF8.GetBytes(RobotRotateString + angel_string + ";"));   //向服务器端发送旋转指令
//                System.Threading.Thread.Sleep(500);
//                angle -= rotateAngle;//angle需要自减直到小于给定的阈值
//            }
//        }
//        else//同上 此时右转
//        {
//            while (angle > MAX_ROTATION)
//            {
//                tcpClientRobot.Send(Encoding.UTF8.GetBytes(RobotRotateString + angel_string + ";"));   //向服务器端发送旋转指令
//                System.Threading.Thread.Sleep(500);//延时
//                angle -= rotateAngle;
//            }
//        }
//    }
//    #endregion

//    IEnumerator RobotAhead(Vector3 targetPoint)
//    {
//        dAheadAngel = GetRobotRotate(targetPoint);//获取角度
//        int nTime = nRotateTimeExtra; // 调整旋转次数
//        SetRobotRotate(dAheadAngel);
//        yield return new WaitForSeconds(2.0f);
//        //dAheadAngel = GetRobotRotate(targetPoint);//获取角度
//        //while (Math.Abs(dAheadAngel) > dAheadangleTolerance && nTime > 0)
//        //{
//        //    SetRobotRotate(Math.Sign(dAheadAngel) * 2); //math.sign获取正负，旋转
//        //    nTime--; //旋转次数减一
//        //    yield return new WaitForSeconds(2.0f); //延时s
//        //    dAheadAngel = GetRobotRotate(targetPoint);
//        //}
//    }

//    IEnumerator GotoNextPoint() //向下一点运动
//    {
//        nextWaypointState = NextWaypointState.NextWaypointStart;
//        // 旋转机器人
//        yield return StartCoroutine("RobotAhead", vcNextPoint);
//        //yield return new WaitForSeconds(2.0f);
//        // 移动机器人
//        nextWaypointState = NextWaypointState.NextWaypointing;
//        dNextPointDistanceOld = dNextPointDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(vcNextPoint.x, 0, vcNextPoint.z));
//        nOverstepTime = 0;
//        tsStay = TimeSpan.Zero;
//        dStay = tsStay.TotalSeconds;
//        dtLastTime = DateTime.MinValue;
//        BackupObstaclesAndUserPos();
//        move_angle = GetRobotRotate(vcNextPoint);
//        move_x = 1.0 * (dNextPointDistance * Math.Cos(move_angle * Math.PI / 180));
//        move_y = 1.25 * (dNextPointDistance * Math.Sin(move_angle * Math.PI / 180));
//        SetRobotMove(move_x, move_y, 0.2, 0.4);
//        yield return new WaitForSeconds(0.5f);
//        // SetRobotSpeed(0.5);
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
//        dTargetDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(transTarget.position.x, 0, transTarget.position.z));
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
//        if (bAutoClawCatch)
//        {
//            if (dTargetDistance < dClawDistanceTolerance)
//            {
//                alongpahtState = AlongPathState.AlongPathIdle;
//                nextWaypointState = NextWaypointState.NextWaypointIdle;
//                bShouldMove = false;
//                // 发机器人停止指令
//                SetRobotStop();
//                // 进入自动抓取流程
//                return;
//            }
//        }
//        */
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

//    private void SetRobotRotate(double dRotate) //控制旋转
//    {
//        byte[] data = new byte[1000];
//        string angel_string = Convert.ToString(dRotate);
//        string messageaToServer = "chassis move vz 180 z " + angel_string + ";";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void SetRobotMove(double dX, double dY, double dXSpeed, double dYSpeed)
//    {
//        byte[] data = new byte[1000];
//        string strDX = dX.ToString();
//        string strDY = dY.ToString();
//        string strXSpeed = dXSpeed.ToString();
//        string strYSpeed = dYSpeed.ToString();
//        //string messageaToServer = "chassis move x " + strDX + " y " + strDY + " vxy " + strSpeed + ";";
//        string messageaToServer = "chassis move x " + strDX + " y " + strDY + " vx " + strXSpeed + " vy " + strYSpeed + ";";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void SetRobotForward(double x_Distance, double y_Distance, double dSpeed)
//    {
//        byte[] data = new byte[1000];
//        string str_xDistance = x_Distance.ToString();
//        string str_yDistance = y_Distance.ToString();
//        string strSpeed = dSpeed.ToString();
//        //string messageaToServer = "chassis move x " + strDistance + " y 0 vxy " + strSpeed + ";";
//        string messageaToServer = "chassis move x " + x_Distance + " y " + y_Distance + ";";
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
//        int length = tcpClientRobot.Receive(data);
//        string message = Encoding.UTF8.GetString(data, 0, length);
//        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
//    }

//    private void OnDestroy()
//    {
//        tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
//    }
//}
