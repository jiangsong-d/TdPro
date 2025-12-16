using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LoadSceneType
{ 
    Login,///登录
    Main,///主城
    BattleLv,//关卡战斗
    BattleDungeon,// 副本战斗
}


/// <summary>
/// 加载系统
/// </summary>
public class LoadingManager : GameSingleton<LoadingManager>
{

    private List<ILoadingStep> StepsList = new();

    private LoadingView view;

    private Action callback ; 

    private LoadSceneType _currentLoadingType;
    
     public override void Init() {  }
    private void Clear()
    {
        StepsList.Clear();
        if (view != null)
        {
            view.Close();
            view = null;
        }

        callback = null;
    }
    private void OpenView()
    {
        view = UIManager.Instance.OpenWindow<LoadingView>("LoadingView");
    }
    private IEnumerator StarUp()
    {
        OpenView();
        yield return null;
        while (view == null || !view.IsLoaded)
        {
            yield return null;
        }
        ILoadingStep currentStep = null;
        int count = StepsList.Count;
        for (int i = 0, index = 1; i < StepsList.Count; i++, index++)
        {
            currentStep = StepsList[i];
            currentStep.Execute();
            while (!currentStep.IsComplete)
            {
                view.SetProgress(currentStep.Progress * (index * 1.0f / count), currentStep.ModelName);
                yield return null;
            }
            view.SetProgress(index * 1.0f / count);
            yield return null;
        }
        if(callback!=null)
        {
            callback();
        }
        Clear();
        yield return new WaitForFixedUpdate();
    }

    #region 提示处理

    #endregion
    
#region  公用方法
    /// <summary>
    /// 开始加载
    /// </summary>
    public void StarLoading()
    {
        // GameManager.Instance.StartCoroutine(StarUp());
    }
    /// <summary>
    /// 添加启动流程
    /// </summary>
    /// <param name="step"></param>
    public void AddLoading(ILoadingStep step)
    {
        if (step == null)
        {
            return;
        }
        StepsList.Add(step);
    }
    /// <summary>
    /// 切换场景
    /// </summary>
    /// <param name="loadSceneType"></param>
    public void SwitchScene(LoadSceneType loadSceneType)
    {
        _currentLoadingType = loadSceneType;
        
         if(loadSceneType == LoadSceneType.Login)
         {
            // 游戏是否初始化
            if (GameInitStep.Instance.isGameInit)
            {
                //主城返回登录
                AddLoading(GameExitStep.Instance);
            }
            else
            {
                AddLoading(GameInitStep.Instance);
            }               
         }
         else if(loadSceneType == LoadSceneType.Main)
         {
            //加载主城 
            AddLoading(LoadingMainCityStep.Instance);
            
         }
        //  else if(loadSceneType == LoadSceneType.BattleLv)
        //  {
        //     //加载战斗
        //     AddLoading(EnterLevelStep.Instance);
        //  }
        //  else if(loadSceneType == LoadSceneType.BattleDungeon)
        //  {
        //    //加载副本
        //  }
         GameManager.Instance.StartCoroutine(StarUp());
    }
#endregion
   
}
