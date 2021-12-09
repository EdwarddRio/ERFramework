using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//0=免费 1=金币 2=视频
public enum PlaneLOCKTYPE
{
    Free = 0,
    DIAMOND,
    ADFREE,
}
public class PlaneData : ITableItem, IComparable<PlaneData>
{


    public int ShipId;
    public string Decription;//描述
    public int SortId;//序号
    public string Model;//模型
    public int CarLockType;//解锁方式：0=免费 1=金币 2=视频
    public int CarLockNum;//解锁数量（车）
    public string Name;
    public string Level;
    public int Cover;//角标 0=无 1=Hot 2=New
    public int SkillType;//技能
    public float SkillNum;//技能数值
    public int RecPriority;//推荐优先级


    public int CompareTo(PlaneData other)
    {
        throw new NotImplementedException();
    }

    public int Key()
    {
        return ShipId;
    }
  
    public PlaneLOCKTYPE LockType
    {
        get
        {
            return (PlaneLOCKTYPE)CarLockType;
        }
    }
};

public class PlaneDataManager : TableManager<PlaneData, PlaneDataManager>
{

    public override string TableName() {
        return ConStr.TABLESPATH+ "PlaneData.txt";
    }
    public PlaneData GetItemByPlanemodel(string planemodel)
    {
        PlaneData[] mItemArray = GetAllItem();
        for (int i = 0; i < mItemArray.Length; i++)
        {
            if (mItemArray[i].Model == planemodel)
            {
                return mItemArray[i];
            }
        }
        return null;
    }
    public PlaneDataManager()
    {
     
    }
}
