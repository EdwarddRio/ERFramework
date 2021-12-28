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
        taskcount = DownmgrNative.Instance.TaskCount + DownmgrNative.Instance.RunnerCount;
        //tasksize = 0;
        downloadcount = DownmgrNative.Instance.RunnerCount;
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