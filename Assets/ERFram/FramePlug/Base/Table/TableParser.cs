using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class TableParser
{
    static void ParsePropertyValue<T>(T obj, FieldInfo fieldInfo, string valueStr)
    {

        System.Object value = valueStr;
        if (fieldInfo.FieldType.IsEnum)
            value = Enum.Parse(fieldInfo.FieldType, valueStr);
        else
        {

            if (fieldInfo.FieldType == typeof(int))
            {
                int iVal = 0;
                if (int.TryParse(valueStr, out iVal))
                    value = iVal;
                else
                {
                    float fVal = float.Parse(valueStr);
                    if (Mathf.Approximately(fVal, (float)((int)fVal)))
                        value = (int)fVal;
                    else
                        throw new FormatException();
                }
            }
            else if (fieldInfo.FieldType == typeof(byte))
                value = byte.Parse(valueStr);
            else if (fieldInfo.FieldType == typeof(float))
                value = float.Parse(valueStr);
            else if (fieldInfo.FieldType == typeof(double))
                value = double.Parse(valueStr);
            else
            {
                if (valueStr.Contains("\"\""))
                    valueStr = valueStr.Replace("\"\"", "\"");

                // process the excel string.
                if (valueStr.Length > 2 && valueStr[0] == '\"' && valueStr[valueStr.Length - 1] == '\"')
                    valueStr = valueStr.Substring(1, valueStr.Length - 2);

                value = valueStr;
            }
        }

        if (value == null)
            return;

        fieldInfo.SetValue(obj, value);
    }

    static T ParseObject<T>(string[] lines, int idx, Dictionary<int, FieldInfo> propertyInfos)
    {

        T obj = Activator.CreateInstance<T>();
        string line = lines[idx];
        string[] values = line.Split('\t');
        foreach (KeyValuePair<int, FieldInfo> pair in propertyInfos)
        {
            if (pair.Key >= values.Length)
                continue;


            string value = values[pair.Key];

            if (string.IsNullOrEmpty(value))
                continue;

            try
            {
                ParsePropertyValue(obj, pair.Value, value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError(string.Format("ParseError: Row={0} Column={1} Name={2} Want={3} Get={4}",
                    idx + 1,
                    pair.Key + 1,
                    pair.Value.Name,
                    pair.Value.FieldType.Name,
                    value));
                throw ex;
            }
        }
        return obj;
    }

    static Dictionary<int, FieldInfo> GetPropertyInfos<T>(string memberLine)
    {
        Type objType = typeof(T);

        string[] members = ParseLine(memberLine) ;

        Dictionary<int, FieldInfo> propertyInfos = new Dictionary<int, FieldInfo>();
        for (int i = 0; i < members.Length; i++)
        {
            FieldInfo fieldInfo = objType.GetField(members[i]);
            if (fieldInfo == null)
                continue;
            
            propertyInfos[i] = fieldInfo;
        }

        return propertyInfos;
    }

    public static T[] Parse<T>(string name)
    {
        var text = ResourceManager.Instance.LoadResource<TextAsset>(name).text; 
        // try parse the table lines.
        //GameLog.Debug("{0} Text Asset {1}", name, textAsset.text);
        if (text == null)
        {
            return null;
        }
        string[] lines = text.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 3)
        {
            Debug.LogError("表格文件行数错误，【1】属性名称【2】变量名称【3-...】值：" + name);
            return null;
        }
        // fetch all of the field infos.
        Dictionary<int, FieldInfo> propertyInfos = GetPropertyInfos<T>(lines[1]);

        try
        {
            // parse it one by one.
            T[] array = new T[lines.Length - 2];
            for (int i = 0; i < lines.Length - 2; i++)
                array[i] = ParseObject<T>(lines, i + 2, propertyInfos);

            return array;
        }
        catch (Exception)
        {
            Debug.LogError("表格文件行数错误：" + name);
            throw;
        }
    }
    public static T[] ParseWithText<T>(string text,string name)
    {
        if (text == null)
        {
            return null;
        }
        string[] lines = text.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 3)
        {
            Debug.LogError("表格文件行数错误，【1】属性名称【2】变量名称【3-...】值：" + name);
            return null;
        }
        // fetch all of the field infos.
        Dictionary<int, FieldInfo> propertyInfos = GetPropertyInfos<T>(lines[1]);

        try
        {
            // parse it one by one.
            T[] array = new T[lines.Length - 2];
            for (int i = 0; i < lines.Length - 2; i++)
                array[i] = ParseObject<T>(lines, i + 2, propertyInfos);

            return array;
        }
        catch (Exception)
        {
            Debug.LogError("表格文件行数错误：" + name);
            throw;
        }
    }
    public static T[] ParseValue<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("数据为空");
            return null;
        }

        // try parse the table lines.
        string[] lines = value.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 3)

        {
            Debug.LogError("表格文件行数错误，【1】属性名称【2】变量名称【3-...】值：" + value);
            return null;
        }

        // fetch all of the field infos.
        Dictionary<int, FieldInfo> propertyInfos = GetPropertyInfos<T>(lines[1]);

        try
        {
            // parse it one by one.
            T[] array = new T[lines.Length - 2];
            for (int i = 0; i < lines.Length - 2; i++)
                array[i] = ParseObject<T>(lines, i + 2, propertyInfos);

            return array;
        }
        catch (Exception)
        {
            Debug.LogError("表格文件行数错误：" + value);
            throw;
        }
    }

    static string[] ParseLine(string line)
    {
        return line.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    }
}

public static class TableUtil
{
    public static int[] IntArray(string val)
    {
        string[] val_list = val.Split('|');
        int[] result = new int[val_list.Length];

        for (int i = 0; i < val_list.Length; ++i)
        {
            int.TryParse(val_list[i], out result[i]);
        }

        return result;
    }

    public static string[] StringArray(string val)
    {
        return val.Split('|');
    }
}
