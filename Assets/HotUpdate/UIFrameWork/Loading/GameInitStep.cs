using System.Collections;

/// <summary>
/// 游戏初始化启动
/// </summary>
public class GameInitStep : GameSingleton<GameInitStep>, ILoadingStep
{
    public bool IsComplete { get ; set ;}
    public float Progress { get ; set ;}
    public string ModelName { get ; set ;}
   
   /// <summary>
   /// 是否初始化
   /// </summary>
    public bool isGameInit =false;
    public void Execute()
    {
        GameManager.Instance.StartCoroutine(ExecuteStep());
    }

    private IEnumerator ExecuteStep()
    {   

        Launcher.Instance.CloseUpdetaView();
        Progress = 0.1f;
        yield return null ;
        
        // 初始化登录模块
        // LoginManager.Instance.Init();
        // LoginNet.Instance.Init();
        // LoginManager.Instance.ConnectLoginServer();
       
        Progress = 0.2f;
        yield return null;
        
        Progress = 0.3f;
        yield return null ;
        Progress = 0.5f;
        yield return null ;

        Progress = 0.7f;
        
        yield return null ;
        Progress = 0.8f;
        yield return null ;
        Progress = 0.9f;
        yield return null ;
        OnComplete();
    }
    public void OnComplete()
    {
        IsComplete = true;
        Progress = 1f;
        isGameInit = true;
        UIManager.Instance.OpenWindow<LoginView>("LoginView");
    }
}
