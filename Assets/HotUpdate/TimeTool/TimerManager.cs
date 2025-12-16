using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定时器管理：负责定时器获取、回收、缓存、调度等管理
/// 注意：
/// 1、任何需要定时更新的函数从这里注册，游戏逻辑层最好使用不带"Co"的接口
/// 2、带有"Co"的接口都是用于协程，它的调度会比普通更新后一步---次序依从Unity函数调用次序：https://docs.unity3d.com/Manual/ExecutionOrder.html
/// 3、UI界面倒计时刷新等不需要每帧去更新的逻辑最好用定时器，少用Updatable，定时器能很好避免频繁的无用调用
/// 4、定时器并非精确定时，误差范围和帧率相关
/// 5、循环定时器不会累积误差，这点和Updater的Update函数自己去控制时间刷新是一致的，很好用
/// 6、定时器是弱引用表，使用临时对象时不会持有引用
/// 7、慎用临时函数、所有临时对象要外部自行维护引用以保障生命周期，否则会被GC掉===>很重要
/// </summary>
public class TimerManager : MonoSingleton<TimerManager>
{
    private Dictionary<string, Timer> _coUpdateTimers = new Dictionary<string, Timer>();
    private Dictionary<string, Timer> _coLateUpdateTimers = new Dictionary<string, Timer>();
    private Dictionary<string, Timer> _coFixedUpdateTimers = new  Dictionary<string, Timer>();
    private Dictionary<string, Timer> _updateTimers = new Dictionary<string, Timer>();
    private Dictionary<string, Timer> _lateUpdateTimers = new Dictionary<string, Timer>();
    private Dictionary<string, Timer> _fixedUpdateTimers = new Dictionary<string, Timer>();

    private void Update()
    {
        UpdateTimers(_updateTimers);
    }

    private void LateUpdate()
    {
        UpdateTimers(_lateUpdateTimers);
    }

    private void FixedUpdate()
    {
        UpdateTimers(_fixedUpdateTimers);
    }

    /// <summary>
    /// 更新定时器列表
    /// </summary>
    /// <param name="timers">定时器列表</param>
    private void UpdateTimers(Dictionary<string, Timer> timers)
    {
        foreach (var timer in timers.Values)
        {
            if (timer.IsOver())
            {
                timer.Stop();
            }
            else
            {
                timer.Update();
            }
        }
    }

    /// <summary>
    /// 获取Update定时器
    /// </summary>
    /// <param name="delay">时长，秒或者帧</param>
    /// <param name="callback">回调函数</param>
    /// <param name="oneShot">是否是一次性计时</param>
    /// <param name="useUnscaledTime">使用deltaTime计时，还是采用unscaledDeltaTime计时, 默认为true（不受时间影响），使用unscale</param>
    /// <returns>Timer</returns>
    public Timer GetTimer(string timerName, float delay, Action callback, bool oneShot = true, bool useUnscaledTime = true)
    {
        if (_updateTimers.ContainsKey(timerName))
        {
            if (_updateTimers[timerName].IsOver())
            {
                _updateTimers.Remove(timerName);
            }
            else
            {
                LogUtlis.Error(this, "TimerManager GetTimer Error: TimerName is already exist!");
                return null;
            }
        }
        Timer timer = new Timer(timerName,delay, callback, oneShot, useUnscaledTime);
        timer.Start();
        _updateTimers.Add(timerName,timer);
        return timer;
    }

    /// <summary>
    /// 获取LateUpdate定时器
    /// </summary>
    /// <param name="delay">时长，秒或者帧</param>
    /// <param name="callback">回调函数</param>
    /// <param name="oneShot">是否是一次性计时</param>
    /// <param name="useUnscaledTime">使用deltaTime计时，还是采用unscaledDeltaTime计时, 默认为true（不受时间影响），使用unscale</param>
    /// <returns>Timer</returns>
    public Timer GetLateTimer(string timerName,float delay, Action callback, bool oneShot = true, bool useUnscaledTime = true)
    {
        if (_lateUpdateTimers.ContainsKey(timerName))
        {
            if (_lateUpdateTimers[timerName].IsOver())
            {
                _lateUpdateTimers.Remove(timerName);
            }
            else
            {
                LogUtlis.Error(this, "TimerManager GetLateTimer Error: TimerName is already exist!");
                return null;
            }
        }
        Timer timer = new Timer(timerName,delay, callback, oneShot, useUnscaledTime);
        timer.Start();
        _lateUpdateTimers.Add(timerName, timer);
        return timer;
    }

