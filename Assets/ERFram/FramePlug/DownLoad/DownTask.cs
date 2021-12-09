using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class DownTask
{
    public string path = string.Empty;
    public uint pathCrc;
    public string tag = string.Empty;
    public string url = string.Empty;
    public Action<UnityWebRequest, string> onload;
    public Action<float, uint> onProgress;
    public bool download;
    public bool isDone;
    public float progress;
    public bool tempFile = false;


    public DownTask() { }
    public void Init(string url, string path, string tag, bool download, Action<UnityWebRequest, string> onload, Action<float, uint> progress)
    {
        Debug.Log("task " + url + " path " + path + " tag " + tag);
        this.url = url;
        this.path = path;
        this.pathCrc = Crc32.GetCrc32(path);
        this.tag = tag;
        this.onload = onload;
        this.download = download;
        this.onProgress = progress;

        this.isDone = false;
    }
    public void Reset()
    {
        url = tag = path = string.Empty;
        pathCrc = 0;
        onload = null;
        onProgress = null;
        download = false;
        isDone = false;
        progress = 0;
        tempFile = false;
    }
    public void AddLoadListener(Action<UnityWebRequest, string> onload)
    {
        this.onload -= onload;
        this.onload += onload;
    }

    public void AddProgressListener(Action<float, uint> progress)
    {
        this.onProgress -= progress;
        this.onProgress += progress;
    }

    public void OnProgress(float progress)
    {
        this.progress = progress;
        if (onProgress != null)
        {
            onProgress(progress, this.pathCrc);
        }
    }

    public void OnLoad(UnityWebRequest www)
    {
        this.isDone = true;
        if (onload != null)
        {
            onload(www, path);
        }
    }


}

public class DownTaskRunner
{
    public UnityWebRequest www;
    public DownTask task;
    public DownloadTaskHandler handler;

    private float mTimer = 5f;
    private float mTimerNum = 5f;
    //三次拉取机会
    private int mCounter = 3;
    private int mCounterNum = 3;

    public DownTaskRunner() { }

    public void Init(DownTask task)
    {
        this.task = task;
        mTimer = mTimerNum;
        mCounter = mCounterNum;
        WebRequest();
    }
    public void Reset()
    {
        if (handler != null)
        {
            handler.Dispose();
            handler = null;
        }
        task = null;
        if (www != null)
        {
            www.Dispose();
            www = null;
        }
        mTimerNum= mTimer = 5f;
        mCounter = mCounterNum = 3;

    }
    public void WebRequest()
    {
        www = UnityWebRequest.Get(this.task.url);
        string savePath = Const.ABCachePath + task.path;
        if (task.tempFile)
        {
            savePath += Const.TempFileName;
        }
     
        if (task.download)
        {
            handler = new DownloadTaskHandler(savePath, www);
            www.downloadHandler = handler;

        }
        else if (IsMedia(task.url))
        {
            var downloadHandler = new DownloadHandlerAudioClip(task.url, AudioType.MPEG);
            downloadHandler.streamAudio = true;
            www.downloadHandler = downloadHandler;
        }
        else if (IsImage(task.url))
        {
            www.downloadHandler = new DownloadHandlerTexture();
        }
        www.SendWebRequest();
    }

    public bool IsMedia(string url)
    {
        if (url.EndsWith(".mp3"))
            return true;
        return false;
    }

    public bool IsImage(string url)
    {
        if (url.EndsWith(".png") || url.EndsWith(".jpg"))
            return true;
        return false;
    }

    public void CleanTemp()
    {
        if (handler != null)
        {
            handler.CleanTemp();
        }
    }

    public void Abort()
    {
        if (www != null)
        {
            Debug.LogFormat("(DownmgrNative) Abort  {0}", task.path);
            www.Abort();
            if (handler != null)
            {
                Debug.LogFormat("(DownmgrNative) Abort After {0}", task.path);
                handler.Dispose();
                handler = null;
            }
        }
    }

    public bool Update(float deltaTime)
    {
        if (www != null && www.downloadProgress <= 0 && mTimer > 0)
        {
            mTimer -= deltaTime;
            if (mTimer < 0)
            {
                mCounter--;
                mTimer = mTimerNum;
                if (mCounter <= 0)
                {
                    Abort();
                    return true;
                }
                else
                {
                    Abort();
                    WebRequest();
                }
            }
        }
        return false;
    }


}

