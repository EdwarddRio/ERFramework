using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CheckDownloadPanel : MonoBehaviour
{
    public Slider m_Slider;
    public Text m_TxtProgress;
    public Text m_TxtDownSpeed;
    public Text m_TxtAllSize;
    public Text m_TxtVersion;
    public Text m_TxtTips;
    //重新加载
    public GameObject m_Panel_ReLoad;
    public Button m_Btn_Ok;
    public Button m_Btn_Cancel;
    //当前下载进度
    public float m_currentDownProgress = 0;

    /// <summary>
    /// 展示时初始化UI信息
    /// </summary>
    public void OnShow()
    {
        m_Slider.value = 0;
        m_TxtProgress.text = "0%";

        m_currentDownProgress = 0;
        m_Panel_ReLoad.SetActive(false);
    }
    /// <summary>
    /// 显示文字 读取版本信息
    /// </summary>
    public void ShowInfoText(string str)
    {
        m_TxtTips.text = str; ;
    }
    /// <summary>
    /// 初始化版本号和
    /// </summary>
    public void InitVersionAndAllSize(float totalDownSize)
    {
        m_TxtVersion.text = string.Format("本地版本：{0}   最新版本：{1}", Const.GAME_VERSION, Const.GAMERemote_VERSION);
        m_TxtAllSize.text = string.Format("总大小：{0} MB",  totalDownSize.ToString("0.00"));
    }
    /// <summary>
    /// 刷新下载进度
    /// </summary>
    public void RefreshDownProgress(float downProgress,float currentDownSpeed)
    {
        m_TxtDownSpeed.text = string.Format("下载速度：{0} KB/S", currentDownSpeed <=0? 1: currentDownSpeed);

        //进度条做缓动 避免太快
        m_currentDownProgress += 0.01f;
        if (m_currentDownProgress > downProgress)
        {
            m_currentDownProgress = downProgress;
        }

        m_Slider.value = m_currentDownProgress;
        ShowProgressText(m_currentDownProgress);
    }
    /// <summary>
    /// 将进度条过渡到1
    /// </summary>
    /// <param name="addNum"></param>
    /// <returns></returns>
    public bool ProgressToOne(float addNum =1)
    {
        float toPro = m_Slider.value + addNum * 0.01f;

        if (toPro >=1)
        {
            toPro = 1;
            m_Slider.value = toPro;
            ShowProgressText(toPro);
            return true;
        }
        m_Slider.value =toPro;
        ShowProgressText(toPro);
        return false;
    }
    /// <summary>
    /// 显示初始化加载配置
    /// </summary>
    public void ShowLoadConfig()
    {
        m_Slider.value = 0;
        ShowInfoText("读取配置中。。。");
    }

    /// <summary>
    /// 显示进度文字
    /// </summary>
    /// <param name="value"></param>
    public void ShowProgressText(float value)
    {
        m_TxtProgress.text = (int)(value * 100) + "%";
    }
    /// <summary>
    /// 显示重新下载提示
    /// </summary>
    public void ShowReDownPanel(bool show)
    {
        m_Panel_ReLoad.SetActive(show);
    }
}
