using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageListenerDefinitionER : IMessageListenerDefinitionER
{
    public string MessageType { get; set; }

    public string CallerStr { get; set; }

    public MessageHandler Handler { get; set; }

    public void Reset()
    {
        MessageType = string.Empty;
        CallerStr = string.Empty;
        Handler = null;
    }
}
