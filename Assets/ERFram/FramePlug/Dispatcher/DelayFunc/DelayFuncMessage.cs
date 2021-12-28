using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//延时函数传参类
public class DelayFuncMessage : IDelayFuncMessage
{
    public string MessageType { get; set; }
    public object Sender { get; set; }
    public object Param1 { get; set; }
    public object Param2 { get; set; }
    public object Param3 { get; set; }

    public void Reset()
    {
        MessageType = string.Empty;
        Sender = Param1 = Param2 = Param3 = null;
    }
}
