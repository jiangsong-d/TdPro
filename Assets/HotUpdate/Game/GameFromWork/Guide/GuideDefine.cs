/// <summary>
/// 新手引导配置数据
/// 临时结构，实际使用Luban生成
/// </summary>
[System.Serializable]
public class GuideStepConfig
{
    // === 基础配置（GuideConfig） ===
    public int Id;                          // 引导步骤ID
    public int GroupId;                     // 引导组ID
    public int StepOrder;                   // 步骤顺序
    public int ResID;                       // 资源配置ID（关联GuideResConfig）
    public GuideSceneType SceneType;        // 引导场景类型（大世界/UI窗口/对话框）
    public GuideForceType ForceType;        // 强弱引导类型（对话框强制为强引导）
    public GuideType GuideType;             // 引导类型
    public string TargetPath;               // 目标UI路径（如: "MainView/BagBtn"）
    public HighlightType HighlightType;     // 高亮类型
    public float OffsetX;                   // X偏移
    public float OffsetY;                   // Y偏移
    public ArrowDirection ArrowDirection;   // 箭头方向
    public string TriggerCondition;         // 触发条件（如: "1001:5,1002:100"）
    public bool AutoNext;                   // 是否自动进入下一步
    public float WaitTime;                  // 等待时间（秒）
    
    // === 资源配置（GuideResConfig）===
    public string BackgroundImage;          // 背景图资源路径
    public string CharacterName;            // 角色名字
    public string CharacterAvatar;          // 角色立绘/头像资源路径
    public CharacterPosition CharacterPos;  // 角色立绘位置
    public string ArrowIcon;                // 箭头图标资源路径
    public string DialogAnimation;          // 对话出现动画
    public string TipTextStyle;             // 提示文本样式
    public string TipText;                  // 提示文本内容
    public string HighlightEffect;          // 高亮特效
    public string PointerAnimation;         // 指针动画
    public string CustomParam;              // 自定义参数（JSON格式）
}

/// <summary>
/// 引导场景类型
/// </summary>
public enum GuideSceneType
{
    World = 1,          // 大世界地图引导
    UI = 2,             // UI窗口引导
    Dialog = 3,         // 对话框引导（强制为强引导）
}

/// <summary>
/// 强弱引导类型
/// </summary>
public enum GuideForceType
{
    Weak = 0,           // 弱引导（不遮罩，不强制）
    Strong = 1,         // 强引导（遮罩，强制完成）
}

/// <summary>
/// 引导类型
/// </summary>
public enum GuideType
{
    None = 0,           // 无
    Click = 1,          // 点击引导
    Dialog = 2,         // 对话引导
    ForceWait = 3,      // 强制等待
    Custom = 4,         // 自定义引导
    Highlight = 5,      // 仅高亮显示
}

/// <summary>
/// 高亮类型
/// </summary>
public enum HighlightType
{
    None = 0,           // 无高亮
    Rect = 1,           // 矩形高亮
    Circle = 2,         // 圆形高亮
    Oval = 3,           // 椭圆高亮
}

/// <summary>
/// 箭头方向
/// </summary>
public enum ArrowDirection
{
    None = 0,           // 无箭头
    Up = 1,             // 向上
    Down = 2,           // 向下
    Left = 3,           // 向左
    Right = 4,          // 向右
}

/// <summary>
/// 角色位置枚举
/// </summary>
public enum CharacterPosition
{
    None = 0,       // 无角色
    Left = 1,       // 左边
    Right = 2,      // 右边
    Center = 3,     // 居中
}

/// <summary>
/// 引导事件类型
/// </summary>
public enum GuideEventType
{
    GuideStart = 1001,      // 引导开始
    GuideStepStart = 1002,  // 步骤开始
    GuideStepEnd = 1003,    // 步骤结束
    GuideComplete = 1004,   // 引导完成
    GuidePause = 1005,      // 引导暂停
    GuideResume = 1006,     // 引导恢复
    GuideSkip = 1007,       // 跳过引导
}
