using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class VerInfo
{
    public string ver;//版本号
    public string path;

    public Dictionary<string, string> filehash = new Dictionary<string, string>();

    public static System.Security.Cryptography.SHA1CryptoServiceProvider osha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
    public void GenHash()
    {
        string[] files = System.IO.Directory.GetFiles(this.path, "*.*", System.IO.SearchOption.AllDirectories);
        foreach (var f in files)
        {
            if (f.IndexOf(".crc.txt") >= 0
                ||
                f.IndexOf(".meta") >= 0
                ||
                f.IndexOf(".db") >= 0 
                || 
                f.IndexOf(".DS_Store") >= 0
                ||
                f.IndexOf(".manifest") >= 0
                || 
                f.IndexOf(Const.ABENCRYPT_INFO) >= 0
                ) continue;
            GenHashOne(f);
        }
    }
    public void GenHashOne(string filename)
    {
        using (System.IO.Stream s = System.IO.File.OpenRead(filename))
        {
            var hash = osha1.ComputeHash(s);
            var shash = Convert.ToBase64String(hash) + "@" + s.Length;
            filename = filename.Substring(path.Length + 1);

            filename = filename.Replace('\\', '/');
            filehash[filename] = shash;
        }
    }
    public void SaveInFileHash(string key,string value)
    {
        string str = string.Empty;
        if (filehash.TryGetValue(key ,out str) && !string.IsNullOrEmpty(str))
        {
            Debug.LogErrorFormat("VerInfo=>SaveInFileHash=> key:{0} is exist in filehashDic. value:{1}  saveValue:{2}", key, str, value);
        }
        filehash.Add(key, value);
    }

    public void ClearFileHashDic()
    {
        filehash.Clear();
    }
    public static VerInfo Read(string path , string datas = null)
    {
        string fullPath = path + Const.FILE_VERSION;
        if (datas==null &&  System.IO.File.Exists(fullPath) == false)
        {
            return null;
        }
        string txt = string.Empty;
        if (datas==null)
        {
            txt =  System.IO.File.ReadAllText(fullPath, Encoding.UTF8);
        }
        else
        {
            //ReadAllBytes读取出来的datas最前面会多个看不到的东西。。。截取掉就好了。。。
            txt = datas.Substring(1,datas.Length -1);
        }
            
        string[] lines = txt.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        VerInfo var = new VerInfo();
        foreach (var l in lines)
        {
            if (l.IndexOf("Ver:") == 0)
            {
                var.ver = l.Split('|')[0].Substring(4);
            }
            else
            {
                var sp = l.Split('|');
                if (sp.Length < 2)
                {
                    Debug.LogError("VerInfo Read text is Error  " + txt);
                    return null;
                }
                var.filehash[sp[0]] = sp[1];
            }
        }
        return var;
    }
    public static VerInfo Read(string path, bool tempFile =false)
    {
        string fullPath = path + Const.FILE_VERSION;
        if (tempFile)
        {
            fullPath += Const.TempFileName;
        }

        if ( System.IO.File.Exists(fullPath) == false)
        {
            return null;
        }
        string txt= System.IO.File.ReadAllText(fullPath, Encoding.UTF8);


        string[] lines = txt.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        VerInfo var = new VerInfo();
        foreach (var l in lines)
        {
            if (l.IndexOf("Ver:") == 0)
            {
                var.ver = l.Split('|')[0].Substring(4);
            }
            else
            {
                var sp = l.Split('|');
                if (sp.Length < 2)
                {
                    Debug.LogError("VerInfo Read text is Error  " + txt);
                    return null;
                }
                var.filehash[sp[0]] = sp[1];
            }
        }
        return var;
    }
    public void SaveToPath(string path)
    {
        string outstr = "Ver:" + ver + "|FileCount:"+ filehash.Count+"\n";

        foreach (var f in filehash)
        {
            outstr += f.Key + "|" + f.Value + "\n";
        }

        System.IO.File.WriteAllText(System.IO.Path.Combine(path, Const.FILE_VERSION), outstr, Encoding.UTF8);
    }

}


//记录加密的ab包的信息。
public class ABEncryptInfo
{
    //读取非加密处的
    public string genPath;
    public string path;

    public Dictionary<string, string> filehash = new Dictionary<string, string>();
    
    public void GenHash()
    {
        string[] files = System.IO.Directory.GetFiles(this.genPath, "*.*", System.IO.SearchOption.AllDirectories);
        foreach (var f in files)
        {
            if (f.IndexOf(".crc.txt") >= 0
                ||
                f.IndexOf(".meta") >= 0
                ||
                f.IndexOf(".db") >= 0
                ||
                f.IndexOf(".DS_Store") >= 0
                ||
                f.IndexOf(".manifest") >= 0
                ||
                f.IndexOf(Const.FILE_VERSION) >= 0
                
                ) continue;
            GenHashOne(f);
        }
    }
    public void GenHashOne(string filename)
    {
        using (System.IO.Stream s = System.IO.File.OpenRead(filename))
        {
            var hash =  VerInfo.osha1.ComputeHash(s);
            var shash = Convert.ToBase64String(hash) +"@";
            filename = filename.Substring(genPath.Length + 1);

            filename = filename.Replace('\\', '/');
            filehash[filename] = shash;
        }
    }
    public static ABEncryptInfo Read(string path)
    {
        string fullPath = path + Const.ABENCRYPT_INFO;
        if ( System.IO.File.Exists(fullPath) == false)
        {
            return null;
        }
        string txt = string.Empty;

            txt = System.IO.File.ReadAllText(fullPath, Encoding.UTF8);

        string[] lines = txt.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        ABEncryptInfo var = new ABEncryptInfo();
        foreach (var l in lines)
        {
            if (l.IndexOf("FileCount:") <0)
            {
                var sp = l.Split('|');
                if (sp.Length < 2)
                {
                    Debug.LogError("ABEncryptInfo Read text is Error  " + txt);
                    return null;
                }
                var.filehash[sp[0]] = sp[1];
            }
        }
        return var;
    }
    public void SaveToPath()
    {
        string outstr = "FileCount:" + filehash.Count + "\n";

        foreach (var f in filehash)
        {
            outstr += f.Key + "|" + f.Value + "\n";
        }

        System.IO.File.WriteAllText(System.IO.Path.Combine(path, Const.ABENCRYPT_INFO), outstr, Encoding.UTF8);
    }

}
