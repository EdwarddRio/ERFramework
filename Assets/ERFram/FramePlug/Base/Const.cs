using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Const 
{
    //可以拿来做数据显示
    //本地端版本号
    public static string GAME_VERSION = "001";
    //远程端版本号
    public static string GAMERemote_VERSION = "001";

    //是否从ab包加载资源使用(仅限编辑器有效)
    public static bool m_LoadFormAssetBundle = true;
    //是否打开远程资源检测下载
    public static bool CheckRemoteFileDown = true;

    //下载的文件只能做临时文件的，不能直接替换掉缓存区的 名字最后加TTT
    public static string TempFileName = "TTT";
    //.temp后缀字符串
    public static string TempSuffix = ".temp";
    //ab包加密长度
    public static ulong ABEncryptLen = 5;
    /// <summary>
    /// 版本文件名称
    /// </summary>
    public static string FILE_VERSION
    {
        get
        {
            return "allver.ver.txt";
        }
    }
    /// <summary>
    /// ab包在StreamingAssets加载路径
    /// 在移动平台 streamingAssets内的文件 用下载方法来读取数据
    /// </summary>
    private static string _AbLoadPath = string.Empty;
    public static string ABLoadPath
    {
        get
        {
            if (string.Equals (_AbLoadPath,string.Empty))
            {
#if UNITY_ANDROID || UNITY_EDITOR
                _AbLoadPath = Application.streamingAssetsPath+ "/ABDir/";
#else
                _AbLoadPath = "file://" + Application.streamingAssetsPath + "/ABDir/";
#endif
            }
            return _AbLoadPath;
        } 
    }
    /// <summary>
    /// ab包在StreamingAssets加载路径 Editor编译器里用
    /// </summary>
    public static string ABLoadPathByEditor
    {
        get
        {
            return  Application.streamingAssetsPath+ "/ABDir/";
        }
    }
    /// <summary>
    /// ab包本地缓存路径
    /// </summary>
    private static string _ABCachePath = string.Empty;
    public static  string ABCachePath {
        get
        {
            if (string.Equals(_ABCachePath, string.Empty))
            {
                _ABCachePath = System.IO.Path.Combine(Application.persistentDataPath, "vercache") +"/";
                GameUtility.CheckFileAndCreateDirWhenNeeded(_ABCachePath);
            }
            return _ABCachePath;
        }
    }
    /// <summary>
    /// ab包远程路径
    /// </summary>
#if UNITY_IOS
    private static readonly string _ABRemotePath = "http://127.0.0.1:1818/IOS/";
#elif UNITY_ANDROID
    private static readonly string _ABRemotePath = "http://127.0.0.1:1818/Android/";
#else
    private static readonly string _ABRemotePath = "http://127.0.0.1:1818/Editor/";
#endif

    public static string ABRemotePath
    {
        get
        {
            return _ABRemotePath;
        }
    }
}
