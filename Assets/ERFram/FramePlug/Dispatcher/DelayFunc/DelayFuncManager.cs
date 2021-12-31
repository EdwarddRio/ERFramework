using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//消息事件的委托
public delegate void DelayFuncHandler(IDelayFuncMessage message);


/* 延时函数调用管理器
 * 
 */
public class DelayFuncManager : Singleton<DelayFuncManager>
{
    //foreach遍历已经不会产生gc了。在unity5.3.5被修复

    //事件的保存字典  string为MessageType
    private Dictionary<string, DelayFuncDefinition> m_DelayFuncListeners = new Dictionary<string, DelayFuncDefinition>();
    //等待移除事件的队列，保存MessageType
    private List<string> m_DelayFuncLisRemoves = new List<string>();


    //中间类，类对象池
    private ClassObjectPool<DelayFuncMessage> m_DelayFuncMessagePool = new ClassObjectPool<DelayFuncMessage>(50);
    private ClassObjectPool<DelayFuncDefinition> m_DelayFuncDefinitionPool = new ClassObjectPool<DelayFuncDefinition>(50);

    //等待添加的延时函数队列
    private List<DelayFuncDefinition> m_DelayFuncAdds = new List<DelayFuncDefinition>();
    //等待移除的延时函数队列
    private List<string> m_DelayFuncRemoves = new List<string>();

