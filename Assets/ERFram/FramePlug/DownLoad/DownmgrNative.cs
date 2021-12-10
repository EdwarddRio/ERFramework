using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class DownmgrNative : Singleton<DownmgrNative>
{
    //类对象池
    protected ClassObjectPool<DownTask> m_DownTashPool = new ClassObjectPool<DownTask>(50);
    protected ClassObjectPool<DownTaskRunner> m_DownTaskRunnerPool = new ClassObjectPool<DownTaskRunner>(50);

    protected Queue<DownTask> task = new Queue<DownTask>();
    protected List<DownTaskRunner> runners = new List<DownTaskRunner>();
    protected Dictionary<string, DownTask> taskList = new Dictionary<string, DownTask>();
    protected float runnerDownSpeed = 0;
    protected float runnerDownSize = 0;
    public bool GetRunnerDownSize { private get; set; } = false;
    public int taskMaxCount { get; private set; }
    //用来获取hash值
    public System.Security.Cryptography.SHA1Managed sha1 { get; private set; }
    public TaskState taskState { get; private set; }
    public int taskCount { get { return task.Count; } }
    public int runnerCount { get { return runners.Count; } }

    public DownmgrNative()
    {
        sha1 = new System.Security.Cryptography.SHA1Managed();
        taskState = new TaskState();

    }
    public string GetHash(byte[] datas)
    {
        return Convert.ToBase64String(sha1.ComputeHash(datas));
    }

    public float GetProgress(string file)
    {
        if (taskList.ContainsKey(file))
        {
            return taskList[file].progress;
        }
        return 0.0f;
    }

    public void ClearTask()
    {
        foreach (var t in task)
        {
            DownTask dtTemp = null;
            if (taskList.TryGetValue(t.url,out dtTemp) && dtTemp!=null)
            {
                RecycleDownTaskAndList(t.url, dtTemp);
            }

        }
        task.Clear();
        for (int i = 0; i < runners.Count; i++)
        {
            DownTaskRunner dtr = runners[i];
            DownTask dt = dtr.task;
            if (dt.download)
            {
                if (taskList.ContainsKey(dt.url))
                {
                    RecycleDownTaskAndList(dt.url, dt);
                }
                dtr.Abort();
                RecycleDownTaskRunner(dtr);
                runners.Remove(dtr);
                i--;
            }
        }
    }
    private long updateTime = 0;
    private bool reGetSpeed = false;
    public void Update()
    {
        if (DateTime.Now.Ticks - updateTime >= 1)
        {
            updateTime = DateTime.Now.Ticks;
            reGetSpeed = true;
            runnerDownSpeed = 0;
        }
        if (GetRunnerDownSize)
        {
            runnerDownSize = 0;
        }

        //处理可多线加载的任务
        if (runners.Count < taskMaxCount && task.Count > 0)
        {
            int count = Math.Min(taskMaxCount - runners.Count, task.Count);
            for (int i = 0; i < count; i++)
            {
                DownTaskRunner downTaskRunner = m_DownTaskRunnerPool.Spawn(true);
                downTaskRunner.Init(task.Dequeue());
                runners.Add(downTaskRunner);
            }
        }

        for (int i = 0; i < runners.Count; i++)
        {
            DownTaskRunner cur = runners[i];
            if (cur.www.isDone)
            {
                //下载失败
                if (cur.www.isHttpError || cur.www.isNetworkError)
                {
                    //Debug.LogErrorFormat("Download {0} Error {1}", cur.www.url, cur.www.error);

                    string fileName = cur.task.path;
                    string[] names = cur.task.path.Split('/');
                    if (names.Length > 0)
                    {
                        fileName = names[names.Length - 1];
                    }

                    if (cur.www.error.Contains("HTTP/1.1 416 Range"))
                    {
                        cur.CleanTemp();
                    }

                    cur.Abort();

                    taskState.downloadcount++;

                    if (cur.task.onProgress != null)
                    {
                        cur.task.OnProgress(0);
                    }

                    cur.task.onload(cur.www, cur.task.tag);

                    if (cur.task.download)
                    {
                        Debug.LogFormat("DownmgrNative=> down fail {0} : {1}", cur.www.url, cur.www.downloadProgress);
                    }

                    RecycleDownTaskAndList(cur.task.url);
                    RecycleDownTaskRunner(cur);
                    runners.RemoveAt(i);
                    i--;
                }
                else
                {
                    //下载成功
                    taskState.downloadcount++;
                    if (cur.task.onProgress != null)
                    {
                        cur.task.OnProgress(cur.www.downloadProgress);
                    }
                    cur.task.onload(cur.www, cur.task.tag);

                    if (cur.task.download)
                    {
                        Debug.LogFormat("DownmgrNative=> down finish {0} : {1}", cur.www.url, cur.www.downloadProgress);
                    }

                    RecycleDownTaskAndList(cur.task.url);
                    RecycleDownTaskRunner(cur);
                    runners.RemoveAt(i);
                    i--;
                }
                break;
            }
            else
            {
                if (GetRunnerDownSize)
                {
                    runnerDownSize += cur.handler.CurrentDownFileSize / 1024 / 1024f;
                }

                //下载中
                if (cur.task.onProgress != null && !cur.www.isDone)
                {
                    cur.task.OnProgress(cur.www.downloadProgress);
                    if (cur.task.download)
                    {
                        Debug.LogFormat("DownmgrNative=> down progress {0} : {1}", cur.task.path, cur.www.downloadProgress);
                    }

                }

                //超过三次拉取都失败了
                if (cur.Update(Time.deltaTime))
                {
                    //超过三次拉取结束
                    taskState.downloadcount++;
                    if (cur.task.onProgress != null)
                    {
                        cur.task.OnProgress(cur.www.downloadProgress);
                    }
                    cur.task.onload(cur.www, cur.task.tag);

                    if (cur.task.download)
                    {
                        Debug.LogFormat("DownmgrNative=> test num over three  {0} : {1}", cur.task.path, cur.www.downloadProgress);
                    }
                    RecycleDownTaskAndList(cur.task.url);
                    RecycleDownTaskRunner(cur);
                    runners.RemoveAt(i);
                    i--;
                }
                //统计速度
                if (reGetSpeed)
                {
                    if (cur.handler!=null)
                    {
                        runnerDownSpeed += cur.handler.Speed;
                    }
                }
                
            }
        }

        //监测完成事件
        if (task.Count == 0 && runners.Count == 0)
        {
            if (TaskFinish != null)
            {
                Action tt = TaskFinish;
                TaskFinish = null;
                tt();
            }
        }
        reGetSpeed = false;
    }
    Action TaskFinish = null;

    public void WaitForTaskFinish(Action finish)
    {
        if (TaskFinish != null)
        {
            TaskFinish += finish;
        }
        else
        {
            TaskFinish = finish;
        }
    }
    /// <summary>
    /// 返回所有运行中任务的已下载大小
    /// </summary>
    /// <returns></returns>
    public float AllRunnerDownFileSize()
    {
        return runnerDownSize;
    }
    /// <summary>
    /// 返回所有运行中任务的下载速度
    /// </summary>
    /// <returns></returns>
    public float AllRunnerDownSpeed()
    {
        return runnerDownSpeed;
    } 
    /// <summary>
    /// 从列表内移除 加入对象池
    /// </summary>
    /// <param name="url"></param>
    public void RecycleDownTaskAndList(string url,DownTask dtTemp =null)
    {
        if (dtTemp==null)
        {
            taskList[url].Reset();
            m_DownTashPool.Recycle(taskList[url]);
        }
        else
        {
            dtTemp.Reset();
            m_DownTashPool.Recycle(dtTemp);
        }
        taskList.Remove(url);
    }

    public void RecycleDownTaskRunner( DownTaskRunner dtrTemp = null)
    {
        dtrTemp.Reset();
        m_DownTaskRunnerPool.Recycle(dtrTemp);
    }

    /// <summary>
    /// 从缓存路径加载文件数据
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal byte[] LoadFromCacheDirect(string path)
    {
        string url = Const.ABCachePath+ path;
        using (System.IO.Stream s = System.IO.File.OpenRead(url))
        {
            byte[] b = new byte[s.Length];
            s.Read(b, 0, (int)s.Length);
            return b;
        }
    }
    /// <summary>
    /// 从StreamingAssets路径内加载文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="tag"></param>
    /// <param name="onLoad"></param>
    internal void LoadFromStreamingAssets(string path, string tag, Action<UnityWebRequest, string> onLoad)
    {
        Load(Const.ABLoadPath +  path, path, tag, onLoad, null);
    }
    /// <summary>
    /// 从远程路径加载文件数据
    /// </summary>
    /// <param name="path"></param>
    /// <param name="tag"></param>
    /// <param name="onLoad"></param>
    /// <param name="onProgress"></param>
    /// <param name="download"></param>
    internal DownTask LoadFromRemote(string path, string tag, Action<UnityWebRequest, string> onLoad, Action<float, string> onProgress = null, bool download = true)
    {
        string remotePath = path;
        //if (!Path.HasExtension(path))
        //    remotePath = path.ToLower();

        string url = System.IO.Path.Combine(Const.ABRemotePath, remotePath);
        //TODO需要安卓输出
        //Debug.Log("DownmgrNative=>LoadFromRemote=> Request url: " + url);

        return Load(url, path, tag, onLoad, onProgress, download);
    }
    /// <summary>
    /// 加入下载列表 绑定事件
    /// </summary>
    /// <param name="url"></param>
    /// <param name="path"></param>
    /// <param name="tag"></param>
    /// <param name="onLoad"></param>
    /// <param name="onProgress"></param>
    /// <param name="remote"></param>
    internal DownTask Load(string url, string path, string tag, Action<UnityWebRequest, string> onLoad, Action<float, string> onProgress, bool remote = false)
    {
        if (!taskList.ContainsKey(url))
        {
            DownTask down = m_DownTashPool.Spawn(true);
            down.Init(url, path, tag, remote, onLoad, onProgress);

            taskList.Add(url, down);
            task.Enqueue(down);
            taskState.taskcount++;
            return down;
        }
        else
        {
            taskList[url].onload = onLoad;
            taskList[url].onProgress = onProgress;
            return taskList[url];
        }
    }
    /// <summary>
    /// 将数据保存成文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="data"></param>
    internal void SaveToCache(string path, byte[] data)
    {
        string outfile = Const.ABCachePath+ path;
        string outpath = System.IO.Path.GetDirectoryName(outfile);

        if (System.IO.Directory.Exists(outpath) == false)
        {
            System.IO.Directory.CreateDirectory(outpath);
        }
        using (var s = System.IO.File.Create(outfile))
        {
            s.Write(data, 0, data.Length);
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="onInit">DownmgrNative初始化完成后执行的事件</param>
    /// <param name="_taskMaxCount"></param>
    public void BeginInit(Action onInit, int _taskMaxCount = 5)
    {
        Debug.LogFormat("DownmgrNative TaskMaxCount {0}", _taskMaxCount);
        this.taskMaxCount = _taskMaxCount;

        
        if (onInit !=null)
        {
            onInit();
        }
    }
    public override string ToString()
    {
        return string.Format("[ResmgrNative loading status] running:{0} - task:{1} - frametask:{2}", runners.Count, task.Count + runners.Count);
    }
    /// <summary>
    /// 判定下载失败还是成功
    /// </summary>
    /// <param name="unityWebRequest"></param>
    /// <returns></returns>
    public bool DownFailed(UnityWebRequest unityWebRequest)
    {
        return unityWebRequest.isHttpError || unityWebRequest.isHttpError || unityWebRequest.downloadProgress < 1 || !string.Equals(unityWebRequest.error,null )
            /* || string.Equals(unityWebRequest.error , "Request aborted") || string.Equals(unityWebRequest.error, "Cannot connect to destination host")*/;
    }
}