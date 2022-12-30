/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

public enum RescanState
{
    RescanIdle,
    RescanStart,
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

public class RobotControl : MonoBehaviour
{
    #region  For debug
    public bool bRescanPath = false;
    public bool bShouldMove = false;
    public bool bAutoClawCatch = false;
    #endregion

    #region For Editor
    /// <summary>
    /// 距目标点的最小阈值，单位米
    /// </summary>
    public double dTargetTolerance = 0.03;
    /// <summary>
    /// 距下一路点的最小阈值，单位米
    /// </summary>
    public double dNextwaypointTolerance = 0.03;
    public double dClawDistanceTolerance = 0.42;
    /// <summary>
    /// 目标角度最小阈值，单位度
    /// </summary>
    public double dAheadangleTolerance = 0.5;
    /// <summary>
    /// 旋转指定角度仍未到达目标角度时，允许的额外旋转次数，单位次数
    /// </summary>
    public int nRotateTimeExtra = 10;
    /// <summary>
    /// 判断是否停止的阈值，单位米
    /// </summary>
    public double dStayTolerance = 0.005;
    /// <summary>
    /// 在行进过程中距目标点应该越来越近，当出现nMaxOverstepTime次越来越远的情况时，重算路径
    /// </summary>
    public int nMaxOverstepTime = 3;
    /// <summary>
    /// 判断overstep的值，应接近于0，单位米
    /// </summary>
    public double dMinValue4Overstep = 0.001;
    /// <summary>
    /// 在行进过程中机器人超过dMaxStaySpan秒未动时，重算路径
    /// </summary>
    public float fMaxStaySpan = 5.0f;
    /// <summary>
    /// 与机器人距离小于该距离的物体移动时，重算路径，单位米
    /// </summary>
    public double dInfluenceDistance = 0.8;
    public Transform[] transObstacles;
    public Transform transUser;
    public Transform transTarget;
    public Transform transRobot;
    public string sIPstring = "192.168.2.1";
    public int nPort = 40923;
    #endregion

    #region For View
    public double dTargetDistance = 0;
    public double dNextPointDistance = 0;
    public double dAheadAngel = 0f;
    #endregion
    private Socket tcpClientRobot;
    private IPAddress ipaddressRobot;
    private EndPoint pointRobot;
    //
    private Path pathRobot;
   
    private Vector3 vcNextPoint;
    private int curPathIndex = 0;
    private Vector3[] vcObstaclesOld;
    private Vector3 vcUser;
    //private Transform transUser;
    // 重算路径相关
    public RescanState rescanState = RescanState.RescanIdle;
    // 路径相关
    public AlongPathState alongpahtState = AlongPathState.AlongPathIdle;
    // 路点相关
    public NextWaypointState nextWaypointState = NextWaypointState.NextWaypointIdle;
    // A*算法seeker
    private Seeker seeker;
    // 上一次距路点的距离,在行进过程中应该不断变小,超过N次由小变大要重算路径
    private double dNextPointDistanceOld = 0;
    // 连续离目标点走远的次数
    private int nOverstepTime = 0;
    // 行进过程中停止的时间，超过N秒的时间要重算路径
    private float fLastTime = 0;
    private float fStaySpan = 0;
    // Start is called before the first frame update
    private void Start()
    {
        seeker = GetComponent<Seeker>();
        transTarget = GameObject.Find("Target").GetComponent<Transform>();
        transRobot = GameObject.Find("Robot").GetComponent<Transform>();
        InitRobot();
        vcObstaclesOld = new Vector3[transObstacles.Length];
        vcUser = new Vector3();
    }

    private void LateUpdate()
    {
        if (bShouldMove)
        {
            if (bRescanPath)
            {
                bRescanPath = false;
                rescanState = RescanState.Rescaning;
                alongpahtState = AlongPathState.AlongPathIdle;
                nextWaypointState = NextWaypointState.NextWaypointIdle;
                SetRobotStop(); // stop the robot
                AstarPath.active.Scan();
                seeker.StartPath(transRobot.position, new Vector3(transTarget.position.x, 0, transTarget.position.z), OnPathComplete);
            }
            if (RescanState.RescanOK == rescanState)
            {

            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        // for test
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // 让机器人朝向世界坐标系的-z轴
            StartCoroutine(RobotAhead(new Vector3(transRobot.position.x, 0, transRobot.position.z - 1)));
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SetRobotMove(0.2, 0.2, 0);
        }
        if (Input.GetKeyDown(KeyCode.F12))
        {
            // 紧急停止
            SetRobotStop();
            bShouldMove = false;
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
                StartCoroutine(GotoNextPoint());
            }
        }

        // for (int index = 0; index < path.vectorPath.Count; index++)
        // {
        //     Debug.Log("path.vectorPath[" + index + "]=" + path.vectorPath[index]);

        //}
    }

    private void InitRobot()
    {
        tcpClientRobot = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ipaddressRobot = IPAddress.Parse(sIPstring);//IPAddress.Parse可以把string类型的ip地址转化为ipAddress型
        pointRobot = new IPEndPoint(ipaddressRobot, nPort);//通过ip地址和端口号定位要连接的服务器端
        tcpClientRobot.Connect(pointRobot);//建立连接
        string messageToServer = "command;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageToServer));//向服务器端发送消息
        byte[] data = new byte[1000];
        int length = tcpClientRobot.Receive(data);//这里的byte数组用来接收数据,返回值length表示接收的数据长度
        string message = Encoding.UTF8.GetString(data, 0, length);//把字节数组转化为字符串
        Debug.Log("RobotReturn of command" + message);
    }


