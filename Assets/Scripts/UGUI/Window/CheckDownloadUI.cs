using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckDownloadUI : Window
{
    private CheckDownloadPanel m_MainPanel;
    public override void Awake(params object[] paralist)
    {
        m_MainPanel = GameObject.GetComponent<CheckDownloadPanel>();
        if (UnityEngine.Object.ReferenceEquals(m_MainPanel, null))
        {
            Debug.LogError("cant getComponent :CheckDownloadPanel");
            return;
        }
    }
    public override void OnShow(params object[] paralist)
    {
        AddButtonClickListener(m_MainPanel.m_Btn_Ok, Btn_OkEvent);
        AddButtonClickListener(m_MainPanel.m_Btn_Cancel, Btn_CancelEvent);

        m_MainPanel.StartCoroutine(LoadCheckDown());
    }
    private WaitForEndOfFrame m_WaitForEndOfFrame = new WaitForEndOfFrame();
    private WaitForSeconds m_WaitForSecondsOne = new WaitForSeconds(1f);
    private bool loadedVerFile = false;
    private bool isReDown = false;
    private bool preLoadFinish = false;
    protected IEnumerator LoadCheckDown()
    {
#if UNITY_EDITOR
        if (!Const.m_LoadFormAssetBundle)
        {
            PreLoadResourcesOrObjs(false);

            while (!preLoadFinish)
            {
                yield return m_WaitForEndOfFrame;
            }
            ERFramework.Instance.GoToMainSceneEvent();

            yield break;
        }
#endif
        m_MainPanel.OnShow();
        if (Const.CheckRemoteFileDown /*开启资源对比下载*/)
        {
            if (!isReDown)
            {
                m_MainPanel.ShowInfoText("读取版本信息中。。。");

                //将本地版本信息 与 服务器上面的对比
                ABSysManager.Instance.CheckVersionWithRemote(ShowRemoteVersion);
                while (!loadedVerFile)
                {
                    yield return m_WaitForEndOfFrame;
                }
            }
            else
            {
                //重新显示开始下载资源UI
                ShowRemoteVersion(true);
            }
        }

        m_MainPanel.ShowInfoText("开始下载文件。。。");

        //下载过程中 每秒刷新显示下载速度
        if (ABSysManager.Instance.AllNeedDownNum >0)
        {
            DownmgrNative.Instance.GetRunnerDownSize = true;
            //开始下载ab资源 如果下载有checkdown的话 需要改下下载的名字
            ABSysManager.Instance.StartDownABFile(DownABFileOnLoadSuccess, DownABFileOnLoadFail);

            while (ABSysManager.Instance.AllNeedDownNum != (ABSysManager.Instance.CurrentDownedNum + ABSysManager.Instance.CurrentDownFailNum))
            {
                yield return m_WaitForEndOfFrame;
                m_MainPanel.RefreshDownProgress(ABSysManager.Instance.GetAllDownFileProgress(), DownmgrNative.Instance.AllRunnerDownSpeed());
            }
            DownmgrNative.Instance.GetRunnerDownSize = false;
            Debug.LogError("下载文件 成功：" + ABSysManager.Instance.CurrentDownedNum + "  失败" + ABSysManager.Instance.CurrentDownFailNum);

            while (m_MainPanel.m_currentDownProgress <1)
            {
                if (m_MainPanel.ProgressToOne(0.5f))
                {
                    break;
                }
                yield return m_WaitForEndOfFrame;
            }

        }           
        //下载完成 保存缓存版本文件
        ABSysManager.Instance.SaveCacheVerFile();

        m_MainPanel.ShowLoadConfig();
        //没有下载的话 就直接往后走游戏
        if (ABSysManager.Instance.AllNeedDownNum <= 0)
        {
            PreLoadResourcesOrObjs(false);
        }
        //全部下载成功的话 重新初始化 AssetBundleManager.Instance.LoadAssetBundleConfig();  然后进游戏
        else if(ABSysManager.Instance.CurrentDownFailNum <=0)
        {
            PreLoadResourcesOrObjs(true);
        }
        else
        {
            //如果有下载失败的，则保存缓存版本文件 出提示重新来一遍
            ShowReDownABEvent();
            yield break;
        }

        while (!preLoadFinish)
        {
            m_MainPanel.ProgressToOne(0.01f);
            yield return m_WaitForEndOfFrame;
        }
        while (true)
        {
            if (m_MainPanel.ProgressToOne(2f))
            {
                break;
            }
            yield return m_WaitForEndOfFrame;
        }
        //切换到主场景，然后在主场景内 卸载checkdown
        ERFramework.Instance.GoToMainSceneEvent();
    }
    /// <summary>
    /// 显示远端版本号
    /// </summary>
    protected void ShowRemoteVersion(bool success )
    {
        m_MainPanel.InitVersionAndAllSize(ABSysManager.Instance.TotalDownSize);

        loadedVerFile = true;
    }
    /// <summary>
    /// 下载ab包成功事件
    /// </summary>
    /// <param name="m_Name"></param>
    protected void DownABFileOnLoadSuccess(string m_Name)
    {
        //XXX:UI显示下载完成的+1


        Debug.Log("download file success:" + m_Name);
    }
    /// <summary>
    /// 下载ab包失败事件
    /// </summary>
    /// <param name="m_Name"></param>
    protected void DownABFileOnLoadFail(string m_Name)
    {

        Debug.Log("download file fail:" + m_Name);
    }
    /// <summary>
    /// 进行预加载数据
    /// </summary>
    protected void PreLoadResourcesOrObjs(bool reload = false)
    {
        Const.GAME_VERSION = Const.GAMERemote_VERSION;
        if (reload)
        {
            AssetBundleManager.Instance.LoadAssetBundleConfig();
        }

        PreLoadMainSceneEvent();
    }
    /// <summary>
    /// 对于首次进入主场景 需要干点啥的函数
    /// </summary>
    public void PreLoadMainSceneEvent()
    {

        ABSysManager.Instance.ClearabFileDownloadList();
        preLoadFinish = true;
    }
    /// <summary>
    /// 显示并确认重新下载UI
    /// </summary>
    protected void ShowReDownABEvent()
    {
        //显示提示框 是否重新下载
        m_MainPanel.ShowReDownPanel(true);
    }
    /// <summary>
    /// 联动ABSysMgr重新开始下载ab资源包
    /// </summary>
    protected void ReDownABEvent()
    {
        ABSysManager.Instance.ReDownABEvent();

        isReDown = true;
        m_MainPanel.StartCoroutine(LoadCheckDown());
    }
    /// <summary>
    /// 重新下载
    /// </summary>
    public void Btn_OkEvent()
    {
        ReDownABEvent();
        m_MainPanel.ShowReDownPanel(false);
    }
    /// <summary>
    /// 取消重新下载 退出游戏
    /// </summary>
    public void Btn_CancelEvent()
    {
        m_MainPanel.ShowReDownPanel(false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }
}
