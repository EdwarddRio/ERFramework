using System;
using System.Collections;
using System.Collections.Generic;

public interface ITableItem
{
    int Key();
}

public interface ITableManager
{
    string TableName();
}

public abstract class TableManager<T, U> : Singleton<U>, ITableManager where T : ITableItem
{
    // abstract functions need tobe implements.
    public abstract string TableName();

    // the data arrays.
    T[] mItemArray;
    Dictionary<int, int> mKeyItemMap = new Dictionary<int, int>();

    // constructor.
    internal TableManager()
    {
        // load from excel txt file.
        mItemArray = TableParser.Parse<T>(TableName());
        if(mItemArray == null)
        {
            UnityEngine.Debug.LogError("Table paser err: " + TableName() + " is null.");
        }

        // build the key-value map.
        for (int i = 0; i < mItemArray.Length; i++)
        {
            int key = mItemArray[i].Key();
            if (0 == key) continue;

            //if (!mKeyItemMap.ContainsKey(key))
                mKeyItemMap[key] = i;
            //else
            //    throw new System.ArgumentException(string.Format("Table {0} has same key in line {1}", TableName(), i));
        }
    }
    public void Reload()
    {
        mItemArray = TableParser.Parse<T>(TableName());
        if (mItemArray == null)
        {
            UnityEngine.Debug.LogError("Table paser err: " + TableName() + " is null.");
        }

        // build the key-value map.

        for (int i = 0; i < mItemArray.Length; i++)
        {
            int key = mItemArray[i].Key();
            if (0 == key) continue;

            //if (!mKeyItemMap.ContainsKey(key))
            mKeyItemMap[key] = i;
            //else
            //    throw new System.ArgumentException(string.Format("Table {0} has same key in line {1}", TableName(), i));
        }
    }

    // get a item base the key.
    public T GetItem(int key)
    {
        int itemIndex;
        if (mKeyItemMap.TryGetValue(key, out itemIndex))
            return mItemArray[itemIndex];
        return default;
    }

    public bool ContainsItem(int key)
    {
        return mKeyItemMap.ContainsKey(key);
    }
	
    // get the item array.
	public T[] GetAllItem()
	{
		return mItemArray;
	}

    public T GetFirstItem()
    {
        if (mItemArray.Length > 0)
            return mItemArray[0];
        return default;
    }

    public int GetAllCount()
    {
        return mItemArray.Length;
    }
}