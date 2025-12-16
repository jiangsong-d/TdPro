using UnityEngine;

/// <summary>
/// SLG相机控制器
/// 配合InputManager使用，响应地图拖拽、缩放等操作
/// 说明：此脚本应该挂在虚拟相机上，控制虚拟相机位置
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("相机设置")]
    [SerializeField] private Camera targetCamera; // 用于InputManager的屏幕坐标转换
    [SerializeField] private Transform controlledTransform; // 要控制的Transform（默认为自身）
    
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private bool invertDragDirection = true; // 反转拖拽方向（更自然）
    [SerializeField] private bool smoothMove = true;
    [SerializeField] private float smoothTime = 0.1f;
    
    [Header("边界限制")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 boundsMin = new Vector2(-100, -100);
    [SerializeField] private Vector2 boundsMax = new Vector2(100, 100);
    
    [Header("缩放设置")]
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 30f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private bool smoothZoom = true;
    
    [Header("边缘滚动")]
    [SerializeField] private bool enableEdgeScroll = true;
    [SerializeField] private float edgeScrollThreshold = 20f; // 像素
    [SerializeField] private float edgeScrollSpeed = 10f;
    
    // 内部状态
    private Vector3 targetPosition;
    private float currentZoom;
    private float targetZoom;
    private Vector3 moveVelocity;
    private float zoomVelocity;
    private void Awake()
    {
        InputManager.Instance.Startup(); // 确保InputManager已初始化
        
        // 获取主相机（仅用于InputManager的坐标转换）
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // 获取要控制的Transform（默认为自身，即虚拟相机）
        if (controlledTransform == null)
        {
            controlledTransform = this.transform;
        }

        // 初始化位置和缩放
        targetPosition = controlledTransform.position;
        
        if (targetCamera != null && targetCamera.orthographic)
        {
            currentZoom = targetCamera.orthographicSize;
            targetZoom = currentZoom;
        }
        else
        {
            // 透视相机：缩放基于Y轴高度
            currentZoom = controlledTransform.position.y;
            targetZoom = currentZoom;
        }
    }

    private void Start()
    {
        // 获取InputManager实例
    
        
        if (InputManager.Instance != null)
        {
            // 注册事件监听
            RegisterInputEvents();
            
            // 配置InputManager参数
            ConfigureInputManager();
        }
        else
        {
            Debug.LogError("CameraController: 找不到InputManager实例！请确保场景中有InputManager对象");
        }
    }

    private void OnDestroy()
    {
        // 取消注册事件
        if (InputManager.Instance != null)
        {
            UnregisterInputEvents();
        }
    }

    private void Update()
    {
        // 边缘滚动
        if (enableEdgeScroll)
        {
            HandleEdgeScroll();
        }

        // 应用边界限制到targetPosition（避免SmoothDamp和边界限制冲突导致抖动）
        if (useBounds)
        {
            targetPosition = ClampToBounds(targetPosition);
        }

        // 平滑移动
        if (smoothMove)
        {
            // 如果已经很接近目标，直接设置位置（避免边界微小抖动）
            if (Vector3.Distance(controlledTransform.position, targetPosition) < 0.01f)
            {
                controlledTransform.position = targetPosition;
                moveVelocity = Vector3.zero;
            }
            else
            {
                controlledTransform.position = Vector3.SmoothDamp(
                    controlledTransform.position,
                    targetPosition,
                    ref moveVelocity,
                    smoothTime
                );
            }
        }
        else
        {
            controlledTransform.position = targetPosition;
        }

        // 平滑缩放
        if (smoothZoom && Mathf.Abs(currentZoom - targetZoom) > 0.01f)
        {
            currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, smoothTime);
            ApplyZoom(currentZoom);
        }
    }

    #region InputManager事件注册
    private void RegisterInputEvents()
    {
        // 地图拖拽事件
        InputManager.Instance.OnMapDrag += HandleMapDrag;
        InputManager.Instance.OnMapDragEnd += HandleMapDragEnd;
        
        // 缩放事件
        InputManager.Instance.OnPinchZoom += HandlePinchZoom;
        InputManager.Instance.OnZoomChanged += HandleZoomChanged;
        
        // 点击事件（可选，用于双击归位等）
        InputManager.Instance.OnDoubleClick += HandleDoubleClick;
        
        // 其他事件（根据需要添加）
        // inputManager.OnClick += HandleClick;
        // inputManager.OnLongPress += HandleLongPress;
    }

    private void UnregisterInputEvents()
    {
        InputManager.Instance.OnMapDrag -= HandleMapDrag;
        InputManager.Instance.OnMapDragEnd -= HandleMapDragEnd;
        InputManager.Instance.OnPinchZoom -= HandlePinchZoom;
        InputManager.Instance.OnZoomChanged -= HandleZoomChanged;
        InputManager.Instance.OnDoubleClick -= HandleDoubleClick;
    }

    private void ConfigureInputManager()
    {
        // 设置目标相机
        InputManager.Instance.SetTargetCamera(targetCamera);
        
        // 设置地图边界（与相机边界一致）
        InputManager.Instance.SetMapBounds(boundsMin, boundsMax);
        
        // 设置缩放限制
        InputManager.Instance.SetZoomLimits(minZoom, maxZoom);
        
        // 启用地图拖拽
        InputManager.Instance.SetEnableMapDrag(true);
        
        // 启用缩放
        InputManager.Instance.SetEnablePinchZoom(true);
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 处理地图拖拽
    /// </summary>
    private void HandleMapDrag(Vector3 worldPos, Vector3 delta)
    {
        // 应用拖拽移动
        Vector3 movement = delta * moveSpeed;
        
        // 反转方向（更符合直觉，拖拽地图而非拖拽相机）
        if (invertDragDirection)
        {
            movement = -movement;
        }

        // 更新目标位置（边界限制在Update中统一处理）
        targetPosition += movement;
    }

    /// <summary>
    /// 地图拖拽结束
    /// </summary>
    private void HandleMapDragEnd(Vector3 worldPos)
    {
        // 可以在这里处理惯性滑动结束后的逻辑
        // InputManager已经处理了惯性，这里只需要确保边界
        if (useBounds)
        {
            targetPosition = ClampToBounds(targetPosition);
        }
    }

    /// <summary>
    /// 处理缩放
    /// </summary>
    private void HandlePinchZoom(float zoomDelta)
    {
        // InputManager已经处理了缩放，这里可以添加额外逻辑
        // 例如：根据缩放级别调整移动速度
        // moveSpeed = Mathf.Lerp(0.5f, 2f, (currentZoom - minZoom) / (maxZoom - minZoom));
    }

    /// <summary>
    /// 缩放值变化
    /// </summary>
    private void HandleZoomChanged(float zoom)
    {
        targetZoom = zoom;
        
        if (!smoothZoom)
        {
            currentZoom = zoom;
            ApplyZoom(currentZoom);
        }
    }

    /// <summary>
    /// 双击事件
    /// </summary>
    private void HandleDoubleClick(Vector3 worldPos)
    {
        // 双击后移动相机到点击位置
        FocusOnPosition(worldPos);
        
        // 或者：双击归位到地图中心
        // FocusOnPosition(Vector3.zero);
    }
    #endregion

    #region 相机控制方法
    /// <summary>
    /// 应用缩放
    /// </summary>
    private void ApplyZoom(float zoom)
    {
        if (targetCamera != null && targetCamera.orthographic)
        {
            // 正交相机 - 注意：如果使用虚拟相机，这里可能需要调整虚拟相机的参数
            targetCamera.orthographicSize = zoom;
        }
        else
        {
            // 透视相机 - 调整虚拟相机的Y轴高度
            Vector3 pos = controlledTransform.position;
            pos.y = zoom;
            controlledTransform.position = pos;
            targetPosition = pos;
        }
    }

    /// <summary>
    /// 边界限制 - 基于相机可视区域，而不是相机坐标点
    /// </summary>
    private Vector3 ClampToBounds(Vector3 position)
    {
        // 如果使用CameraManager，利用其计算可视范围的方法
        if (CameraManager.Instance != null && CameraManager.Instance.Camera != null)
        {
            // 手动计算可视范围（不改变transform位置，避免触发其他逻辑）
            Camera cam = CameraManager.Instance.Camera;
            float distance = Mathf.Abs(position.y - 0f); // 到地面的距离
            float vFov = cam.fieldOfView * Mathf.Deg2Rad;
            float viewHeight = 2f * distance * Mathf.Tan(vFov / 2f);
            float viewWidth = viewHeight * cam.aspect;
            
            // 根据相机旋转计算实际投影到地面的尺寸
            // 对于俯视相机，需要考虑角度造成的投影变形
            Transform camTransform = controlledTransform;
            float angleRad = camTransform.eulerAngles.x * Mathf.Deg2Rad;
            float projectionFactor = 1f / Mathf.Cos(angleRad); // 投影修正系数
            
            viewHeight *= projectionFactor;
            
            // 计算可视区域的半宽和半高
            float halfWidth = viewWidth * 0.5f;
            float halfHeight = viewHeight * 0.5f;
            
            // 限制相机位置，确保可视区域在边界内
            float minX = boundsMin.x + halfWidth;
            float maxX = boundsMax.x - halfWidth;
            float minZ = boundsMin.y + halfHeight;
            float maxZ = boundsMax.y - halfHeight;
            
            // 处理地图太小的情况（可视范围大于地图）
            if (minX > maxX) minX = maxX = (boundsMin.x + boundsMax.x) * 0.5f;
            if (minZ > maxZ) minZ = maxZ = (boundsMin.y + boundsMax.y) * 0.5f;
            
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
        }
        else
        {
            // Fallback: 简单的坐标点限制
            position.x = Mathf.Clamp(position.x, boundsMin.x, boundsMax.x);
            position.z = Mathf.Clamp(position.z, boundsMin.y, boundsMax.y);
        }
        
        return position;
    }

    /// <summary>
    /// 边缘滚动
    /// </summary>
    private void HandleEdgeScroll()
    {
        Vector3 mousePos = Input.mousePosition;
        
        // 检查鼠标是否在游戏窗口内
        if (mousePos.x < 0 || mousePos.x > Screen.width || 
            mousePos.y < 0 || mousePos.y > Screen.height)
        {
            return;
        }
        
        Vector3 moveDir = Vector3.zero;

        // 左边缘
        if (mousePos.x < edgeScrollThreshold)
        {
            moveDir.x = -1;
        }
        // 右边缘
        else if (mousePos.x > Screen.width - edgeScrollThreshold)
        {
            moveDir.x = 1;
        }

        // 下边缘
        if (mousePos.y < edgeScrollThreshold)
        {
            moveDir.z = -1;
        }
        // 上边缘
        else if (mousePos.y > Screen.height - edgeScrollThreshold)
        {
            moveDir.z = 1;
        }

        // 应用边缘滚动
        if (moveDir != Vector3.zero)
        {
            targetPosition += moveDir * edgeScrollSpeed * Time.deltaTime;
            
            if (useBounds)
            {
                targetPosition = ClampToBounds(targetPosition);
            }
        }
    }

    /// <summary>
    /// 聚焦到指定位置
    /// </summary>
    public void FocusOnPosition(Vector3 worldPos, float zoom = -1)
    {
        Vector3 newPos = controlledTransform.position;
        newPos.x = worldPos.x;
        newPos.z = worldPos.z;
        
        if (useBounds)
        {
            newPos = ClampToBounds(newPos);
        }
        
        targetPosition = newPos;

        // 可选：设置缩放
        if (zoom > 0)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }
    }

    /// <summary>
    /// 设置相机位置（立即）
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        if (useBounds)
        {
            position = ClampToBounds(position);
        }
        
        targetPosition = position;
        controlledTransform.position = position;
    }

    /// <summary>
    /// 设置缩放（立即）
    /// </summary>
    public void SetZoom(float zoom)
    {
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        targetZoom = zoom;
        currentZoom = zoom;
        ApplyZoom(zoom);
    }

    /// <summary>
    /// 移动到目标位置（平滑）
    /// </summary>
    public void MoveTo(Vector3 position, float duration = 1f)
    {
        if (useBounds)
        {
            position = ClampToBounds(position);
        }
        
        // 可以使用协程或DOTween实现平滑移动
        targetPosition = position;
    }
    #endregion

    #region 运行时控制
    /// <summary>
    /// 设置地图边界
    /// </summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        boundsMin = min;
        boundsMax = max;
        
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetMapBounds(min, max);
        }
        
        // 确保当前位置在边界内
        if (useBounds)
        {
            targetPosition = ClampToBounds(targetPosition);
        }
    }

    /// <summary>
    /// 启用/禁用相机控制
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetEnableMapDrag(enabled);
            InputManager.Instance.SetEnablePinchZoom(enabled);
        }
    }

    /// <summary>
    /// 启用/禁用边缘滚动
    /// </summary>
    public void SetEnableEdgeScroll(bool enabled)
    {
        enableEdgeScroll = enabled;
    }

    /// <summary>
    /// 获取当前相机位置
    /// </summary>
    public Vector3 GetPosition()
    {
        return controlledTransform.position;
    }

    /// <summary>
    /// 获取当前缩放值
    /// </summary>
    public float GetZoom()
    {
        return currentZoom;
    }
    #endregion

    #region 编辑器可视化
    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            // 绘制地图边界
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((boundsMin.x + boundsMax.x) / 2f, 0, (boundsMin.y + boundsMax.y) / 2f);
            Vector3 size = new Vector3(boundsMax.x - boundsMin.x, 0.1f, boundsMax.y - boundsMin.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
    #endregion
}