    /// <summary>
    /// 延时函数消息回收函数
    /// </summary>
    /// <param name="message"></param>
    private void DelayFuncMessageRest(DelayFuncMessage message)
    {
        message.Reset();
        m_DelayFuncMessagePool.Recycle(message);
    }
    /// <summary>
    /// 延时函数回收函数
    /// </summary>
    /// <param name="delayFuncDefinition"></param>
    private void DelayFuncDefinitionRest(DelayFuncDefinition delayFuncDefinition)
    {
        delayFuncDefinition.Reset();
        m_DelayFuncDefinitionPool.Recycle(delayFuncDefinition);
    }
    /// <summary>
    /// 清空所有延时函数
    /// </summary>
    public void ClearListeners()
    {
        foreach (string typeStr in m_DelayFuncListeners.Keys)
        {
            DelayFuncDefinitionRest(m_DelayFuncListeners[typeStr]);
        }

        for (int i = 0; i < m_DelayFuncAdds.Count; i++)
        {
            DelayFuncDefinitionRest(m_DelayFuncAdds[i]);
        }

        m_DelayFuncListeners.Clear();
        m_DelayFuncAdds.Clear();
        m_DelayFuncRemoves.Clear();
    }

    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="handler">事件</param>
    public void AddListener(string messageType, DelayFuncHandler handler)
    {
         AddListener(messageType, null, 0, null, null, null, handler, false);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="handler">事件</param>
    public void AddListener(string messageType, object sender, DelayFuncHandler handler)
    {
        AddListener(messageType, sender, 0, null, null, null, handler, false);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="delay">延时时间</param>
    /// <param name="handler">事件</param>
    public void AddListener(string messageType, object sender, float delay, DelayFuncHandler handler)
    {
        AddListener(messageType, sender, delay, null, null, null, handler, false);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="delay">延时时间</param>
    /// <param name="param1">传参1</param>
    /// <param name="handler">事件</param>
    public void AddListener(string messageType, object sender, float delay, object param1, DelayFuncHandler handler)
    {
        AddListener(messageType, sender, delay, param1, null, null, handler, false);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="delay">延时时间</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="handler">事件</param>
    public void AddListener(string messageType, object sender, float delay, object param1, object param2,  DelayFuncHandler handler)
    {
        AddListener(messageType, sender, delay, param1, param2, null, handler, false);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="delay">延时时间</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="param3">传参3</param>
    /// <param name="handler">事件</param>
    public void AddListener(string messageType, object sender, float delay, object param1, object param2, object param3, DelayFuncHandler handler)
    {
        AddListener(messageType, sender, delay, param1, param2, param3, handler, false);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="handler">事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, DelayFuncHandler handler, bool immediate)
    {
        AddListener(messageType, null, 0, null, null, null, handler, immediate);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="handler">事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, object sender, DelayFuncHandler handler, bool immediate)
    {
        AddListener(messageType, sender, 0, null, null, null, handler, immediate);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="delay">延时时间</param>
    /// <param name="handler">事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, object sender, float delay, DelayFuncHandler handler, bool immediate)
    {
        AddListener(messageType, sender, delay, null, null, null, handler, immediate);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="delay">延时时间</param>
    /// <param name="param1">传参1</param>
    /// <param name="handler">事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, object sender, float delay, object param1, DelayFuncHandler handler, bool immediate)
    {
        AddListener(messageType, sender, delay, param1, null, null, handler, immediate);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType">事件key</param>
    /// <param name="sender">发起者</param>
    /// <param name="delay">延时时间</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="handler">事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, object sender, float delay, object param1, object param2, DelayFuncHandler handler, bool immediate)
    {
        AddListener(messageType, sender, delay, param1, param2, null, handler, immediate);
    }
    /// <summary>
    /// 添加延时调用函数监听器
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="sender"></param>
    /// <param name="delay"></param>
    /// <param name="param1"></param>
    /// <param name="param2"></param>
    /// <param name="param3"></param>
    /// <param name="handler"></param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, object sender, float delay, object param1, object param2, object param3, DelayFuncHandler handler, bool immediate)
    {
        DelayFuncDefinition listener = m_DelayFuncDefinitionPool.Spawn(true);
        listener.MessageType = messageType;
        listener.Sender = sender;
        listener.Delay = delay;
        listener.Param1 = param1;
        listener.Param2 = param2;
        listener.Param3 = param3;
        listener.Handler = handler;

        if (immediate)
        {
            AddListener(listener);
        }
        else
        {
            m_DelayFuncAdds.Add(listener);
        }
    }
    /// <summary>
    /// 正式添加延时调用函数监听
    /// </summary>
    /// <param name="listener"></param>
    private void AddListener(DelayFuncDefinition listener)
    {
        if (m_DelayFuncListeners.ContainsKey(listener.MessageType))
        {
            Debug.LogError("DelayFuncManager->AddListener  MessageType is alreay addListener. MessageType:" + listener.MessageType);
            return;
        }

        m_DelayFuncListeners.Add(listener.MessageType, listener);
    }
    /// <summary>
    /// 移除延时调用函数监听
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="immediate"></param>
    public void RemoveListener(string messageType, bool immediate)
    {
        if (immediate)
        {
            RemoveListener(messageType);
        }
        else
        {
            m_DelayFuncRemoves.Add(messageType);
        }
    }
    /// <summary>
    /// 正式移除延时调用函数监听
    /// </summary>
    /// <param name="messageType">事件key</param>
    public void RemoveListener(string messageType)
    {
        bool removeFail = true;
        DelayFuncDefinition listener = null;
        if (m_DelayFuncListeners.TryGetValue(messageType,out listener) && listener !=null)
        {
            m_DelayFuncListeners.Remove(messageType);
            DelayFuncDefinitionRest(listener);
            removeFail = false;
        }

        //不在已添加监听的字典内  可能在待添加内
        for (int i = 0; i < m_DelayFuncAdds.Count; i++)
        {
            listener = m_DelayFuncAdds[i];
            if (listener.MessageType == messageType)
            {
                m_DelayFuncAdds.RemoveAt(i);
                DelayFuncDefinitionRest(listener); 
                removeFail = false;
            }
        }

        if (removeFail)
        {
            Debug.LogError("DelayFuncManager->RemoveListener  messageType is not exist. MessageType:" + listener.MessageType);
        }
    }
    
    public void ResponseDelayFuncEvent(DelayFuncDefinition listener)
    {
        if (listener.Handler==null)
        {
            Debug.LogError("DelayFuncManager->ResponseDelayFuncEvent  Handler is not exist. MessageType:" + listener.MessageType);

            //修改状态 等待从字典移除
            listener.IsDone = true;
            DelayFuncDefinitionRest(listener);
            return;
        }

        DelayFuncMessage message = m_DelayFuncMessagePool.Spawn(true);
        message.MessageType = listener.MessageType;
        message.Sender = listener.Sender;
        message.Param1 = listener.Param1;
        message.Param2 = listener.Param2;
        message.Param3 = listener.Param3;

        listener.Handler(message);

        //修改状态 等待从字典移除
        listener.IsDone = true;
        DelayFuncMessageRest(message);
    }

    public void Update()
    {
        foreach (string messageType in m_DelayFuncListeners.Keys)
        {
            DelayFuncDefinition listener = m_DelayFuncListeners[messageType];

            listener.Delay -= Time.deltaTime;

            //没调用过  delay小于0 可以响应了
            if (!listener.IsDone && listener.Delay <= 0)
            {
                ResponseDelayFuncEvent(listener);
            }
            if (listener.IsDone)
            {
                m_DelayFuncLisRemoves.Add(messageType);
            }
        }
        //移除已响应过的监听
        for (int i = m_DelayFuncLisRemoves.Count - 1; i >= 0; i--)
        {
            DelayFuncDefinitionRest(m_DelayFuncListeners[m_DelayFuncLisRemoves[i]]);

            m_DelayFuncListeners.Remove(m_DelayFuncLisRemoves[i]);
        }

        //添加监听队列 响应
        for (int i = 0; i < m_DelayFuncAdds.Count; i++)
        {
            AddListener(m_DelayFuncAdds[i]);
        }

        //移除监听队列 响应
        for (int i = 0; i < m_DelayFuncRemoves.Count; i++)
        {
            RemoveListener(m_DelayFuncRemoves[i]);
        }

        m_DelayFuncRemoves.Clear();
        m_DelayFuncAdds.Clear();
        m_DelayFuncLisRemoves.Clear();

    }
}
