//本脚本挂载到Unity场景中的玩家或主摄像机上 玩家和主摄像机两者只能存在一个 挂载目标需要有AudioSource组件
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Wit.BaiduAip.Speech;//需要有百度云包

public class RealtimeRecord : MonoBehaviour
{
    public Text DescriptionText;//Unity场景中的文本框 需要手动加
    #region 录音参数
    [SerializeField] private float volume;//音量
    private const int VOLUME_DATA_LENGTH = 128;    //录制的声音长度
    private const int frequency = 16000; //码率
    private const int lengthSec = 600;   //录制时长
    [SerializeField] private const float minVolume = 20;//录音关闭音量值
    [SerializeField] private const float maxVolume = 50;//录音开启音量值
    private const int minVolume_Sum = 15;//小音量总和值
    #endregion
    #region 录音用到的变量
    private AudioSource micRecord;  //录制的音频
    private bool isRecord;//录音开关
    private bool isStart;//录音开启的起点
    private int minVolume_Number;//记录的小音量数量
    private int origin;//录音起点
    private int terminal;//录音终点  
    #endregion
    #region WitBaidu
    private Asr _asr;//Asr脚本创建的对象  
    private string APIKey = "cWDf0GBKZw3TUfQvL9MApCh2";//API Key 方便起见 直接黏贴在这里
    private string SecretKey = "gBTdnPfO6gqbcyYnp5GeYjMpbhYgAa5T";//API密钥 不要外传这两个key
    #endregion
    void Start()
    {
        micRecord = GetComponent<AudioSource>();
        micRecord.clip = Microphone.Start(null, true, lengthSec, frequency);
        _asr = new Asr(APIKey, SecretKey);
        StartCoroutine(_asr.GetAccessToken());
    }

    void Update()
    {
        volume = GetVolume(micRecord.clip, VOLUME_DATA_LENGTH);
        RecordOpenClose();
    }

    /// <summary>
    /// 录音自动开关
    /// </summary>
    private void RecordOpenClose()
    {
        //开
        if (GetVolume(micRecord.clip, VOLUME_DATA_LENGTH) >= maxVolume)
        {
            if (!isStart)
            {
                isStart = true;
                origin = Microphone.GetPosition(Microphone.devices[0]);
            }
            minVolume_Number = 0;
            isRecord = true;
        }
        //关
        if (isRecord && GetVolume(micRecord.clip, VOLUME_DATA_LENGTH) < minVolume)
        {
            if (minVolume_Number > minVolume_Sum)
            {
                terminal = Microphone.GetPosition(Microphone.devices[0]);
                minVolume_Number = 0;
                isRecord = false;
                isStart = false;
                byte[] playerClipByte = AudioClipToByte(micRecord.clip, origin, terminal);
                StartCoroutine(_asr.Recognize(playerClipByte, s =>
                {
                    //如果返回结果不为空 返回得到的识别结果
                    if (s.result != null && s.result.Length > 0)
                    {
                        DescriptionText.text = s.result[0];
                    }
                }));
            }
            minVolume_Number++;
        }
    }

    /// <summary>
    /// 获取音量
    /// </summary>
    /// <param name="clip">音频片段</param>
    /// <param name="lengthVolume">长度</param>
    /// <returns></returns>
    private float GetVolume(AudioClip clip, int lengthVolume)
    {
        if (Microphone.IsRecording(null))
        {
            float maxVolume = 0f;
            //用于储存一段时间内的音频信息
            float[] volumeData = new float[lengthVolume];
            //获取录制的音频的开头位置
            int offset = Microphone.GetPosition(null) - (lengthVolume + 1);
            if (offset < 0)
                return 0f;
            //获取数据
            clip.GetData(volumeData, offset);
            //解析数据
            for (int i = 0; i < lengthVolume; i++)
            {
                float tempVolume = volumeData[i];
                if (tempVolume > maxVolume)
                    maxVolume = tempVolume;
            }
            return maxVolume * 999;
        }
        return 0;
    }

    /// <summary>
    /// clip转byte[]
    /// </summary>
    /// <param name="clip">音频片段</param>
    /// <param name="origin">开始点</param>
    /// <param name="terminal">结束点</param>
    /// <returns></returns>
    public byte[] AudioClipToByte(AudioClip clip, int origin, int terminal)
    {
        float[] data;
        if (terminal > origin)
            data = new float[terminal - origin];
        else
            data = new float[clip.samples - origin + terminal];
        clip.GetData(data, origin);
        int rescaleFactor = 32767; //to convert float to Int16
        byte[] outData = new byte[data.Length * 2];
        for (int i = 0; i < data.Length; i++)
        {
            short temshort = (short)(data[i] * rescaleFactor);
            byte[] temdata = BitConverter.GetBytes(temshort);
            outData[i * 2] = temdata[0];
            outData[i * 2 + 1] = temdata[1];
        }
        return outData;
    }
}

