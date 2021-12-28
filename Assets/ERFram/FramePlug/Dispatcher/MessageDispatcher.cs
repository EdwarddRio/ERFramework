using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//消息事件的委托
public delegate void MessageHandler(IMessageER message);


/* 消息分发器
 * 分发器的update放在 framework 里面做管理。
 * unity的点击触发事件 会在一帧的渲染完毕后再触发，触发消息后判定 使用时机
 * -1：下一帧触发
 * 0：立即触发
 * N：n秒后触发
 * 
 * 底层用对象池 管理分发器的消息类
 * 消息触发后 及时回收
 */
public class MessageDispatcher : Singleton<MessageDispatcher>
{
    //消息事件的保存字典 第一个key是MessageType  第二个key是name或空字符串 用来指定可以被谁调用
    private Dictionary<string, Dictionary<string, MessageHandler>> m_MessageHandlers = new Dictionary<string, Dictionary<string, MessageHandler>>();

    //中间类，类对象池
    private ClassObjectPool<MessageER> m_MessageERPool = new ClassObjectPool<MessageER>(50);
    private ClassObjectPool<MessageListenerDefinitionER> m_MessageListenerDefinitionERPool = new ClassObjectPool<MessageListenerDefinitionER>(50);

    //还未发送的消息列表
    private List<MessageER> m_Messages = new List<MessageER>();
    //等待添加的消息监听器队列
    private Queue<MessageListenerDefinitionER> m_ListenerAdds = new Queue<MessageListenerDefinitionER>();
    //等待移除的消息监听器队列
    private Queue<MessageListenerDefinitionER> m_ListenerRemoves = new Queue<MessageListenerDefinitionER>();

    /// <summary>
    /// 消息回收函数
    /// </summary>
    /// <param name="message"></param>
    private void MessageRest(MessageER message)
    {
        message.Reset();
        m_MessageERPool.Recycle(message);
    }
    /// <summary>
    /// 消息中间键回收函数
    /// </summary>
    /// <param name="messageListenerDefinition"></param>
    private void MessageListenerDefinitionRest(MessageListenerDefinitionER messageListenerDefinition)
    {
        messageListenerDefinition.Reset();
        m_MessageListenerDefinitionERPool.Recycle(messageListenerDefinition);
    }
    /// <summary>
    /// 清空所有消息监听
    /// </summary>
    public void ClearMessages()
    {
        for (int i = 0; i < m_Messages.Count; i++)
        {
            MessageRest(m_Messages[i]);
        }
        m_Messages.Clear();
    }
    /// <summary>
    /// 清空所有监听事件
    /// </summary>
    public void ClearListeners()
    {
        foreach (string typeStr in m_MessageHandlers.Keys)
        {
            Dictionary<string, MessageHandler> recipientDictionary = m_MessageHandlers[typeStr];
            foreach (string callerStr in recipientDictionary.Keys)
            {
                recipientDictionary[callerStr] = null;
            }
            recipientDictionary.Clear();
        }

        while (m_ListenerAdds.Count > 0)
        {
            MessageListenerDefinitionRest(m_ListenerAdds.Dequeue());
        }

        while (m_ListenerRemoves.Count > 0)
        {
            MessageListenerDefinitionRest(m_ListenerRemoves.Dequeue());
        }

        m_MessageHandlers.Clear();
    }
    /// <summary>
    /// 添加消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    public void AddListener(string messageType, MessageHandler handler)
    {
        AddListener(messageType, string.Empty, handler, false);
    }
    /// <summary>
    /// 添加消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, MessageHandler handler, bool immediate)
    {
        AddListener(messageType, string.Empty, handler, immediate);
    }
    /// <summary>
    /// 添加消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="callerStr">过滤器，为空字符串不限制</param>
    /// <param name="handler">消息事件</param>
    public void AddListener(string messageType, string callerStr, MessageHandler handler)
    {
        AddListener(messageType, callerStr, handler, false);
    }
    /// <summary>
    /// 添加消息监听函数
    /// </summary>
    /// <param name="owner">获取添加者的名字</param>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    public void AddListener(UnityEngine.Object owner, string messageType, MessageHandler handler)
    {
        AddListener(owner, messageType, handler, false);
    }

