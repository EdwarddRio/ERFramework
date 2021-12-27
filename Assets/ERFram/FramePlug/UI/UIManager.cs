using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//消息枚举
public enum UIMsgID {
    None =0,

}
public enum WndLayer { 
    wnd,
    pop,
    loading
}

public class UIManager : Singleton<UIManager>
{
    //UI节点
    private RectTransform m_UIRoot;
    //Wnd节点
    private RectTransform m_WndRoot;
    //PopWnd节点
    private RectTransform m_PopRoot;
    //Loading节点
    private RectTransform m_LoadingRoot;
    //UI摄像机
    private Camera m_UICamera;
    //EventSyatem
    private EventSystem m_EventSystem;
    //屏幕宽高比
    private float m_CanvasRate = 0;

    //路径
    private const string UIPREFABPATH = "Assets/GameData/Prefabs/UGUI/Panel/";
    //所有打开的窗口
    private Dictionary<string, Window> m_WindowDic = new Dictionary<string, Window>();
    //对应字典的list表
    private List<Window> m_WindowList = new List<Window>();
    //注册的字典
    private Dictionary<string, System.Type> m_RegisterDic = new Dictionary<string, System.Type>();

    /// <summary>
    /// 初始化UI/窗口父节点 UI摄像机
    /// </summary>
    /// <param name="uiRoot"></param>
    /// <param name="wndRoot"></param>
    /// <param name="uiCamera"></param>
    public void Init(RectTransform uiRoot, Camera uiCamera, EventSystem eventSystem)
    {
        m_UIRoot = uiRoot;
        m_WndRoot = uiRoot.Find("WndRoot") as RectTransform;
        m_PopRoot = uiRoot.Find("PopRoot") as RectTransform;
        m_LoadingRoot = uiRoot.Find("LoadingRoot") as RectTransform;

        m_UICamera = uiCamera;
        m_EventSystem = eventSystem;

        m_CanvasRate = Screen.height / (m_UICamera.orthographicSize * 2);
    }
    /// <summary>
    /// 隐藏或显示所有UI
    /// </summary>
    /// <param name="show"></param>
    public void ShowOrHideUI(bool show)
    {
        if (m_UIRoot !=null)
        {
            m_UIRoot.gameObject.SetActive(show);
        }
    }
    /// <summary>
    /// 设置默认选择对象
    /// </summary>
    /// <param name="obj"></param>
    public void SetNormalSelectObj(GameObject obj)
    {
        if (m_EventSystem ==null)
        {
            m_EventSystem = EventSystem.current;
        }
        m_EventSystem.firstSelectedGameObject = obj;
    }
    /// <summary>
    /// 窗口的更新
    /// </summary>
    public void OnUpdate()
    {
        for (int i = 0; i < m_WindowList.Count; i++)
        {
            if (m_WindowList[i] != null)
            {
                m_WindowList[i].OnUpdate();
            }
        }
    }

    /// <summary>
    /// 窗口注册方法
    /// </summary>
    /// <typeparam name="T">窗口反省类</typeparam>
    /// <param name="name">窗口名</param>
    public void Register<T>(string name) where T : Window
    {
        m_RegisterDic[name] = typeof(T);
    }
    /// <summary>
    /// 窗口注册方法 通过类型字符串找到类型
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type">类型的字符串</param>
    public void Register(string name,string type) 
    {
        m_RegisterDic[name] =System.Type.GetType(type, true, true);
    }
    /// <summary>
    /// 发送消息给窗口
    /// </summary>
    /// <param name="name">窗口名</param>
    /// <param name="uIMsgID">消息id</param>
    /// <param name="paras">参数数组</param>
    /// <returns></returns>
    public bool SendMessageToWnd<T,U,X>(string name,UIMsgID uIMsgID ,T param1,U param2,X param3)
    {
        Window wnd = FindWndByName<Window>(name);
        if (wnd !=null)
        {
            return wnd.OnMessage(uIMsgID, param1,param2,param3);
        }
        return false;
    }

