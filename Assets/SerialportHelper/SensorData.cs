using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorData : MonoBehaviour 
{
    private bool isTouched;//触摸模块
    private int C2H6O;//乙醇模块
    private int MaxC2H6O = 500;
	void Start () 
    {
		
	}
	
    private bool Touched()
    {
        if (Listener.isTouched == 1)
            return true;
        return false;
    }

    private void Alcohol()
    {
        C2H6O = Listener.C2H6O;
        if(C2H6O >= MaxC2H6O)
        {
            Debug.Log("酒精浓度超出阈值!");
        }
    }
}
