using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Profiling;


//XXX:框架内如果做单个数据的下载的话 可以再加一个ab包类 然后一个个数据一个ab包，等要用的时候 单独判定并且下载
public class BundleEditor
{
    public static string BunleTargetPath
    {
        get { return m_BunleTargetPath; }
    }
    private static string m_BunleTargetPath = Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString();

    //private static string m_BunleTargetPath = Application.streamingAssetsPath;

    private static string ABCONFIGPATH = "Assets/ERFram/Editor/Resource/ABConfig.asset";
    private static string ABBYTEPATH = RealConfig.GetRealFram().m_ABBytePath;
    //key是ab包名，value是路径，所有文件夹ab包dic
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();
    //过滤的list
    private static List<string> m_AllFileAB = new List<string>();
    //单个prefab的ab包
    private static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>();
    //储存所有有效路径
    private static List<string> m_ConfigFil = new List<string>();

    [MenuItem("Tools/打包",false,1)]
    public static void Build()
    {
        m_ConfigFil.Clear();
        m_AllFileAB.Clear();
        m_AllFileDir.Clear();
        m_AllPrefabDir.Clear();
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        foreach (ABConfig.FileDirABName fileDir in abConfig.m_AllFileDirAB)
        {
            if (m_AllFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包配置名字重复，请检查！");
            }
            else
            {
                m_AllFileDir.Add(fileDir.ABName, fileDir.Path);
                m_AllFileAB.Add(fileDir.Path);
                m_ConfigFil.Add(fileDir.Path);
            }
        }

        string[] allStr = AssetDatabase.FindAssets("t:Prefab", abConfig.m_AllPrefabPath.ToArray());
        for (int i = 0; i < allStr.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefab:" + path, i * 1.0f / allStr.Length);
            m_ConfigFil.Add(path);
            if (!ContainAllFileAB(path))
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDepend = AssetDatabase.GetDependencies(path);
                List<string> allDependPath = new List<string>();
                m_AllFileAB.Add(path);
                allDependPath.Add(path);
                for (int j = 0; j < allDepend.Length; j++)
                {
                    //Debug.Log(allDepend[j]); //所有依赖项
                    //脚本不能打进ab包 屏蔽掉prefab自己
                    if (!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs") && !allDepend[j].EndsWith(".prefab"))
                    {
                        m_AllFileAB.Add(allDepend[j]);
                        allDependPath.Add(allDepend[j]);
                    }
                }
                if (m_AllPrefabDir.ContainsKey(obj.name))
                {
                    Debug.LogError("存在相同名字的Prefab！名字：" + obj.name);
                }
                else
                {
                    m_AllPrefabDir.Add(obj.name, allDependPath);
                }
            }
        }

        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }

        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }

        BunildAssetBundle();

        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字：" + oldABNames[i], i * 1.0f / oldABNames.Length);
        }
        //创建版本信息
        CreateVersionFile();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    static void SetABName(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null)
        {
            Debug.LogError("不存在此路径文件：" + path);
        }
        else
        {
            assetImporter.assetBundleName = name;
        }
    }

    static void SetABName(string name, List<string> paths)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            SetABName(name, paths[i]);
        }
    }

    static void BunildAssetBundle()
    {
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
        //key为全路径，value为包名
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for (int i = 0; i < allBundles.Length; i++)
        {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundles[i]);
            for (int j = 0; j < allBundlePath.Length; j++)
            {
                if (allBundlePath[j].EndsWith(".cs"))
                    continue;

                Debug.Log("此AB包：" + allBundles[i] + "下面包含的资源文件路径：" + allBundlePath[j]);
                resPathDic.Add(allBundlePath[j], allBundles[i]);
            }
        }

        if (!Directory.Exists(m_BunleTargetPath))
        {
            Directory.CreateDirectory(m_BunleTargetPath);
        }

        DeleteAB();
        //生成自己的配置表
        WriteData(resPathDic);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(m_BunleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null)
        {
            Debug.LogError("AssetBundle 打包失败！");
        }
        else
        {
            Debug.Log("AssetBundle 打包完毕");
        }
    }

    static void WriteData(Dictionary<string ,string> resPathDic)
    {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();
        foreach (string path in resPathDic.Keys)
        {
            if (!ValidPath(path))
                continue;

            ABBase abBase = new ABBase();
            abBase.Path = path;
            abBase.Crc = Crc32.GetCrc32(path);
            abBase.ABName = resPathDic[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            abBase.ABDependce = new List<string>();
            string[] resDependce = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDependce.Length; i++)
            {
                string tempPath = resDependce[i];
                if (tempPath == path || path.EndsWith(".cs"))
                    continue;

                string abName = "";
                if (resPathDic.TryGetValue(tempPath, out abName))
                {
                    if (abName == resPathDic[path])
                        continue;

                    if (!abBase.ABDependce.Contains(abName))
                    {
                        abBase.ABDependce.Add(abName);
                    }
                }
            }
            config.ABList.Add(abBase);
        }

        //写入xml
        string xmlPath = Application.dataPath + "/AssetbundleConfig.xml";
        GameUtility.SafeDeleteFile(xmlPath);
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xs = new XmlSerializer(config.GetType());
        xs.Serialize(sw, config);
        sw.Close();
        fileStream.Close();

        //写入二进制   二进制没必要存path，path只是给我们自己看的
        foreach (ABBase abBase in config.ABList)
        {
            abBase.Path = "";
        }
        FileStream fs = new FileStream(ABBYTEPATH, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        fs.Seek(0, SeekOrigin.Begin);
        fs.SetLength(0);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, config);
        fs.Close();
        AssetDatabase.Refresh();

        SetABName("assetbundleconfig", ABBYTEPATH);
    }

    /// <summary>
    /// 删除无用AB包
    /// </summary>
    static void DeleteAB()
    {
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo direction = new DirectoryInfo(m_BunleTargetPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (ConatinABName(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta")|| files[i].Name.EndsWith(".manifest") || files[i].Name.EndsWith("assetbundleconfig"))
            {
                continue;
            }
            else
            {
                Debug.Log("此AB包已经被删或者改名了：" + files[i].Name);

                GameUtility.SafeDeleteFile(files[i].FullName);
                GameUtility.SafeDeleteFile(files[i].FullName + ".manifest");
            }
        }
    }

    /// <summary>
    /// 遍历文件夹里的文件名与设置的所有AB包进行检查判断
    /// </summary>
    /// <param name="name"></param>
    /// <param name="strs"></param>
    /// <returns></returns>
    static bool ConatinABName(string name, string[] strs)
    {
        for (int i = 0; i < strs.Length; i++)
        {
            if (name == strs[i])
                return true;
        }
        return false;
    }

    /// <summary>
    /// 是否包含在已经有的AB包里，用来做AB包冗余剔除
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ContainAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAB.Count; i++)
        {
            if (path == m_AllFileAB[i] || (path.Contains(m_AllFileAB[i]) && (path.Replace(m_AllFileAB[i],"")[0] == '/')))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 是否有效路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ValidPath(string path)
    {
        for (int i = 0; i < m_ConfigFil.Count; i++)
        {
            if (path.Contains(m_ConfigFil[i]))
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 创建版本信息文件
    /// </summary>
    static void CreateVersionFile()
    {
        GameUtility.SafeDeleteFile(m_BunleTargetPath + Const.FILE_VERSION);

        VerInfo vernew = new VerInfo();
        vernew.path = m_BunleTargetPath;
        vernew.GenHash();
        Debug.Log("文件数量：" + vernew.filehash.Count);
        vernew.ver = Const.GAME_VERSION;

        vernew.SaveToPath(m_BunleTargetPath);
        Debug.Log("版本文件写入成功");

    }

    [MenuItem("Tools/复制AB包到LocalPath",false,1)]
    /// <summary>
    /// 将AB包复制到StreamingAssets
    /// </summary>
    public static void CopyABToSA() {
        string abInSAPath = Const.ABLoadPathByEditor;
        VerInfo verSA = null;
        if (!Directory.Exists(abInSAPath))
        {
            Directory.CreateDirectory(abInSAPath);
        }
        else
        {
            //对比StreamingAssets中已经有的版本信息文件是否都存在或者多的
            verSA = VerInfo.Read(abInSAPath,false);
            verSA = CheckSAVerInfo(verSA, abInSAPath);
        }
        if (verSA == null)
        {
            verSA = new VerInfo();
        }
        //对比哪些文件需要复制过来
        VerInfo verRead = VerInfo.Read(m_BunleTargetPath +"/", false);

        verSA.ver = verRead.ver;

        List<string> saNames = new List<string>();
        //列表内加入文件名 后面用来删除
        foreach (KeyValuePair<string, string> fh in verSA.filehash)
        {
            saNames.Add(fh.Key);
        }

        foreach (KeyValuePair<string, string> fh in verRead.filehash)
        {
            string abValue = fh.Value;
            string saValue;
            if (verSA.filehash.TryGetValue(fh.Key, out saValue))
            {
                saNames.Remove(fh.Key);
                if (!string.Equals(abValue, saValue))
                {
                    //文件复制替换
                    GameUtility.SafeCopyFile(m_BunleTargetPath + "/" + fh.Key, Const.ABLoadPathByEditor + fh.Key);
                    Debug.Log("复制文件替换 ：" + fh.Key);
                }
            }
            else
            {
                //文件复制
                GameUtility.SafeCopyFile(m_BunleTargetPath + "/" + fh.Key, Const.ABLoadPathByEditor + fh.Key);
                Debug.Log("复制文件替换 ：" + fh.Key);
            }
        }
        GameUtility.SafeCopyFile(m_BunleTargetPath + "/" +Const.FILE_VERSION, Const.ABLoadPathByEditor + Const.FILE_VERSION);
        Debug.Log("复制文件替换 ：" + Const.FILE_VERSION);
        for (int i = 0; i < saNames.Count; i++)
        {
            GameUtility.SafeDeleteFile(Const.ABLoadPathByEditor + saNames[i]);
        }
        saNames = null;
        verRead = null;
        verSA = null;

        AssetDatabase.Refresh();
        Debug.Log("ab包资源文件复制到StreamingAssets完毕");
    }
    /// <summary>
    /// 检查一下StreamingAssets内的文件是否对得上版本文件
    /// </summary>
    /// <param name="verSA"></param>
    /// <param name="abInSAPath"></param>
    /// <returns></returns>
    protected static VerInfo CheckSAVerInfo(VerInfo verSA,string abInSAPath)
    {
        if (verSA != null)
        {
            string strTemp = string.Empty;
            string[] sAFiles = System.IO.Directory.GetFiles(abInSAPath, "*.*", System.IO.SearchOption.AllDirectories);
            List<string> sAFileNames = new List<string>();
            List<string> tempList = new List<string>();
            //提取出文件名
            for (int i = 0; i < sAFiles.Length; i++)
            {
                strTemp = sAFiles[i];
                strTemp = strTemp.Substring(abInSAPath.Length + 1);
                strTemp = strTemp.Replace('\\', '/');
                sAFileNames.Add(strTemp);
            }
            //对比版本文件 是否有不存在的
            foreach (KeyValuePair<string, string> fh in verSA.filehash)
            {
                strTemp = fh.Key;
                if (!sAFileNames.Contains(strTemp))
                {
                    tempList.Add(strTemp);
                }
            }
            for (int i = 0; i < tempList.Count; i++)
            {
                verSA.filehash.Remove(tempList[i]);
            }

            //遍历文件 寻找是否有不存在版本文件内的
            for (int i = 0; i < sAFileNames.Count; i++)
            {
                strTemp = sAFileNames[i];
                if (sAFileNames[i].IndexOf(".meta") >= 0 || sAFileNames[i].IndexOf(Const.FILE_VERSION) >= 0) continue;

                if (!verSA.filehash.TryGetValue(sAFileNames[i], out strTemp))
                {
                    GameUtility.SafeDeleteFile(sAFiles[i]);
                }
            }
            sAFileNames = null;
            strTemp = null;
        }
        else
        {
            GameUtility.SafeDeleteDir(abInSAPath);
            Directory.CreateDirectory(abInSAPath);
        }
        return verSA;
    }

}
