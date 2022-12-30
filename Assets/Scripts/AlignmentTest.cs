using System;
using UnityEngine;
using UnityEngine.UI;
public class AlignmentTest : MonoBehaviour
{
    public GameObject Controller;// 手柄 steamVR
    public GameObject Avatar;// 玩家的头盔 steamvr
    public Vector3 TrackerPosition;// 服务器给的tracker坐标
    public int trackerID;// 服务器给的tracker的ID
    public Text GetText;// 服务器传回的text
    private Transform transController;// GameObject的属性之一
    private Transform transAvatar;
    private Transform[] transTarget = new Transform[10];
    private Vector3 translateVector, AbsoluteVector, TargetVector;

    private void Awake()
    {
        // 假设玩家全程 戴着hololens 拿着手柄或者Tracker
        // 我们将手柄和戴着hololens的玩家看作处于同一个点
        // 开始运行后 手柄在unity中处于某一个绝对坐标 
        // 这个绝对坐标可以反映hololens原点（玩家）在Unity坐标系中的位置
        transController = Controller.transform;
        transAvatar = Avatar.transform;
        // 偏移向量 = steamVR原点（头盔) - 手柄（hololens）绝对坐标 
        translateVector = transAvatar.position - transController.position;
    }
    void Start()
    {
        GetServerTrackerPosition();
        SetTrackerPosition();
    }

    /// <summary>
    /// 获得服务器传来的tracker位置
    /// </summary>
    public void GetServerTrackerPosition()
    {
        string str = GetText.text;
        string[] splitStr = str.Split(new char[] { 'H', 'O', 'X', 'Y', 'Z', 'L', 'S' });// 分割字符串 本来是字节流传过来的

        int id;
        decimal x, y, z;
        id = Convert.ToInt32(splitStr[0]);
        x = Convert.ToDecimal(splitStr[1]);
        y = Convert.ToDecimal(splitStr[2]);
        z = Convert.ToDecimal(splitStr[3]);

        trackerID = id;
        TrackerPosition = new Vector3((float)x, (float)y, (float)z);
    }

    /// <summary>
    /// 将tracker在LightHouse中的位置 转变为在hololens坐标系中的位置
    /// </summary>
    public void SetTrackerPosition()
    {
        // tracker和头盔处于同一个世界坐标系中 它们的位置差可看作是绝对的
        // 绝对向量 = tracker - 头盔
        AbsoluteVector = TrackerPosition - transAvatar.position;

        // 从hololens指向tracker的目标向量 = 绝对向量 + 偏移向量 
        TargetVector = AbsoluteVector + translateVector;

        // 以Hololens为原点 以TargetVector为距离 在目标点生成Tracker全息体
        // 最终tracker位置 = 手柄（玩家）的位置 + 目标向量
        transTarget[trackerID].position = transController.position + TargetVector;

        // Tracker全息体的旋转需要结合实际物体的角度去考虑
        //transTarget[trackerID].rotation = ?
    }

}
