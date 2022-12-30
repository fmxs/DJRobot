using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class TestTrackerPos : MonoBehaviour
{
    public static Transform tracker;
    public static Vector3 selfPos, TargetPos;
    [SerializeField]private Vector3[] savePos = new Vector3[100];
    [SerializeField]private float[] CalPos = new float[100];
    private int i = 1, j = 0;

    private void Awake()
    {
        tracker = GameObject.Find("tracker").GetComponent<Transform>();
    }

    //void Start()
    //{ 
    //    savePos[0] = selfPos = tracker.position;//首先保存初始tracker方位
    //    InvokeRepeating("DebugPos", 0.2f, 1f);
    //}

    //private void DebugPos()
    //{
    //    SavePos(tracker.position); 
    //}

    //private void SavePos (Vector3 vec)
    //{
    //    savePos[i] = vec;
    //    CalPos[j] = savePos[i].z - savePos[i - 1].z;
    //    i++;
    //    j++;
    //}

    void Update()
    {
        selfPos = tracker.position;
    }
}
