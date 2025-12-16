//*******************************
// Name: 蒋松 Time:2025.01.25
// Des: 预序列化节点类,用于UI的节点获取
//******************************
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 序列化节点类,表示一个UI节点
/// </summary>
[System.Serializable]
public sealed class UINodeInfo
{
    /// <summary>
    /// 节点名称,使用的Key
    /// </summary>
    public string KeyName;

    /// <summary>
    /// 绑定类型后的组件
    /// </summary>
    public UnityEngine.Object Obj;

    /// <summary>
    /// gameObject对象单独存储
    /// </summary>
    [HideInInspector]
    public UnityEngine.GameObject gameObject;

    /// <summary>
    /// 节点类
    /// </summary>
    /// <param name="name">使用的key名称</param>
    /// <param name="ComponentName">组件原始名称</param>
    /// <param name="obj">绑定类型后的组件</param>
    public UINodeInfo(string keyname, UnityEngine.Object obj, UnityEngine.GameObject gameObject)
    {
        KeyName = keyname;
        Obj = obj;
        this.gameObject = gameObject;
    }
}
/// <summary>
/// UI节点获取类
/// </summary>
[DisallowMultipleComponent]
public sealed class UIGoTable : MonoBehaviour
{
    public delegate void OnClickBtnAction(ButtonPro g);
    public delegate void OnClickToggleAction(Toggle g, bool isOn);
    public delegate void OnClickUpAction(ButtonPro g);
    public delegate void OnClickDownAction(ButtonPro g);

    public delegate void OnClickTmpAction(List<TMPTextPro> t, TMPTextPro g, string linkId);

    /// <summary>
    /// 导出的节点数组
    /// </summary>
    [SerializeField]
    public UINodeInfo[] uiNodeArray;
    [SerializeField]
    public List<ButtonPro> uiBtnList;
    [SerializeField]
    public List<Toggle> uiToggleList;
    [SerializeField]
    public List<TMPTextPro> uiTmpList;

    public OnClickBtnAction OnClickBtn;
    /// <summary>
    /// View 代码对应的Toggle响应事件
    /// </summary>
    public OnClickToggleAction OnClickToggle;


    public OnClickUpAction OnClickUp;
    public OnClickDownAction OnClickDown;
    /// <summary>
    /// View 代码对应的超链接响应事件
    /// </summary>
    public OnClickTmpAction OnClickTmp;

    public void Release()
    {
        OnClickBtn = null;
        OnClickToggle = null;
        OnClickTmp = null;
        OnClickDown = null;
        OnClickUp = null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 获取所有被标记的节点
    /// </summary>
    //[BlackList]
    public void AddTrans()
    {
        List<Transform> ls = UINodeSingleton.instance.AllFlagTrans(transform);//获取所有子节点
        uiNodeArray = new UINodeInfo[ls.Count];               //根据节点数量初始化序列化数组
        for (int i = 0; i < ls.Count; i++)                           //遍历节点
        {
            string keyname = UINodeSingleton.instance.GetUINodeKey(ls[i].name);  //获取使用的节点key
            if (keyname == string.Empty)
            {
                LogUtlis.Error("[class]UIGoTable 无法获取类型Key [name]" + ls[i].name);
                continue;
            }
            var temp = UINodeSingleton.instance.GetUINodeType(ls[i], keyname); //节点与keyname进行节点和类型的绑定
            if (temp == null)
            {
                LogUtlis.Error(ls[i], "无法从节点正确获取指定组件:" + string.Join("/", ls[i].GetComponentsInParent<Transform>(true).Reverse().Select((s) => { return s.name; }).ToArray()));
                continue;
            }

            UINodeInfo uINodeInfo = new UINodeInfo(temp.name, temp, ls[i].gameObject);     //初始化节点信息类
            uiNodeArray[i] = uINodeInfo;                               //存储节点
        }

        //序列化 Button
        uiBtnList = new List<ButtonPro>();
        uiToggleList = new List<Toggle>();
        uiTmpList = new List<TMPTextPro>();
        for (int i = 0; i < uiNodeArray.Length; i++)
        {
            var nodeinfo = uiNodeArray[i];
            if (nodeinfo.Obj != null)
            {
                if (nodeinfo.gameObject)
                {
                    var btn = nodeinfo.gameObject.GetComponent<ButtonPro>();
                    if (btn != null)
                    {
                        uiBtnList.Add(btn);
                    }
                    var toggle = nodeinfo.gameObject.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        uiToggleList.Add(toggle);
                    }
                    var tmp = nodeinfo.gameObject.GetComponent<TMPTextPro>();
                    if (tmp != null)
                    {
                        uiTmpList.Add(tmp);
                    }
                }
            }
        }

        Array.Sort(uiNodeArray, (a, b) => { return string.Compare(a.KeyName, b.KeyName); });
    }

