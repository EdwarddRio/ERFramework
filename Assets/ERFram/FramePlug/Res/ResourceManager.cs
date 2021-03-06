using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

/* 可优化：
 * 传参部分object类型会涉及到装箱拆箱，
 * 
 * 思路：所有类型参数放到字典内，传值以两个int为键值对 前一个:(key的index) 后一个:(value的index)    
 * 
 * 值类型可以固定住特殊索引，实际获取时用switch判定
 * Dictionary<object,int> object:类型 int：对应的key的index
 * 值类型直接用多个list<值类型>存入
 * Dictionary<int,list<object>> int:key的index  list<object>:暂存的值对应(value的index)   
 * 
 * 数据存入时可以用for循环判定list有null的就直接放入 否则添加到最后   值类型的话可以判定是否为0等，用一个特殊规则限定
 * 
 */

public enum LoadResPriority
{
    RES_HIGHT = 0,//最高优先级
    RES_MIDDLE,//一般优先级
    RES_SLOW,//低优先级
    RES_NUM,
}
/// <summary>
/// 中间类，用来ObjectManager和ResourceManager结合
/// </summary>
public class ResouceObj
{
    //路径对应CRC
    public uint m_Crc = 0;
    //存ResouceItem  从ObjectManager到ResourceManager去取到ResouceItem
    public ResouceItem m_ResItem = null;
    //实例化出来的GameObject
    public GameObject m_CloneObj = null;
    //是否跳场景清除
    public bool m_bClear = true;
    //储存GUID
    public long m_Guid = 0;
    //是否已经放回对象池
    public bool m_Already = false;
    //--------------------------------
    //是否放到场景节点下面
    public bool m_SetSceneParent = false;
    //实例化资源加载完成回调
    public OnAsyncObjFinish m_DealFinish = null;
    //异步参数
    public object m_Param1, m_Param2, m_Param3 = null;
    //离线数据
    public OfflineData m_OfflineData = null;

    public void Reset()
    {
        m_Crc = 0;
        m_CloneObj = null;
        m_bClear = true;
        m_Guid = 0;
        m_ResItem = null;
        m_Already = false;
        m_SetSceneParent = false;
        m_DealFinish = null;
        m_Param1 = m_Param2 = m_Param3 = null;
        m_OfflineData = null;
    }
}

public class AsyncLoadResParam
{
    //加载完成的回调列表  防止同一个资源多个加载完成事件
    public List<AsyncCallBack> m_CallBackList = new List<AsyncCallBack>();

    public uint m_Crc;
    public string m_Path;
    //Obj和Sprite没法相互转换，加一个状态来判断
    public bool m_Sprite = false;
    public LoadResPriority m_Priority = LoadResPriority.RES_SLOW;

    public void Reset()
    {
        m_CallBackList.Clear();
        m_Crc = 0;
        m_Path = "";
        m_Sprite = false;
        m_Priority = LoadResPriority.RES_SLOW;
    }
}

public class AsyncCallBack
{
    //加载完成的回调(针对ObjectManager)
    public OnAsyncFinsih m_DealFinish = null;
    //ObjectManager对应的中间
    public ResouceObj m_ResObj = null;
//---------------------------------------------
    //加载完成的回调
    public OnAsyncObjFinish m_DealObjFinish = null;
    //回调参数
    public object m_Param1 = null, m_Param2 = null, m_Param3 = null;

    public void Reset()
    {
        m_DealObjFinish = null;
        m_DealFinish = null;
        m_Param1 = null;
        m_Param2 = null;
        m_Param3 = null;
        m_ResObj = null;
    }
}

//资源加载完成回调
public delegate void OnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null);

//实例化对象加载完成回调
public delegate void OnAsyncFinsih(string path, ResouceObj resObj, object param1 = null, object param2 = null, object param3 = null);

/// <summary>
/// ResourceManager管理的都是底层不需要实例化的资源
/// </summary>
public class ResourceManager : Singleton<ResourceManager>
{
    protected long m_Guid = 0;
    //缓存使用的资源列表
    public Dictionary<uint, ResouceItem> AssetDic { get; set; } = new Dictionary<uint, ResouceItem>();
    //缓存引用计数为零的资源列表，达到缓存最大的时候释放这个列表里面最早没用的资源
    protected CMapList<ResouceItem> m_NoRefrenceAssetMapList = new CMapList<ResouceItem>();

