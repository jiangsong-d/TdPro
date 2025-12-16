#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using UnityEngine.UI;
using Unity.VisualScripting;

[CustomEditor(typeof(UIGoTable))]
public class UIGoTableEditor : UnityEditor.Editor
{
    protected static GUILayoutOption miniWidth = GUILayout.Width(25f);
    protected static GUILayoutOption txtWidth = GUILayout.Width(40f);
    protected static GUILayoutOption GUIWidth1 = GUILayout.Width(60f);
    private string _curSeekText = "";
    static public bool IsAddListener = false;

    private void Awake()
    {
        if (UIGoTableEditor.IsAddListener == false)
        {
            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            // EditorApplication.hierarchyChanged += hierarchyChanged;
            Debug.Log("添加退出编辑时的自动保存");
            UIGoTableEditor.IsAddListener = true;
        }
    }

    private void OnPrefabStageClosing(UnityEditor.SceneManagement.PrefabStage ps)
    {
        GameObject instance = ps.prefabContentsRoot;
        Selection.activeGameObject = instance;
        ManualInitialize();

        string prefabPath = ps.assetPath;
        if (!string.IsNullOrEmpty(prefabPath))
        {
            bool isSuccss = false;
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out isSuccss);
            if (isSuccss)
            {
                Debug.Log("退出编辑时的自动保存:  " + prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        UnityEditor.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        UIGoTableEditor.IsAddListener = false;
    }

    private void OnEnable()
    {
        _curSeekText = EditorPrefs.GetString("UIGoTableEditor._curSeekText", "");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var goTable = this.target as UIGoTable;

        EditorGUILayout.Space();
        ////显示搜索框
        this.FindUI(goTable);
        EditorGUILayout.Space();

        GUI.color = Color.green;
        if (GUILayout.Button("保存预设体"))
        {
            ManualInitialize();
        }
        GUI.color = Color.white;

        if (GUILayout.Button("恢复预设文字"))
        {
            UpdateAsyncText(goTable.gameObject);
        }

        EditorGUILayout.Space();
    }

    private bool IsShowFind = false;
    private void FindUI(UIGoTable goTable)
    {


        IsShowFind = EditorGUILayout.Foldout(IsShowFind, "显示 go table 详细列表");

        if (!IsShowFind) { return; }

        GUILayout.BeginHorizontal();
        {
            string after = EditorGUILayout.TextField("", _curSeekText, "SearchTextField");
            if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(40f)))
            {
                after = "";
                GUIUtility.keyboardControl = 0;
            }
            if (_curSeekText != after)
            {
                _curSeekText = after;
                EditorPrefs.SetString("UIGoTableEditor._curSeekText", _curSeekText);
            }
        }
        GUILayout.EndHorizontal();

        //显示搜索信息
        EditorGUI.BeginDisabledGroup(true);
        var nodeArray = goTable.GetUiNodeArray();
        if (nodeArray != null)
        {
            for (int i = 0; i < nodeArray.Length; i++)
            {
                var node = nodeArray[i];
                if (node.Obj == null)
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField(node.KeyName);
                    GUI.color = Color.white;
                    continue;
                }
                if (string.IsNullOrEmpty(_curSeekText) || node.Obj.name.ToLower().Contains(_curSeekText))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(string.Format("({0})", i), GUILayout.Width(32));
                    EditorGUILayout.ObjectField(node.Obj as UnityEngine.Object, typeof(UnityEngine.Object), false);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        EditorGUI.EndDisabledGroup();
    }

    private static bool _forbidUpdatePrefab = false;

    void ManualInitialize()
    {
        _forbidUpdatePrefab = false;
        if (_forbidUpdatePrefab)
        {
            return;
        }

        GameObject instance = Selection.activeGameObject;
        var gotList = instance.GetComponentsInParent<UIGoTable>();
        if (gotList != null && gotList.Length > 0)
        {
            instance = gotList[gotList.Length - 1].gameObject;
        }
        if (instance == null)
        {
            return;
        }

        //常规检测
        CheckPrefabStandard(instance);

        //YKEditorUtils.RebuildTransf(instance);

        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(instance);
        if (prefabStage != null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        }

        var goTable = instance.GetComponent<UIGoTable>();
        if (goTable != null)
        {
            UpdatePrefab(instance);
            _forbidUpdatePrefab = true;

            EditorUtility.SetDirty(instance);
            AssetDatabase.SaveAssets();
        }
    }

    public static void UpdateAsyncText(GameObject instance)
    {
        //var tmps = instance.GetComponentsInChildren<AorTMP>();
        //if (tmps != null && tmps.Length > 0)
        //{
        //    foreach (var item in tmps)
        //    {
        //        if (item)
        //        {
        //            if (!string.IsNullOrEmpty(item.languageKey))
        //            {
        //                item.text = LangPackGetUtils.Inst.GetLangPackValue(item.languageKey, false);
        //            }
        //        }
        //    }
        //}
        instance.gameObject.SetActive(false);
        instance.gameObject.SetActive(true);
    }

    /// <summary>
    /// 检查规范
    /// </summary>
    /// <param name="instance"></param>
    public static void CheckPrefabStandard(GameObject instance)
    {
        //foreach (Image item in YKEditorUtils.GetObjsByType<Image>())
        //{
        //    if (item is Image == false)
        //    {
        //        Debug.LogErrorFormat(item, "请用AorImage而不是Image!" + item.transform.GetHierarchyPath());
        //    }
        //    if (item.raycastTarget && !item.name.ToLower().Contains("mask") && !item.name.ToLower().Contains("bg") && (item.GetComponent<AorButton>() == null && item.transform.parent.GetComponent<AorButton>() == null))
        //    {
        //        Debug.LogErrorFormat(item, "Image的射线检测开启！检查是否有需求!？" + item.transform.GetHierarchyPath());
        //    }
        //}

        //foreach (RawImage item in YKEditorUtils.GetObjsByType<RawImage>())
        //{
        //    if (item is AorRawImage == false)
        //    {
        //        Debug.LogErrorFormat(item, "请用AorRawImage而不是RawImage!" + item.transform.GetHierarchyPath());
        //    }
        //    if (item.raycastTarget && !item.name.ToLower().Contains("mask") && !item.name.ToLower().Contains("bg"))
        //    {
        //        Debug.LogErrorFormat(item, "AorRawImage的射线检测开启！检查是否有需求!？" + item.transform.GetHierarchyPath());
        //    }
        //}

        //foreach (AorRawImage item in YKEditorUtils.GetObjsByType<AorRawImage>())
        //{
        //    if (item.texture != null && item.name.Contains("@_aorrawimage_"))
        //    {
        //        Debug.LogErrorFormat(item, "AorRawImage是动态赋值，但关联了贴图！如无需动态加载，请改名，如果代码已动态加载，请及时清理" + item.transform.GetHierarchyPath());
        //    }
        //    if (item.texture == null && item.Alpha != 0)
        //    {
        //        item.Alpha = 0;
        //    }
        //}

        //foreach (Text item in YKEditorUtils.GetObjsByType<Text>())
        //{
        //    if (item.raycastTarget)
        //    {
        //        item.raycastTarget = false;
        //        Debug.LogErrorFormat(item, "自动取消Text的射线检测！" + item.name);
        //    }
        //    Debug.LogErrorFormat(item, "请使用AorTMP!!!" + item.transform.GetHierarchyPath());
        //}

        //foreach (AorTMP item in YKEditorUtils.GetObjsByType<AorTMP>())
        //{
        //    if (item.raycastTarget)
        //    {
        //        if (!item.name.Contains("click"))
        //        {
        //            item.raycastTarget = false;
        //            Debug.LogErrorFormat(item, "自动取消AorTMP的射线检测！" + item.name);
        //        }
        //    }
        //    if (string.IsNullOrEmpty(item.languageKey) && !string.IsNullOrEmpty(item.text))
        //    {
        //        if (!item.name.Contains("@_aortmp_"))
        //        {
        //            Debug.LogErrorFormat(item, "AorTMP没有写languageKey！，内容：" + item.text + " " + item.transform.GetHierarchyPath());
        //        }
        //        else
        //        {
        //            Debug.LogErrorFormat(item, "AorTMP是动态赋值，已清理内容：" + item.text + " " + item.transform.GetHierarchyPath());
        //            item.text = "";
        //        }
        //    }
        //    if (string.IsNullOrEmpty(item.languageKey) && string.IsNullOrEmpty(item.text) && !item.name.Contains("@_aortmp_"))
        //    {
        //        Debug.LogErrorFormat(item, "无效AorTMP？，无内容无动态赋值，" + item.transform.GetHierarchyPath());
        //    }
        //    if (!string.IsNullOrEmpty(item.languageKey) && !string.IsNullOrEmpty(item.text))
        //    {
        //        if (item.name.Contains("@_aortmp_"))
        //        {
        //            Debug.LogErrorFormat(item, "AorTMP是动态赋值，确认是否需要languageKey！又或者去除 @_aortmp_ ！" + item.transform.GetHierarchyPath());
        //        }
        //        item.text = "";
        //    }
        //}

        //foreach (var item in YKEditorUtils.GetObjsByType<Mask>())
        //{
        //    Debug.LogErrorFormat(item, "矩形Mask请改用Mask2D!!!" + item.transform.GetHierarchyPath());
        //}

        //EditorUtility.SetDirty(instance);
    }

    public static void UpdatePrefab(GameObject instance)
    {
        var goTable = instance.GetComponent<UIGoTable>();

        //判断到该预制体有UIGoTable则为所有带有$_标记的加上UIGoTable
        if (goTable)
        {
            var tfArray = instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < tfArray.Length; i++)
            {
                Transform node = tfArray[i];
                if (node.name.Contains("$_"))
                {
                    if (node.GetComponent<UIGoTable>() == null)
                    {
                        node.gameObject.AddComponent<UIGoTable>();
                    }
                    if (node.name.Contains("@_"))
                    {
                        if (node.GetComponent<UIGoTable>())
                        {
                            DestroyImmediate(node.GetComponent<UIGoTable>());
                        }
                    }
                }

                //TODO:不适用Text组件的话，有没有必要检测？？
                //除掉所有小数
                //var rectNode = node as RectTransform;
                //if (rectNode != null)
                //{
                //    var text = rectNode.GetComponent<UnityEngine.UI.Text>();
                //    //if (text == null)
                //    {
                //        var lpos = rectNode.anchoredPosition;
                //        var x = Mathf.Round(lpos.x);
                //        var y = Mathf.Round(lpos.y);
                //        if (x != lpos.x || y != lpos.y)
                //        {
                //            //到顶节点之间, 所有宽度综合, 看单双
                //            rectNode.anchoredPosition = Vector2.zero;
                //            rectNode.anchoredPosition = new Vector2(x, y);
                //            Debug.LogWarningFormat(node, "ui中anchoredPosition不允许存在小数, 节点:{0}被自动规整", node.name);
                //        }
                //    }
                //}
                //else
                //{
                //    Debug.LogWarningFormat(node, "该节点{0}, 没有发现RectTransform组件???", node.name);
                //}
            }
        }

        // 重新获取该预制体下所有UIGoTable 执行一遍刷新操作
        UIGoTable[] gotableChilds = instance.GetComponentsInChildren<UIGoTable>(true);
        for (int i = 0; i < gotableChilds.Length; i++)
        {
            var gotable = gotableChilds[i];
            if (gotable != null)
            {
                gotable.AddTrans();
            }
        }

        _CreateGoTableDeclare(goTable);
        //AssetDatabase.SaveAssets();
    }