    private double GetRobotRotate(Vector3 target)
    {
        Vector3 vcRobot2Target = new Vector3(target.x, 0, target.z) - new Vector3(transRobot.position.x, 0, transRobot.position.z);
        double f = Vector3.Angle(transRobot.forward, vcRobot2Target);
        Vector3 normal = Vector3.Cross(transRobot.forward, vcRobot2Target);
        f *= Mathf.Sign(Vector3.Dot(normal, transRobot.up));
        return f;
    }

    IEnumerator RobotAhead(Vector3 targetPoint)
    {
        // fAngel = GetRobotRotate(new Vector3(transRobot.position.x, 0, transRobot.position.z - 1));
        dAheadAngel = GetRobotRotate(targetPoint);
        int nTime = (int)Math.Abs(dAheadAngel) + nRotateTimeExtra;
        while (Math.Abs(dAheadAngel) > dAheadangleTolerance && nTime > 0)
        {
            SetRobotRotate(Math.Sign(dAheadAngel) * 3);
            nTime--;
            yield return new WaitForSeconds(0.4f);
            dAheadAngel = GetRobotRotate(targetPoint);
        }
    }

    IEnumerator GotoNextPoint()
    {
        nextWaypointState = NextWaypointState.NextWaypointStart;
        // 旋转机器人
        yield return StartCoroutine(RobotAhead(vcNextPoint));
        yield return new WaitForSeconds(0.1f);
        // 移动机器人
        nextWaypointState = NextWaypointState.NextWaypointing;
        dNextPointDistanceOld = dNextPointDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(vcNextPoint.x, 0, vcNextPoint.z));
        nOverstepTime = 0;
        fStaySpan = 0;
        fLastTime = 0;
        BackupObstaclesAndUserPos();
        SetRobotForward(dNextPointDistance, 0.4);
        // SetRobotSpeed(0.5);
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
        dTargetDistance = Vector3.Distance(new Vector3(transRobot.position.x, 0, transRobot.position.z), new Vector3(transTarget.position.x, 0, transTarget.position.z));
        if (dTargetDistance < dTargetTolerance)
        {
            alongpahtState = AlongPathState.AlongPathIdle;
            nextWaypointState = NextWaypointState.NextWaypointIdle;
            // 发机器人停止指令
            SetRobotStop();
            bShouldMove = false;
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
                }
                // is overstep
                if (dNextPointDistanceOld - dNextPointDistance < dMinValue4Overstep)
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
                    if (fLastTime < 0.0000001)
                    {
                        fLastTime = Time.time;
                        fStaySpan = 0;
                    }
                    else
                    {
                        fStaySpan += Time.time - fLastTime;
                    }
                }
                else
                {
                    fLastTime = 0;
                    fStaySpan = 0;
                }
                if (fStaySpan > fMaxStaySpan) // 判定机器人停滞
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
        if (bShouldMove)
        {
            switch (alongpahtState)
            {
                case AlongPathState.AlongPathing:
                    AlongPathing();
                    break;
            }
        }
        *//*
        if (bAutoClawCatch)
        {
            if (dTargetDistance < dClawDistanceTolerance)
            {
                alongpahtState = AlongPathState.AlongPathIdle;
                nextWaypointState = NextWaypointState.NextWaypointIdle;
                bShouldMove = false;
                // 发机器人停止指令
                SetRobotStop();
                // 进入自动抓取流程
                return;
            }
        }
        *//*
    }

    private void SetRobotSpeed(double dSpeed)
    {
        byte[] data = new byte[1000];
        string strSpeed = dSpeed.ToString();
        string messageaToServer = "chassis speed x " + strSpeed + " z 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        int length = tcpClientRobot.Receive(data);
        string message = Encoding.UTF8.GetString(data, 0, length);
        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
    }

    private void SetRobotStop()
    {
        byte[] data = new byte[1000];
        string messageaToServer = "chassis wheel w2 0 w1 0 w3 0 w4 0 ;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        int length = tcpClientRobot.Receive(data);
        string message = Encoding.UTF8.GetString(data, 0, length);
        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
    }

    private void SetRobotRotate(double dRotate)
    {
        byte[] data = new byte[1000];
        string angel_string = Convert.ToString(dRotate);
        string messageaToServer = "chassis move vz 180 z " + angel_string + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        int length = tcpClientRobot.Receive(data);
        string message = Encoding.UTF8.GetString(data, 0, length);
        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
    }

    private void SetRobotMove(double dX, double dY, double dSpeed)
    {
        byte[] data = new byte[1000];
        string strDX = dX.ToString();
        string strDY = dY.ToString();
        string strSpeed = dSpeed.ToString();
        //string messageaToServer = "chassis move x " + strDX + " y " + strDY + " vxy " + strSpeed + ";";
        string messageaToServer = "chassis move x " + strDX + " y " + strDY + ";";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        int length = tcpClientRobot.Receive(data);
        string message = Encoding.UTF8.GetString(data, 0, length);
        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
    }

    private void SetRobotForward(double dDistance, double dSpeed)
    {
        byte[] data = new byte[1000];
        string strDistance = dDistance.ToString();
        string strSpeed = dSpeed.ToString();
        //string messageaToServer = "chassis move x " + strDistance + " y 0 vxy " + strSpeed + ";";
        string messageaToServer = "chassis move x " + strDistance + " y 0;";
        tcpClientRobot.Send(Encoding.UTF8.GetBytes(messageaToServer));
        int length = tcpClientRobot.Receive(data);
        string message = Encoding.UTF8.GetString(data, 0, length);
        Debug.Log("RobotReturn of " + messageaToServer + ":" + message);
    }

    private void OnDestroy()
    {
        tcpClientRobot.Send(Encoding.UTF8.GetBytes("quit;"));
    }
}
*/