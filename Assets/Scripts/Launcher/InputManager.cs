using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 滑动方向枚举
/// </summary>
public enum SwipeDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// 拖拽类型
/// </summary>
public enum DragType
{
    None,
    Map,        // 地图拖拽
    Unit,       // 单位拖拽
    BoxSelect   // 框选
}

/// <summary>
/// 输入管理器 
/// 支持：地图拖拽、单位选择、框选、拖拽攻击、缩放、旋转核心操作
/// </summary>
public class InputManager : MonoSingleton<InputManager>
{
    #region 配置参数
    [Header("基础设置")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool ignoreUI = true; // 是否忽略UI上的操作
    [SerializeField] private LayerMask raycastLayers = -1; // 射线检测层级
    [SerializeField] private LayerMask unitLayer; // 单位层级
    [SerializeField] private LayerMask buildingLayer; // 建筑层级

    [Header("手势阈值")]
    [SerializeField] private float dragThreshold = 5f; // 拖拽阈值（像素）
    [SerializeField] private float longPressTime = 0.5f; // 长按时间阈值（秒）
    [SerializeField] private float doubleClickTime = 0.3f; // 双击时间阈值（秒）
    [SerializeField] private float swipeMinDistance = 50f; // 滑动最小距离（像素）

    [Header("SLG特性")]
    [SerializeField] private bool enableMapDrag = true; // 地图拖拽
    [SerializeField] private bool enableBoxSelect = true; // 框选
    [SerializeField] private bool enableUnitDrag = true; // 单位拖拽
    [SerializeField] private bool enableInertia = true; // 惯性滑动
    [SerializeField] private float inertiaDeceleration = 3f; // 惯性减速度
    [SerializeField] private float mapDragSensitivity = 1f; // 地图拖拽灵敏度
    [SerializeField] private Vector2 mapBoundsMin = new Vector2(-100, -100); // 地图边界
    [SerializeField] private Vector2 mapBoundsMax = new Vector2(100, 100);

    [Header("缩放控制")]
    [SerializeField] private float minZoom = 5f; // 最小缩放
    [SerializeField] private float maxZoom = 20f; // 最大缩放
    [SerializeField] private float zoomSpeed = 1f; // 缩放速度
    [SerializeField] private bool smoothZoom = true; // 平滑缩放

    [Header("功能开关")]
    [SerializeField] private bool enableDrag = true;
    [SerializeField] private bool enablePinchZoom = true;
    [SerializeField] private bool enableRotate = false; // SLG通常不需要旋转
    [SerializeField] private bool enableLongPress = true;
    [SerializeField] private bool enableSwipe = true;
    [SerializeField] private bool enableDoubleClick = true;

    [Header("框选设置")]
    [SerializeField] private float boxSelectMinSize = 20f; // 框选最小尺寸
    [SerializeField] private Color boxSelectColor = new Color(0, 1, 0, 0.3f);
    #endregion

    #region 事件定义
    // 基础输入事件
    public event Action<Vector3> OnClick; // 点击世界坐标
    public event Action<GameObject> OnClickObject; // 点击到对象
    public event Action<Vector3> OnDragStart;
    public event Action<Vector3, Vector3> OnDrag; // 当前位置, 增量
    public event Action<Vector3> OnDragEnd;
    public event Action<float> OnPinchZoom;
    public event Action<float> OnRotate;
    public event Action<Vector3> OnLongPress;
    public event Action<GameObject> OnLongPressObject; // 长按对象
    public event Action<Vector3, Vector3, SwipeDirection> OnSwipe;
    public event Action<Vector3> OnDoubleClick;

    // SLG特定事件
    public event Action<Vector3, Vector3> OnMapDrag; // 地图拖拽（世界坐标，增量）
    public event Action<Vector3> OnMapDragEnd; // 地图拖拽结束
    public event Action<GameObject, Vector3> OnUnitDragStart; // 单位开始拖拽（单位对象，起始位置）
    public event Action<GameObject, Vector3> OnUnitDrag; // 单位拖拽中（单位对象，当前位置）
    public event Action<GameObject, Vector3> OnUnitDragEnd; // 单位拖拽结束（单位对象，目标位置）
    public event Action<Vector2, Vector2> OnBoxSelectStart; // 框选开始（屏幕坐标）
    public event Action<Vector2, Vector2> OnBoxSelecting; // 框选中（起始屏幕坐标，当前屏幕坐标）
    public event Action<List<GameObject>> OnBoxSelectEnd; // 框选结束（选中的对象列表）
    public event Action<float> OnZoomChanged; // 缩放变化（当前缩放值）
    #endregion

    #region 状态变量
    private Vector3 touchStartPos;
    private Vector3 touchStartWorldPos;
    private Vector3 lastTouchPos;
    private float touchStartTime;
    private float lastClickTime;
    private bool isDragging;
    private bool isLongPressing;
    private bool isPinching;

    // SLG状态
    private DragType currentDragType = DragType.None;
    private GameObject draggedUnit; // 正在拖拽的单位
    private GameObject clickedObject; // 点击的对象
    private Vector2 boxSelectStartPos; // 框选起始屏幕坐标
    private bool isBoxSelecting;

    // 惯性滑动
    private Vector3 dragVelocity;
    private bool isInertiaMoving;

    // 多点触控
    private float lastPinchDistance;
    private float lastRotationAngle;
    
    // 缩放
    private float currentZoom;
    private float targetZoom;
    
    // 对象池
    private List<GameObject> boxSelectedObjects = new List<GameObject>();
    #endregion
    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // 初始化缩放
        if (targetCamera != null)
        {
            if (targetCamera.orthographic)
            {
                currentZoom = targetCamera.orthographicSize;
                targetZoom = currentZoom;
            }
            else
            {
                currentZoom = targetCamera.transform.position.y;
                targetZoom = currentZoom;
            }
        }
    }