    /// <summary>
    /// 获取FixedUpdate定时器
    /// </summary>
    /// <param name="delay">时长，秒或者帧</param>
    /// <param name="callback">回调函数</param>
    /// <param name="oneShot">是否是一次性计时</param>
    /// <param name="useUnscaledTime">使用deltaTime计时，还是采用unscaledDeltaTime计时, 默认为true（不受时间影响），使用unscale</param>
    /// <returns>Timer</returns>
    public Timer GetFixedTimer(string timerName,float delay, Action callback, bool oneShot = true, bool useUnscaledTime = true)
    {
        if (_fixedUpdateTimers.ContainsKey(timerName))
        {
            if (_fixedUpdateTimers[timerName].IsOver())
            {
                _fixedUpdateTimers.Remove(timerName);
            }
            else
            {
                LogUtlis.Error(this, "TimerManager GetTimer Error: TimerName is already exist!");
                return null;
            }
        }
        Timer timer = new Timer(timerName,delay, callback, oneShot, useUnscaledTime);
        timer.Start();
        _fixedUpdateTimers.Add(timerName,timer);
        return timer;
    }

    /// <summary>
    /// 获取CoUpdate定时器
    /// </summary>
    /// <param name="delay">时长，秒或者帧</param>
    /// <param name="callback">回调函数</param>
    /// <param name="oneShot">是否是一次性计时</param>
    /// <param name="useUnscaledTime">使用deltaTime计时，还是采用unscaledDeltaTime计时, 默认为true（不受时间影响），使用unscale</param>
    /// <returns>Timer</returns>
    public Timer GetCoTimer(string timerName,float delay, Action callback, bool oneShot = true, bool useUnscaledTime = true)
    {
        Timer timer = new Timer(timerName,delay, callback, oneShot, useUnscaledTime);
        timer.Start();
        _coUpdateTimers.Add(timerName,timer);
        StartCoroutine(CoUpdateTimer(timer));
        return timer;
    }

    /// <summary>
    /// 获取CoLateUpdate定时器
    /// </summary>
    /// <param name="delay">时长，秒或者帧</param>
    /// <param name="callback">回调函数</param>
    /// <param name="oneShot">是否是一次性计时</param>
    /// <param name="useUnscaledTime">使用deltaTime计时，还是采用unscaledDeltaTime计时, 默认为true（不受时间影响），使用unscale</param>
    /// <returns>Timer</returns>
    public Timer GetCoLateTimer(string timerName,float delay, Action callback, bool oneShot = true, bool useUnscaledTime = true)
    {
        Timer timer = new Timer(timerName,delay, callback, oneShot, useUnscaledTime);
        timer.Start();
        _coLateUpdateTimers.Add(timerName,timer);
        StartCoroutine(CoLateUpdateTimer(timer));
        return timer;
    }

    /// <summary>
    /// 获取CoFixedUpdate定时器
    /// </summary>
    /// <param name="delay">时长，秒或者帧</param>
    /// <param name="callback">回调函数</param>
    /// <param name="oneShot">是否是一次性计时</param>
    /// <param name="useUnscaledTime">使用deltaTime计时，还是采用unscaledDeltaTime计时, 默认为true（不受时间影响），使用unscale</param>
    /// <returns>Timer</returns>
    public Timer GetCoFixedTimer(string timerName,float delay, Action callback, bool oneShot = true, bool useUnscaledTime = true)
    {
        Timer timer = new Timer(timerName,delay, callback, oneShot, useUnscaledTime);
        timer.Start();
        _coFixedUpdateTimers.Add(timerName,timer);
        StartCoroutine(CoFixedUpdateTimer(timer));
        return timer;
    }

    /// <summary>
    /// 协程更新定时器
    /// </summary>
    /// <param name="timer">定时器</param>
    /// <returns>IEnumerator</returns>
    private IEnumerator CoUpdateTimer(Timer timer)
    {
        while (!timer.IsOver())
        {
            timer.Update();
            yield return null;
        }
        timer.Stop();
        _coUpdateTimers.Remove(timer.TimerName);
    }

    /// <summary>
    /// 协程LateUpdate定时器
    /// </summary>
    /// <param name="timer">定时器</param>
    /// <returns>IEnumerator</returns>
    private IEnumerator CoLateUpdateTimer(Timer timer)
    {
        while (!timer.IsOver())
        {
            timer.Update();
            yield return new WaitForEndOfFrame();
        }
        timer.Stop();
        _coLateUpdateTimers.Remove(timer.TimerName);
    }

    /// <summary>
    /// 协程FixedUpdate定时器
    /// </summary>
    /// <param name="timer">定时器</param>
    /// <returns>IEnumerator</returns>
    private IEnumerator CoFixedUpdateTimer(Timer timer)
    {
        while (!timer.IsOver())
        {
            timer.Update();
            yield return new WaitForFixedUpdate();
        }
        timer.Stop();
        _coFixedUpdateTimers.Remove(timer.TimerName);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        DelayRecycle(_updateTimers);
        DelayRecycle(_lateUpdateTimers);
        DelayRecycle(_fixedUpdateTimers);
        DelayRecycle(_coUpdateTimers);
        DelayRecycle(_coLateUpdateTimers);
        DelayRecycle(_coFixedUpdateTimers);
    }

    public  void DelayRecycle(Dictionary<string, Timer> timers)
    {
        foreach (var timer in timers.Values)
        {
            timer.Stop();
        }
        timers.Clear();
    }
    private void OnDestroy()
    {
        Dispose();
    }
}