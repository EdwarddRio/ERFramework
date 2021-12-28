using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageER : IMessageER
{
    public string Type { get; set; }
    public object Sender { get; set; }
    public object Recipient { get; set; }
    public float Delay { get; set; }
    public object Param1 { get; set; }
    public object Param2 { get; set; }
    public object Param3 { get; set; }
    public bool IsDone { get; set; }

    public void Reset()
    {
        Type = string.Empty;
        Sender = Recipient = null;
        Delay = 0;
        Param1 = Param2 = Param3 = null;
        IsDone = false;
    }
}
