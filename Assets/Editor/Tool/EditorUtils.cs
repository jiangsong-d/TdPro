using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
// Add the correct namespace for FileUtils if it exists, for example:

public static class EditorUtils
{
    #region 批量导入优化

    private static bool isStartAssetEditing = false;
    /// <summary>
    /// 开始编辑
    /// </summary>
    public static void StartAssetEditing()
    {
        if (!isStartAssetEditing)
        {
            AssetDatabase.StartAssetEditing();
            isStartAssetEditing = true;
        }
    }
    /// <summary>
    /// 停止编辑
    /// </summary>
    public static void StopAssetEditing()
    {
        if (isStartAssetEditing)
        {
            AssetDatabase.StopAssetEditing();
            isStartAssetEditing = false;
        }
    }

    #endregion

    /// <summary>
    /// 调整一下UI，方便Unity识别修改过
    /// </summary>
    public static void RebuildTransf(GameObject target)
    {
        if (target == null)
            return;
        Vector3 temp = new Vector3(1, 0);
        target.transform.localPosition += temp;
        target.transform.localPosition -= temp;
    }

    /// <summary>
    /// 调整一下UI，方便Unity识别修改过
    /// </summary>
    public static void RebuildTransf(RectTransform target)
    {
        if (target == null)
            return;
        Vector2 temp = new Vector2(1, 0);
        target.sizeDelta += temp;
        target.sizeDelta -= temp;
    }

    #region GUI
    public static bool SpriteButton(Sprite s, string style, params GUILayoutOption[] options)
    {
        if (s == null)
            return false;

        Rect tr = s.textureRect; // actual rect within the texture (handles atlases)
        float spriteW = tr.width;
        float spriteH = tr.height;
        Rect rect = GUILayoutUtility.GetRect(spriteW, spriteH, options);

        Texture2D tex = s.texture;
        if (tex == null)
            return false;

        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        bool clicked = GUI.Button(rect, new GUIContent("", s.name), style);
        GUI.DrawTextureWithTexCoords(rect, tex, uv);
        return clicked;
    }

    public static bool SpriteButton(Rect rect, Sprite s, string style, params GUILayoutOption[] options)
    {
        if (s == null)
            return false;

        Texture2D tex = s.texture;
        if (tex == null)
            return false;

        Rect tr = s.textureRect;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        bool clicked = GUI.Button(rect, new GUIContent("", s.name), style);
        GUI.DrawTextureWithTexCoords(rect, tex, uv);
        return clicked;
    }
    #endregion

    #region 资源路径相关

    /// <summary>
    /// 获取指定Asset资源全路径
    /// </summary>
    /// <param name="obj">Asset资源</param>
    /// <returns>全路径</returns>
    public static string GetAseetFullPath(Object obj)
    {
        if (obj == null)
        {
            return string.Empty;
        }
        return AssetPathToFullPath(AssetDatabase.GetAssetOrScenePath(obj));
    }

    /// <summary>
    /// 获取指定Asset资源所在的文件夹名字
    /// </summary>
    /// <param name="obj">Asset资源</param>
    /// <returns>文件夹名字</returns>
    public static string GetAssetFloderName(Object obj)
    {
        if (obj == null)
        {
            return string.Empty;
        }
        var path = FileUtils.GetFloderName(GetAseetFullPath(obj));
        return path;
    }

    /// <summary>
    /// Asset内相对路径转换为全路径
    /// </summary>
    /// <param name="assetPath">Asset内相对路径</param>
    /// <returns>全路径</returns>
    public static string AssetPathToFullPath(string assetPath)
    {
        return FileUtils.FormatToUnityPath(assetPath).Replace("Assets", Application.dataPath);
    }

    /// <summary>
    /// 全路径转换为Asset内相对路径
    /// </summary>
    /// <param name="fullPath">全路径</param>
    /// <returns>Asset内相对路径</returns>
    public static string FullPathToAssetPath(string fullPath)
    {
        return FileUtils.FormatToUnityPath(fullPath).Replace(Application.dataPath, "Assets");
    }

    public static string[] GetFilesPaths(string dirPath, string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
    {
        var paths = new List<string>();
        var files = FileUtils.GetFiles(dirPath, searchPattern, searchOption);
        string tempStr;
        if (files != null && files.Length > 0)
        {
            for (int j = 0; j < files.Length; j++)
            {
                tempStr = FullPathToAssetPath(files[j].FullName);
                paths.Add(tempStr);
            }
        }
        return paths.ToArray();
    }

    /// <summary>
    /// string转byte[]
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] FromHexString(string str)
    {
        string[] byteStrings = str.Split(",".ToCharArray());
        byte[] byteOut = new byte[byteStrings.Length];
        for (int i = 0; i < byteStrings.Length; i++)
        {
            byteOut[i] = byte.Parse(byteStrings[i]);
        }
        return byteOut;
    }
    #endregion

    /// <summary>
    /// 设置宏
    /// </summary>
    /// <param name="newDefine">宏</param>
    public static void SetScriptingDefineSymbolsForGroup(string newDefine)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
        if (string.IsNullOrEmpty(defines) || !defines.Contains(newDefine))
        {
            List<string> definesList = new List<string>(defines.Split(';'));
            definesList.Add(newDefine);
            string newDefines = string.Join(";", definesList);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newDefines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, newDefines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, newDefines);
        }
    }

    /// <summary>
    /// 移除宏
    /// </summary>
    /// <param name="targetDefine">宏</param>
    public static void RemoveScriptingDefineSymbolsForGroup(string targetDefine)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
        if (!string.IsNullOrEmpty(defines) && defines.Contains(targetDefine))
        {
            List<string> definesList = new List<string>(defines.Split(';'));
            int length = definesList.Count;
            for (int i = 0; i < length; i++)
            {
                if (definesList[i].Equals(targetDefine))
                {
                    definesList.RemoveAt(i);
                    break;
                }
            }
            string newDefines = string.Empty;
            if (definesList.Count > 0)
            {
                if (definesList.Count > 1)
                    string.Join(";", definesList);
                else
                    newDefines = definesList[0];
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, newDefines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, newDefines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, newDefines);
        }
    }

    /// <summary>
    /// 获取场景中可编辑的指定类型节点列表
    /// </summary>
    /// <typeparam name="T">指定类型</typeparam>
    /// <returns>List</returns>
    public static List<T> GetObjsByType<T>() where T : MonoBehaviour
    {
        List<T> objs = new List<T>();
        foreach (T item in Resources.FindObjectsOfTypeAll(typeof(T)) as T[])
        {
            if (!EditorUtility.IsPersistent(item.transform.root.gameObject) && !(item.hideFlags == HideFlags.NotEditable || item.hideFlags == HideFlags.HideAndDontSave))
            {
                objs.Add(item);
            }
        }
        return objs;
    }
}