    //中间类，回调类的类对象池
    protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool = new ClassObjectPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncCallBack> m_AsyncCallBackPool = new ClassObjectPool<AsyncCallBack>(100);

    //Mono脚本
    protected MonoBehaviour m_Startmono;
    //正在异步加载的资源列表
    protected List<AsyncLoadResParam>[] m_LoadingAssetList = new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
    //正在异步加载的Dic
    protected Dictionary<uint, AsyncLoadResParam> m_LoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();

    //最长连续卡着加载资源的时间，单位微妙
    private const long MAXLOADRESTIME = 200000;

    //最大缓存个数
    private const int MAXCACHECOUNT = 500;

    public void Init(MonoBehaviour mono)
    {
        for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
        {
            m_LoadingAssetList[i] = new List<AsyncLoadResParam>();
        }
        m_Startmono = mono;
        m_Startmono.StartCoroutine(AsyncLoadCor());
    }

    /// <summary>
    /// 创建唯一的GUID
    /// </summary>
    /// <returns></returns>
    public long CreateGuid()
    {
        //new System.Guid();//这样会产生gc
        return m_Guid++;
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void ClearCache()
    {
        List<ResouceItem> tempList = new List<ResouceItem>();
        foreach (ResouceItem item in AssetDic.Values)
        {
            if (item.m_Clear)
            {
                tempList.Add(item);
            }
        }

        foreach (ResouceItem item in tempList)
        {
            DestoryResouceItme(item, true);
        }
        tempList.Clear();
    }

    /// <summary>
    /// 取消异步加载资源
    /// </summary>
    /// <returns></returns>
    public bool CancleLoad(ResouceObj res)
    {
        AsyncLoadResParam para = null;
        //如果取消时正好在加载过程中的话m_LoadingAssetList当中不会存在
        if (m_LoadingAssetDic.TryGetValue(res.m_Crc, out para) && m_LoadingAssetList[(int)para.m_Priority].Contains(para))
        {
            for (int i = para.m_CallBackList.Count; i >= 0; i--)
            {
                AsyncCallBack tempCallBack = para.m_CallBackList[i];
                if (tempCallBack != null && res == tempCallBack.m_ResObj)
                {
                    tempCallBack.Reset();
                    m_AsyncCallBackPool.Recycle(tempCallBack);
                    para.m_CallBackList.Remove(tempCallBack);
                }
            }

            if (para.m_CallBackList.Count <= 0)
            {
                para.Reset();
                m_LoadingAssetList[(int)para.m_Priority].Remove(para);
                m_AsyncLoadResParamPool.Recycle(para);
                m_LoadingAssetDic.Remove(res.m_Crc);
                return true;
            }
        }
        else
        {
            if (res.m_ResItem == null)
            {
                Debug.LogError("res cant cancle load. crc:" + res.m_Crc + "  and m_ResItem is null");
            }
            else
            {
                Debug.LogError("res cant cancle load. crc:" + res.m_Crc + "  m_AssetName:" + res.m_ResItem.m_AssetName);
            }
        }

        return false;
    }

    /// <summary>
    /// 根据ResObj增加引用计数
    /// </summary>
    /// <returns></returns>
    public int IncreaseResouceRef( ResouceObj resObj, int count =1)
    {
        return resObj != null ? IncreaseResouceRef(resObj.m_Crc, count) : 0;
    }

    /// <summary>
    /// 根据path增加引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int IncreaseResouceRef(uint crc = 0, int count = 1)
    {
        ResouceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
            return 0;

        item.RefCount += count;
        item.m_LastUseTime = Time.realtimeSinceStartup;

        return item.RefCount;
    }

    /// <summary>
    /// 根据ResouceObj减少引用计数
    /// </summary>
    /// <param name="resObj"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResoucerRef(ResouceObj resObj, int count = 1)
    {
        return resObj != null ? DecreaseResoucerRef(resObj.m_Crc, count) : 0;
    }

    /// <summary>
    /// 根据路径减少引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResoucerRef(uint crc, int count = 1)
    {
        ResouceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
            return 0;

        item.RefCount -= count;

        return item.RefCount;
    }

    /// <summary>
    /// 预加载资源
    /// </summary>
    /// <param name="path"></param>
    public void PreloadRes(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        uint crc = Crc32.GetCrc32(path);
        ResouceItem item = GetCacheResouceItem(crc, 0);
        if (item != null)
        {
            return;
        }

        Object obj = null;
#if UNITY_EDITOR
        if (!Const.m_LoadFormAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItme(crc);
            if (item != null && item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                if (item == null)
                {
                    item = new ResouceItem();
                    item.m_Crc = crc;
                }
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResouceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                }
            }
        }

        CacheResource(path, ref item, crc, obj);
        //跳场景不清空缓存
        item.m_Clear = false;
        ReleaseResouce(obj, false);
        obj = null;
    }



