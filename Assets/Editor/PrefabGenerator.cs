using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 预制体视图模式实体生成器编辑器窗口
/// 功能：根据选择的预制体自动生成View、Net、Manager三层架构代码
/// </summary>
public class PrefabGenerator : EditorWindow
{
    // 选择的预制体对象
    private GameObject selectedPrefab;

    // 功能模块名称（用于创建文件夹和类名）
    private string featureName = "";

    // 基础生成路径
    private string basePath = "Assets/HotUpdate/HotScripts/Game/";

    // 生成选项开关
    private bool generateManager = true;  // 是否生成Manager类
    private bool generateNet = true;      // 是否生成Net类 
    private bool generateView = true;     // 是否生成View类

    [MenuItem("Tools/MVC代码生成器")]
    public static void ShowWindow()
    {
        GetWindow<PrefabGenerator>("MVC代码生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("MVC代码自动生成器", EditorStyles.boldLabel);

        // 预制体选择字段
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("选择预制体", selectedPrefab, typeof(GameObject), false);

        // 功能模块名称输入字段（独立于预制体名称）
        featureName = EditorGUILayout.TextField("功能模块名称", featureName);

        // 如果选择了预制体但功能名称为空，使用预制体名称作为默认值
        if (selectedPrefab != null && string.IsNullOrEmpty(featureName))
        {
            featureName = selectedPrefab.name;
        }

        if (selectedPrefab != null)
        {
            string baseName = string.IsNullOrEmpty(featureName) ? selectedPrefab.name : featureName;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("将生成以下类:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Manager: {baseName}Manager");
            EditorGUILayout.LabelField($"Net: {baseName}Net");
            EditorGUILayout.LabelField($"View: {baseName}View");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("生成路径:", EditorStyles.boldLabel);
            string fullPath = Path.Combine(basePath, baseName);
            EditorGUILayout.LabelField($"Manager路径: {Path.Combine(fullPath, "Manager")}");
            EditorGUILayout.LabelField($"Net路径: {Path.Combine(fullPath, "Net")}");
            EditorGUILayout.LabelField($"View路径: {Path.Combine(fullPath, "View")}");

            // 检查路径是否已存在
            if (Directory.Exists(fullPath))
            {
                EditorGUILayout.HelpBox($"路径 {fullPath} 已存在！请先删除该文件夹再生成。", MessageType.Error);
                GUI.enabled = false;
            }
            else
            {
                GUI.enabled = true;
            }
        }

        EditorGUILayout.Space();
        generateManager = EditorGUILayout.Toggle("生成Manager", generateManager);
        generateNet = EditorGUILayout.Toggle("生成Net", generateNet);
        generateView = EditorGUILayout.Toggle("生成View", generateView);

        EditorGUILayout.Space();
        if (GUILayout.Button("生成代码", GUILayout.Height(30)))
        {
            if (selectedPrefab == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个预制体!", "确定");
                return;
            }

            if (string.IsNullOrEmpty(featureName))
            {
                EditorUtility.DisplayDialog("错误", "请输入功能模块名称!", "确定");
                return;
            }

            GenerateAllEntities();
        }
    }

    /// <summary>
    /// 生成所有实体类文件
    /// </summary>
    private void GenerateAllEntities()
    {
        try
        {
            string baseName = string.IsNullOrEmpty(featureName) ? selectedPrefab.name : featureName;
            string featurePath = Path.Combine(basePath, baseName);

            // 检查路径是否已存在
            if (Directory.Exists(featurePath))
            {
                EditorUtility.DisplayDialog("错误", $"路径 {featurePath} 已存在！请先删除该文件夹再生成。", "确定");
                return;
            }

            // 创建功能模块主目录
            Directory.CreateDirectory(featurePath);

            // 创建各层子目录
            string managerPath = Path.Combine(featurePath, "Manager");
            string netPath = Path.Combine(featurePath, "Net");
            string viewPath = Path.Combine(featurePath, "View");

            // 根据选项生成各类文件
            if (generateManager) GenerateManagerFile(managerPath, baseName);
            if (generateNet) GenerateNetFile(netPath, baseName);
            if (generateView) GenerateViewFile(viewPath, baseName);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("成功", "代码生成完成!", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成失败: {e.Message}", "确定");
        }
    }

    /// <summary>
    /// 生成Manager类文件
    /// </summary>
    /// <param name="path">生成路径</param>
    /// <param name="baseName">基础类名</param>
    private void GenerateManagerFile(string path, string baseName)
    {
        // 确保目录存在
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // 构建完整文件路径
        string scriptPath = Path.Combine(path, baseName + "Manager.cs");

        // Manager类模板
        string scriptContent = $@"using UnityEngine;

/// <summary>
/// {baseName}功能管理器
/// 负责{baseName}功能的业务逻辑处理
/// 对应预制体: {selectedPrefab.name}
/// </summary>
public class {baseName}Manager : GameSingleton<{baseName}Manager>
{{
    /// <summary>
    /// 初始化方法
    /// </summary>
    public override void Init()
    {{
        base.Init();
        // 初始化代码
    }}
    public override void Startup()
    {{
        //开始游戏
        base.Startup();
    }}
    public override void OnDestroy()
    {{
        //销毁
        base.OnDestroy();
    }}
}}";

        // 写入文件
        File.WriteAllText(scriptPath, scriptContent);
    }

    /// <summary>
    /// 生成Net类文件
    /// </summary>
    /// <param name="path">生成路径</param>
    /// <param name="baseName">基础类名</param>
    private void GenerateNetFile(string path, string baseName)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string scriptPath = Path.Combine(path, baseName + "Net.cs");

        // Net类模板
        string scriptContent = $@"using UnityEngine;

/// <summary>
/// {baseName}网络通信层
/// 负责{baseName}功能的网络通信处理
/// 对应预制体: {selectedPrefab.name}
/// </summary>
public class {baseName}Net : GameSingleton<{baseName}Net>
{{
    public override void Init()
    {{base.Init();
        //GameServerNet.Instance.AddMsgListener<RespInfo>(OnRespInfo);
    }}



    public void SendMessage()
    {{

    }}

    //private void OnRespInfo(RespInfo info)
    //{{

    //}}
}}";

        File.WriteAllText(scriptPath, scriptContent);
    }

    /// <summary>
    /// 生成View类文件
    /// </summary>
    /// <param name="path">生成路径</param>
    /// <param name="baseName">基础类名</param>
    private void GenerateViewFile(string path, string baseName)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string scriptPath = Path.Combine(path, baseName + "View.cs");

        // View类模板
        string scriptContent = $@"using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// {baseName}视图层
/// 负责{baseName}功能的界面显示和交互
/// 对应预制体: {selectedPrefab.name}
/// </summary>
public class {baseName}View : BaseUIView
{{
    #region 生命周期方法
    public override void Awake()
    {{  
        base.Awake();
        //AddEvent(EventID.WEAPONOVER, EventHandle);
    }}
    /// <summary>
    /// 创建时初始化
    /// </summary>
    public override void OnCreate()
    {{
        base.OnCreate();
        
        // 初始化界面多语言
        InitializeTexts();
    }}

    /// <summary>
    /// 界面显示时调用
    /// </summary>
    public override void OnEnable()
    {{
        base.OnEnable();
        // 界面显示时的逻辑
    }}

    /// <summary>
    /// 界面隐藏时调用
    /// </summary>
    public override void OnDisable()
    {{
        base.OnDisable();
        // 界面隐藏时的逻辑
    }}
    /// <summary>
    /// 按钮调用
    /// </summary>
    public override void OnClickBtn(Button btn)
    {{
        base.OnClickBtn(btn);
        if (btn == Btn[""""])
        {{

        }}
    }}
    /// <summary>
    /// 监听事件
    /// </summary>
    /// <param name=""engineEvent""></param>
    public override void EventHandle(EngineEvent engineEvent)
    {{
        //if (engineEvent.eventType == EventID.LOGINACCOUNT_SUCCESS)
        //{{

        //}}
    }}
    #endregion

    #region 初始化方法

    /// <summary>
    /// 初始化UI组件引用
    /// </summary>
    private void InitializeUIComponents()
    {{
        // 获取并缓存UI组件
    }}

    /// <summary>
    /// 初始化多语言文本
    /// </summary>
    private void InitializeTexts()
    {{
        // 设置界面文本的多语言
    }}

    /// <summary>
    /// 初始化按钮事件监听
    /// </summary>
    private void InitializeButtonEvents()
    {{
        // 绑定按钮点击事件
    }}

    #endregion

    #region 公共方法

    /// <summary>
    /// 更新界面数据
    /// </summary>
    public void UpdateView()
    {{
        // 更新界面显示
    }}

    #endregion
}}";

        File.WriteAllText(scriptPath, scriptContent);
    }
}