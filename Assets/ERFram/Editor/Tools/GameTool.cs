using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameTool : Editor
{
    [MenuItem("Tools/打开文件夹/CacheDir")]
    protected static void OpenPersistent(MenuCommand command)
    {
        Debug.Log("Persistent File Path Is " + Application.persistentDataPath);
        Application.OpenURL(Application.persistentDataPath);
    }
    [MenuItem("Tools/打开文件夹/AssetBundleDir")]
    protected static void OpenAssetBundle(MenuCommand command)
    {
        Debug.Log("AssetBundle File Path Is " + BundleEditor.BundleTargetPath);
        Application.OpenURL(BundleEditor.BundleTargetPath);
    }
    [MenuItem("Tools/打开文件夹/EncryAssetBundleDir")]
    protected static void OpenEncryAssetBundle(MenuCommand command)
    {
        Debug.Log("AssetBundle File Path Is " + BundleEditor.BundleTargetEncryPath);
        Application.OpenURL(BundleEditor.BundleTargetEncryPath);
    }
    [MenuItem("Tools/打开文件夹/LocalDir")]
    protected static void OpenLocalDir(MenuCommand command)
    {
        Debug.Log("AssetBundle File Path Is " + Const.ABLoadPathByEditor);
        Application.OpenURL(Const.ABLoadPathByEditor);
    }

    [MenuItem("Tools/说明", false, 2)]
    public static void OpenInstruction()
    {
        EditorWindow.GetWindow<InstructionsWindow>(true, "使用说明", true).Show();
    }

}
public class InstructionsWindow : EditorWindow
{
    string instructions;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        instructions =
        "打包：\n"
      + "  ab包配置方式：(主要分为两种配置方式)："
      + "    打开ERFram/Edtior/Resource/ABConfig（分别为AllPrefabPath与AllFileDirAB）\n"
      + "    AllPrefabPath为prefab文件夹路径，可以设置多个,最终编辑器会去根据文件夹查找里面所有的Prefab去计算依赖打包\n"
      + "   （注意不要出现同名Prefab，因为每个prefab会单独根据prefab名字打包ab包）\n "
      + "    AllFileDirAB为单个文件夹ab包设置，设置的时候需要设置ab包名与ab包对应文件夹路径（如：uiatlas Assets/GameData/UIAtlas）\n"
      + "    设置好之后打包就会根据设置自动筛选及自动设定ab包，进行打包。\n"

      + "复制AB包到LocalPath：\n"
      + "    根据Editor的平台将打包出来的ab资源替换到StreamingAssets/ABDir文件夹内\n"
      + "打开文件夹：\n"
      + "    CacheDir：本地缓存文件夹\n"
      + "    AssetBundleDir：存放打包ab资源的文件夹\n"
      + "    LocalDir：StreamingAssets/ABDir文件夹\n"
      + "SceneAutoLoad：\n"
      + "    通过配置可以设置Editor内开始运行时自动切换到设置好的场景再开始。\n"
      + "离线数据：\n"
      + "    就是将预设体默认的一些配置保存下来，回收到对象池内时还原成默认配置\n"


      + "\n框架内的一些配置属性，在Const脚本内。\n";
    }


    /// <summary>
    /// 
    /// </summary>
    void OnGUI()
    {
        GUILayout.Label(instructions);
    }
}