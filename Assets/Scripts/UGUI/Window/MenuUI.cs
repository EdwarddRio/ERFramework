using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : Window
{
    private MenuPanel m_MainPanel;
    public override void Awake<T, U, X>(T param1, U param2, X param3)
    {
        m_MainPanel = GameObject.GetComponent<MenuPanel>();
        if (UnityEngine.Object.ReferenceEquals(m_MainPanel, null))
        {
            Debug.LogError("cant getComponent :MenuPanel");
            return;
        }
        //释放检查下载的资源
        UIManager.Instance.ClearCheckDownUI();

        AddButtonClickListener(m_MainPanel.m_StartButton, OnClickStart);

        //加载图片
        ChangeImageSprite(ConStr.UIMainPATH + "rush_light.png", m_MainPanel.m_Image, true);

        ObjectManager.Instance.InstantiateObject(ConStr.PREFAB_TREEPATH, true);
    }

    private void OnClickStart()
    {
        Debug.Log("OnClickStart");
    }

}