    /// <summary>
    /// 根据窗口名查找窗口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="wndName"></param>
    /// <returns></returns>
    public T FindWndByName<T>(string wndName) where T : Window
    {
        Window outWnd = null;
        if (m_WindowDic.TryGetValue(wndName, out outWnd))
        {
            return (T)outWnd;
        }
        return null;
    }
    /// <summary>
    /// 打开ui窗口
    /// </summary>
    /// <param name="wndName"></param>
    /// <param name="btop"> true</param>
    /// <param name="wndLayer"> WndLayer.wnd</param>
    /// <param name="paras"></param>
    /// <returns></returns>
    public Window PopUpWindow(string wndName, bool btop = true, WndLayer wndLayer = WndLayer.wnd)
    {
        return PopUpWindow<object, object, object>(wndName, btop, wndLayer, null, null, null);
    }
    public Window PopUpWindow<T>(string wndName, bool btop , WndLayer wndLayer , T param1)
    {
        return PopUpWindow<T, object, object>(wndName, btop, wndLayer, param1, null, null);
    }
    public Window PopUpWindow<T, U>(string wndName, bool btop, WndLayer wndLayer, T param1, U param2)
    {
        return PopUpWindow<T, U, object>(wndName, btop, wndLayer, param1, param2, null);
    }
    public Window PopUpWindow<T,U,X>(string wndName, bool btop,WndLayer wndLayer ,T param1, U param2, X param3)
    {
        Window wnd = FindWndByName<Window>(wndName);
        if (wnd == null)
        {
            //根据名字查找wnd预设
            System.Type tp = null;
            if (m_RegisterDic.TryGetValue(wndName, out tp))
            {
                wnd = System.Activator.CreateInstance(tp) as Window;
            }
            else
            {
                Debug.LogError("Cant use WndName find Script. WndName:" + wndName);
                return null;
            }

            GameObject wndObj = ObjectManager.Instance.InstantiateObject(UIPREFABPATH + wndName, false, false);
            if (wndObj == null)
            {
                Debug.LogError("Create Wnd Prefab Failed, wndName:" + wndName);
                return null;
            }

            m_WindowDic.Add(wndName, wnd);
            m_WindowList.Add(wnd);

#if UNITY_EDITOR
            wndObj.name = wndName;
#endif
            wnd.Name = wndName;
            wnd.GameObject = wndObj;
            wnd.Transform = wndObj.transform;
            wnd.Awake(param1,param2,param3);

            switch (wndLayer)
            {
                case WndLayer.wnd:
                    wndObj.transform.SetParent(m_WndRoot, false);
                    break;
                case WndLayer.pop:
                    wndObj.transform.SetParent(m_PopRoot, false);
                    break;
                case WndLayer.loading:
                    wndObj.transform.SetParent(m_LoadingRoot, false);
                    break;
                default:
                    break;
            }

            if (btop)
            {
                wndObj.transform.SetAsLastSibling();
            }
            wnd.OnShow(param1, param2, param3);
        }
        else
        {
            ShowWnd(wndName, btop, param1, param2, param3);
        }

        return wnd;
    }
    /// <summary>
    /// 根据窗口名称 关闭窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="destroy"></param>
    public void CloseWnd(string name, bool destroy = false)
    {
        Window wnd = FindWndByName<Window>(name);
        CloseWnd(wnd, destroy);
    }
    /// <summary>
    /// 根据窗口对象 关闭窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="destroy"></param>
    public void CloseWnd(Window wnd, bool destroy = false)
    {
        if (wnd != null)
        {
            wnd.OnDisable();
            wnd.OnClose();

            Window outWnd = null;
            if (m_WindowDic.TryGetValue(wnd.Name, out outWnd))
            {
                m_WindowList.Remove(outWnd);
                m_WindowDic.Remove(outWnd.Name);
                outWnd = null;
            }
            if (destroy)
            {
                ObjectManager.Instance.ReleaseObject(ref wnd.m_GameObject, 0, true);
            }
            else
            {
                ObjectManager.Instance.ReleaseObject(ref wnd.m_GameObject, recycleParent: false);
            }
            //wnd.GameObject = null;
            wnd = null;
        }
    }
    /// <summary>
    /// 关闭所有窗口
    /// </summary>
    public void CloseAllWnd()
    {
        for (int i = m_WindowList.Count - 1; i >= 0; i--)
        {
            CloseWnd(m_WindowList[i]);
        }
    }
    /// <summary>
    /// 切换到唯一窗口
    /// </summary>
    public void SwitchStateByName(string name, bool bTop = true)
    {
        SwitchStateByName(name, bTop);
    }
    public void SwitchStateByName<T, U, X>(string name,bool bTop , T param1, U param2, X param3)
    {
        CloseAllWnd();
        PopUpWindow(name, bTop, WndLayer.wnd, param1,param2,param3);
    }
    /// <summary>
    /// 根据窗口名字隐藏窗口
    /// </summary>
    /// <param name="name"></param>
    public void HideWnd(string name)
    {
        Window wnd = FindWndByName<Window>(name);
        HideWnd(wnd);
    }
    /// <summary>
    /// 根据窗口对象隐藏窗口
    /// </summary>
    /// <param name="name"></param>
    public void HideWnd(Window wnd) 
    {
        if (wnd !=null)
        {
            wnd.GameObject.SetActive(false);
            wnd.OnDisable();
        }
    }
    /// <summary>
    /// 根据窗口名字显示窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="btop"></param>
    /// <param name="paras"></param>
    public void ShowWnd(string name, bool btop = true)
    {
        ShowWnd<object,object,object>(name, btop,null,null,null );
    }
    public void ShowWnd<T, U, X>(string name, bool btop , T param1, U param2, X param3)
    {
        Window wnd = FindWndByName<Window>(name);
        ShowWnd(wnd, btop, param1,param2,param3);
    }
    /// <summary>
    /// 根据窗口对象显示窗口
    /// </summary>
    /// <param name="wnd"></param>
    /// <param name="btop"></param>
    /// <param name="paras"></param>
    public void ShowWnd<T, U, X>(Window wnd, bool btop , T param1, U param2, X param3)
    {
        if (wnd != null)
        {
            if (!UnityEngine.Object.ReferenceEquals(wnd.GameObject, null) && !wnd.GameObject.activeSelf)
            {
                wnd.GameObject.SetActive(true);
            }
#if UNITY_EDITOR
            //编辑器下方便查看 改个名字
            if (wnd.GameObject.name.EndsWith("(Recycle)"))
            {
                wnd.GameObject.name = wnd.GameObject.name.Replace("(Recycle)", "");
            }
#endif
            if (btop)
            {
                wnd.GameObject.transform.SetAsLastSibling();
            }
            wnd.OnShow(param1, param2, param3);
        }
    }
    /// <summary>
    /// 清除卸载CheckDownUI的资源
    /// </summary>
    public void ClearCheckDownUI()
    {
        CloseWnd(ConStr.CHECKDOWNLOADPANEL, true);

        ABSysManager.Instance.ReNameCheckDownUI();
    }
}
