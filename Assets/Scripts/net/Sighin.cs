using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.CodeDom;
using UnityEngine.SceneManagement;

public class Sighin : MonoBehaviour
{
    private string url = "http://47.93.213.106:9000/unity/login";
    public string m_info = "Nothing";
    //private Text UsernameText;
    //private Text PasswordText;
    public String Username;
    public String Password;
    public bool isRF = false;

    void Start()
    {

    }
    void Awake()
    {
        //UsernameText = GameObject.Find("Username").GetComponent<Text>();
        //PasswordText = GameObject.Find("Password").GetComponent<Text>();
    }
    void Update()
    {
        //UsernameText.text = Username;
        //PasswordText.text = Password;
        //Username = UsernameText.text;
        //Password = PasswordText.text;
        if (isRF)
        {
            SceneManager.LoadScene("RobotControl");
        }
        if (m_info.Contains("登录成功"))
        {
            m_info = "登录成功";
            isRF = true;
        }
        else
        {
            m_info = "登录失败";
        }
    }
    public void OnClickOkBtn()
    {
        //m_info = "ok";
        StartCoroutine(StartWeb());
    }
    IEnumerator StartWeb()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", Username);
        form.AddField("password", Password);
        WWW getData = new WWW(url, form);
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
    #region 参数传输
    public void Hololens_User(string Username,string password)//用户名、密码
    {
        this.Username = Username;
        this.Password = password;
    }
    #endregion
}

