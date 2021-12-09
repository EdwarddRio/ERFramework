using UnityEngine;
using System.Collections;

public class TaskState
{
    public int taskcount = 0;
    //public int tasksize;
    public int downloadcount = 0;
    //public int downloadsize;

    public void Clear()
    {
        taskcount = DownmgrNative.Instance.taskCount + DownmgrNative.Instance.runnerCount;
        //tasksize = 0;
        downloadcount = DownmgrNative.Instance.runnerCount;
        //downloadsize = 0;
    }

    public override string ToString()
    {
        return downloadcount + "/" + taskcount;
    }

    public float per()
    {
        return (float)downloadcount / (float)taskcount;
    }
}