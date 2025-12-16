//***********************************
// Des: 处理标记节点的方法单例
//***********************************
using SuperScrollView;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

public class UINodeSingleton
{
    private UINodeSingleton() { }
    public static readonly UINodeSingleton instance = new UINodeSingleton();

    /// <summary>
    /// 需要绑定GoTable的组件都在这里删减
    /// </summary>
    public readonly Dictionary<Type, string> GoTableItemMap = new Dictionary<Type, string>()
    {
        {typeof(InputField), "inputfield"},
        {typeof(Button), "btn"},
        {typeof(Image), "image"},
        {typeof(RawImage), "rawimage"},
        {typeof(Slider), "slider"},
        {typeof(TMPTextPro), "tmp"},
        {typeof(TextMeshPro), "tmp3d"},
        {typeof(Text), "text"},
        {typeof(TMP_Dropdown), "tmpdropdown"},
        {typeof(Dropdown), "dropdown"},
        {typeof(CanvasGroup), "canvasgroup"},
        {typeof(AudioSource), "audiosource"},
        {typeof(ToggleGroup), "togglegroup"},
        {typeof(Toggle), "toggle"},
        {typeof(Animator), "animator"},
        {typeof(LoopGridView),"gridView" },
        {typeof(LoopListView),"listView" },
        {typeof(ScrollRect),"scrollRect"},
        //--------------------------分隔-------------------------
        {typeof(GameObject), "obj"},//组件肯定有Transform，作为标记obj的存在
        //--------------------------分隔-------------------------
        {typeof(Transform), "trans"}, //这不是bug，故意这样设计，避免tran被创建
        {typeof(RectTransform), "rect"}, //这不是bug，故意这样设计，避免rect被创建
    };

    //以此字符串的节点开头,会向子节点继续遍历查找
    public const string NAMEFLAG1 = "@_";

    //以此字符串的节点开头,不会向子节点继续遍历查找
    public const string NAMEFLAG2 = "$_";

    /// <summary>
    /// 获取所有的标记节点
    /// </summary>
    /// <param name="transform">UI根Trans</param>
    /// <returns></returns>
    public List<Transform> AllFlagTrans(Transform go)
    {
        List<Transform> select_list = new List<Transform>();
        List<Transform> childList = new List<Transform>();
        go.GetComponentsInChildren(true, childList);

        //从根节点遍历所有子节点,将符合要求(即含有标记)的节点存储到 child_list中
        foreach (Transform item in childList)
        {
            //"不符合命名规范的节点不需要再寻找";
            if (!item.name.StartsWith(NAMEFLAG1) && !item.name.StartsWith(NAMEFLAG2))//包含自身, 某些地方在调用自身的操作
            {
                continue;
            }
            bool is_continue = true;
            foreach (Transform parentNode in item.GetComponentsInParent<Transform>(true))
            {
                if (parentNode.name == go.name)//不包含本身节点
                {
                    break;
                }
                if (item != parentNode && parentNode.name.StartsWith(NAMEFLAG2))
                {
                    is_continue = false;
                    break;
                }
            }
            //"$_下的节点不需要再寻找";
            if (!is_continue)
            {
                //Debug.Log("    $_下的节点不需要再寻找  " + item.name);
                continue;
            }

            string keyname = UINodeSingleton.instance.GetUINodeKey(item.name);  //获取使用的节点key
            if (keyname == string.Empty)
            {
                LogUtlis.Error("[class]UIGoTable 无法获取类型Key [name]" + item.name);
                continue;
            }
            var temp = UINodeSingleton.instance.GetUINodeType(item, keyname); //节点与keyname进行节点和类型的绑定
            if (temp == null)
            {
                Debug.LogError("无法从节点正确获取指定组件:" + string.Join("/", item.GetComponentsInParent<Transform>(true).Reverse().Select((s) => { return s.name; }).ToArray()), item);
                continue;
            }

            select_list.Add(item);
        }
        return select_list;
    }

    /// <summary>
    /// 使用节点名称转换出使用的节点key
    /// </summary>
    /// <param name="name">节点原始名称</param>
    /// <returns></returns>
    public string GetUINodeKey(string name)
    {
        string key = string.Empty;
        if (name.StartsWith(UINodeSingleton.NAMEFLAG1))
        {
            key = name.Replace(UINodeSingleton.NAMEFLAG1, string.Empty);
        }
        if (name.StartsWith(UINodeSingleton.NAMEFLAG2))
        {
            key = name.Replace(UINodeSingleton.NAMEFLAG2, string.Empty);
        }
        return key;
    }
    /// <summary>
    /// 节点类型名称
    /// </summary>
    /// <param name="keyname">使用的节点key</param>
    /// <returns></returns>
    private string GetUINodeTypeNmae(string keyname)
    {
        string typename = string.Empty;
        typename = keyname.Split('_')[0];
        return typename;

    }

    public UObject GetUINodeType(Transform trans, string keyname)
    {
        UObject obj = null;
        string type = GetUINodeTypeNmae(keyname);

        if (type == string.Empty)
        {
            LogUtlis.Error("[class]UINodeSingleton [fun]GetUINodeType [des]ui node get type is empty");
            return null;
        }

        foreach (var item in GoTableItemMap)
        {
            if (item.Value == type)
            {
                if (type == "obj")
                    obj = trans.gameObject;
                else
                    obj = trans.GetComponent(item.Key);
                if (obj != null)
                {
                    break;
                }
            }
        }
        if (obj == null)
        {
            LogUtlis.Error(trans.name + "    " + type + "is null!!!");
        }
        return obj;
    }
}
