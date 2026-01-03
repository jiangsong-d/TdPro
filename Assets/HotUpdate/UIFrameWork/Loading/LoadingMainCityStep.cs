using System.Collections;
using UnityEngine;

public class LoadingMainCityStep : GameSingleton<LoadingMainCityStep>, ILoadingStep
{
    public bool IsComplete { get; set; }
    public float Progress { get; set; }
    public string ModelName { get; set; }
    public void Execute()
    {
        GameManager.Instance.StartCoroutine(EnterGame());
    }

    IEnumerator EnterGame()
    {

        ModelName = "获取玩家数据...";
        yield return new WaitForSeconds(0.5f);
        while (!PlayerDataManager.Instance.isPlayerDataInit)
        {
            yield return null;
        }
        Progress = 0.3f;
       // SceneManager.Instance.SwitchScene<MainCityScene>("MainCityScene");
        ModelName = "加载主城场景...";
        yield return new WaitForSeconds(1f);
        Progress = 0.5f;
        yield return new WaitForSeconds(0.3f);
        ModelName = "初始化主城数据...";
        yield return new WaitForSeconds(0.5f);
        Progress = 1f;
        ModelName = "进入主城...";
        yield return new WaitForSeconds(0.5f);

        OnComplete();
    }

    public void OnComplete()
    {
        IsComplete = true;
        Progress = 1;
        UIManager.Instance.OpenWindow<MainView>("MainView");
    }
}