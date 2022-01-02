using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class GameUtility 
{
    public static bool SafeRenameFile(string sourceFileName, string destFileName)
    {
        try
        {
            if (string.IsNullOrEmpty(sourceFileName))
            {
                return false;
            }

            if (!File.Exists(sourceFileName))
            {
                return true;
            }
            SafeDeleteFile(destFileName);
            File.SetAttributes(sourceFileName, FileAttributes.Normal);
            File.Move(sourceFileName, destFileName);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("SafeRenameFile failed! path = {0} with err: {1}", sourceFileName, ex.Message));
            return false;
        }
    }

    public static bool SafeDeleteFile(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return true;
            }

            if (!File.Exists(filePath))
            {
                return true;
            }
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("SafeDeleteFile failed! path = {0} with err: {1}", filePath, ex.Message));
            return false;
        }
    }
    public static bool SafeDeleteDir(string folderPath)
    {
        try
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return true;
            }

            if (Directory.Exists(folderPath))
            {
                DeleteDirectory(folderPath);
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("SafeDeleteDir failed! path = {0} with err: {1}", folderPath, ex.Message));
            return false;
        }
    }
    /// <summary>
    /// 删除文件夹内的所有文件
    /// </summary>
    /// <param name="dirPath"></param>
    /// <returns></returns>
    public static bool SafeDeleteAllFileInDir(string dirPath)
    {
        try
        {
            if (string.IsNullOrEmpty(dirPath))
            {
                return true;
            }

            if (Directory.Exists(dirPath))
            {
                DeleteDirectory(dirPath);
                Directory.CreateDirectory(dirPath);
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("SafeDeleteAllFileInDir failed! path = {0} with err: {1}", dirPath, ex.Message));
            return false;
        }
    }


    public static void SafeCopyPath(string source, string target)
    {
        SafeDeleteDir(target);
        try
        {
            string[] allfiles = GetSpecifyFilesInFolder(source);
            foreach (var file in allfiles)
            {
                string fileName = Path.GetFileName(file);
                SafeCopyFile(file, target + "/" + fileName);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Something wrong, you need manual delete : " + ex);
            return;
        }

    }
    public static bool SafeCopyFile(string fromFile, string toFile)
    {
        try
        {
            if (string.IsNullOrEmpty(fromFile))
            {
                return false;
            }

            if (!File.Exists(fromFile))
            {
                return false;
            }
            CheckFileAndCreateDirWhenNeeded(toFile);
            SafeDeleteFile(toFile);
            File.Copy(fromFile, toFile, true);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("SafeCopyFile failed! formFile = {0}, toFile = {1}, with err = {2}",
                fromFile, toFile, ex.Message));
            return false;
        }
    }
    public static byte[] LoadFile(string filePath, bool isAbsolutePath = false)
    {
        if (filePath == null || filePath.Length == 0)
            return null;
        string path = filePath;
        if (!isAbsolutePath)
            path = GetWritablePath(filePath);
        if (File.Exists(path))
            return File.ReadAllBytes(path);
        return null;
    }
    public static string GetWritablePath(string relativeFilePath)
    {
        string empty = string.Empty;
        return Application.persistentDataPath + "/" + relativeFilePath;
    }
    public static void CheckFileAndCreateDirWhenNeeded(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        FileInfo file_info = new FileInfo(filePath);
        DirectoryInfo dir_info = file_info.Directory;
        if (!dir_info.Exists)
        {
            Directory.CreateDirectory(dir_info.FullName);
        }
    }

    public static void DeleteDirectory(string dirPath)
    {
        string[] files = Directory.GetFiles(dirPath);
        string[] dirs = Directory.GetDirectories(dirPath);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(dirPath, false);
    }
    public static string[] GetSpecifyFilesInFolder(string path, string[] extensions = null, bool exclude = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (extensions == null)
        {
            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        }
        else if (exclude)
        {
            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(f => !extensions.Contains(GetFileExtension(f))).ToArray();
        }
        else
        {
            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(GetFileExtension(f))).ToArray();
        }
    }

    public static string GetFileExtension(string path)
    {
        return Path.GetExtension(path).ToLower();
    }
}
