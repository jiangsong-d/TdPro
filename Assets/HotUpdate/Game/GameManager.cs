using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;

public class GameManager : MonoSingleton<GameManager>
{
    public Transform ModelRoot;
    public void SetGameScale(float scale)
    {
        Time.timeScale = scale;
    }
    protected override void Init()
    {
        // EngineEventManager.Instance.AddEventListener(EventID.LOGIN_SUCCESS, EnterGame);
        // 其他初始化 请放在 GameInitStep 里面
    }
    public override void Startup()
    {
        LoadManager.Instance.Init();
        ConfigManager.Instance.Init();
        LanguageManager.Instance.Init();
        LanguageManager.Instance.SetLanguage(Launcher.Instance.LangType);
        InputManager.Instance.Startup();
        SceneManager.Instance.Init();
        UIManager.Instance.Init();
        LoadingManager.Instance.Init();
    }

    /// <summary>
    /// 游戏开始
    /// </summary>
    public static void GameStart()
    {
        
        Instance.Startup();
        
        // 切换到登录场景
        LoadingManager.Instance.SwitchScene(LoadSceneType.Login);
    }

    /// <summary>
    /// 进入游戏
    /// </summary>
    /// <param name="engineEvent"></param>
    public void EnterGame(EngineEvent engineEvent)
    {

        LogUtlis.Info("进入游戏");
        SetFrame(60);
        LoadingManager.Instance.SwitchScene(LoadSceneType.Main);
    }

#if UNITY_EDITOR

    void OnDrawGizmos()
    {
        // 在这里绘制调试信息，例如当前场景中的重要对象位置等
        SceneManager.Instance.OnDrawGizmos();
    }

#endif


    /// <summary>
    /// 从新启动游戏。
    /// </summary>
    public void ResetGame()
    {
        
    }
    public void Update()
    {
        SceneManager.Instance.Update(Time.deltaTime);
    }

    public void LateUpdate()
    {
       
    }
    /// <summary>
    /// 设置帧率
    /// </summary>
    /// <param name="frame"></param>
    public void SetFrame(int frame)
    {
        Application.targetFrameRate = frame;
    }

    /// <summary>
    /// 真暂停，设置timeScale
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
    }

    public void OnDestroy()
    {
        // 使用IsInstance检查避免在OnDestroy中重新创建单例
        SceneManager.Instance.CloseAllScenes();
        LoginManager.Instance.OnDestroy();
        // UIManager.Instance.OnDestroy();
    }

    private void OnApplicationQuit()
    {
        OnDestroy();
    }
}