    /// <summary>
    /// 添加消息监听函数
    /// </summary>
    /// <param name="owner">获取添加者的名字</param>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(UnityEngine.Object owner, string messageType, MessageHandler handler, bool immediate)
    {
        if (owner == null)
        {
            AddListener(messageType, string.Empty, handler, immediate);
        }
        else if (owner is UnityEngine.Object)
        {
            AddListener(messageType, owner.name, handler, immediate);
        }
        else
        {
            AddListener(messageType, string.Empty, handler, immediate);
        }
    }
    /// <summary>
    /// 添加消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="callerStr">过滤器，为空字符串不限制</param>
    /// <param name="handler">消息事件</param>
    /// <param name="immediate">是否直接加入监听列表(false:不立即加入列表，不破坏当前帧循环到的函数执行。列表增加时是在分发器的循环中)</param>
    public void AddListener(string messageType, string callerStr, MessageHandler handler, bool immediate)
    {
        MessageListenerDefinitionER listener = m_MessageListenerDefinitionERPool.Spawn(true);
        listener.MessageType = messageType;
        listener.CallerStr = callerStr;
        listener.Handler = handler;

        if (immediate)
        {
            AddListener(listener);
        }
        else
        {
            m_ListenerAdds.Enqueue(listener);
        }
    }
    /// <summary>
    /// 正式添加消息监听
    /// </summary>
    /// <param name="listener"></param>
    private void AddListener(MessageListenerDefinitionER listener)
    {
        Dictionary<string, MessageHandler> recipientDictionary = null;

        //不在字典内
        if (!m_MessageHandlers.TryGetValue(listener.MessageType, out recipientDictionary) || recipientDictionary == null)
        {
            recipientDictionary = new Dictionary<string, MessageHandler>();
            m_MessageHandlers.Add(listener.MessageType, recipientDictionary);
        }

        if (!recipientDictionary.ContainsKey(listener.CallerStr))
        {
            recipientDictionary.Add(listener.CallerStr, null);
        }
        //事件链
        recipientDictionary[listener.CallerStr] += listener.Handler;
        

        MessageListenerDefinitionRest(listener);
    }
    /// <summary>
    /// 移除消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    public void RemoveListener(string messageType, MessageHandler handler)
    {
        RemoveListener(messageType, string.Empty, handler, false);
    }
    /// <summary>
    /// 移除消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    /// <param name="immediate">是否直接加入监听列表</param>
    public void RemoveListener(string messageType, MessageHandler handler, bool immediate)
    {
        RemoveListener(messageType, string.Empty, handler, immediate);
    }
    /// <summary>
    /// 移除消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="callerStr">过滤器，为空字符串不限制</param>
    /// <param name="handler">消息事件</param>
    public void RemoveListener(string messageType, string callerStr, MessageHandler handler)
    {
        RemoveListener(messageType, callerStr, handler, false);
    }
    /// <summary>
    /// 移除消息监听函数
    /// </summary>
    /// <param name="owner">获取添加者的名字</param>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    public void RemoveListener(UnityEngine.Object owner, string messageType, MessageHandler handler)
    {
        RemoveListener(owner, messageType, handler, false);
    }
    /// <summary>
    /// 移除消息监听函数
    /// </summary>
    /// <param name="owner">获取添加者的名字</param>
    /// <param name="messageType">消息key</param>
    /// <param name="handler">消息事件</param>
    /// <param name="immediate">是否直接加入监听列表</param>
    public void RemoveListener(UnityEngine.Object owner, string messageType, MessageHandler handler, bool immediate)
    {
        if (owner == null)
        {
            RemoveListener(messageType, string.Empty, handler, immediate);
        }
        else if (owner is UnityEngine.Object)
        {
            RemoveListener(messageType, owner.name, handler, immediate);
        }
        else
        {
            RemoveListener(messageType, string.Empty, handler, immediate);
        }
    }
    /// <summary>
    /// 移除消息监听函数
    /// </summary>
    /// <param name="messageType">消息key</param>
    /// <param name="callerStr">过滤器，为空字符串不限制</param>
    /// <param name="handler">消息事件</param>
    /// <param name="immediate">是否直接加入监听列表</param>
    public void RemoveListener(string messageType, string callerStr, MessageHandler handler, bool immediate)
    {
        MessageListenerDefinitionER listener = m_MessageListenerDefinitionERPool.Spawn(true);
        listener.MessageType = messageType;
        listener.CallerStr = callerStr;
        listener.Handler = handler;

        if (immediate)
        {
            RemoveListener(listener);
        }
        else
        {
            m_ListenerRemoves.Enqueue(listener);
        }
    }
    /// <summary>
    /// 正式移除消息监听
    /// </summary>
    /// <param name="listener"></param>
    private void RemoveListener(MessageListenerDefinitionER listener)
    {
        Dictionary<string, MessageHandler> recipientDictionary = null;

        //在字典内
        if (m_MessageHandlers.TryGetValue(listener.MessageType, out recipientDictionary) && recipientDictionary != null)
        {
            if (recipientDictionary.ContainsKey(listener.CallerStr))
            {
                if (recipientDictionary[listener.CallerStr] != null)
                {
                    recipientDictionary[listener.CallerStr] -= listener.Handler;
                }

                if (recipientDictionary[listener.CallerStr] == null)
                {
                    recipientDictionary.Remove(listener.CallerStr);
                }
            }
            if (recipientDictionary.Count == 0)
            {
                m_MessageHandlers.Remove(listener.MessageType);
            }
        }
        MessageListenerDefinitionRest(listener);
    }

    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="type">消息名</param>
    public void SendMessage(string type)
    {
        MessageER message = m_MessageERPool.Spawn(true);
        message.Sender = null;
        message.Recipient = string.Empty;
        message.Type = type;
        message.Param1 = message.Param2 = message.Param3 = null;
        message.Delay = MessageDelayEnum.IMMEDIATE;

        SendMessage(message);
    }

    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="type">消息名</param>
    /// <param name="recipient">接收者(对象)</param>
    public  void SendMessage(string type, string recipient)
    {
        MessageER message = m_MessageERPool.Spawn(true);
        message.Sender = null;
        message.Recipient = recipient;
        message.Type = type;
        message.Param1 = message.Param2 = message.Param3 = null;
        message.Delay = MessageDelayEnum.IMMEDIATE;

        SendMessage(message);
    }

    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="type">消息名</param>
    /// <param name="delay">延时</param>
    public void SendMessage(string type, float delay)
    {
        MessageER message = m_MessageERPool.Spawn(true);
        message.Sender = null;
        message.Recipient = string.Empty;
        message.Type = type;
        message.Param1 = message.Param2 = message.Param3 = null;
        message.Delay = delay;

        SendMessage(message);
    }

    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="type">消息名</param>
    /// <param name="recipient">接收者(对象)</param>
    /// <param name="delay">延时</param>
    public void SendMessage(string type, string recipient, float delay)
    {
        MessageER message = m_MessageERPool.Spawn(true);
        message.Sender = null;
        message.Recipient = recipient;
        message.Type = type;
        message.Param1 = message.Param2 = message.Param3= null;
        message.Delay = delay;

        SendMessage(message);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, string type, object param1, float delay)
    {
        SendMessage(sender, type, param1, null, null, delay);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, string type, object param1, object param2,float delay)
    {
        SendMessage(sender, type, param1, param2, null, delay);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="param3">传参3</param>
    /// <param name="delay">延时</param>
    public  void SendMessage(object sender, string type, object param1, object param2, object param3, float delay)
    {
        MessageER message = m_MessageERPool.Spawn(true);
        message.Sender = sender;
        message.Recipient = string.Empty;
        message.Type = type;
        message.Param1 = param1;
        message.Param2 = param2;
        message.Param3 = param3;
        message.Delay = delay;

        SendMessage(message);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="recipient">接收者(对象)</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, object recipient, string type, object param1, float delay)
    {
        SendMessage(sender, recipient, type, param1, null, null, delay);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="recipient">接收者(对象)</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, object recipient, string type, object param1, object param2, float delay)
    {
        SendMessage(sender, recipient, type, param1, param2, null, delay);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="recipient">接收者(对象)</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="param3">传参3</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, object recipient, string type,object param1, object param2, object param3, float delay)
    {
        MessageER message = m_MessageERPool.Spawn(true);
        message.Sender = sender;
        message.Recipient = recipient;
        message.Type = type;
        message.Param1 = param1;
        message.Param2 = param2;
        message.Param3 = param3;
        message.Delay = delay;

        SendMessage(message);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="recipient">接收者(直接用字符串)</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, string recipient, string type, object param1, float delay)
    {
        SendMessage(sender, recipient, type, param1, null, null, delay);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="recipient">接收者(直接用字符串)</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, string recipient, string type, object param1, object param2,  float delay)
    {
        SendMessage(sender, recipient, type, param1, param2, null, delay);
    }
    /// <summary>
    /// 通过消息调用函数
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="recipient">接收者(直接用字符串)</param>
    /// <param name="type">消息名</param>
    /// <param name="param1">传参1</param>
    /// <param name="param2">传参2</param>
    /// <param name="param3">传参3</param>
    /// <param name="delay">延时</param>
    public void SendMessage(object sender, string recipient, string type, object param1, object param2, object param3, float delay)
    {
        MessageER message = m_MessageERPool.Spawn(true);
        message.Sender = sender;
        message.Recipient = recipient;
        message.Type = type;
        message.Param1 = param1;
        message.Param2 = param2;
        message.Param3 = param3;
        message.Delay = delay;

        SendMessage(message);
    }
    /// <summary>
    /// 通过消息调用对应函数
    /// </summary>
    /// <param name="message"></param>
    public void SendMessage(MessageER message)
    {
        //没找到对应消息函数
        bool missRecipient = true;

        //非立即调用 加入列表等时间到了再调用
        if (message.Delay !=MessageDelayEnum.IMMEDIATE)
        {
            if (!m_Messages.Contains(message))
            {
                m_Messages.Add(message);
            }
            missRecipient = false;
        }
        //立即调用对应函数
        else
        {
            Dictionary<string, MessageHandler> recipientDictionary = null;
            if (m_MessageHandlers.TryGetValue(message.Type, out recipientDictionary) && recipientDictionary != null)
            {
                foreach (string callerStr in recipientDictionary.Keys)
                {
                    //发现对应callerStr为空，移除掉。 做个保护
                    if (recipientDictionary[callerStr] == null)
                    {
                        RemoveListener(message.Type, callerStr, null);
                        continue;
                    }

                    if (callerStr == string.Empty)
                    {
                        message.IsDone = true;
                        recipientDictionary[callerStr](message);
                        missRecipient = false;
                    }
                    else if (message.Recipient is UnityEngine.Object)
                    {
                        //名称相同 对应调用
                        if (callerStr.ToLower() == (message.Recipient as UnityEngine.Object).name.ToLower())
                        {
                            message.IsDone = true;
                            recipientDictionary[callerStr](message);
                            missRecipient = false;
                        }
                    }
                    else if (message.Recipient is string)
                    {
                        message.IsDone = true;
                        recipientDictionary[callerStr](message);
                        missRecipient = false;
                    }
                }
            }
            //调用到消息了，那再回收
            if (!missRecipient)
            {
                //回收掉调用消息
                MessageRest(message);
            }
        }

        // If we were unable to send the message, we may need to report it
        if (missRecipient)
        {
            Debug.LogError("MessageDispatcher->Message type Func is not exist.  type:" + message.Type + "  Recipient:" + message.Recipient);

            message.IsDone = true;
            //回收掉调用消息
            MessageRest(message);
        }
    }

    public void Update()
    {
        //循环消息
        for (int i = 0; i < m_Messages.Count; i++)
        {
            MessageER message = m_Messages[i];

            //说明添加时就是小于0的，放到下一帧再去调用
            if (message.Delay < 0)
            {
                message.Delay = 0;
                continue;
            }

            message.Delay -= Time.deltaTime;
            if (message.Delay < 0)
            {
                message.Delay = MessageDelayEnum.IMMEDIATE;
            }
            //没调用过  delay又是0 立马调用一下
            if (!message.IsDone && message.Delay == MessageDelayEnum.IMMEDIATE)
            {
                SendMessage(message);
            }
        }
        //移除已响应过的消息类
        for (int i = m_Messages.Count - 1; i >= 0; i--)
        {
            MessageER message = m_Messages[i];
            if (message.IsDone)
            {
                m_Messages.RemoveAt(i);
            }
        }

        //移除监听队列 响应
        while (m_ListenerRemoves.Count > 0)
        {
            RemoveListener(m_ListenerRemoves.Dequeue());
        }

        //添加监听队列 响应
        while (m_ListenerAdds.Count > 0)
        {
            AddListener(m_ListenerAdds.Dequeue());
        }
    }
}

public class MessageDelayEnum
{
    //立即调用
    public static float IMMEDIATE = 0;
    //下一帧调用
    public static float NEXT_UPDATE = -1;
    
    //其他就是根据float值 延时调用了

}