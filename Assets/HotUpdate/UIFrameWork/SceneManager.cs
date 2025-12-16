using System;
using System.Collections.Generic;
using UnityEngine;


    /// <summary>
/// 场景管理器
/// </summary>
public class SceneManager : GameSingleton<SceneManager>
{
    // 当前打开的场景
    private Dictionary<string, BaseScene> openScenes = new Dictionary<string, BaseScene>();

    /// <summary>
    /// 当前场景
    /// </summary>
    private BaseScene _curScene;

    /// <summary>
    /// 当前场景配置
    /// </summary>
    private SceneConfig _curSceneCfg;

    /// <summary>
    /// 是否加载完成
    /// </summary>
    public bool IsComplete = true;

    /// <summary>
    /// 打开场景
    /// </summary>
    /// <typeparam name="T">场景类型</typeparam>
    /// <param name="sceneType">场景类型</param>
    /// <param name="parentNode">场景挂载节点</param>
    /// <param name="onOpenCompleted">场景完全打开回调</param>
    /// <param name="para">初始化场景时传入的参数</param>
    private void OpenScene<T>(string sceneName, Action<BaseScene> onOpenCompleted = null, object para = null)
        where T : BaseScene, new()
    {
        if (openScenes.ContainsKey(sceneName))
        {
            LogUtlis.Error($"场景 {sceneName} 已经打开！");
            return;
        }


        T scene = new T();
        scene.SceneName = sceneName;
        scene.SceneConfig = _curSceneCfg;
        scene.Init(UIModel.Inst.ModelRoot, (curscene) =>
        {
            onOpenCompleted?.Invoke(curscene);
            IsComplete = true;
        }, para);
        openScenes.Add(sceneName, scene);
    }

    /// <summary>
    /// 切换场景
    /// </summary>
    /// <param name="sceneType"></param>
    public void SwitchScene<T>(string sceneName, Action<BaseScene> onOpenCompleted = null, object para = null)
        where T : BaseScene, new()
    {
        if (!IsComplete)
        {
            return;
        }
        if (_curSceneCfg != null)
        {
            CloseScene(_curSceneCfg.Name);
        }
        _curSceneCfg = UIConfigManager.SConfig[sceneName];
        IsComplete = false;
        if (_curSceneCfg == null)
        {
            LogUtlis.Error($"场景: {sceneName} 不存在！ 请检查 UIConfigManager中SConfig配置");
            return;
        }
        OpenScene<T>(_curSceneCfg.Name,onOpenCompleted,para);
    }


    public void Update(float dt)
    {
        foreach (var scene in openScenes.Values)
        {
            scene.Update(dt);
        }
    }

    public void FixedUpdate(float dt)
    {
        foreach (var scene in openScenes.Values)
        {
            scene.FixedUpdate(dt);
        }
    }

    private void ToBattle()
    {

    }

    /// <summary>
    /// 关闭场景
    /// </summary>
    /// <param name="sceneType">场景类型</param>
    public void CloseScene(string sceneName)
    {
        if (openScenes.TryGetValue(sceneName, out var scene))
        {
            scene.Destroy();
            openScenes.Remove(sceneName);
        }
        else
        {
            LogUtlis.Warn($"场景 {sceneName} 未打开！");
        }
    }
#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        foreach (var scene in openScenes.Values)
        {
            scene.OnDrawGizmos();
        }
    }
#endif
    /// <summary>
    /// 获取场景
    /// </summary>
    /// <param name="sceneType">场景类型</param>
    /// <returns>场景实例</returns>
    public BaseScene GetScene(string sceneName)
    {
        if (openScenes.TryGetValue(sceneName, out var scene))
        {
            return scene;
        }
        else
        {
            LogUtlis.Warn($"场景 {sceneName} 未打开！");
            return null;
        }
    }

    /// <summary>
    /// 关闭所有场景
    /// </summary>
    public void CloseAllScenes()
    {
        foreach (var scene in openScenes.Values)
        {
            scene.Destroy();
        }

        openScenes.Clear();
    }
}


