using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraManager : MonoSingleton<CameraManager>
{
    [SerializeField] Camera mainCam; // 主摄像机
    [SerializeField] float transitionSpeed = 0.1f; // 镜头移动速度
    [SerializeField] CinemachineVirtualCamera _activeVCam;

    // 相机震动相关参数
    [Header("相机震动设置")]
    [SerializeField] float defaultShakeIntensity = 2f;
    [SerializeField] float defaultShakeTime = 0.5f;

    private CinemachineBasicMultiChannelPerlin noise;
    private float shakeTimer;
    private float startingIntensity;

    UniversalAdditionalCameraData cameradata;

    public Camera Camera => mainCam;
    public CinemachineVirtualCamera ActiveVirtualCamera => _activeVCam;
    public Vector2 Position => mainCam.transform.position;

    public void Start()
    {
        // 如果Inspector中没有赋值，尝试自动查找
        if (mainCam == null)
        {
            mainCam = GetComponentInChildren<Camera>();
            if (mainCam == null)
            {
                Debug.LogError("[CameraManager] 未找到主摄像机！请在预制体Prefabs/UIModel/CameraManager中添加Camera子对象");
                return;
            }
            Debug.Log($"[CameraManager] 自动找到摄像机: {mainCam.name}");
        }

        if (_activeVCam == null)
        {
            _activeVCam = GetComponentInChildren<CinemachineVirtualCamera>();
            if (_activeVCam != null)
            {
                Debug.Log($"[CameraManager] 自动找到虚拟摄像机: {_activeVCam.name}");
            }
        }

        cameradata = mainCam.GetComponent<UniversalAdditionalCameraData>();
        if (cameradata != null)
        {
            cameradata.cameraStack.Add(UIModel.Inst.UICamera);
            UIModel.Inst.UICamera.depth = mainCam.depth + 1;
        }

        // 初始化相机震动组件
        InitializeCameraShake();
    }

    private void InitializeCameraShake()
    {
        if (_activeVCam == null) return;

        // 获取虚拟相机的噪声组件
        noise = _activeVCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        // 如果没有噪声组件，则添加一个
        if (noise == null)
        {
            noise = _activeVCam.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
        // 初始禁用震动
        noise.m_AmplitudeGain = 0f;
    }

    public void LateUpdate()
    {
        if (!_activeVCam) return;

        //主相机同步数据（带平滑过渡）
        mainCam.transform.position = Vector3.Lerp(
            mainCam.transform.position,
            _activeVCam.transform.position,
            Time.deltaTime * transitionSpeed
        );

        mainCam.transform.rotation = Quaternion.Slerp(
            mainCam.transform.rotation,
            _activeVCam.transform.rotation,
            Time.deltaTime * transitionSpeed
        );

        mainCam.fieldOfView = Mathf.Lerp(
            mainCam.fieldOfView,
            _activeVCam.m_Lens.FieldOfView,
            Time.deltaTime * transitionSpeed
        );

        // 处理相机震动
        HandleCameraShake();
    }

    /// <summary>
    /// 获取3D透视相机在指定高度平面的可视范围矩形
    /// </summary>
    /// <param name="scale">扩展倍数(用于AOI范围)</param>
    /// <param name="checkHeight">检测平面的Y坐标(默认0为地面)</param>
    /// <returns>在XZ平面上的矩形范围</returns>
    public Rect GetCameraAOIRect(float scale = 1.5f, float checkHeight = 0f)
    {
        if (mainCam == null) return new Rect();

        Vector3 cameraPos = mainCam.transform.position;
        
        // 计算相机到检测平面的距离
        float distance = Mathf.Abs(cameraPos.y - checkHeight);
        
        // 使用FOV和距离计算视锥体在该高度的尺寸
        float vFov = mainCam.fieldOfView * Mathf.Deg2Rad;
        float height = 2f * distance * Mathf.Tan(vFov / 2f) * scale;
        float width = height * mainCam.aspect;

        // 以相机XZ位置为中心
        float minX = cameraPos.x - width / 2f;
        float minZ = cameraPos.z - height / 2f;

        return new Rect(minX, minZ, width, height);
    }
    public bool IsInCameraAOI(Vector3 worldPos)
    {
        Rect aoi = GetCameraAOIRect(1.5f, 0f);
        Vector2 pos2D = new Vector2(worldPos.x, worldPos.z);
        return aoi.Contains(pos2D);
    }

    /// <summary>
    /// 地图的检测范围 
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    public bool IsInGroudAOIRect(Vector3 worldPos)
    {
        Rect aoi = GetCameraAOIRect(3f, 0f);
        Vector2 pos2D = new Vector2(worldPos.x, worldPos.z);
        return aoi.Contains(pos2D);
    }

    private void HandleCameraShake()
    {
        if (shakeTimer > 0)
        {
            // 应用震动效果
            shakeTimer -= Time.deltaTime;
            noise.m_AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (shakeTimer / defaultShakeTime));
        }
        else
        {
            // 确保震动完全停止
            shakeTimer = 0f;
            noise.m_AmplitudeGain = 0f;
        }
    }

    // 切换虚拟相机
    public void SwitchTo(CinemachineVirtualCamera newVCam)
    {
        if (_activeVCam == newVCam) return;

        // 关闭旧相机的渲染
        if (_activeVCam)
            _activeVCam.gameObject.SetActive(false);

        // 启用新相机
        newVCam.gameObject.SetActive(true);
        _activeVCam = newVCam;

        // 更新震动组件引用
        InitializeCameraShake();

        // 性能优化：自动调整渲染距离
        mainCam.farClipPlane = 100f; // Replace with an appropriate value or property
    }

    // 触发相机震动（默认参数）
    public void TriggerCameraShake()
    {
        TriggerCameraShake(defaultShakeIntensity, defaultShakeTime);
    }

    // 触发相机震动（自定义参数）
    public void TriggerCameraShake(float intensity, float time)
    {
        if (noise == null) InitializeCameraShake();

        startingIntensity = intensity;
        shakeTimer = time;
        noise.m_AmplitudeGain = intensity;
    }

    public void SetFollow(Transform follow)
    {
        _activeVCam.Follow = follow;
        if (follow != null)
            _activeVCam.OnTargetObjectWarped(_activeVCam.Follow, _activeVCam.transform.position - follow.position);
    }

    public void SetFollow2(Transform follow)
    {
        _activeVCam.Follow = follow;
    }
#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        
        // Transform camTransform = _activeVCam != null ? _activeVCam.transform : 
        //                           (mainCam != null ? mainCam.transform : null);
        
        // if (camTransform == null) return;

        // // 获取FOV（虚拟相机或主摄像机）
        // float fov = _activeVCam != null ? _activeVCam.m_Lens.FieldOfView : 
        //             (mainCam != null ? mainCam.fieldOfView : 60f);

        // // 绘制相机在Y=0平面的可见范围（黄色）
        // Gizmos.color = Color.yellow;
        // DrawCameraFrustumAtHeight(camTransform, fov, 0f, 1f);

        // // 绘制扩展的AOI范围（红色）
        // Gizmos.color = Color.red;
        // DrawCameraFrustumAtHeight(camTransform, fov, 0f, 1.5f);
    }

    private void DrawCameraFrustumAtHeight(Transform camTransform, float fieldOfView, float height, float scale)
    {
        Vector3 cameraPos = camTransform.position;
        float distance = Mathf.Abs(cameraPos.y - height);
        
        // 计算视锥体在该高度的尺寸
        float vFov = fieldOfView * Mathf.Deg2Rad;
        float frustumHeight = 2f * distance * Mathf.Tan(vFov / 2f) * scale;
        
        // 使用主摄像机的aspect（如果有），否则用默认值
        float aspect = mainCam != null ? mainCam.aspect : 16f / 9f;
        float frustumWidth = frustumHeight * aspect;

        // 计算四个角（在XZ平面上）
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;
        
        // 投影中心点到Y=height平面
        float t = (height - cameraPos.y) / forward.y;
        Vector3 centerAtHeight = cameraPos + forward * t;

        Vector3 topLeft = centerAtHeight - right * (frustumWidth / 2f) + new Vector3(0, 0, frustumHeight / 2f);
        Vector3 topRight = centerAtHeight + right * (frustumWidth / 2f) + new Vector3(0, 0, frustumHeight / 2f);
        Vector3 bottomRight = centerAtHeight + right * (frustumWidth / 2f) - new Vector3(0, 0, frustumHeight / 2f);
        Vector3 bottomLeft = centerAtHeight - right * (frustumWidth / 2f) - new Vector3(0, 0, frustumHeight / 2f);

        // 绘制矩形
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
#endif
}