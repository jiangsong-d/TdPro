using UnityEngine;
using UnityEditor;

/// <summary>
/// 清理场景中所有丢失的脚本引用
/// 使用方法：Tools -> Clean Missing Scripts
/// </summary>
public class CleanMissingScripts : EditorWindow
{
    private int missingCount = 0;
    private int componentsRemovedCount = 0;
    private int gameObjectsAffectedCount = 0;

    [MenuItem("Tools/Clean Missing Scripts")]
    static void ShowWindow()
    {
        GetWindow<CleanMissingScripts>("Clean Missing Scripts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Clean Missing Script References", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "此工具会扫描当前场景中所有GameObject，并移除丢失的脚本引用。\n" +
            "建议在清理前先保存场景备份！", 
            MessageType.Warning);

        GUILayout.Space(10);

        if (GUILayout.Button("扫描当前场景", GUILayout.Height(30)))
        {
            ScanScene();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("清理丢失的脚本", GUILayout.Height(30)))
        {
            CleanScene();
        }

        GUILayout.Space(10);

        if (missingCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"发现 {missingCount} 个丢失的脚本引用", 
                MessageType.Info);
        }

        if (componentsRemovedCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"已清理：\n" +
                $"- 移除了 {componentsRemovedCount} 个丢失的脚本\n" +
                $"- 影响了 {gameObjectsAffectedCount} 个GameObject", 
                MessageType.Info);
        }
    }

    private void ScanScene()
    {
        missingCount = 0;
        componentsRemovedCount = 0;
        gameObjectsAffectedCount = 0;

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            
            foreach (Component c in components)
            {
                if (c == null)
                {
                    missingCount++;
                    Debug.LogWarning($"发现丢失的脚本: {go.name}", go);
                }
            }
        }

        Debug.Log($"扫描完成！发现 {missingCount} 个丢失的脚本引用");
        Repaint();
    }

    private void CleanScene()
    {
        if (!EditorUtility.DisplayDialog(
            "确认清理", 
            "确定要清理当前场景中所有丢失的脚本引用吗？此操作无法撤销！", 
            "确定", 
            "取消"))
        {
            return;
        }

        missingCount = 0;
        componentsRemovedCount = 0;
        gameObjectsAffectedCount = 0;

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject go in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            
            if (removed > 0)
            {
                componentsRemovedCount += removed;
                gameObjectsAffectedCount++;
                Debug.Log($"从 {go.name} 移除了 {removed} 个丢失的脚本", go);
            }
        }

        Debug.Log($"清理完成！移除了 {componentsRemovedCount} 个丢失的脚本，影响了 {gameObjectsAffectedCount} 个GameObject");
        
        // 标记场景为已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Repaint();
    }
}
