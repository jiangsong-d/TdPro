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

        Progress = 0.3f;

        SceneManager.Instance.SwitchScene<MainCityScene>("MainCityScene");

        yield return new WaitForSeconds(1f);
        Progress = 0.5f;
        yield return new WaitForSeconds(0.3f);

        yield return new WaitForSeconds(0.5f);
        Progress = 1f;

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