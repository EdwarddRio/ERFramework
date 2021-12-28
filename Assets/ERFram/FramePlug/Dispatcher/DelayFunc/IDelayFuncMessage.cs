using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//延时函数传参类接口
public interface IDelayFuncMessage
{
    string MessageType { get; set; }
    object Sender { get; set; }
    object Param1 { get; set; }
    object Param2 { get; set; }
    object Param3 { get; set; }

    void Reset();
}
