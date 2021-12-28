using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayFuncDefinition
{
    public string MessageType { get; set; }
    public object Sender { get; set; }
    public float Delay { get; set; }
    public object Param1 { get; set; }
    public object Param2 { get; set; }
    public object Param3 { get; set; }
    public bool IsDone { get; set; }
    public DelayFuncHandler Handler { get; set; }

    public  void Reset()
    {
        MessageType = string.Empty;
        Sender = Param1 = Param2 = Param3 = null;
        Delay = 0;
        IsDone = false;
        Handler = null;
    }
}