    /// <summary>
    /// 新图集同步加载方法
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Sprite LoadSpriteBySpriteAtlas(string path)
    {
        string spriteName = path.Remove(0, path.LastIndexOf('/') + 1);
        spriteName = spriteName.Remove(spriteName.LastIndexOf('.'));
        string filePath = path.Remove(path.LastIndexOf('/'));
        string spriteAtlasPath = string.Format("{0}.spriteatlas", filePath);
        SpriteAtlas spriteAtlas = LoadResource<SpriteAtlas>(spriteAtlasPath);
        if (spriteAtlas == null)
        {
            Debug.LogError("SpriteAtlas is not exist：" + spriteAtlasPath);
            return null;
        }
        else
        {
            return spriteAtlas.GetSprite(spriteName);
        }
    }
    /// <summary>
    /// 新图集异步加载方法
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void LoadSpriteBySpriteAtlasAsync(string path, OnAsyncObjFinish onAsyncObjFinish, object param1 = null, object param2 = null, object param3 = null)
    {
        string spriteName = path.Remove(0, path.LastIndexOf('/') + 1);
        spriteName = spriteName.Remove(spriteName.LastIndexOf('.'));
        string filePath = path.Remove(path.LastIndexOf('/'));
        string spriteAtlasPath = string.Format("{0}.spriteatlas", filePath);
        //spriteAtlas用Object类型能读取到
        AsyncLoadResource(spriteAtlasPath, onAsyncObjFinish, LoadResPriority.RES_HIGHT, false, param1, param2,spriteName);
    }

    /// <summary>
    /// 同步加载资源，针对给ObjectManager的接口
    /// </summary>
    /// <param name="path"></param>
    /// <param name="resObj"></param>
    /// <returns></returns>
    public ResouceObj LoadResource(string path, ResouceObj resObj)
    {
        if (resObj == null)
        {
            return null;
        }

        uint crc = resObj.m_Crc == 0 ? Crc32.GetCrc32(path) : resObj.m_Crc;

        ResouceItem item = GetCacheResouceItem(crc);
        if (item != null)
        {
            resObj.m_ResItem = item;
            return resObj;
        }

        Object obj = null;
#if UNITY_EDITOR
        if (!Const.m_LoadFormAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItme(crc);
            if (item != null && item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                if (item == null)
                {
                    item = new ResouceItem();
                    item.m_Crc = crc;
                }
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResouceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as Object;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                }
            }
        }

        CacheResource(path, ref item, crc, obj);
        resObj.m_ResItem = item;
        item.m_Clear = resObj.m_bClear;

        return resObj;
    }

    /// <summary>
    /// 同步资源加载，外部直接调用，仅加载不需要实例化的资源，例如Texture,音频等等
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        uint crc = Crc32.GetCrc32(path);
        ResouceItem item = GetCacheResouceItem(crc);
        if (item != null)
        {
            return item.m_Obj as T;
        }

        T obj = null;