    public int GetObjCount()
    {
        RectTransform[] tTFList = transform.GetComponentsInChildren<RectTransform>(true);
        return tTFList.Length;
    }
#endif

    public void AddClickBtnEvent(OnClickBtnAction action, OnClickToggleAction action1, OnClickUpAction action2, OnClickDownAction action3)
    {
        OnClickBtn += action;
        OnClickToggle += action1;
        OnClickUp += action2;
        OnClickDown += action3;
    }
    public void RemoveClickBtnEvent()
    {
        OnClickBtn = null;
        OnClickToggle = null;
    }


    /// <summary>
    /// 获取View下节点上的信息
    /// </summary>
    /// <returns></returns>
    public UINodeInfo[] GetUiNodeArray()
    {
        return uiNodeArray;
    }

    private void Awake()
    {
        //CreateGoTable();
        BindClick();
    }

    private bool isBind = false;
    public void OnBtnUp(ButtonPro button)
    {
        LogUtlis.Info("OnBtnUp");
        OnClickUp?.Invoke(button);
    }
    public void OnBtnDown(ButtonPro button)
    {
        LogUtlis.Info("OnBtnDown");
        OnClickDown?.Invoke(button);
    }

    /// <summary>
    /// 绑定按钮响应事件
    /// </summary>
    public void BindClick()
    {
        if (isBind) return;
        int btnCount = uiBtnList.Count;
        int toggleCount = uiToggleList.Count;
        
        for (int i = 0; i < btnCount; i++)
        {
            var btn = uiBtnList[i];
            if (btn != null)
            {
            
                bool isAnimation = btn.transition == Selectable.Transition.Animation;
                var currentBtn = btn;
                btn.onClick.AddListener(() =>
                {
                    OnBtnClick(currentBtn, null, true, isAnimation);
                });
            }
        }

        for (int i = 0; i < toggleCount; i++)
        {
            var toggle = uiToggleList[i];
            if (toggle != null)
            {
                var currentToggle = toggle;
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    OnToggleClick(currentToggle, null, isOn, false);
                });
            }
        }

        isBind = true;
    }

    private void OnBtnClick(ButtonPro go, PointerEventData eventData, bool isOn, bool isAnimation = false)
    {
        // LogUtlis.Info($"UIGoTable.OnBtnClick: {go.name}, OnClickBtn是否为null: {OnClickBtn == null}");
        
        //if (isAnimation)
        //{
        //    Tweener tweener = go.transform.DOScale(new Vector3(0.95f, 0.95f, 1), 0.1f);
        //    tweener.OnComplete(() =>
        //    {
        //        luaOnClickBtn?.Invoke(goTable, go);
        //        tweener = go.transform.DOScale(new Vector3(1, 1, 1), 0.1f);
        //    });
        //}
        //else
        //{
        //    OnClickBtn?.Invoke(goTable, go);
        //}

        OnClickBtn?.Invoke(go);
    }

    private void OnToggleClick(Toggle go, PointerEventData eventData, bool isOn, bool isAnimation = false)
    {
        if (isAnimation)
        {
            Tweener tweener = go.transform.DOScale(new Vector3(0.95f, 0.95f, 1), 0.1f);
            tweener.OnComplete(() =>
            {
                OnClickToggle?.Invoke(go, isOn);
                tweener = go.transform.DOScale(new Vector3(1, 1, 1), 0.1f);
            });
        }
        else
        {
            OnClickToggle?.Invoke(go, isOn);
        }
    }

    private void OnTmpClick(TMPTextPro go, PointerEventData eventData, string linkId)
    {
        OnClickTmp?.Invoke(uiTmpList, go, linkId);
    }
}

