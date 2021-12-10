using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/* ver1.0:
 * 对比sa的版本信息 和 缓存区的版本信息。如果缓存区为空则标记为全local
 * 如果缓存中有checkdowm的ab就用缓存中的 否则用local的  
 * 加载显示checkDown的UI面板
 * 与服务器上的版本信息作对比，然后有不一样的就下载，下载到本地后，
 * 并且读取hash值做是否正确的对比然后再标记为缓存区。
 * 如果找不到则就用前一步状态的信息
 * 最后将信息写到本地缓存区
 */
public class ABSysManager : Singleton<ABSysManager>
{
    //ab包文件的信息字典
    public Dictionary<string, ABFileInfo> ABFileInfoDic;
    //将需要下载的ABFileInfo加入，做判定
    public List<ABFileInfo> abFileDownloadList;
    //联网下载完版本文件的事件
    public Action<bool> DownloadVerFileFinsh;
    //下载单个ab包成功事件
    public Action<string> DownABFileOnLoadSuccess;
    //下载单个ab包失败事件
    public Action<string> DownABFileOnLoadFail;


    //需要下载ab的总大小  单位转为mb
    public float TotalDownSize = 0;
    //需要更新的文件数量
    public int AllNeedDownNum = 0;
    //当前已经更新完成的数量
    public int CurrentDownedNum = 0;
    //当前已经更新失败的数量
    public int CurrentDownFailNum = 0;

    //分割字符
    protected const char m_separator = '@';
    //正确判定字符
    protected const string m_inCacheTrue = "1";
    //ab文件的manifest后缀 提取出来
    protected const string m_manifestExten = ".manifest";
    //检查下载界面的ab名称
    protected const string m_checkdownloadpanel = "checkdownloadpanel";
    //远程版本信息
    protected VerInfo remoteVer = null;
    //是否已经保存过缓存区内的版本文件， 在没保存之前突然退出游戏的话保存下文件
    protected bool m_isSaveCacheVerInfo = false;
    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        ABFileInfoDic = new Dictionary<string, ABFileInfo>();