#if UNITY_EDITOR
        if (!Const.m_LoadFormAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItme(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = (T)item.m_Obj;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                }
            }
            else
            {
                if (item == null)
                {
                    item = new ResouceItem();
                    item.m_Crc = crc;
                }
                obj = LoadAssetByEditor<T>(path);
            }
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResouceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as T;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                }
            }
        }

        CacheResource(path, ref item, crc, obj);
        return obj;
    }

    /// <summary>
    /// 根据ResouceObj卸载资源  防止在对象池内的obj被单独删除一个后 AssetDic字典内被清除
    /// </summary>
    /// <param name="resObj"></param>
    /// <param name="destoryObj"></param>
    /// <returns></returns>
    public bool ReleaseResouce(ResouceObj resObj, bool destoryObj = false)
    {
        if (resObj == null)
            return false;

        ResouceItem item = null;
        if ( !AssetDic.TryGetValue(resObj.m_Crc, out item) || null == item)
        {
            Debug.LogError(resObj.m_CloneObj.name + " :is not in AssetDic . maybe Release more num");
            return false;
        }

        GameObject.Destroy(resObj.m_CloneObj);

        item.RefCount--;


        //检查objManager内m_ObjectPoolDic有没有对应路径的对象池，有的话，计数减一，但是不要进DestoryResouceItme。
        List<ResouceObj> st = ObjectManager.Instance.FindObjectPool(resObj.m_Crc);
        bool alsoHave = !(st == null || st.Count <= 0 );
        //对应的池子空了 或者 做缓存的才进入。 这样就不会将其从AssetDic内删除了
        if (!alsoHave || !destoryObj) {
            DestoryResouceItme(item, destoryObj);
        }

        return true;
    }

    /// <summary>
    /// 不需要实例化的资源的卸载，根据对象
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="destoryObj"></param>
    /// <returns></returns>
    public bool ReleaseResouce(Object obj, bool destoryObj = false)
    {
        if (obj == null)
        {
            Debug.LogWarning("ReleaseResouce  : obj is not exist");
            return false;
        }

        ResouceItem item = null;
        //TODO考虑要不要再加一个guid的字典，大量资源下查找更快，更占内存
        foreach (ResouceItem res in AssetDic.Values)
        {
            if (res.m_Guid == obj.GetInstanceID())
            {
                item = res;
            }
        }

        if (item == null)
        {
            Debug.LogError(obj.name + " :is not in AssetDic . maybe Release more num");
            return false;
        }

        item.RefCount--;

        DestoryResouceItme(item, destoryObj);

        //if (!UnityEngine.Object.ReferenceEquals(obj, null))
        //{
        //    obj = null;
        //}
        return true;
    }

    /// <summary>
    /// 不需要实例化的资源卸载，根据路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="destoryObj"></param>
    /// <returns></returns>
    public bool ReleaseResouce(string path, bool destoryObj = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        uint crc = Crc32.GetCrc32(path);
        ResouceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || null == item)
        {
            Debug.LogError("asset is not exist in AssetDic ：" + path + "  maybe release more num");
        }

        item.RefCount--;

        DestoryResouceItme(item, destoryObj);
        return true;
    }

    /// <summary>
    /// 缓存加载的资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="item"></param>
    /// <param name="crc"></param>
    /// <param name="obj"></param>
    /// <param name="addrefcount"></param>
    void CacheResource(string path, ref ResouceItem item, uint crc, Object obj, int addrefcount = 1)
    {
        //缓存太多，清除最早没有使用的资源
        WashOut();

        if (item == null)
        {
            Debug.LogError("ResouceItem is null, path: " + path);
        }

        if (obj == null)
        {
            Debug.LogError("ResouceLoad Fail :  " + path);
        }

        item.m_Obj = obj;
        item.m_Guid = obj.GetInstanceID();
        item.m_LastUseTime = Time.realtimeSinceStartup;
        item.RefCount += addrefcount;
        ResouceItem oldItme = null;
        if (AssetDic.TryGetValue(item.m_Crc, out oldItme))
        {

            Debug.LogError("AssetDic exist crc:" + item.m_Crc + " . This item is Cached. Please Check.  Old :" + oldItme.m_ABName +"  " + oldItme.m_AssetName + "  New:" + item.m_ABName + "  " + item.m_AssetName);

            AssetDic[item.m_Crc] = item;
        }
        else
        {
            AssetDic.Add(item.m_Crc, item);
        }
    }

    /// <summary>
    /// 缓存太多，清除最早没有使用的资源
    /// </summary>
    protected void WashOut()
    {
        //当大于缓存个数时，进行一半释放
        if (m_NoRefrenceAssetMapList.Size() >= MAXCACHECOUNT)
        {
            for (int i = 0; i < MAXCACHECOUNT / 2; i++)
            {
                ResouceItem item = m_NoRefrenceAssetMapList.Back();
                DestoryResouceItme(item, true);
            }
        }
    }
    /// <summary>
    /// 回收一个资源 对应给ObjectManager
    /// </summary>
    /// <param name="resObj"></param>
    public void DestoryResouceItme(ResouceObj resObj)
    {
        ResouceItem item = resObj.m_ResItem;
        if (item == null || item.RefCount > 0)
        {
            return;
        }
        
        //没有在缓存使用的列表内 就不用往下了
        if (!AssetDic.Remove(item.m_Crc))
        {
            return;
        }
        m_NoRefrenceAssetMapList.Remove(item);

        //释放assetbundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);

        if (item.m_Obj != null)
        {
            item.m_Obj = null;
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
        }
    }
    /// <summary>
    /// 回收一个资源
    /// </summary>
    /// <param name="item"></param>
    /// <param name="destroy"></param>
    protected void DestoryResouceItme(ResouceItem item, bool destroyCache = false)
    {
        if (item == null || item.RefCount > 0)
        {
            return;
        }
        //存入引用计数为0的双向链表内
        //需要缓存的资源 那就不用移出AssetDic，等到缓存太多需要清楚时还会再进来，那时候再移除就行了
        if (!destroyCache)
        {
            m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }

        //没有在缓存使用的列表内 就不用往下了
        if (!AssetDic.Remove(item.m_Crc))
        {
            return;
        }

        m_NoRefrenceAssetMapList.Remove(item);

        //释放assetbundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);

        //清空资源对应的对象池
        ObjectManager.Instance.ClearPoolObject(item.m_Crc,true);

        if (item.m_Obj != null)
        {
            //这样是标记资源可以回收，等待gc自动回收
            //Resources.UnloadAsset(item.m_Obj);
            item.m_Obj = null;
#if UNITY_EDITOR
            //这样可以直接就回收掉资源
            Resources.UnloadUnusedAssets();
#endif
        }
    }

