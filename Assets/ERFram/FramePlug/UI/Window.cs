using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class Window
{

    public string Name { get; set; }

    public GameObject m_GameObject;
    //引用GameObject
    public GameObject GameObject
    {
        get
        {
            return m_GameObject;
        }
        set
        {
            m_GameObject = value;
        }
    }

    //引用Transform
    public Transform Transform { get; set; }

    //所有的Button
    protected List<Button> m_AllButton = new List<Button>();

    //所有的Toggle
    protected List<Toggle> m_AllToggle = new List<Toggle>();

    //消息传递
    public virtual bool OnMessage(UIMsgID uIMsgID, params object[] paras)
    {
        return true;
    }

    //初始胡调用
    public virtual void Awake(params object[] paralist) { }
    //显示就调用
    public virtual void OnShow(params object[] paralist) { }

    public virtual void OnDisable() { }

    public virtual void OnUpdate() { }

    public virtual void OnClose() {
        RemoveAllButtonListener();
        RemoveAllToggleListener();
        m_AllButton.Clear();
        m_AllToggle.Clear();
    }
    /// <summary>
    /// 同步替换图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="img"></param>
    /// <param name="setNatvieSize"></param>
    /// <returns></returns>
    public bool ChangeImageSprite(string path,Image img,bool setNatvieSize = false)
    {
        if (UnityEngine.Object.ReferenceEquals(img,null))
        {
            Debug.LogError("Window -> ChangeImageSprite  img is not exist");
            return false;
        }
        Sprite sp = ResourceManager.Instance.LoadSpriteBySpriteAtlas(path);
        if (UnityEngine.Object.ReferenceEquals(sp, null))
        {
            Debug.LogWarning("Sprite Path is not spriteatlas type , try load sprite by path without spriteatlas");
            sp = ResourceManager.Instance.LoadResource<Sprite>(path);
        }
        if (UnityEngine.Object.ReferenceEquals(sp, null))
        {
            return false;
        }
        if (!UnityEngine.Object.ReferenceEquals(img.sprite, null))
        {
            img.sprite = null;
        }
        img.sprite = sp;
        if (setNatvieSize)
        {
            img.SetNativeSize();
        }
        return true;
    }
    /// <summary>
    /// 异步替换图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="img"></param>
    /// <param name="setNatvieSize"></param>
    /// <param name="bySpriteatlas">使用Spriteatlas图集加载图片</param>
    public void ChangeImageSpriteAsync(string path, Image img, bool setNatvieSize = false,bool bySpriteatlas = true)
    {
        if (UnityEngine.Object.ReferenceEquals(img, null))
        {
            return;
        }
        if (bySpriteatlas)
        {
            ResourceManager.Instance.LoadSpriteBySpriteAtlasAsync(path, OnLoadSpriteAtlasFinish, img, setNatvieSize);
        }
        else
        {
            ResourceManager.Instance.AsyncLoadResource(path, OnLoadSpriteFinish, LoadResPriority.RES_HIGHT,true ,img, setNatvieSize);
        }
    }
    /// <summary>
    /// 异步加载SpriteAtlas的回调
    /// </summary>
    /// <param name="path"></param>
    /// <param name="obj"></param>
    /// <param name="param1"></param>
    /// <param name="param2"></param>
    /// <param name="param3"></param>
    protected void OnLoadSpriteAtlasFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        SpriteAtlas spriteAtlas = obj as SpriteAtlas;
        if (UnityEngine.Object.ReferenceEquals(spriteAtlas,null))
        {
            Debug.LogError("SpriteAtlas is not exist：" + path);
        }
        else
        {
            Image img = param1 as Image;
            bool setNatvieSize = (bool)param2 ;
            Sprite sp = spriteAtlas.GetSprite(param3 as string);
            if (!UnityEngine.Object.ReferenceEquals(img.sprite, null))
            {
                img.sprite = null;
            }
            img.sprite = sp;
            if (setNatvieSize)
            {
                img.SetNativeSize();
            }
        }
    }
    /// <summary>
    /// 异步加载Sprite回调
    /// </summary>
    /// <param name="path"></param>
    /// <param name="obj"></param>
    /// <param name="param1"></param>
    /// <param name="param2"></param>
    /// <param name="param3"></param>
    protected void OnLoadSpriteFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        Sprite sp = obj as Sprite;
        if (UnityEngine.Object.ReferenceEquals(sp,null))
        {
            Debug.LogError("Sprite is not exist：" + path);
        }
        else
        {
            Image img = param1 as Image;
            bool setNatvieSize = (bool)param2;
            if (!UnityEngine.Object.ReferenceEquals(img.sprite, null))
            {
                img.sprite = null;
            }
            img.sprite = sp;
            if (setNatvieSize)
            {
                img.SetNativeSize();
            }
        }
    }
    /// <summary>
    /// 添加button事件监听
    /// </summary>
    /// <param name="btn"></param>
    /// <param name="action"></param>
    public void AddButtonClickListener(Button btn,UnityEngine.Events.UnityAction action)
    {
        if (!UnityEngine.Object.ReferenceEquals(btn,null))
        {
            if (!m_AllButton.Contains(btn))
            {
                m_AllButton.Add(btn);
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            btn.onClick.AddListener(BtnPlaySound);
        }
    }
    /// <summary>
    /// 添加Toggle的事件监听
    /// </summary>
    /// <param name="toggle"></param>
    /// <param name="action"></param>
    public void AddToggleValueChangeListener(Toggle toggle,UnityEngine.Events.UnityAction<bool> action)
    {
        if (!UnityEngine.Object.ReferenceEquals(toggle, null))
        {
            if (!m_AllToggle.Contains(toggle))
            {
                m_AllToggle.Add(toggle);
            }
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(action);
            toggle.onValueChanged.AddListener(TogglePlaySound);
        }
    }
    /// <summary>
    /// 移除所有Button事件
    /// </summary>
    public void RemoveAllButtonListener()
    {
        for (int i = 0; i < m_AllButton.Count; i++)
        {
            Button btn = m_AllButton[i];
            btn.onClick.RemoveAllListeners();
        }
    }
    /// <summary>
    /// 移除所有Toggle事件
    /// </summary>
    public void RemoveAllToggleListener()
    {
        for (int i = 0; i < m_AllToggle.Count; i++)
        {
            Toggle toggle = m_AllToggle[i];
            toggle.onValueChanged.RemoveAllListeners();
        }
    }
    /// <summary>
    /// 按钮的点击音效事件
    /// </summary>
    public virtual void BtnPlaySound()
    {

    }
    /// <summary>
    /// Toggle的点击音效事件
    /// </summary>
    public virtual void TogglePlaySound(bool isOn)
    {

    }
}