    private void Update()
    {
        // 处理移动端触摸输入
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        // 处理PC端鼠标输入
        else
        {
            HandleMouseInput();
        }

        // 检测长按
        if (enableLongPress && !isDragging && !isBoxSelecting && Input.GetMouseButton(0))
        {
            CheckLongPress();
        }

        // 惯性滑动
        if (enableInertia && isInertiaMoving)
        {
            UpdateInertia();
        }

        // 平滑缩放
        if (smoothZoom && Mathf.Abs(currentZoom - targetZoom) > 0.01f)
        {
            UpdateSmoothZoom();
        }
    }

    private void OnGUI()
    {
        // 绘制框选框
        if (isBoxSelecting && enableBoxSelect)
        {
            DrawBoxSelect();
        }
    }

    #region 鼠标输入处理
    private void HandleMouseInput()
    {
        // 检查是否点击在UI上
        if (ignoreUI && UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
           
            return;
        }

        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {

            HandleMouseDown(Input.mousePosition);
        }
        // 鼠标拖拽
        else if (Input.GetMouseButton(0) && enableDrag)
        {
            HandleMouseDrag(Input.mousePosition);
        }
        // 鼠标抬起
        else if (Input.GetMouseButtonUp(0))
        {
            HandleMouseUp(Input.mousePosition);
        }

        // 鼠标滚轮缩放
        if (enablePinchZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                SetZoom(-scroll * 10f); // 注意方向
                OnPinchZoom?.Invoke(scroll * 10f);
            }
        }
    }

    private void HandleMouseDown(Vector3 screenPos)
    {
        touchStartPos = screenPos;
        touchStartWorldPos = ScreenToWorldPoint(screenPos);
        lastTouchPos = screenPos;
        touchStartTime = Time.time;
        isDragging = false;
        isLongPressing = false;
        isInertiaMoving = false;
        currentDragType = DragType.None;
        draggedUnit = null;
        clickedObject = null;

        // 检测点击的对象
        clickedObject = DetectObject(screenPos);
        
        // 判断点击的是单位还是空地
        if (clickedObject != null && IsUnit(clickedObject))
        {
            // 点击了单位，可能要开始单位拖拽
            if (enableUnitDrag)
            {
                draggedUnit = clickedObject;
            }
        }
    }

    private void HandleMouseDrag(Vector3 screenPos)
    {
        float distance = Vector3.Distance(screenPos, touchStartPos);

        // 开始拖拽
        if (!isDragging && distance > dragThreshold)
        {
            isDragging = true;
            isLongPressing = false;
            
            // 判断拖拽类型
            if (draggedUnit != null && enableUnitDrag)
            {
                // 单位拖拽
                currentDragType = DragType.Unit;
                OnUnitDragStart?.Invoke(draggedUnit, touchStartWorldPos);
            }
            else if (enableBoxSelect && clickedObject == null)
            {
                // 框选（空地上开始拖拽）
                currentDragType = DragType.BoxSelect;
                boxSelectStartPos = touchStartPos;
                isBoxSelecting = true;
                OnBoxSelectStart?.Invoke(touchStartPos, screenPos);
            }
            else if (enableMapDrag)
            {
                // 地图拖拽
                currentDragType = DragType.Map;
            }

            OnDragStart?.Invoke(touchStartWorldPos);
        }

        // 拖拽中
        if (isDragging)
        {
            Vector3 currentWorldPos = ScreenToWorldPoint(screenPos);
            Vector3 lastWorldPos = ScreenToWorldPoint(lastTouchPos);
            Vector3 delta = currentWorldPos - lastWorldPos;

            // 计算速度（用于惯性）
            dragVelocity = delta / Time.deltaTime;

            switch (currentDragType)
            {
                case DragType.Map:
                    // 地图拖拽
                    delta *= mapDragSensitivity;
                    OnMapDrag?.Invoke(currentWorldPos, delta);
                    break;

                case DragType.Unit:
                    // 单位拖拽
                    if (draggedUnit != null)
                    {
                        OnUnitDrag?.Invoke(draggedUnit, currentWorldPos);
                    }
                    break;

                case DragType.BoxSelect:
                    // 框选
                    isBoxSelecting = true;
                    OnBoxSelecting?.Invoke(boxSelectStartPos, screenPos);
                    break;
            }

            OnDrag?.Invoke(currentWorldPos, delta);
        }

        lastTouchPos = screenPos;
    }

    private void HandleMouseUp(Vector3 screenPos)
    {
        Vector3 worldPos = ScreenToWorldPoint(screenPos);
        float distance = Vector3.Distance(screenPos, touchStartPos);
        float duration = Time.time - touchStartTime;

        // 判断是否为点击
        if (!isDragging && distance < dragThreshold)
        {
            // 双击检测
            if (enableDoubleClick && Time.time - lastClickTime < doubleClickTime)
            {
                OnDoubleClick?.Invoke(worldPos);
                lastClickTime = 0; // 重置，避免三击触发两次双击
            }
            else
            {
                OnClick?.Invoke(worldPos);
                
                // 点击对象事件
                if (clickedObject != null)
                {
                    OnClickObject?.Invoke(clickedObject);
                }
                
                lastClickTime = Time.time;
            }
        }
        // 判断是否为滑动
        else if (enableSwipe && !isDragging && distance > swipeMinDistance)
        {
            SwipeDirection direction = GetSwipeDirection(touchStartPos, screenPos);
            OnSwipe?.Invoke(touchStartWorldPos, worldPos, direction);
        }
        // 拖拽结束
        else if (isDragging)
        {
            switch (currentDragType)
            {
                case DragType.Map:
                    // 地图拖拽结束，触发惯性
                    OnMapDragEnd?.Invoke(worldPos);
                    if (enableInertia && dragVelocity.magnitude > 0.1f)
                    {
                        isInertiaMoving = true;
                    }
                    break;

                case DragType.Unit:
                    // 单位拖拽结束
                    if (draggedUnit != null)
                    {
                        OnUnitDragEnd?.Invoke(draggedUnit, worldPos);
                    }
                    break;

                case DragType.BoxSelect:
                    // 框选结束
                    if (Vector2.Distance(boxSelectStartPos, screenPos) > boxSelectMinSize)
                    {
                        PerformBoxSelect(boxSelectStartPos, screenPos);
                    }
                    isBoxSelecting = false;
                    break;
            }

            OnDragEnd?.Invoke(worldPos);
        }

        // 重置状态
        isDragging = false;
        isLongPressing = false;
        currentDragType = DragType.None;
        draggedUnit = null;
        clickedObject = null;
    }
    #endregion

    #region 触摸输入处理
    private void HandleTouchInput()
    {
        // 单点触摸
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // 检查是否点击在UI上
            if (ignoreUI && UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleMouseDown(touch.position);
                    break;
                case TouchPhase.Moved:
                    if (enableDrag)
                    {
                        HandleMouseDrag(touch.position);
                    }
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleMouseUp(touch.position);
                    break;
            }
        }
        // 双指触摸 - 缩放和旋转
        else if (Input.touchCount == 2)
        {
            HandleMultiTouch();
        }
    }

    private void HandleMultiTouch()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // 计算两指距离和角度
        float currentDistance = Vector2.Distance(touch0.position, touch1.position);
        Vector2 direction = touch1.position - touch0.position;
        float currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            isPinching = true;
            lastPinchDistance = currentDistance;
            lastRotationAngle = currentAngle;
            isDragging = false; // 取消拖拽
        }
        else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
        {
            // 缩放
            if (enablePinchZoom)
            {
                float distanceDelta = currentDistance - lastPinchDistance;
                float zoomDelta = distanceDelta * 0.01f; // 缩放系数
                SetZoom(-zoomDelta); // 缩放相机
                OnPinchZoom?.Invoke(zoomDelta);
                lastPinchDistance = currentDistance;
            }

            // 旋转
            if (enableRotate)
            {
                float angleDelta = Mathf.DeltaAngle(lastRotationAngle, currentAngle);
                OnRotate?.Invoke(angleDelta);
                lastRotationAngle = currentAngle;
            }
        }
        else if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
        {
            isPinching = false;
        }
    }
    #endregion

    #region 长按检测
    private void CheckLongPress()
    {
        if (!isLongPressing && !isDragging)
        {
            float pressDuration = Time.time - touchStartTime;
            if (pressDuration >= longPressTime)
            {
                isLongPressing = true;
                OnLongPress?.Invoke(touchStartWorldPos);
                
                // 长按对象事件
                if (clickedObject != null)
                {
                    OnLongPressObject?.Invoke(clickedObject);
                }
            }
        }
    }
    #endregion

    #region SLG特定功能
    /// <summary>
    /// 检测点击的对象
    /// </summary>
    private GameObject DetectObject(Vector3 screenPos)
    {
        Ray ray = targetCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f, raycastLayers))
        {
            return hit.collider.gameObject;
        }

        // 2D检测
        Vector3 worldPos = ScreenToWorldPoint(screenPos);
        RaycastHit2D hit2D = Physics2D.Raycast(worldPos, Vector2.zero, 0, raycastLayers);
        if (hit2D.collider != null)
        {
            return hit2D.collider.gameObject;
        }

        return null;
    }

    /// <summary>
    /// 判断是否为单位
    /// </summary>
    private bool IsUnit(GameObject obj)
    {
        if (obj == null) return false;
        
        // 检查层级
        int layer = obj.layer;
        if (((1 << layer) & unitLayer.value) != 0)
        {
            return true;
        }

        // 检查标签（暂时注释，避免标签未定义错误）
        // if (obj.CompareTag("Unit") || obj.CompareTag("Soldier"))
        // {
        //     return true;
        // }

        // 检查组件（如果项目中有 EntityReference，取消下面的注释）
        // return obj.GetComponent<EntityReference>() != null;
        return false;
    }

    /// <summary>
    /// 执行框选
    /// </summary>
    private void PerformBoxSelect(Vector2 startScreenPos, Vector2 endScreenPos)
    {
        boxSelectedObjects.Clear();

        // 计算框选矩形
        Rect selectRect = GetScreenRect(startScreenPos, endScreenPos);

        // 查找所有单位
        GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
        
        foreach (GameObject unit in allUnits)
        {
            Vector3 screenPos = targetCamera.WorldToScreenPoint(unit.transform.position);
            
            if (selectRect.Contains(new Vector2(screenPos.x, screenPos.y)))
            {
                boxSelectedObjects.Add(unit);
            }
        }

        // 也可以通过碰撞检测框选
        Collider[] colliders = Physics.OverlapSphere(Vector3.zero, 1000f, unitLayer);
        foreach (Collider col in colliders)
        {
            Vector3 screenPos = targetCamera.WorldToScreenPoint(col.transform.position);
            if (selectRect.Contains(new Vector2(screenPos.x, screenPos.y)))
            {
                if (!boxSelectedObjects.Contains(col.gameObject))
                {
                    boxSelectedObjects.Add(col.gameObject);
                }
            }
        }

        OnBoxSelectEnd?.Invoke(new List<GameObject>(boxSelectedObjects));
    }

    /// <summary>
    /// 获取屏幕矩形（处理任意拖拽方向）
    /// </summary>
    private Rect GetScreenRect(Vector2 start, Vector2 end)
    {
        float minX = Mathf.Min(start.x, end.x);
        float minY = Mathf.Min(start.y, end.y);
        float maxX = Mathf.Max(start.x, end.x);
        float maxY = Mathf.Max(start.y, end.y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// 绘制框选框
    /// </summary>
    private void DrawBoxSelect()
    {
        Vector2 currentPos = Input.mousePosition;
        currentPos.y = Screen.height - currentPos.y; // GUI坐标系Y轴反转
        Vector2 startPos = new Vector2(boxSelectStartPos.x, Screen.height - boxSelectStartPos.y);

        Rect rect = GetScreenRect(startPos, currentPos);

        // 绘制填充
        GUI.color = boxSelectColor;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        // 绘制边框
        GUI.color = new Color(boxSelectColor.r, boxSelectColor.g, boxSelectColor.b, 1f);
        DrawRectBorder(rect, 2);
    }

    /// <summary>
    /// 绘制矩形边框
    /// </summary>
    private void DrawRectBorder(Rect rect, int thickness)
    {
        // 上
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        // 下
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        // 左
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        // 右
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }

    /// <summary>
    /// 更新惯性滑动
    /// </summary>
    private void UpdateInertia()
    {
        if (dragVelocity.magnitude < 0.01f)
        {
            isInertiaMoving = false;
            dragVelocity = Vector3.zero;
            return;
        }

        // 应用惯性移动
        Vector3 delta = dragVelocity * Time.deltaTime;
        OnMapDrag?.Invoke(ScreenToWorldPoint(Input.mousePosition), delta);

        // 减速
        dragVelocity = Vector3.Lerp(dragVelocity, Vector3.zero, inertiaDeceleration * Time.deltaTime);
    }

    /// <summary>
    /// 更新平滑缩放
    /// </summary>
    private void UpdateSmoothZoom()
    {
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * 5f);
        
        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = currentZoom;
        }
        else
        {
            Vector3 pos = targetCamera.transform.position;
            pos.y = currentZoom;
            targetCamera.transform.position = pos;
        }

        OnZoomChanged?.Invoke(currentZoom);
    }

    /// <summary>
    /// 设置缩放（带限制）
    /// </summary>
    private void SetZoom(float zoomDelta)
    {
        targetZoom = Mathf.Clamp(targetZoom - zoomDelta * zoomSpeed, minZoom, maxZoom);
        
        if (!smoothZoom)
        {
            currentZoom = targetZoom;
            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = currentZoom;
            }
            else
            {
                Vector3 pos = targetCamera.transform.position;
                pos.y = currentZoom;
                targetCamera.transform.position = pos;
            }
            OnZoomChanged?.Invoke(currentZoom);
        }
    }

    /// <summary>
    /// 检查地图边界
    /// </summary>
    public bool IsInMapBounds(Vector3 worldPos)
    {
        return worldPos.x >= mapBoundsMin.x && worldPos.x <= mapBoundsMax.x &&
               worldPos.z >= mapBoundsMin.y && worldPos.z <= mapBoundsMax.y;
    }

    /// <summary>
    /// 限制在地图边界内
    /// </summary>
    public Vector3 ClampToMapBounds(Vector3 worldPos)
    {
        worldPos.x = Mathf.Clamp(worldPos.x, mapBoundsMin.x, mapBoundsMax.x);
        worldPos.z = Mathf.Clamp(worldPos.z, mapBoundsMin.y, mapBoundsMax.y);
        return worldPos;
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 屏幕坐标转世界坐标
    /// </summary>
    private Vector3 ScreenToWorldPoint(Vector3 screenPos)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera.orthographic)
        {
            // 正交相机
            Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            return worldPos;
        }
        else
        {
            // 透视相机 - 投射到Y=0平面（地面）
            Ray ray = targetCamera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            return Vector3.zero;
        }
    }

    /// <summary>
    /// 判断滑动方向
    /// </summary>
    private SwipeDirection GetSwipeDirection(Vector3 startPos, Vector3 endPos)
    {
        Vector2 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 上: 45~135, 下: -135~-45, 左: 135~-135, 右: -45~45
        if (angle >= 45f && angle <= 135f)
            return SwipeDirection.Up;
        else if (angle >= -135f && angle <= -45f)
            return SwipeDirection.Down;
        else if (Mathf.Abs(angle) > 135f)
            return SwipeDirection.Left;
        else
            return SwipeDirection.Right;
    }

    /// <summary>
    /// 射线检测
    /// </summary>
    public bool Raycast(Vector3 worldPos, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(worldPos, Vector2.zero, 0, raycastLayers);
        return hit.collider != null;
    }

    /// <summary>
    /// 3D射线检测
    /// </summary>
    public bool Raycast3D(Vector3 screenPos, out RaycastHit hit)
    {
        Ray ray = targetCamera.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out hit, 1000f, raycastLayers);
    }
    #endregion

    #region 运行时控制
    /// <summary>
    /// 启用/禁用拖拽功能
    /// 使用场景：
    /// 1. 打开UI面板时禁用拖拽，避免误操作
    /// 2. 播放剧情动画时禁用所有输入
    /// 示例：InputManager.Instance.SetEnableDrag(false);
    /// </summary>
    public void SetEnableDrag(bool enabled)
    {
        enableDrag = enabled;
    }

    /// <summary>
    /// 启用/禁用缩放功能
    /// 使用场景：
    /// 1. 战斗中禁用缩放，保持固定视角
    /// 2. 新手引导时锁定缩放级别
    /// 示例：InputManager.Instance.SetEnablePinchZoom(false);
    /// </summary>
    public void SetEnablePinchZoom(bool enabled)
    {
        enablePinchZoom = enabled;
    }

    /// <summary>
    /// 启用/禁用旋转功能
    /// 使用场景：一般SLG不需要旋转，默认关闭
    /// 示例：InputManager.Instance.SetEnableRotate(true);
    /// </summary>
    public void SetEnableRotate(bool enabled)
    {
        enableRotate = enabled;
    }

    /// <summary>
    /// 启用/禁用长按功能
    /// 使用场景：
    /// 1. 某些界面下不需要长按查看详情
    /// 2. 快节奏战斗时禁用长按
    /// 示例：InputManager.Instance.SetEnableLongPress(false);
    /// </summary>
    public void SetEnableLongPress(bool enabled)
    {
        enableLongPress = enabled;
    }

    /// <summary>
    /// 启用/禁用滑动手势
    /// 使用场景：特定界面下禁用滑动切换
    /// 示例：InputManager.Instance.SetEnableSwipe(false);
    /// </summary>
    public void SetEnableSwipe(bool enabled)
    {
        enableSwipe = enabled;
    }

    /// <summary>
    /// 启用/禁用双击功能
    /// 使用场景：避免误触双击
    /// 示例：InputManager.Instance.SetEnableDoubleClick(false);
    /// </summary>
    public void SetEnableDoubleClick(bool enabled)
    {
        enableDoubleClick = enabled;
    }

    /// <summary>
    /// 设置是否忽略UI上的点击
    /// 使用场景：
    /// 1. 默认为true，点击UI时不触发游戏内操作
    /// 2. 特殊情况下设为false，穿透UI点击
    /// 示例：InputManager.Instance.SetIgnoreUI(true);
    /// </summary>
    public void SetIgnoreUI(bool ignore)
    {
        ignoreUI = ignore;
    }

    /// <summary>
    /// 设置目标相机
    /// 使用场景：
    /// 1. 切换相机时（主相机 → UI相机）
    /// 2. 多相机场景下动态切换
    /// 示例：InputManager.Instance.SetTargetCamera(mainCamera);
    /// </summary>
    public void SetTargetCamera(Camera camera)
    {
        targetCamera = camera;
    }

    /// <summary>
    /// 设置地图边界
    /// 使用场景：
    /// 1. 初始化地图时设置可拖拽范围
    /// 2. 动态加载新地图区域时更新边界
    /// 示例：
    /// Vector2 min = new Vector2(-100, -100);
    /// Vector2 max = new Vector2(100, 100);
    /// InputManager.Instance.SetMapBounds(min, max);
    /// </summary>
    public void SetMapBounds(Vector2 min, Vector2 max)
    {
        mapBoundsMin = min;
        mapBoundsMax = max;
    }

    /// <summary>
    /// 设置缩放限制
    /// 使用场景：
    /// 1. 不同场景设置不同的缩放范围
    /// 2. 世界地图：大范围（10-50），战斗：小范围（5-15）
    /// 示例：InputManager.Instance.SetZoomLimits(5f, 20f);
    /// </summary>
    public void SetZoomLimits(float min, float max)
    {
        minZoom = min;
        maxZoom = max;
    }

    /// <summary>
    /// 获取当前缩放值
    /// 使用场景：
    /// 1. UI显示当前缩放级别
    /// 2. 根据缩放级别调整LOD
    /// 示例：float zoom = InputManager.Instance.GetCurrentZoom();
    /// </summary>
    public float GetCurrentZoom()
    {
        return currentZoom;
    }

    /// <summary>
    /// 聚焦到指定位置（带可选缩放）
    /// 使用场景：
    /// 1. 点击小地图时，相机移动到目标位置
    /// 2. 选中单位时，相机聚焦到单位
    /// 3. 剧情引导时，移动相机到特定建筑
    /// 示例：
    /// // 移动到位置，保持当前缩放
    /// InputManager.Instance.FocusOnPosition(targetPos);
    /// // 移动到位置并设置缩放为10
    /// InputManager.Instance.FocusOnPosition(targetPos, 10f);
    /// </summary>
    public void FocusOnPosition(Vector3 worldPos, float zoom = -1)
    {
        if (targetCamera == null) return;

        // 移动相机到目标位置
        Vector3 camPos = targetCamera.transform.position;
        camPos.x = worldPos.x;
        camPos.z = worldPos.z;
        targetCamera.transform.position = camPos;

        // 设置缩放
        if (zoom > 0)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }
    }

    /// <summary>
    /// 启用/禁用地图拖拽
    /// 使用场景：
    /// 1. 战斗锁定视角时禁用地图拖拽
    /// 2. 固定相机的剧情场景
    /// 示例：InputManager.Instance.SetEnableMapDrag(false);
    /// </summary>
    public void SetEnableMapDrag(bool enabled)
    {
        enableMapDrag = enabled;
    }

    /// <summary>
    /// 启用/禁用框选功能
    /// 使用场景：
    /// 1. 单位管理界面启用框选
    /// 2. 战斗结束后禁用框选
    /// 示例：InputManager.Instance.SetEnableBoxSelect(true);
    /// </summary>
    public void SetEnableBoxSelect(bool enabled)
    {
        enableBoxSelect = enabled;
    }

    /// <summary>
    /// 启用/禁用单位拖拽
    /// 使用场景：
    /// 1. 战斗中允许拖拽单位移动/攻击
    /// 2. 编队界面拖拽单位调整位置
    /// 3. 非战斗状态禁用拖拽
    /// 示例：InputManager.Instance.SetEnableUnitDrag(true);
    /// </summary>
    public void SetEnableUnitDrag(bool enabled)
    {
        enableUnitDrag = enabled;
    }

    /// <summary>
    /// 设置单位所在的层级
    /// 使用场景：
    /// 1. 初始化时设置单位层级（Layer 8: "Unit"）
    /// 2. 动态切换可交互的对象类型
    /// 示例：InputManager.Instance.SetUnitLayer(LayerMask.GetMask("Unit", "Soldier"));
    /// </summary>
    public void SetUnitLayer(LayerMask layer)
    {
        unitLayer = layer;
    }

    /// <summary>
    /// 设置建筑所在的层级
    /// 使用场景：
    /// 1. 区分单位和建筑的点击响应
    /// 2. 建筑编辑模式下只响应建筑层
    /// 示例：InputManager.Instance.SetBuildingLayer(LayerMask.GetMask("Building"));
    /// </summary>
    public void SetBuildingLayer(LayerMask layer)
    {
        buildingLayer = layer;
    }
    #endregion


    /*
    ================================================================================================
    InputManager 使用指南
    ================================================================================================
    ------------------------------------------------------------------------------------------------
    
    1. 相机控制系统（CameraController）
       - 监听 OnMapDrag 移动相机
       - 监听 OnZoomChanged 调整相机缩放
       - 使用 SetMapBounds 限制相机移动范围
    
    2. 单位选择系统（UnitSelectionManager）
       - 监听 OnClickObject 选中单位
       - 监听 OnBoxSelectEnd 批量选中单位
       - 监听 OnDoubleClick 聚焦到单位
    
    3. 单位移动系统（UnitMovementController）
       - 监听 OnUnitDragEnd 移动单位到目标位置
       - 监听 OnClick 点击地面移动单位
    
    4. UI管理系统（UIManager）
       - 打开UI时调用 SetEnableMapDrag(false)
       - 关闭UI时调用 SetEnableMapDrag(true)
    
    5. 建筑系统（BuildingManager）
       - 监听 OnClickObject 选中建筑
       - 监听 OnLongPressObject 显示建筑详情
    
    6. 战斗系统（BattleManager）
       - 监听 OnUnitDragEnd 判断攻击目标
       - 战斗中禁用框选：SetEnableBoxSelect(false)
    
    
    性能优化建议
    ------------------------------------------------------------------------------------------------
    
    1. 事件监听
       - 始终在 OnDestroy 中取消事件订阅，防止内存泄漏
       - 不要在事件处理函数中执行耗时操作
    
    2. 层级设置
       - 合理设置 unitLayer 和 buildingLayer，避免不必要的射线检测
       - 使用 LayerMask.GetMask() 指定具体层级
    
    3. 边界检测
       - 使用 IsInMapBounds() 和 ClampToMapBounds() 限制移动范围
       - 避免相机超出地图边界
    
    4. 框选优化
       - 设置合理的 boxSelectMinSize，避免误触
       - 框选时只检测可见单位，提升性能
    
    5. UI忽略
       - ignoreUI 默认为 true，点击UI时不触发游戏内操作
       - 使用 EventSystem.IsPointerOverGameObject() 判断UI点击
    调试技巧
    ------------------------------------------------------------------------------------------------
    1. 在事件处理函数中添加 Debug.Log，查看事件触发情况
    2. 使用 OnDrawGizmos 可视化框选区域、地图边界
    3. 在 Inspector 中调整参数，实时测试手感
    4. 使用 Unity Remote 测试移动端触摸输入
    注意事项
    ------------------------------------------------------------------------------------------------
    1. 确保场景中有 EventSystem（UGUI 必需）
    2. 单位和建筑必须有 Collider 才能被射线检测到
    3. 相机必须设置正确的 Culling Mask
    4. 地图边界应该略小于实际地形，避免相机看到地图外
    5. 缩放范围应该根据游戏类型调整
    ================================================================================================
    */
}
