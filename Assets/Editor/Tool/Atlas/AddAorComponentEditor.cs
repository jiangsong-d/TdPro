#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class AddAorComponentEditor : MonoBehaviour
{
    private const string UILayerKey = "UI";
    // private const string MapSamplePath = "Assets/Res/Prefabs/Scene/BattleLevel/BattleMapSample.prefab";


    [MenuItem("GameObject/UI/循环列表", priority = 0)]
    private static void AddLoopList(MenuCommand command)
    {
        GameObject go = command.context as GameObject;
        GameObject orgin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/tempRes/LoopList.prefab");
        if (orgin != null)
        {
            GameObject target = Instantiate(orgin, Vector3.zero, Quaternion.identity, go.transform);
            target.name = "LoopList";
            target.GetComponent<RectTransform>().anchoredPosition3D = Vector3.zero;
            target.transform.localScale = Vector3.one;
            Selection.activeTransform = target.transform.Find("@_listView_");
        }
        //go.transform.SetLayer(UILayerKey);
        // go.layer = UILayerKey;
    }

    [MenuItem("GameObject/UI/循环网格列表", priority = 1)]
    private static void AddGridView(MenuCommand command)
    {
        GameObject go = command.context as GameObject;
        GameObject orgin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/tempRes/GridView.prefab");
        if (orgin != null)
        {
            GameObject target = Instantiate(orgin, Vector3.zero, Quaternion.identity, go.transform);
            target.name = "GridList";
            target.GetComponent<RectTransform>().anchoredPosition3D = Vector3.zero;
            target.transform.localScale = Vector3.one;
            Selection.activeTransform = target.transform.Find("@_gridView_");
        }
        //go.transform.SetLayer(UILayerKey);
    }

    
    [MenuItem("GameObject/UI/YImage", false, 2000)]
    static void AddImage(MenuCommand command)
    {
        GameObject contextGo = command.context as GameObject;
        GameObject prefabRoot = null;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            prefabRoot = prefabStage.prefabContentsRoot;
    
        GameObject chosenParent = null;
        if (prefabRoot != null)
            chosenParent = prefabRoot;
        else if (contextGo != null && !EditorUtility.IsPersistent(contextGo) && contextGo.scene.IsValid())
            chosenParent = contextGo;
        else if (Selection.activeTransform != null)
            chosenParent = Selection.activeTransform.gameObject;
    
        if (chosenParent == null)
        {
                
            return;
        }
    
        GameObject orgin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/tempRes/YIamge.prefab");
        if (orgin != null)
        {
            GameObject target = Object.Instantiate(orgin, Vector3.zero, Quaternion.identity, chosenParent.transform);
            target.name = "image";
            var rt = target.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition3D = Vector3.zero;
            target.transform.localScale = Vector3.one;
            Selection.activeObject = target;
        }
    }

    [MenuItem("GameObject/UI/YRawImage", false, 2001)]
    static void AddRawImage(MenuCommand command)
    {
        GameObject contextGo = command.context as GameObject;
        GameObject prefabRoot = null;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            prefabRoot = prefabStage.prefabContentsRoot;
    
        GameObject chosenParent = null;
        if (prefabRoot != null)
            chosenParent = prefabRoot;
        else if (contextGo != null && !EditorUtility.IsPersistent(contextGo) && contextGo.scene.IsValid())
            chosenParent = contextGo;
        else if (Selection.activeTransform != null)
            chosenParent = Selection.activeTransform.gameObject;
    
        if (chosenParent == null)
        {
            EditorUtility.DisplayDialog("Create YRawImage", "Please select a parent in the Hierarchy or open a Prefab for editing. Aborting creation to avoid placing under Scene root.", "OK");
            return;
        }
    
        GameObject orgin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/tempRes/YRawImage.prefab");
        if (orgin != null)
        {
            GameObject target = Object.Instantiate(orgin, Vector3.zero, Quaternion.identity, chosenParent.transform);
            target.name = "rawimage";
            var rt = target.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition3D = Vector3.zero;
            target.transform.localScale = Vector3.one;
            Selection.activeObject = target;
            return;
        }
    }

    [MenuItem("GameObject/UI/ButtonPro", false, 2002)]
    static void AddButtonPro(MenuCommand command)
    {
        GameObject contextGo = command.context as GameObject;
        GameObject prefabRoot = null;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            prefabRoot = prefabStage.prefabContentsRoot;
    
        GameObject chosenParent = null;
        if (prefabRoot != null)
            chosenParent = prefabRoot;
        else if (contextGo != null && !EditorUtility.IsPersistent(contextGo) && contextGo.scene.IsValid())
            chosenParent = contextGo;
        else if (Selection.activeTransform != null)
            chosenParent = Selection.activeTransform.gameObject;
    
        if (chosenParent == null)
        {
            EditorUtility.DisplayDialog("Create ButtonPro", "Please select a parent in the Hierarchy or open a Prefab for editing.", "OK");
            return;
        }
    
        GameObject orgin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/tempRes/ButtonPro.prefab");
        if (orgin != null)
        {
            GameObject target = Object.Instantiate(orgin, Vector3.zero, Quaternion.identity, chosenParent.transform);
            target.name = "ButtonPro";
            var rt = target.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition3D = Vector3.zero;
            target.transform.localScale = Vector3.one;
            Selection.activeObject = target;
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "未找到 ButtonPro 预制体！\n请在 Assets/tempRes/ 目录创建 ButtonPro.prefab", "确定");
        }
    }

    [MenuItem("GameObject/UI/TMPTextPro", false, 2003)]
    static void AddTMPTextPro(MenuCommand command)
    {
        GameObject contextGo = command.context as GameObject;
        GameObject prefabRoot = null;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            prefabRoot = prefabStage.prefabContentsRoot;
    
        GameObject chosenParent = null;
        if (prefabRoot != null)
            chosenParent = prefabRoot;
        else if (contextGo != null && !EditorUtility.IsPersistent(contextGo) && contextGo.scene.IsValid())
            chosenParent = contextGo;
        else if (Selection.activeTransform != null)
            chosenParent = Selection.activeTransform.gameObject;
    
        if (chosenParent == null)
        {
            EditorUtility.DisplayDialog("Create TMPTextPro", "Please select a parent in the Hierarchy or open a Prefab for editing.", "OK");
            return;
        }
    
        GameObject orgin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/tempRes/TMPTextPro.prefab");
        if (orgin != null)
        {
            GameObject target = Object.Instantiate(orgin, Vector3.zero, Quaternion.identity, chosenParent.transform);
            target.name = "TMPTextPro";
            var rt = target.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition3D = Vector3.zero;
            target.transform.localScale = Vector3.one;
            Selection.activeObject = target;
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "未找到 TMPTextPro 预制体！\n请在 Assets/tempRes/ 目录创建 TMPTextPro.prefab", "确定");
        }
    }

    // 隐藏系统默认的 Button 菜单
    [MenuItem("GameObject/UI/Button", true)]
    static bool HideSystemButton()
    {
        return false;
    }

    // 隐藏系统默认的 Text 菜单
    [MenuItem("GameObject/UI/Text - TextMeshPro", true)]
    static bool HideSystemText()
    {
        return false;
    }
    
    // 隐藏系统默认的 Legacy Text
    [MenuItem("GameObject/UI/Legacy/Text", true)]
    static bool HideSystemLegacyText()
    {
        return false;
    }
    
    // 隐藏系统默认的 Dropdown - TextMeshPro
    [MenuItem("GameObject/UI/Dropdown - TextMeshPro", true)]
    static bool HideSystemDropdownTMP()
    {
        return false;
    }
    
    // 隐藏系统默认的 Button - TextMeshPro
    [MenuItem("GameObject/UI/Button - TextMeshPro", true)]
    static bool HideSystemButtonTMP()
    {
        return false;
    }
    
    // 辅助方法：获取或创建Canvas（含必要组件）
    private static GameObject GetOrCreateCanvasParent(GameObject context)
    {
        Canvas canvas;
        if (context != null)
        {
            // 若上下文是Canvas或其子对象，直接使用上下文
            canvas = context.GetComponentInParent<Canvas>();
            if (canvas != null) return context.gameObject;
        }

        // 否则创建新的Canvas
        GameObject canvasGo = new GameObject("Canvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 添加必要组件
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.layer = LayerMask.NameToLayer("UI");

        // 注册撤销操作
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

        return canvasGo;
    }
    
    
    
   
}
#endif