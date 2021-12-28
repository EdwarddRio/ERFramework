using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 添加消息监听器的中间键，定义基本类型就接口
/// </summary>
public interface IMessageListenerDefinitionER
{
    //消息字符串 相当于key
    string MessageType { get; set; }
    //消息调用者(过滤器)，用来指定被调用者或者所有人都可以
    string CallerStr { get; set; }
    //消息事件
    MessageHandler Handler { get; set; }

    //回收清理数据
    void Reset();
}