        DownmgrNative.Instance.BeginInit(CheckVerionLocalAndCache);

    }
    /// <summary>
    /// 重新对比并下载ab资源包
    /// </summary>
    public void ReDownABEvent()
    {
        List<ABFileInfo> ablistTemp = new List<ABFileInfo>();

        int listCount = abFileDownloadList.Count;
        for (int i = 0; i < listCount; i++)
        {
            ABFileInfo aBFileInfo = abFileDownloadList[i];
            if (aBFileInfo.m_FileLength == 0 )
            {
                aBFileInfo.m_DownProgress = 0;
                ablistTemp.Add(aBFileInfo);
            }
            else
            {
                TotalDownSize -= (aBFileInfo.m_FileLengthMB );
            }
        }
        abFileDownloadList = ablistTemp;
        if (abFileDownloadList.Count != CurrentDownFailNum)
        {
            Debug.LogErrorFormat("ReDown: CurrentDownFailNum:{0}  is not this same with abFileDownloadList.Count:{1}", CurrentDownFailNum, abFileDownloadList.Count);
            //如果必要可以考虑 重头开始读取需要下载的数据
            //return;
        }
        AllNeedDownNum = CurrentDownFailNum;
        CurrentDownedNum = 0;
        CurrentDownFailNum = 0;
    }
    /// <summary>
    /// 检查本地(sa和临时路径下的)的文件 然后对比服务器上的
    /// </summary>
    public void CheckVerionLocalAndCache()
    {
#if UNITY_EDITOR
        if (!Const.m_LoadFormAssetBundle)
        {
            UIManager.Instance.PopUpWindow(ConStr.CHECKDOWNLOADPANEL, true,WndLayer.loading);
            return;
        }
#endif

        VerInfo perVer = VerInfo.Read(Const.ABCachePath, false);
        if (perVer == null)
        {
#if UNITY_EDITOR
            perVer = VerInfo.Read(Const.ABLoadPath, false);
            CheckVerionLocalAndCacheNext(perVer);
#else
            DownmgrNative.Instance.LoadFromStreamingAssets(Const.FILE_VERSION, string.Empty, CheckVerionLocalAndCacheOnLoad);
#endif
        }
        else
        {
            CheckVerionLocalAndCacheNext(perVer);
        }
    }
    /// <summary>
    /// 移动平台streamingAsset内txt文本用www读取
    /// </summary>
    /// <param name="unityWebRequest"></param>
    /// <param name="str"></param>
    protected void CheckVerionLocalAndCacheOnLoad(UnityWebRequest unityWebRequest, string str)
    {
        if (DownmgrNative.Instance.DownFailed(unityWebRequest))
        {
            //加载失败
            //TODO出提示重新来一遍
            Debug.LogError("Load StreamingAsset VersionFile Failed");

            return;
        }
        else
        {
            //data[]转换为string后  读取出的第一行版本号的 无法识别切割。。。去掉第一个字符虽然可以，但是以防万一还是保存到缓存区再读出来
            //XXX:查出是用utf-8保存的话会有一个乱码(红点)在最前面，但VerInfo保存时用ascll编码的话 就不会有。
            //VerInfo perVer = VerInfo.Read(string.Empty, System.Text.Encoding.UTF8.GetString(unityWebRequest.downloadHandler.data));
            //CheckVerionLocalAndCacheNext(perVer);

            Debug.Log("Load StreamingAsset VersionFile Success");

            File.WriteAllBytes(Const.ABCachePath + Const.FILE_VERSION + Const.TempFileName, unityWebRequest.downloadHandler.data);
            VerInfo perVer = VerInfo.Read(Const.ABCachePath, true);
            CheckVerionLocalAndCacheNext(perVer);
        }
    }
    /// <summary>
    /// 读取版本文件数据
    /// </summary>
    /// <param name="perVer"></param>
    protected void CheckVerionLocalAndCacheNext(VerInfo perVer)
    {

        Debug.LogError("perVer.ver:" + perVer.ver);
        if (perVer == null)
        {
            Debug.LogError("CheckVerionSAAndPer=> Cache And Sa Ver.text is all not exist");
            return;
        }
        Const.GAME_VERSION = perVer.ver;

        if (perVer != null)
        {
            //填充字典
            foreach (KeyValuePair<string, string> fh in perVer.filehash)
            {
                ABFileInfo aBFileInfo = new ABFileInfo();
                aBFileInfo.m_Name = fh.Key;
                string[] infos = fh.Value.Split(m_separator);
                aBFileInfo.m_Hash = infos[0];
                aBFileInfo.m_FileLength = long.Parse(infos[1]);
                aBFileInfo.m_FileLengthMB = aBFileInfo.m_FileLength / 1024 / 1024f;
                aBFileInfo.m_InCacheAsset = (infos.Length <= 2 || infos[2] == null) ? false : (string.Equals(infos[2], m_inCacheTrue) ? true : false);
                aBFileInfo.m_Upgrade = (infos.Length <= 3 || infos[3] == null) ? false : (string.Equals(infos[3], m_inCacheTrue) ? true : false);
                //TODO需要安卓输出
                //Debug.LogErrorFormat("CheckVerionSAAndPer=>aBFileInfo  : name:{0}  fileLenght:{2} inCache:{3}  upgrade:{4}  hash:{1} ", aBFileInfo.m_Name, aBFileInfo.m_Hash, aBFileInfo.m_FileLength, aBFileInfo.m_InCacheAsset, aBFileInfo.m_Upgrade);
                ABFileInfoDic.Add(fh.Key, aBFileInfo);
            }
        }
        //先初始化一次AssetBundle配置，用本地的。后面下载完成后如果有更新 再重新加载一遍
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        //显示下载加载的进度界面，如果不需要更新的话 则显示加载进度
        UIManager.Instance.PopUpWindow(ConStr.CHECKDOWNLOADPANEL, true, WndLayer.loading);
    }
    /// <summary>
    /// 将ABFileInfoDic内的数据 与 服务器上的作对比，判定需要下载的
    /// </summary>
    public void CheckVersionWithRemote(Action<bool> downloadVerFileFinsh)
    {
        DownloadVerFileFinsh = downloadVerFileFinsh;
        DownmgrNative.Instance.LoadFromRemote(Const.FILE_VERSION, string.Empty, CheckVersionWithRemoteOnLoad).tempFile = true;
    }
    /// <summary>
    /// 远程版本文件下载完成
    /// </summary>
    /// <param name="unityWebRequest"></param>
    /// <param name="str"></param>
    protected void CheckVersionWithRemoteOnLoad(UnityWebRequest unityWebRequest, string str)
    {
        if (DownmgrNative.Instance.DownFailed(unityWebRequest))
        {
            //下载失败 当成没有更新直接进入游戏
            Debug.LogError("Download Remote VersionFile Failed");
            if (unityWebRequest.downloadHandler !=null)
            {
                (unityWebRequest.downloadHandler as DownloadTaskHandler).Dispose();
            }
            //加载资源 进行游戏
            DownloadVerFileFinsh?.Invoke(false);
            DownloadVerFileFinsh = null;
            return;
        }
        else
        {
            Debug.LogError("Download Remote VersionFile success");

            if (unityWebRequest.downloadHandler != null)
            {
                (unityWebRequest.downloadHandler as DownloadTaskHandler).Dispose();
            }

            remoteVer = VerInfo.Read(Const.ABCachePath, true);
            Const.GAMERemote_VERSION = remoteVer.ver;
            //对比哪些文件需要下载
            CheckNeedDownFiles(remoteVer);
            //调用事件 显示开始下载/加载资源
            DownloadVerFileFinsh?.Invoke(true);
            DownloadVerFileFinsh = null;
        }
    }
    /// <summary>
    /// 检查文件下载
    /// </summary>
    protected void CheckNeedDownFiles(VerInfo remoteVer)
    {
        if (remoteVer == null)
        {
            Debug.LogError("CheckNeedDownFiles=> remote Ver.text is not exist");
            return;
        }
        //填充字典
        foreach (KeyValuePair<string, string> fh in remoteVer.filehash)
        {
            ABFileInfo aBFileInfo = null;
            bool needUpgrade = false;
            if (!ABFileInfoDic.TryGetValue(fh.Key, out aBFileInfo) || aBFileInfo == null)
            {
                //新文件 需要加入下载列表
                aBFileInfo = new ABFileInfo();
                aBFileInfo.m_Name = fh.Key;
                string[] infos = fh.Value.Split(m_separator);
                aBFileInfo.m_Hash = infos[0];
                aBFileInfo.m_FileLength = long.Parse(infos[1]);
                aBFileInfo.m_FileLengthMB = aBFileInfo.m_FileLength / 1024 / 1024f;
                //新加的文件 只能使用在缓存内的
                aBFileInfo.m_InCacheAsset = true;
                aBFileInfo.m_Upgrade = true;
                //TODO需要安卓输出
                //Debug.LogErrorFormat("CheckNeedDownFiles=>NEW=>aBFileInfo  : name:{0}  fileLenght:{2} inCache:{3}  upgrade:{4}  hash:{1} ", aBFileInfo.m_Name, aBFileInfo.m_Hash, aBFileInfo.m_FileLength, aBFileInfo.m_InCacheAsset, aBFileInfo.m_Upgrade);

                ABFileInfoDic.Add(fh.Key, aBFileInfo);
                needUpgrade = true;

            }
            else
            {
                string[] infos = fh.Value.Split(m_separator);
                long m_FileLen = long.Parse(infos[1]);
                //判断是否需要加入下载列表   hash不对 文件长度不对
                if (aBFileInfo.m_Upgrade ||  aBFileInfo.m_Hash != infos[0] || m_FileLen == 0 || aBFileInfo.m_FileLength != m_FileLen)
                {
                    if (aBFileInfo.m_Hash != infos[0] && aBFileInfo.m_Upgrade)
                    {
                        //hash值对不上 但是标记为需要更新的 那就有可能是之前文件下载了一部分，然后再次打开游戏时线上这文件又有更新了，将本地可能存在的temp文件删除，以免断点续传影响
                        GameUtility.SafeDeleteFile(Const.ABCachePath + aBFileInfo.m_Name+ Const.TempSuffix);
                    }

                    aBFileInfo.m_InCacheAsset = true;
                    aBFileInfo.m_Upgrade = true;
                    aBFileInfo.m_Hash = infos[0];
                    if (aBFileInfo.m_FileLength != m_FileLen)
                    {
                        aBFileInfo.m_FileLength = m_FileLen;
                        aBFileInfo.m_FileLengthMB = aBFileInfo.m_FileLength / 1024 / 1024f;
                    }

                    needUpgrade = true;
                    //TODO需要安卓输出
                    //Debug.LogErrorFormat("CheckNeedDownFiles=>Upgrade=>aBFileInfo  : name:{0}  ", aBFileInfo.m_Name);
                }
            }
            if (needUpgrade)
            {
                if (abFileDownloadList == null) abFileDownloadList = new List<ABFileInfo>();
                abFileDownloadList.Add(aBFileInfo);
                AllNeedDownNum += 1;
                if (aBFileInfo.m_FileLength == 0)
                {
                    Debug.LogErrorFormat("CheckNeedDownFiles=>aBFileInfo=> m_FileLength is 0!!: name:{0} ", aBFileInfo.m_Name);
                }
                TotalDownSize += (aBFileInfo.m_FileLengthMB);
            }
        }
        //遍历需要更新的文件，如果文件或者.manifest单独有一个更新的话 判定另外一个是不是在缓存区内，如果不是的话 也需要加入下载，配对使用的
        int downListCount = abFileDownloadList == null ? 0 : abFileDownloadList.Count;
        for (int i = 0; i < downListCount; i++)
        {
            ABFileInfo aBFITemp = abFileDownloadList[i];
            string abOtherName = string.Empty;
            if (aBFITemp.m_Name.IndexOf(m_manifestExten) >0)
            {
                abOtherName = aBFITemp.m_Name.Replace(m_manifestExten, "");
            }
            else
            {
                abOtherName = aBFITemp.m_Name + m_manifestExten;
            }

            ABFileInfo aBFileInfo = null;
            if (!ABFileInfoDic.TryGetValue(abOtherName, out aBFileInfo) || aBFileInfo==null) {
                Debug.LogErrorFormat("CheckNeedDownFiles=>Get other match file is null !!: name:{0} ", abOtherName);
                continue;
            }
            //不需要升级并且不在缓存文件区域的 也需要加入下载列表
            if (!aBFileInfo.m_Upgrade && !aBFileInfo.m_InCacheAsset)
            {
                aBFileInfo.m_InCacheAsset = true;
                aBFileInfo.m_Upgrade = true;

                abFileDownloadList.Add(aBFileInfo);
                AllNeedDownNum += 1;
                if (aBFileInfo.m_FileLength == 0)
                {
                    Debug.LogErrorFormat("CheckNeedDownFiles=>aBFileInfo=> m_FileLength is 0!!: name:{0} ", aBFileInfo.m_Name);
                }
                TotalDownSize += (aBFileInfo.m_FileLengthMB);
            }
        }
        Debug.LogFormat("CheckNeedDownFiles=> all need down file num:{0}  size:{1}", AllNeedDownNum, TotalDownSize);
    }
    /// <summary>
    /// 开始下载ab文件 如果下载有checkdown的话 需要改下下载的名字
    /// </summary>
    public void StartDownABFile(Action<string> successEvent, Action<string> failEvent)
    {
        if (abFileDownloadList==null)
        {
            return;
        }
        DownABFileOnLoadSuccess = successEvent;
        DownABFileOnLoadFail = failEvent;
        for (int i = 0; i < abFileDownloadList.Count; i++)
        {
            ABFileInfo aBFileInfo = abFileDownloadList[i];

            DownTask downTask = DownmgrNative.Instance.LoadFromRemote(aBFileInfo.m_Name, aBFileInfo.m_Name, DownABFileOnLoad);
            if (aBFileInfo.m_Name.IndexOf(m_checkdownloadpanel) >= 0  && File.Exists(Const.ABCachePath+ m_checkdownloadpanel))
            {
                //修改为添加后缀 防止冲突
                downTask.tempFile = true;
            }
        }
    }
    /// <summary>
    /// ab文件下载完成回调
    /// </summary>
    /// <param name="unityWebRequest"></param>
    /// <param name="str"></param>
    protected void DownABFileOnLoad(UnityWebRequest unityWebRequest, string str)
    {
        ABFileInfo aBFileInfo = ABFileInfoDic[str];
        if (DownmgrNative.Instance.DownFailed(unityWebRequest)) 
        {
            DownAbFailEvnet(aBFileInfo, str);
            return;
        }
        else
        {
            //XXX:文件下载完成后不保存data,考虑到下载的ab包不一定马上就会用，就让他自己释放掉。有需要的话可以按需保存


            //验证下下载文件的hash 是否相同
            string hash = DownmgrNative.Instance.GetHash(unityWebRequest.downloadHandler.data);
            //hash值对不上 下载的文件有问题 加入到失败里面去
            if (aBFileInfo.m_Hash !=hash)
            {
                Debug.LogError("DownABFileOnLoad Fail, hash is not Equal  name:" + str + "  local hash: " + aBFileInfo.m_Hash + "  remote hash :" + hash);
                DownAbFailEvnet(aBFileInfo, str);

                return;
            }
            aBFileInfo.m_DownProgress = 1;
            aBFileInfo.m_Upgrade = false;

            //(unityWebRequest.downloadHandler as DownloadTaskHandler).Dispose();
            CurrentDownedNum++;
            DownABFileOnLoadSuccess?.Invoke(str);
        }
    }
    protected void DownAbFailEvnet(ABFileInfo aBFileInfo, string str)
    {
        //下载失败，最后统计失败的数量，然后出提示 统一再来下载一次
        CurrentDownFailNum++;
        //将字典内file长度修改为0
        aBFileInfo.m_FileLength = 0;
        aBFileInfo.m_FileLengthMB = 0;
        aBFileInfo.m_InCacheAsset = false;
        aBFileInfo.m_DownProgress = 0;


        DownABFileOnLoadFail?.Invoke(str);
    }
    /// <summary>
    /// 保存缓存文件到缓存区
    /// </summary>
    public void SaveCacheVerFile(bool onDestroy = false)
    {
        if (!onDestroy)
        {
            m_isSaveCacheVerInfo = true;
        }
        if (onDestroy && m_isSaveCacheVerInfo)
        {
            Debug.Log("SaveCacheVerFile=>already save");
            return;
        }
        GameUtility.SafeDeleteFile(Const.ABCachePath + Const.FILE_VERSION + Const.TempFileName);
        if (AllNeedDownNum<=0)
        {
            Debug.Log("SaveCacheVerFile=> not file Download");
            return;
        }
        DownABFileOnLoadSuccess = null;
        DownABFileOnLoadFail = null;

        VerInfo vernew = new VerInfo();

        foreach (KeyValuePair<string, ABFileInfo> fh in ABFileInfoDic)
        {
            ABFileInfo aBFileInfo = fh.Value;
            //文件名  hash值@文件长度@是否缓存区@是否需要升级
            vernew.SaveInFileHash(fh.Key, aBFileInfo.m_Hash + m_separator+ aBFileInfo.m_FileLength + m_separator + (aBFileInfo.m_InCacheAsset ? 1 : 0) + m_separator + (aBFileInfo.m_Upgrade ? 1 : 0)) ;
        }

        if (ABSysManager.Instance.CurrentDownFailNum == 0)
        {
            vernew.ver = Const.GAMERemote_VERSION;
        }
        else
        {
            vernew.ver = Const.GAME_VERSION;
        }
        GameUtility.SafeDeleteFile(Const.ABCachePath +  Const.FILE_VERSION);

        vernew.SaveToPath(Const.ABCachePath);
        Debug.Log("缓存版本文件写入成功");
    }
    /// <summary>
    /// 重命名CheckDown的UI资源
    /// </summary>
    public void ReNameCheckDownUI()
    {
#if UNITY_EDITOR
        if (!Const.m_LoadFormAssetBundle)
        {
            return;
        }
#endif
        //清空远端版本文件信息
        remoteVer = null;

        //如果有更新checkdown的话 释放掉资源后把名字改回去

        string destFilePathName = Const.ABCachePath + m_checkdownloadpanel;
        string tempFilePathName = destFilePathName + Const.TempFileName;
        if (File.Exists(tempFilePathName))
        {
            GameUtility.SafeRenameFile(tempFilePathName, destFilePathName);
        }

        destFilePathName = Const.ABCachePath + m_checkdownloadpanel + m_manifestExten;
        tempFilePathName = destFilePathName + Const.TempFileName;
        if (File.Exists(tempFilePathName))
        {
            GameUtility.SafeRenameFile(tempFilePathName, destFilePathName);
        }
    }
    /// <summary>
    /// 获得当前已下载的大小除以总大小获得比例
    /// </summary>
    /// <returns></returns>
    public float GetAllDownFileProgress()
    {
        float progress = 0;
        float cursize = 0;
        ABFileInfo aBFileInfo = null;
        for (int i = 0; i < abFileDownloadList.Count; i++)
        {
            aBFileInfo = abFileDownloadList[i];

            if (aBFileInfo.m_DownProgress >=1)
            {
                cursize += aBFileInfo.m_FileLengthMB;
            }
        }
        cursize += DownmgrNative.Instance.AllRunnerDownFileSize();
        progress = cursize / TotalDownSize;
        return progress;
    }
    /// <summary>
    /// 清空abFileDownloadList列表
    /// </summary>
    public void ClearabFileDownloadList()
    {
        if (abFileDownloadList!=null)
        {
            abFileDownloadList.Clear();
        }
    }

}
