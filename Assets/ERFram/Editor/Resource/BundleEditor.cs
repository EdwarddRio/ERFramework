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
    public static string BundleTargetPath
    {
        get { return m_BundleTargetPath; }
    }
    public static string BundleTargetEncryPath
    {
        get { return m_BundleTargetEncryPath; }
    }
    private static string m_BundleTargetPath = Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString();
    private static string m_BundleTargetEncryPath = Application.dataPath + "/../AssetBundle/Encry" + EditorUserBuildSettings.activeBuildTarget.ToString();

    //private static string m_BundleTargetPath = Application.streamingAssetsPath;

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
    //写xml配置的字典 key为全路径，value为包名
    private static Dictionary<string, string> m_ResPathDic = new Dictionary<string, string>();

    [MenuItem("Tools/打包", false, 1)]
    public static void Build()
    {

        m_ConfigFil.Clear();
        m_AllFileAB.Clear();
        m_AllFileDir.Clear();
        m_AllPrefabDir.Clear();
        m_ResPathDic.Clear();
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        foreach (ABConfig.FileDirABName fileDir in abConfig.m_AllFileDirAB)
        {
            if (m_AllFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包配置名字重复，请检查！" + fileDir.ABName);
            }
            else
            {
                m_AllFileDir.Add(fileDir.ABName, fileDir.Path);
                m_AllFileAB.Add(fileDir.Path);
                m_ConfigFil.Add(fileDir.Path);
            }
        }
        //所有prefabs
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

        //防止有手动设置的，防一手
        ClearAllABName();
        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }

        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }

        BunildAssetBundle();



        //根据所有设置的ab包，判断是否有不用了的ab包存在
        DeleteAB(m_BundleTargetPath);

        //创建版本信息
        VerInfo verInfo = CreateVersionFile();

        //ab包加密
        EncryptAB(verInfo);

        ClearAllABName();
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
        for (int i = 0; i < allBundles.Length; i++)
        {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundles[i]);
            for (int j = 0; j < allBundlePath.Length; j++)
            {
                if (allBundlePath[j].EndsWith(".cs"))
                    continue;

                //Debug.Log("此AB包：" + allBundles[i] + "下面包含的资源文件路径：" + allBundlePath[j]);
                m_ResPathDic.Add(allBundlePath[j],allBundles[i]);
            }
        }

        //生成自己的配置表
        WriteData(m_ResPathDic);


        string targerPath = m_BundleTargetPath;

        if (!Directory.Exists(targerPath))
        {
            Directory.CreateDirectory(targerPath);
        }


        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(targerPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

        if (manifest == null)
        {
            Debug.LogError("AssetBundle 打包失败！" + targerPath);
        }

    }

    static void WriteData(Dictionary<string, string> resPathDic)
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
    static void ClearAllABName()
    {
        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字：" + oldABNames[i], i * 1.0f / oldABNames.Length);
        }
    }
    /// <summary>
    /// 加密ab包  防君子不防小人版
    /// </summary>
    static void EncryptAB(VerInfo notEncryVer)
    {
        if (!Directory.Exists(m_BundleTargetEncryPath))
        {
            Directory.CreateDirectory(m_BundleTargetEncryPath);
        }

        DirectoryInfo direction = new DirectoryInfo(m_BundleTargetPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);

        string fullName = string.Empty;
        string fileName = string.Empty;

        //对比加密的ab包是否有已经不用了的
        ABEncryptInfo aBEncryptInfo = ABEncryptInfo.Read(m_BundleTargetEncryPath +"/");
        if (aBEncryptInfo==null)
        {
            aBEncryptInfo = new ABEncryptInfo();
            //加密包内不存在加密信息文件，直接删掉整个加密ab文件夹
            GameUtility.SafeDeleteAllFileInDir(m_BundleTargetEncryPath);
            aBEncryptInfo.genPath = m_BundleTargetPath;
            aBEncryptInfo.GenHash();
        }
        List<string> needEncryFileNames = new List<string>();
        List<string> notNeedChangeFiles = new List<string>();
    
        //对比hash值 有不同的/不存在的就删除
        List<string> delFiles = new List<string>();
        //加密的对比没加密的
        foreach (KeyValuePair<string, string> keyValuePair in aBEncryptInfo.filehash)
        {
            string name = keyValuePair.Key;
            string[] values = keyValuePair.Value.Split('@');
            string notEnHash = values[0];
            string enHash = values[1];

            //非加密文件夹内的版本文件
            string verValue = string.Empty;
            if (!notEncryVer.filehash.TryGetValue(name, out verValue) || verValue == null || verValue == string.Empty)
            {
                Debug.Log("加密:在默认文件夹内，此文件找不到，删除：" + name);
                delFiles.Add(name);
                continue;
            }
            string verhash = notEncryVer.filehash[name].Split('@')[0];

            if (verhash != notEnHash)
            {
                Debug.Log("加密:同名文件的默认hash不同了，删除并且重新加密：" + name);
                //delFiles.Add(name);
                needEncryFileNames.Add(name);
                continue;
            }

            if (enHash == string.Empty)
            {
                Debug.Log("加密:当前文件还未加密" + name);
                needEncryFileNames.Add(name);
                continue;
            }
            //余下的说没有问题 加入已检测正常的列表内
            if (File.Exists(Path.Combine(BundleTargetEncryPath, name)))
            {
                notNeedChangeFiles.Add(name);
            }
            else
            {
                Debug.Log("加密:此文件不存在于文件夹内 加密：" + name);
                needEncryFileNames.Add(name);
            }
        }
        //没加密的对比加密的，找到要增加的
        foreach (KeyValuePair<string, string> keyValuePair in notEncryVer.filehash)
        {
            string name = keyValuePair.Key;
            if (name.Equals("allver.ver.txt") || notNeedChangeFiles.IndexOf(name) >= 0 || name.EndsWith(".manifest"))
            {
                continue;
            }
            string[] values = keyValuePair.Value.Split('@');
            string verHash = values[0];
            
            //加密文件夹内的信息文件
            string encryValue = string.Empty;
            if (!aBEncryptInfo.filehash.TryGetValue(name, out encryValue) || encryValue == null || encryValue == string.Empty)
            {
                if (needEncryFileNames.IndexOf(name) < 0)
                {
                    Debug.Log("加密:在加密信息内 没找到的，需要加密：" + name);
                    needEncryFileNames.Add(name);
                    aBEncryptInfo.GenHashOne(Path.Combine(BundleTargetPath, name));
                    continue;
                }
            }
        }
        
        //读取一下加密包内的所有文件，然后对比有没有有问题的
        DirectoryInfo encrydirection = new DirectoryInfo(m_BundleTargetEncryPath);
        FileInfo[] encryfiles = encrydirection.GetFiles("*", SearchOption.AllDirectories);
        
        for (int encI = 0; encI < encryfiles.Length; encI++)
        {
            fileName = encryfiles[encI].Name;
            if (fileName.Equals(Const.FILE_VERSION) || fileName.Equals(Const.ABENCRYPT_INFO))
            {
                continue;
            }
            int delIndex = delFiles.IndexOf(fileName);
            if (delIndex >= 0)
            {
                //删除文件 删除filehash的对应参数
                GameUtility.SafeDeleteFile(encryfiles[encI].FullName);
                aBEncryptInfo.filehash.Remove(fileName);

                delFiles.RemoveAt(delIndex);
            }
            else if (needEncryFileNames.IndexOf(fileName) >= 0)
            {
                GameUtility.SafeDeleteFile(encryfiles[encI].FullName);
            }
            else if (notNeedChangeFiles.IndexOf(fileName) <0)
            {
                GameUtility.SafeDeleteFile(encryfiles[encI].FullName);
                Debug.Log("加密：此文件不在需要删除和需加密列表内，同时也不在不需要改变列表内，说明是多余的：" + fileName);
            }
        }
        //防止 delFiles中还有没删除的，说明文件夹内没了 但是配置表内还有
        for (int i = delFiles.Count-1; i >=0; i--)
        {
            aBEncryptInfo.filehash.Remove(delFiles[i]);
            delFiles.RemoveAt(i);
        }

        byte[] secret = new byte[Const.ABEncryptLen];

        for (int i = 0; i < files.Length; i++)
        {
            fileName = files[i].Name;
            fullName = files[i].FullName;
            if (fileName.Equals("allver.ver.txt"))
            {
                continue;
            }
            //manifest不需要
            if (fileName.EndsWith(".manifest"))
            {
                continue;
            }
            if (needEncryFileNames.IndexOf(fileName)<0)
            {
                continue;
            }
            //这个包有修改 需要加密
            EditorUtility.DisplayProgressBar("加密AB包", "名字：" + fileName, i * 1.0f / files.Length);
            Debug.Log("加密AB包，名字:" + fileName);
            //随机加密byte
            for (int rI = 0; rI < secret.Length; rI++)
            {
                secret[rI] = (byte)Random.Range(0, 256);
            }
            //加密byte写入开头
            byte[] temp = File.ReadAllBytes(fullName);
            byte[] newTemp = new byte[(int)Const.ABEncryptLen + temp.Length];
            for (int bI = 0; bI < newTemp.Length; bI++)
            {
                if (bI < (int)Const.ABEncryptLen)
                {
                    newTemp[bI] = secret[bI];
                }
                else
                {
                    newTemp[bI] = temp[bI - (int)Const.ABEncryptLen];
                }
            }

            File.WriteAllBytes(Path.Combine(m_BundleTargetEncryPath, fileName), newTemp);

            string notEnHash = System.Convert.ToBase64String(VerInfo.osha1.ComputeHash(temp));
            string enHash = System.Convert.ToBase64String(VerInfo.osha1.ComputeHash(newTemp));

            aBEncryptInfo.filehash[fileName] = notEnHash + "@" + enHash;
        }

        GameUtility.SafeDeleteFile(Path.Combine(m_BundleTargetEncryPath, Const.ABENCRYPT_INFO));
       
        aBEncryptInfo.path = m_BundleTargetEncryPath;
        aBEncryptInfo.SaveToPath();

        //创建版本信息
        CreateVersionFile(true);
    }

    /// <summary>
    /// 删除无用AB包
    /// </summary>
    static void DeleteAB(string checkPath)
    {
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo direction = new DirectoryInfo(checkPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            if (ConatinABName(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta") || files[i].Name.EndsWith(".manifest") || files[i].Name.EndsWith("assetbundleconfig"))
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
    static bool ConatinABName(string name, string[] strs,params string[] dicNames)
    {
        for (int i = 0; i < strs.Length; i++)
        {

            if (name == strs[i])
            {
                return true;
            }

            for (int ni = 0; ni < dicNames.Length; ni++)
            {
                if (dicNames[ni]+ name == strs[i])
                {
                    return true;
                }
            }
           
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
            if (path == m_AllFileAB[i] || (path.Contains(m_AllFileAB[i]) && (path.Replace(m_AllFileAB[i], "")[0] == '/')))
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
    static VerInfo CreateVersionFile(bool encry =false)
    {
        string path = m_BundleTargetPath;
        if (encry)
        {
            path = m_BundleTargetEncryPath;
        }

        GameUtility.SafeDeleteFile(Path.Combine(path, Const.FILE_VERSION));

        VerInfo vernew = new VerInfo();
        vernew.path = path;
        vernew.GenHash();
        Debug.Log("文件数量：" + vernew.filehash.Count);
        vernew.ver = Const.GAME_VERSION;

        vernew.SaveToPath(path);


        Debug.Log("版本文件写入成功");
        return vernew;
    }

    //[MenuItem("Tools/复制AB包到LocalPath", false, 1)]
    /// <summary>
    /// 将AB包复制到StreamingAssets
    /// </summary>
    public static void CopyDefaultABToSA()
    {
        CopyABToSA(m_BundleTargetPath);
    }
    [MenuItem("Tools/复制加密后AB包到LocalPath", false, 1)]
    /// <summary>
    /// 将AB包复制到StreamingAssets
    /// </summary>
    public static void CopyEncryABToSA()
    {
        CopyABToSA(m_BundleTargetEncryPath);
    }

    public static void CopyABToSA(string path)
    {
        string abInSAPath = Const.ABLoadPathByEditor;
        VerInfo verSA = null;
        if (!Directory.Exists(abInSAPath))
        {
            Directory.CreateDirectory(abInSAPath);
        }
        else
        {
            //对比StreamingAssets中已经有的版本信息文件是否都存在或者多的
            verSA = VerInfo.Read(abInSAPath, false);
            verSA = CheckSAVerInfo(verSA, abInSAPath);
        }
        if (verSA == null)
        {
            verSA = new VerInfo();
        }
        //对比哪些文件需要复制过来
        VerInfo verRead = VerInfo.Read(path + "/", false);

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
                    GameUtility.SafeCopyFile(path + "/" + fh.Key, Const.ABLoadPathByEditor + fh.Key);
                    Debug.Log("复制文件替换 ：" + fh.Key);
                }
            }
            else
            {
                //文件复制
                GameUtility.SafeCopyFile(path + "/" + fh.Key, Const.ABLoadPathByEditor + fh.Key);
                Debug.Log("复制文件替换 ：" + fh.Key);
            }
        }
        GameUtility.SafeCopyFile(path + "/" + Const.FILE_VERSION, Const.ABLoadPathByEditor + Const.FILE_VERSION);
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
    protected static VerInfo CheckSAVerInfo(VerInfo verSA, string abInSAPath)
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
                strTemp = strTemp.Substring(abInSAPath.Length);
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
