using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RealFramConfig : ScriptableObject
{
    //打包时生成AB包配置表的二进制路径
    public string m_ABBytePath;
}

[CustomEditor(typeof(RealFramConfig))]
public class RealFramConfigInspector : Editor
{
    public SerializedProperty m_ABBytePath;

    private void OnEnable()
    {
        m_ABBytePath = serializedObject.FindProperty("m_ABBytePath");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(m_ABBytePath, new GUIContent("ab包二进制路径"));
        GUILayout.Space(5);
        serializedObject.ApplyModifiedProperties();
    }
}

public class RealConfig
{
    private const string RealFramPath = "Assets/ERFram/Editor/RealFramConfig.asset";

    public static RealFramConfig GetRealFram()
    {
        RealFramConfig realConfig = AssetDatabase.LoadAssetAtPath<RealFramConfig>(RealFramPath);
        return realConfig;
    }
}
