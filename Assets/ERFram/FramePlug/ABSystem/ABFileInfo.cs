using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABFileInfo
{
    //对应xml内的abName 也作为字典的key
    public string m_Name = string.Empty;
    //文件Hash值
    public string m_Hash = string.Empty;
    //文件大小 字节为单位  转为kb:/1024  转为mb:/1024/1024
    public long m_FileLength = 0;
    //文件大小 字节为MB
    public float m_FileLengthMB = 0;
    //是否需要更新
    public bool m_Upgrade = false;
    //false:文件在streamingAssets内的 true:缓存区域
    public bool m_InCacheAsset = false;
    //ab文件的下载进度
    public float m_DownProgress = 0;
}
