using System;
using UnityEngine;

/// <summary>
/// 定时器类
/// 1、定时器需要暂停使用Pause、恢复使用Resume
/// 2、定时器使用Stop停止，一旦停止逻辑层脚本就应该将引用置空，因为它随后会被管理类回收，引用已经不再正确
/// </summary>
public class Timer
{
    private float _delay;
    private Action _callback;
    private bool _oneShot;
    private bool _useUnscaledTime;
    private float _startTime;
    private bool _isRunning;
    private bool _isPaused;
    private bool _isOver;
    private float _leftTime;

    public string TimerName { get; set; }

    public Timer(string timerName, float delay, Action callback, bool oneShot = true, bool useUnscaledTime = true)
    {
        Init(timerName , delay, callback, oneShot, useUnscaledTime);
    }

    /// <summary>
    /// 初始化定时器
    /// </summary>
    /// <param name="delay">时长，秒或者帧</param>
    /// <param name="callback">回调函数</param>
    /// <param name="oneShot">是否是一次性计时</param>
    /// <param name="useUnscaledTime">使用deltaTime计时，还是采用unscaledDeltaTime计时</param>
    public void Init(string timerName, float delay, Action callback, bool oneShot, bool useUnscaledTime)
    {
        TimerName = timerName;
        _delay = delay;
        _callback = callback;
        _oneShot = oneShot;
        _useUnscaledTime = useUnscaledTime;
        _startTime = GetTime();
        _isRunning = false;
        _isPaused = false;
        _isOver = false;
        _leftTime = delay;
    }

    /// <summary>
    /// 更新定时器
    /// </summary>
    public void Update()
    {
        if (!_isRunning || _isOver) return;

        float currentTime = GetTime();
        float elapsedTime = currentTime - _startTime;

        if (elapsedTime >= _delay)
        {
            _callback?.Invoke();
            if (_oneShot)
            {
                Stop();
            }
            else
            {
                _startTime = currentTime;
            }
        }
    }

    /// <summary>
    /// 启动定时器
    /// </summary>
    public void Start()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            _startTime = GetTime();
        }
    }

    /// <summary>
    /// 暂停定时器
    /// </summary>
    public void Pause()
    {
        if (_isRunning && !_isPaused)
        {
            _isPaused = true;
            _leftTime -= GetTime() - _startTime;
        }
    }

    /// <summary>
    /// 恢复定时器
    /// </summary>
    public void Resume()
    {
        if (_isPaused)
        {
            _isPaused = false;
            _startTime = GetTime() - (_delay - _leftTime);
        }
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _isOver = true;
    }

    /// <summary>
    /// 复位定时器
    /// </summary>
    public void Reset()
    {
        _leftTime = _delay;
        _startTime = GetTime();
        _isOver = false;
    }

    /// <summary>
    /// 检查定时器是否已结束
    /// </summary>
    /// <returns>是否已结束</returns>
    public bool IsOver()
    {
        return _isOver;
    }

    /// <summary>
    /// 获取当前时间
    /// </summary>
    /// <returns>当前时间</returns>
    private float GetTime()
    {
        return _useUnscaledTime ? Time.unscaledTime : Time.time;
    }
}