    private static Vector2 GetPositionAll(Transform head, RectTransform trans, Vector2 offset)
    {
        if (trans == null || trans == head)
        {
            return offset;
        }

        var pos = trans.anchoredPosition;
        var sizeDelta = trans.sizeDelta;
        var pivot = trans.pivot;
        var x = pos.x + sizeDelta.x * (pivot.x - 0.5f);
        var y = pos.y + sizeDelta.y * (pivot.y - 0.5f);
        offset.x += x;
        offset.y += y;
        return GetPositionAll(head, trans.parent as RectTransform, offset);
    }

    //生成gotable成员信息
    [MenuItem("CONTEXT/UIGoTable/复制GoTable定义信息")]
    private static void LogNodeArray(MenuCommand command)
    {
        var goTable = command.context as UIGoTable;
        var text = _GetGoTableDefineInfo(goTable);
        GUIUtility.systemCopyBuffer = text;
    }

    [MenuItem("CONTEXT/UIGoTable/整理所有文字")]
    private static void SelectAllText(MenuCommand command)
    {
        var goTable = command.context as UIGoTable;
        if (goTable)
        {
            var texts = goTable.GetComponentsInChildren<UnityEngine.UI.Text>();
            foreach (var item in texts)
            {
                var transform = item.transform;
                transform.position = new Vector3(Mathf.Ceil(transform.position.x * 100) / 100, Mathf.Ceil(transform.position.y * 100) / 100, Mathf.Ceil(transform.position.z * 100) / 100);
            }
        }
    }

