using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfflineData :MonoBehaviour
{
    public Rigidbody m_Rigidbody;
    public Collider m_Collider;
    //所有节点tran
    public Transform[] m_AllPoint;
    //子节点个数  树状结构
    public int[] m_AllPointChildCount;
    //激活 隐藏状态
    public bool[] m_AllPointActive;
    //移动 旋转 缩放
    public Vector3[] m_Pos;
    public Quaternion[] m_Rot;
    public Vector3[] m_Scale;

    /// <summary>
    /// 还原属性
    /// </summary>
    public virtual void ResetProp()
    {
        int allPointCount = m_AllPoint.Length;
        for (int i = 0; i < allPointCount; i++)
        {
            Transform tempTrs = m_AllPoint[i];
            if (tempTrs != null)
            {
                tempTrs.localPosition = m_Pos[i];
                tempTrs.localRotation = m_Rot[i];
                tempTrs.localScale = m_Scale[i];

                if (m_AllPointActive[i])
                {
                    if (!tempTrs.gameObject.activeSelf)
                    {
                        tempTrs.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (tempTrs.gameObject.activeSelf)
                    {
                        tempTrs.gameObject.SetActive(false);
                    }
                }

                if (tempTrs.childCount > m_AllPointChildCount[i])
                {
                    int childCount = tempTrs.childCount;
                    for (int j = m_AllPointChildCount[i]; j < childCount; j++)
                    {
                        GameObject tempObj = tempTrs.GetChild(j).gameObject;
                        if (!ObjectManager.Instance.IsObjectManagerCreat(tempObj))
                        {
                            GameObject.Destroy(tempObj);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 编辑器下保存初始数据
    /// </summary>
    public virtual void BindData()
    {
        m_Collider = gameObject.GetComponentInChildren<Collider>(true);
        m_Rigidbody = gameObject.GetComponentInChildren<Rigidbody>(true);
        m_AllPoint = gameObject.GetComponentsInChildren<Transform>(true);
        int allPointCount = m_AllPoint.Length;
        m_AllPointChildCount = new int[allPointCount];
        m_AllPointActive = new bool[allPointCount];
        m_Pos = new Vector3[allPointCount];
        m_Rot = new Quaternion[allPointCount];
        m_Scale = new Vector3[allPointCount];
        for (int i = 0; i < allPointCount; i++)
        {
            Transform temp = m_AllPoint[i] as Transform;
            m_AllPointChildCount[i] = temp.childCount;
            m_AllPointActive[i] = temp.gameObject.activeSelf;
            m_Pos[i] = temp.localPosition;
            m_Rot[i] = temp.localRotation;
            m_Scale[i] = temp.localScale;
        }
    }
}
