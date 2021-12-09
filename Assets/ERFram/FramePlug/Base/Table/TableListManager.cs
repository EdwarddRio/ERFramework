using System.Collections;
using System.Collections.Generic;

public abstract class TableListManager<T, U> : Singleton<U> where T : ITableItem
{
    Dictionary<string, T[]> mTables = new Dictionary<string, T[]>();

    internal TableListManager()
    {

    }

    public T[] Load(string table)
    {
        if (!mTables.ContainsKey(table))
        {
            T[] datas = TableParser.Parse<T>(table);
            if (datas == null)
            {
                UnityEngine.Debug.LogError("Table paser err: " + table + " is null.");
                return null;
            }

            mTables.Add(table, datas);
        }

        return mTables[table];
    }

    public T[] GetTable(string table)
    {
        if (mTables.ContainsKey(table))
            return mTables[table];
        return null;
    }
}

