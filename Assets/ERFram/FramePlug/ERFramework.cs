using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ERFramework : MonoSingleton<ERFramework>
{
    protected override void OnAwake()
    {
        Debug.Log("初始化开始");
        GameObject.DontDestroyOnLoad(gameObject);
        //针对资源加载的管理器
        ResourceManager.Instance.Init(this);
        //针对GameObject对象池的管理器
        ObjectManager.Instance.Init(transform.Find("RecyclePool"), transform.Find("ActiveParent"));
        //UI管理器
        UIManager.Instance.Init(transform.Find("UIRoot") as RectTransform,  transform.Find("UICamera").GetComponent<Camera>(), transform.Find("UIRoot/EventSystem").GetComponent<EventSystem>());
        //注册UI界面和UI逻辑脚本关联
        RegisterUI();
        //初始化场景加载管理器
        GameMapManager.Instance.Init(this);
        //初始化ab系统管理器 对比版本信息 初始化下载器
        ABSysManager.Instance.Init();

    }
    //注册UI
    private void RegisterUI()
    {
        UIManager.Instance.Register<MenuUI>(ConStr.MENUPANEL);
        UIManager.Instance.Register<LoadingUI>(ConStr.LOADINGPANEL);
        UIManager.Instance.Register(ConStr.CHECKDOWNLOADPANEL, "CheckDownloadUI");
    }
    void Start ()
    {

    }
	void Update ()
    {
        UIManager.Instance.OnUpdate();
        DownmgrNative.Instance.Update();

    }
    /// <summary>
    /// 进入主场景
    /// </summary>
    public void GoToMainSceneEvent()
    {
        GameMapManager.Instance.LoadScene(ConStr.MENUSCENE);
        //手动回收下资源  比如下载的ab包资源就可以回收掉
        GC.Collect();
    }
    private void OnApplicationQuit()
    {
        //清理下载任务，关闭文件流
        DownmgrNative.Instance.ClearTask();
#if UNITY_EDITOR
        ResourceManager.Instance.ClearCache();
        Resources.UnloadUnusedAssets();
        Debug.Log("清空编辑器缓存");
#endif
    }
}