    //获取gotable的全名定义
    private static string _GetGoTableDefineInfo(UIGoTable goTable)
    {
        var nameInfo = _GetGoTableViewName(goTable);
        return string.Format("---@field private go_table {0}_GoTable", nameInfo);
    }


    /// <summary>
    /// 生成gotable指定声明信息到特定文件下
    /// </summary>
    /// <param name="goTable"></param>
    private static void _CreateGoTableDeclare(UIGoTable goTable)
    {
        if (goTable == null)
            return;

        var gotables = goTable.GetComponentsInChildren<UIGoTable>(true);
        var name = goTable.name;

        //避免不同窗口下的同名组件相互覆盖
        if (name.StartsWith("$_"))
        {
            var nameInfo = _GetGoTableViewName(goTable);
            name = "$_" + nameInfo.Replace(".", "");
        }

        //Debug.LogFormat("开始对{0}, 生成声明文件", name);

        //筛选重名
        Dictionary<string, UIGoTable> gotableDic = new Dictionary<string, UIGoTable>();
        for (int i = 0, length = gotables.Length; i < length; i++)
        {
            var gotable = gotables[i];

            var gotList = gotable.GetComponentsInParent<UIGoTable>(true);

            string keyName = "";
            if (gotList != null && gotList.Length > 0)
            {
                for (int j = gotList.Length - 1; j >= 0; j--)
                {
                    keyName += gotList[j].gameObject.name;
                    if (j > 0)
                    {
                        keyName += "/";
                    }
                }
            }

            if (gotableDic.ContainsKey(keyName))
            {
                Debug.LogWarningFormat(gotable, "节点命名冲突:{0}", gotable.name);
                continue;
            }

            //从第2个节点开始, 没有前置的也不进行声明
            if (i > 0)
            {
                if (!_IsGoTableClass(gotable))
                {
                    Debug.LogWarningFormat(gotable, "节点命名不规范:{0}", gotable.name);
                    continue;
                }
            }

            gotableDic[keyName] = gotable;
        }
    }

