using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingUI : Window
{
    private LoadingPanel m_MainPanel;
    private string m_SceneName;
    private bool m_LoadOtherSceneFinish = false;
    public override void Awake<T, U, X>(T param1, U param2, X param3)
    {
        m_MainPanel = GameObject.GetComponent<LoadingPanel>();
        if (UnityEngine.Object.ReferenceEquals(m_MainPanel, null))
        {
            Debug.LogError("cant getComponent :LoadingPanel");
            return;
        }
        m_SceneName = param1 as string;
    }
    public override void OnShow<T, U, X>(T param1, U param2, X param3)
    {
        m_LoadOtherSceneFinish = false;
    }
    public override void OnUpdate()
    {
        if (UnityEngine.Object.ReferenceEquals(m_MainPanel,null))
        {
            return;
        }
        m_MainPanel.m_Slider.value = GameMapManager.LoadingProgress /3 *0.01f;
        m_MainPanel.m_Text.text = (GameMapManager.LoadingProgress / 3) + "%";
        if (!m_LoadOtherSceneFinish && GameMapManager.LoadingProgress >=100)
        {
            m_LoadOtherSceneFinish = true;
            LoadingOtherScene();
        }
        if (GameMapManager.LoadingProgress >= 300)
        {
            CloseLoadingPanel();
        }
    }
    /// <summary>
    /// 加载对应场景的第一个UI
    /// </summary>
    public void LoadingOtherScene()
    {
        //根据场景名字打开对应场景第一个界面
        if (m_SceneName == ConStr.MENUSCENE)
        {
            UIManager.Instance.PopUpWindow(ConStr.MENUPANEL,true,WndLayer.wnd);
        }


    }
    /// <summary>
    /// 关闭loading的界面
    /// </summary>
    protected void CloseLoadingPanel()
    {
        UIManager.Instance.CloseWnd(ConStr.LOADINGPANEL);
    }
}