#if UNITY_EDITOR
    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif

    /// <summary>
    /// 从资源池获取缓存资源
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="addrefcount"></param>
    /// <returns></returns>
    ResouceItem GetCacheResouceItem(uint crc, int addrefcount = 1)
    {
        ResouceItem item = null;
        if (AssetDic.TryGetValue(crc, out item))
        {
            if (item != null)
            {
                item.RefCount += addrefcount;
                item.m_LastUseTime = Time.realtimeSinceStartup;
            }
        }

        return item;
    }

    /// <summary>
    /// 异步加载资源（仅仅是不需要实例化的资源，例如音频，图片等等）
    /// </summary>
    public void AsyncLoadResource(string path, OnAsyncObjFinish dealFinish, LoadResPriority priority, bool isSprite = false, object param1 = null, object param2 = null,object param3 =null, uint crc = 0)
    {
        if (crc == 0)
        {
            crc = Crc32.GetCrc32(path);
        }

        ResouceItem item = GetCacheResouceItem(crc);
        //在缓存内，直接用就完事了
        if (item != null)
        {
            dealFinish?.Invoke(path, item.m_Obj, param1, param2, param3);
            return;
        }

        //判断是否在加载中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(crc, out para) || para == null)
        {
            //不在加载中
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = crc;
            para.m_Path = path;
            para.m_Sprite = isSprite;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
        }

        //往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_DealObjFinish = dealFinish;
        callBack.m_Param1 = param1;
        callBack.m_Param2 = param2;
        callBack.m_Param3 = param3;
        para.m_CallBackList.Add(callBack);
    }

    /// <summary>
    /// 针对ObjectManager的异步加载接口
    /// </summary>
    /// <param name="path"></param>
    /// <param name="resObj"></param>
    /// <param name="dealfinish"></param>
    /// <param name="priority"></param>
    public void AsyncLoadResource(string path, ResouceObj resObj, OnAsyncFinsih dealfinish, LoadResPriority priority)
    {
        ResouceItem item = GetCacheResouceItem(resObj.m_Crc);
        //在缓存内，直接用就完事了
        if (item != null)
        {
            resObj.m_ResItem = item;
            dealfinish?.Invoke(path, resObj);
            return;
        }

        //判断是否在加载中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(resObj.m_Crc, out para) || para == null)
        {
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = resObj.m_Crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(resObj.m_Crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
        }

        //往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_DealFinish = dealfinish;
        callBack.m_ResObj = resObj;
        para.m_CallBackList.Add(callBack);
    }

    /// <summary>
    /// 异步加载
    /// </summary>
    /// <returns></returns>
    IEnumerator AsyncLoadCor()
    {
        List<AsyncCallBack> callBackList = null;
        //上一次yield的时间
        long lastYiledTime = System.DateTime.Now.Ticks;
        while (true)
        {
            //haveYield防止for循环内等了一帧，出来还要等一帧的问题
            bool haveYield = false;
            for(int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
            {
                //防止在低优先级中加载时 高优先级的加载进来东西了
                if (m_LoadingAssetList[(int)LoadResPriority.RES_HIGHT].Count > 0)
                {
                    i = (int)LoadResPriority.RES_HIGHT;
                }
                else if (m_LoadingAssetList[(int)LoadResPriority.RES_MIDDLE].Count > 0)
                {
                    i = (int)LoadResPriority.RES_MIDDLE;
                }

                List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                if (loadingList.Count <= 0)
                    continue;

                //拿出最前面的加载类
                AsyncLoadResParam loadingItem = loadingList[0];
                loadingList.RemoveAt(0);
                callBackList = loadingItem.m_CallBackList;

                Object obj = null;
                ResouceItem item = null;
#if UNITY_EDITOR
                if (!Const.m_LoadFormAssetBundle)
                {
                    if (loadingItem.m_Sprite)
                    {
                        obj = LoadAssetByEditor<Sprite>(loadingItem.m_Path);
                    }
                    else
                    {
                        obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                    }
                    //模拟异步加载
                    yield return new WaitForSeconds(0.5f);

                    item = AssetBundleManager.Instance.FindResourceItme(loadingItem.m_Crc);
                    if (item == null)
                    {
                        item = new ResouceItem();
                        item.m_Crc = loadingItem.m_Crc;
                    }
                }
#endif
                if (obj == null)
                {
                    item = AssetBundleManager.Instance.LoadResouceAssetBundle(loadingItem.m_Crc);
                    if (item != null && item.m_AssetBundle != null)
                    {
                        AssetBundleRequest abRequest = null;
                        if (loadingItem.m_Sprite)
                        {
                            abRequest = item.m_AssetBundle.LoadAssetAsync<Sprite>(item.m_AssetName);
                        }
                        else
                        {
                            abRequest = item.m_AssetBundle.LoadAssetAsync(item.m_AssetName);
                        }
                        yield return abRequest;
                        if (abRequest.isDone)
                        {
                            obj = abRequest.asset;
                        }
                        lastYiledTime = System.DateTime.Now.Ticks;
                    }
                }

                CacheResource(loadingItem.m_Path, ref item, loadingItem.m_Crc, obj, callBackList.Count);

                for (int j = 0; j < callBackList.Count; j++)
                {
                    AsyncCallBack callBack = callBackList[j];

                    //objectManager的异步加载
                    if (callBack != null && callBack.m_DealFinish != null && callBack.m_ResObj != null)
                    {
                        ResouceObj tempResObj = callBack.m_ResObj;
                        tempResObj.m_ResItem = item;
                        callBack.m_DealFinish(loadingItem.m_Path, tempResObj, tempResObj.m_Param1, tempResObj.m_Param2, tempResObj.m_Param3);
                        callBack.m_DealFinish = null;
                        tempResObj = null;
                    }
                    //resourceManager的异步加载
                    if (callBack != null && callBack.m_DealObjFinish != null)
                    {
                        callBack.m_DealObjFinish(loadingItem.m_Path, obj, callBack.m_Param1, callBack.m_Param2, callBack.m_Param3);
                        callBack.m_DealObjFinish = null;
                    }

                    //还原 并回收到对象池
                    callBack.Reset();
                    m_AsyncCallBackPool.Recycle(callBack);
                }
                //清除临时变量 移除加载中的东西
                obj = null;
                callBackList.Clear();
                m_LoadingAssetDic.Remove(loadingItem.m_Crc);
                //还原 并回收到对象池
                loadingItem.Reset();
                m_AsyncLoadResParamPool.Recycle(loadingItem);

                //等一帧，防止界面卡太久，玩家感觉不爽
                if (System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
                {
                    yield return null;
                    lastYiledTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }
            }

            //没有加载的东西   或者时间过长了，  while内延时一帧，防止卡着
            if (!haveYield || System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
            {
                lastYiledTime = System.DateTime.Now.Ticks;
                yield return null;
            }

        }
    }
}

//双向链表结构节点
public class DoubleLinkedListNode<T> where T : class, new()
{
    //前一个节点
    public DoubleLinkedListNode<T> prev = null;
    //后一个节点
    public DoubleLinkedListNode<T> next = null;
    //当前节点
    public T t = null;
}

//双向链表结构
public class DoubleLinedList<T> where T : class, new()
{
    //表头
    public DoubleLinkedListNode<T> Head = null;
    //表尾
    public DoubleLinkedListNode<T> Tail = null;
    //双向链表结构类对象池
    protected ClassObjectPool<DoubleLinkedListNode<T>> m_DoubleLinkNodePool = ObjectManager.Instance.GetOrCreatClassPool<DoubleLinkedListNode<T>>(50);
    //个数
    protected int m_Count = 0;
    public int Count
    {
        get { return m_Count; }
    }

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(T t)
    {
        DoubleLinkedListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
        pList.next = null;
        pList.prev = null;
        pList.t = t;
        return AddToHeader(pList);
    }

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;

        pNode.prev = null;
        if (Head == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;
        }
        m_Count++;
        return Head;
    }

    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(T t)
    {
        DoubleLinkedListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
        pList.next = null;
        pList.prev = null;
        pList.t = t;
        return AddToTail(pList);
    }

    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;

        pNode.next = null;
        if (Tail == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.prev = Tail;
            Tail.next = pNode;
            Tail = pNode;
        }
        m_Count++;
        return Tail;
    }

    /// <summary>
    /// 移除某个节点
    /// </summary>
    /// <param name="pNode"></param>
    public void RemoveNode(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return;

        if (pNode == Head)
            Head = pNode.next;

        if (pNode == Tail)
            Tail = pNode.prev;

        if (pNode.prev != null)
            pNode.prev.next = pNode.next;

        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        pNode.next = pNode.prev = null;
        pNode.t = null;
        m_DoubleLinkNodePool.Recycle(pNode);
        m_Count--;
    }

    /// <summary>
    /// 把某个节点移动到头部
    /// </summary>
    /// <param name="pNode"></param>
    public void MoveToHead(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null || pNode == Head)
            return;

        if (pNode.prev == null && pNode.next == null)
            return;

        if (pNode == Tail)
            Tail = pNode.prev;

        if (pNode.prev != null)
            pNode.prev.next = pNode.next;

        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        pNode.prev = null;
        pNode.next = Head;
        Head.prev = pNode;
        Head = pNode;
        if (Tail == null)
        {
            Tail = Head;
        }
    }
}

