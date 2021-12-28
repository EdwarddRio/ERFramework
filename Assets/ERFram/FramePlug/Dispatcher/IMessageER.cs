using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 消息收发器中消息必须有的基本属性接口
/// </summary>
public interface IMessageER 
{
    //消息字符串 相当于key
    String Type { get; set; }
    //消息发送者 gameobj或者字符串
    object Sender { get; set; }
    //消息接收者 gameobj或者字符串
    object Recipient { get; set; }
    //调用的延迟时间
    float Delay { get; set; }
    //传递的数据
    object Param1 { get; set; }
    object Param2 { get; set; }
    object Param3 { get; set; }
    //消息是否传递完毕 完毕后做回收
    Boolean IsDone { get; set; }
    //回收清理数据
    void Reset();
}
