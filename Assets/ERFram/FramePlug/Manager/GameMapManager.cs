using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMapManager : Singleton<GameMapManager>
{
    //切换场景进度条
    public static int LoadingProgress = 0;
    //加载场景完成回调
    public Action LoadSceneOverCallBack;
    //加载场景开始回调
    public Action LoadSceneEnterCallBack;
    //加载配置\初始化对象池等回调
    public Action LoadFinishDoOtherCallBack;


    //当前场景名
    public string CurrentMapName { get; set; }
    //场景是否加载完成
    public bool AllreadyLoadScene { get; set; }= false;

    protected bool m_DoOtherFinish = false;

    private MonoBehaviour m_Mono;
    private WaitForEndOfFrame m_WaitForEndOfFrame = new WaitForEndOfFrame();
    /// <summary>
    /// 场景管理初始化
    /// </summary>
    /// <param name="mono"></param>
    public void Init(MonoBehaviour mono)
    {
        m_Mono = mono;
    }
    /// <summary>
    /// 加载场景
    /// 先打开Loading的进度条界面并且确定加载的哪个场景。等到场景加载完成后再打开对应场景的默认UI
    /// </summary>
    /// <param name="name">场景名称</param>
    public void LoadScene(string name)
    {
        UIManager.Instance.PopUpWindow(ConStr.LOADINGPANEL, true, WndLayer.loading, name);
        LoadingProgress = 0;
        m_DoOtherFinish = false;
        m_Mono.StartCoroutine(LoadSceneAsync(name));
    }
    /// <summary>
    /// 设置场景环境
    /// </summary>
    /// <param name="name"></param>
    void SetSceneSetting(string name)
    {
        //设置各种场景环境,可以配表来做
        
    }
    /// <summary>
    /// 分成两部分 加载场景 加载配置\初始化对象池等
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected IEnumerator LoadSceneAsync(string name)
    {
        LoadSceneEnterCallBack?.Invoke();
        ClearCache();
        AllreadyLoadScene = false;
        AsyncOperation unLoadScene= SceneManager.LoadSceneAsync(ConStr.EMPTYSCENE, LoadSceneMode.Single);
        while(unLoadScene != null && !unLoadScene.isDone)
        {
            yield return m_WaitForEndOfFrame;
        }

        LoadingProgress = 0;
        int targetProgress = 0;

        AsyncOperation loadSseneAsync = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
        if (loadSseneAsync != null && !loadSseneAsync.isDone)
        {
            loadSseneAsync.allowSceneActivation = false;
            while (loadSseneAsync.progress <0.9f)
            {
                targetProgress = (int)loadSseneAsync.progress * 100;
                yield return m_WaitForEndOfFrame;
                //平滑过渡
                while (LoadingProgress < targetProgress)
                {
                    ++LoadingProgress;
                    yield return m_WaitForEndOfFrame;
                }
            }
            CurrentMapName = name;
            SetSceneSetting(name);
            //自行加载剩余的10%
            targetProgress = 98;
            while (LoadingProgress < targetProgress)
            {
                ++LoadingProgress;
                yield return m_WaitForEndOfFrame;
            }
            LoadingProgress = 100;
            //允许显示场景
            loadSseneAsync.allowSceneActivation = true;
            AllreadyLoadScene = true;
        }
        if (LoadFinishDoOtherCallBack != null)
        {
            //XXX:回调函数放入子线程 这样就不会堵塞协程运行，让进度条正常加载
            //但是子线程中有些Unity的API无法运行，需自行测试
            Task.Run(() =>
            {
                LoadFinishDoOtherCallBack();
                m_DoOtherFinish = true;
            });
        }
        else
        {
            m_DoOtherFinish = true;
        }
        targetProgress = 299;
        //加载剩下的进度
        while (LoadingProgress < targetProgress)
        {
            if (LoadingProgress + 5 > targetProgress)
            {
                LoadingProgress = targetProgress;
            }
            else
            {
                LoadingProgress += 5;
            }
            yield return m_WaitForEndOfFrame;
        }
        while (!m_DoOtherFinish)
        {
            yield return m_WaitForEndOfFrame;
        }

        //完成的回调
        LoadSceneOverCallBack?.Invoke();

        ClearAllCallBack();
        //联动loading 关闭界面结束协程
        LoadingProgress = targetProgress = 300;
    }
    /// <summary>
    /// 清理所有回调函数
    /// </summary>
    private void ClearAllCallBack()
    {
        LoadFinishDoOtherCallBack= LoadSceneOverCallBack = LoadSceneEnterCallBack = null;
    }
    //跳场景需要清楚数据
    private void ClearCache()
    {
        ObjectManager.Instance.ClearCache();
        ResourceManager.Instance.ClearCache();
    }

}