public class CMapList<T> where T : class, new()
{
    DoubleLinedList<T> m_DLink = new DoubleLinedList<T>();

    ~CMapList()
    {
        Clear();
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    public void Clear()
    {
        while (m_DLink.Tail != null)
        {
            Remove(m_DLink.Tail.t);
        }
    }

    /// <summary>
    /// 插入一个节点到表头
    /// </summary>
    /// <param name="t"></param>
    public void InsertToHead(T t)
    {
        DoubleLinkedListNode<T> node = Find(t);
        if (node == null)
        {
            return;
        }
        m_DLink.AddToHeader(t);
    }

    /// <summary>
    /// 从表尾弹出一个结点
    /// </summary>
    public void Pop()
    {
        if (m_DLink.Tail != null)
        {
            Remove(m_DLink.Tail.t);
        }
    }

    /// <summary>
    /// 删除某个节点
    /// </summary>
    /// <param name="t"></param>
    public void Remove(T t)
    {
        DoubleLinkedListNode<T> node = Find(t);
        if (node == null)
        {
            return;
        }
        m_DLink.RemoveNode(node);
    }

    /// <summary>
    /// 获取到尾部节点
    /// </summary>
    /// <returns></returns>
    public T Back()
    {
        return m_DLink.Tail == null ? null : m_DLink.Tail.t;
    }

    /// <summary>
    /// 返回节点个数
    /// </summary>
    /// <returns></returns>
    public int Size()
    {
        return m_DLink.Count;
    }

    /// <summary>
    /// 查找节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> Find(T t)
    {
        if (m_DLink.Count <= 0)
        {
            if (m_DLink.Head != null)
            {
                Debug.LogError("DoubleLinedList->Find : Count is zero but Head is not null.Please Check");
            }
            return null;
        }

        DoubleLinkedListNode<T> currentNode = m_DLink.Head;
        if (currentNode == null)
        {
            Debug.LogError("DoubleLinedList->Find : Count is not zero but Head is null.Please Check");
            return null;
        }
        while (true)
        {
            if (currentNode.t == t)
            {
                return currentNode;
            }
            if (currentNode.next == null)
            {
                break;
            }
            currentNode = currentNode.next;
        }

        return null;
    }

    /// <summary>
    /// 刷新某个节点，把节点移动到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Refresh(T t)
    {
        DoubleLinkedListNode<T> node = Find(t);
        if (node == null)
        {
            return false;
        }
        m_DLink.MoveToHead(node);
        return true;
    }
}
