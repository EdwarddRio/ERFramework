using System;
using System.Collections;
using System.Collections.Generic;


[Serializable]
public abstract class TableCollectionManager<T, U> : Singleton<U>, ITableManager where T : ITableItem
{
	// abstract functions need tobe implements.
    public abstract string TableName();
    public object TableData { get { return mAllItemArray; } }

    T[] mAllItemArray;
	List< List<T> > mItemCollection = new List< List<T> >();
    Dictionary<int, int> mKeyCollectionMap = new Dictionary<int, int>();

    // constructor.
    internal TableCollectionManager()
    {
        // load from excel txt file.
        mAllItemArray = TableParser.Parse<T>(TableName());
		
		int key = 0;
		int index = 0;
        // build the key-value map.
        for (int i=0; i<mAllItemArray.Length && index<mAllItemArray.Length; i++)
		{
			// now key is for Collection, no longer for each item
			key = mAllItemArray[index].Key();
			mKeyCollectionMap[key] = i;
	
			index = FillItemCollection(key, index);
		}
    }

	// find all item that has the same key.
	int FillItemCollection(int key, int startPos)
	{
		List<T> itemList = new List<T>();
		itemList.Add(mAllItemArray[startPos]);
		for(int i = startPos+1; i < mAllItemArray.Length; i++)
		{
			if (mAllItemArray[i].Key() == key)
				itemList.Add(mAllItemArray[i]);
			else
			{
				startPos = i;
				break;
			}
		}
		mItemCollection.Add(itemList);
		return startPos;
	}
    // get a Collection base the key.
    public List<T> GetCollection(int key)
    {
        int CollectionIndex;
        if (mKeyCollectionMap.TryGetValue(key, out CollectionIndex))
            return mItemCollection[CollectionIndex];
        return default(List<T>);
    }
	
    // get the Collection.
	public List< List<T> > GetAllCollection()
	{
		return mItemCollection;
	}
}