public class DownloadTaskHandler : DownloadHandlerScript
{
    /// <summary>
    /// 文件正式开始下载事件,此事件触发以后即可获取到文件的总大小
    /// </summary>
    public event System.Action StartDownloadEvent;

    private byte[] mData = new byte[0];

    #region 属性
    /// <summary>
    /// 下载速度,单位:KB/S 保留两位小数
    /// </summary>
    public float Speed
    {
        get
        {
            return ((int)(DownloadSpeed / 1024 * 100)) / 100.0f;
        }
    }

    /// <summary>
    /// 文件的总大小
    /// </summary>
    public ulong FileSize
    {
        get
        {
            return TotalFileSize;
        }
    }

    /// <summary>
    /// 下载进度[0,1]
    /// </summary>
    public float DownloadProgress
    {
        get
        {
            return GetProgress();
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 使用1MB的缓存,在补丁2017.2.1p1中对DownloadHandlerScript的优化中,目前最大传入数据量也仅仅是1024*1024,再多也没用
    /// </summary>
    /// <param name="path">文件保存的路径</param>
    /// <param name="request">UnityWebRequest对象,用来获文件大小,设置断点续传的请求头信息</param>
    public DownloadTaskHandler(string path, UnityWebRequest request) : base(new byte[1024 * 200])
    {
        mPath = path;
        string tempPath = mPath + ".temp";

        string outpath = System.IO.Path.GetDirectoryName(tempPath);

        if (System.IO.Directory.Exists(outpath) == false)
        {
            System.IO.Directory.CreateDirectory(outpath);
        }

        mFileStream = new FileStream(tempPath, FileMode.Append, FileAccess.Write);
        www = request;

        mLocalFileSize = (ulong)mFileStream.Length;
        CurFileSize = mLocalFileSize;

        www.SetRequestHeader("Range", "bytes=" + mLocalFileSize + "-");
        www.chunkedTransfer = true;
        www.disposeDownloadHandlerOnDispose = true;
    }
    /// <summary>
    /// 清理资源,该方法没办法重写,只能隐藏,如果想要强制中止下载,并清理资源(UnityWebRequest.Dispose()),该方法并不会被调用,这让人很苦恼
    /// </summary>
    new public void Dispose()
    {
        //GameLog.Error("(DownmgrNative) Dispose Download hander {0}");
        Clean();
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 关闭文件流
    /// </summary>
    private void Clean()
    {
        DownloadSpeed = 0.0f;
        if (mFileStream != null)
        {
            mFileStream.Close();
            mFileStream.Dispose();
            mFileStream = null;
        }
    }
    #endregion

    #region 私有继承的方法
    /// <summary>
    /// 下载完成后清理资源
    /// </summary>
    protected override void CompleteContent()
    {
        base.CompleteContent();

        Clean();

        //long lastYiledTime = System.DateTime.Now.Ticks;
        SaveTemp();
        //Debug.LogError("下载完读取完文件使用时间  ：" + (System.DateTime.Now.Ticks - lastYiledTime) / 1000 + "毫秒" + "   "+ mPath);
    }

    public void CleanTemp()
    {
        if (mFileStream != null)
        {
            mFileStream.Close();
            mFileStream.Dispose();
            mFileStream = null;
        }
        string tempFilePath = mPath + ".temp";
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }

    private void SaveTemp()
    {
        string tempFilePath = mPath + ".temp";
        if (File.Exists(tempFilePath))
        {
            //if (File.Exists(mPath))
            //{
            //    File.Delete(mPath);
            //}

            //float size = new System.IO.FileInfo(tempFilePath).Length;
            GameUtility.SafeRenameFile(tempFilePath, mPath);
            mData = GameUtility.LoadFile(mPath, true);
        }
        else
        {
            Debug.LogFormat("(DownTaskHandler) Download Failure " + tempFilePath);
        }
    }

    /// <summary>
    /// 调用UnityWebRequest.downloadHandler.data属性时,将会调用该方法,用于以byte[]的方式返回下载的数据
    /// </summary>
    /// <returns></returns>
    protected override byte[] GetData()
    {
        return mData;
    }

    /// <summary>
    /// 调用UnityWebRequest.downloadProgress属性时,将会调用该方法,用于返回下载进度
    /// </summary>
    /// <returns></returns>
    protected override float GetProgress()
    {
        return TotalFileSize == 0 ? 0 : ((float)CurFileSize) / TotalFileSize;
    }

    /// <summary>
    /// 调用UnityWebRequest.downloadHandler.text属性时,将会调用该方法,用于以string的方式返回下载的数据,目前总是返回null
    /// </summary>
    /// <returns></returns>
    protected override string GetText()
    {
        return null;
    }

    //Note:当下载的文件数据大于2G时,该int类型的参数将会数据溢出,所以先自己通过响应头来获取长度,获取不到再使用参数的方式
    protected override void ReceiveContentLengthHeader(ulong contentLength)
    {
        string contentLengthStr = www.GetResponseHeader("Content-Length");
        if (!string.IsNullOrEmpty(contentLengthStr))
        {
            try
            {
                TotalFileSize = ulong.Parse(contentLengthStr);
            }
            catch (System.FormatException e)
            {
                //UnityEngine.Debug.Log("获取文件长度失败,contentLengthStr:" + contentLengthStr + "," + e.Message);
                Debug.LogError("Down Task " + e.ToString());
                TotalFileSize = contentLength;
            }
            catch (System.Exception e)
            {
                //UnityEngine.Debug.Log("获取文件长度失败,contentLengthStr:" + contentLengthStr + "," + e.Message);
                Debug.LogError("Down Task " + e.ToString());
                TotalFileSize = contentLength;
            }
        }
        else
        {
            TotalFileSize = contentLength;
        }

        Debug.Log("DownmgrNative=>Total File Size  " + TotalFileSize + "  " + mLocalFileSize + " " + contentLength + "  mpath:"+mPath);
        //这里拿到的下载大小是待下载的文件大小,需要加上本地已下载文件的大小才等于总大小
        TotalFileSize += mLocalFileSize;
        LastTime = UnityEngine.Time.time;
        LastDataSize = CurFileSize;
        if (StartDownloadEvent != null)
        {
            StartDownloadEvent();
        }
    }

    //在2017.3.0(包括该版本)以下的正式版本中存在一个性能上的问题
    //该回调方法有性能上的问题,每次传入的数据量最大不会超过65536(2^16)个字节,不论缓存区有多大
    //在下载速度中的体现,大约相当于每秒下载速度不会超过3.8MB/S
    //这个问题在 "补丁2017.2.1p1" 版本中被优化(2017.12.21发布)(https://unity3d.com/cn/unity/qa/patch-releases/2017.2.1p1)
    //(965165) - Web: UnityWebRequest: improve performance for DownloadHandlerScript.
    //优化后,每次传入数据量最大不会超过1048576(2^20)个字节(1MB),基本满足下载使用
    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0 || www.responseCode > 400)
        {
            return false;
        }
        mFileStream.Write(data, 0, dataLength);
        CurFileSize += (uint)dataLength;

        //GameLog.Debug("Curfile Size Datalength " + CurFileSize + "/" + TotalFileSize + " path " + mPath);
        //统计下载速度
        if (UnityEngine.Time.time - LastTime >= 1.0f)
        {
            DownloadSpeed = (CurFileSize - LastDataSize) / (UnityEngine.Time.time - LastTime);
            LastTime = UnityEngine.Time.time;
            LastDataSize = CurFileSize;
        }
        return true;
    }

    ~DownloadTaskHandler()
    {
        Clean();
    }
    #endregion

    #region 私有字段
    private string mPath;//文件保存的路径
    private FileStream mFileStream;
    private UnityWebRequest www;
    private ulong mLocalFileSize = 0;//本地已经下载的文件的大小
    private ulong TotalFileSize = 0;//文件的总大小
    private ulong CurFileSize = 0;//当前的文件大小
    private float LastTime = 0;//用作下载速度的时间统计
    private float LastDataSize = 0;//用来作为下载速度的大小统计
    private float DownloadSpeed = 0;//下载速度,单位:Byte/S
    #endregion
}