    private static bool _IsGoTableClass(UIGoTable goTable)
    {
        var goName = goTable.name;
        if (goName.StartsWith("@_"))
        {
            return false;
        }
        else if (goName.StartsWith("$_"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static string _GetGoTableViewName(UIGoTable goTable)
    {
        var viewName = "View";
        var goName = goTable.name;
        if (goName.StartsWith("@_"))
        {
            viewName = goName.Substring(1);
        }
        else if (goName.StartsWith("$_"))
        {
            viewName = goName.Substring(1);
            if (viewName.StartsWith("_obj_"))
            {
                viewName = viewName.Substring("_obj_".Length);
            }
            var n = _GetGoTableParentViewName(goTable.transform.parent, "");
            if (string.IsNullOrEmpty(n))
            {
                return viewName;
            }
            return n + "_" + viewName;
        }
        else
        {
            viewName = goName;
        }
        return viewName;
    }

    private static string _GetGoTableParentViewName(Transform node, string cname)
    {
        // 找到第一个不带@或$但带Gotable的节点即为View节点(为了支持嵌套)
        if (node == null)
            return null;
        var pgotabele = node.GetComponent<UIGoTable>();
        if (pgotabele != null && !(pgotabele.name.StartsWith("$") || pgotabele.name.StartsWith("@")))
        {
            return $"{pgotabele.name}{cname}";
        }
        else
        {
            return _GetGoTableParentViewName(node.parent, cname);
        }
    }

    /// <summary>
    /// 获得gotable声明信息
    /// </summary>
    /// <param name="goTable"></param>
    /// <param name="sb"></param>
    private static void _GetGoTableDeclareInfo(UIGoTable goTable, StringBuilder sb)
    {
        //得到视口名字
        var viewName = _GetGoTableViewName(goTable);

        var uiNodeArray = goTable.GetUiNodeArray();
        var nodeArrayLength = uiNodeArray == null ? 0 : uiNodeArray.Length;

        //go_table 声明
        sb.AppendLine(string.Format("---@class {0}_GoTable", viewName));
        sb.AppendLine(string.Format("---@field public {0} {1}", "transform", typeof(Transform).FullName));
        sb.AppendLine(string.Format("---@field public {0} {1}", "gameObject", typeof(GameObject).FullName));
        if (uiNodeArray != null)
        {
            for (int i = 0; i < nodeArrayLength; i++)
            {
                var un = uiNodeArray[i];
                //自己这个结点不打印
                if (un.Obj == goTable.gameObject)
                    continue;

                if (un.Obj == null)
                {
                    sb.AppendLine(string.Format("---@field public {0} {1}", un.KeyName, "null"));
                }
                else
                {
                    sb.AppendLine(string.Format("---@field public {0} {1}", un.KeyName, un.Obj.GetType().FullName));
                }
            }
        }
    }

    #region UI节点快速命名

    [MenuItem("Tools/UI/UI节点快速命名 &F1", priority = 0)]
    public static void QuickReName()
    {
        Transform[] selecttransforms = Selection.transforms;
        if (selecttransforms.Length == 0)
        {
            Debug.LogError("请选择要命名的对象");
            return;
        }

        foreach (var selection in selecttransforms)
        {
            ModifyName(selection);
        }
    }

    private static void ModifyName(Transform selection)
    {
        if (selection != null)
        {
            var selectGameObject = selection.gameObject;
            Transform target = selection;

            bool isGoTable = selection.GetComponent<UIGoTable>();
            string startStr = isGoTable ? UINodeSingleton.NAMEFLAG2 : UINodeSingleton.NAMEFLAG1;//如果挂载UIGoTable，必须是$

            if (target.name.StartsWith(UINodeSingleton.NAMEFLAG1))
            {
                if (isGoTable)//遇到挂载UIGoTable，命名为@的情况
                {
                    target.name = target.name.Replace(UINodeSingleton.NAMEFLAG1, UINodeSingleton.NAMEFLAG2);
                }
                if (!ChangeKey(target, startStr))
                {
                    return;
                }
            }

            if (target.name.StartsWith(UINodeSingleton.NAMEFLAG2))
            {
                if (!isGoTable)//遇到命名为$,没有挂载UIGoTable的情况
                {
                    UIGoTable uIGoTable = target.GetComponent<UIGoTable>();

                    if (uIGoTable == null)
                    {

                        target.gameObject.AddComponent<UIGoTable>();
                    }
                }
                if (!ChangeKey(target, startStr))
                {
                    return;
                }
            }

            foreach (var item in UINodeSingleton.instance.GoTableItemMap)
            {
                if (item.Key == typeof(GameObject))
                {
                    target.name = $"{startStr}{item.Value}_" + target.name;//obj为结束点
                    break;
                }
                if (selectGameObject.GetComponent(item.Key))
                {
                    target.name = $"{startStr}{item.Value}_" + target.name;
                    break;
                }
            }

        }
    }

    /// <summary>
    /// 轮换关键字
    /// </summary>
    /// <param name="target">选中目标</param>
    /// <param name="flag">标记</param>
    /// <returns>是否需要重新取名</returns>
    private static bool ChangeKey(Transform target, string flag)
    {
        if (target.name.StartsWith($"{flag}"))
        {
            if (target.name.StartsWith($"{flag}obj_"))//obj轮换到trans
            {
                target.name = $"{flag}trans_" + target.name.Replace($"{flag}obj_", "");
                return false;
            }
            else if (target.name.StartsWith($"{flag}trans_"))//如果有rect，trans轮换到rect
            {
                if (target.GetComponent<RectTransform>())
                {
                    target.name = $"{flag}rect_" + target.name.Replace($"{flag}trans_", "");
                    return false;
                }
                else
                {
                    target.name = target.name.Replace($"{flag}trans_", "");//trans轮换到其他
                }
            }
            else if (target.name.StartsWith($"{flag}rect_"))
            {
                target.name = target.name.Replace($"{flag}rect_", "");//重置
                return false;
            }
            else//其他轮换到obj
            {
                target.name = $"{flag}obj_" + target.name.Substring(target.name.IndexOf("_", 2) + 1);
                return false;
            }
        }
        return true;
    }

    #endregion
}
#endif