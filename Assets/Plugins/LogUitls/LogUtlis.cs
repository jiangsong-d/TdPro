using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 自定义Debuger类的扩展类
/// 通过 EnableLog 开关控制日志打印
/// </summary>
public static class LogUtlis
{
    /// <summary>
    /// 日志开关
    /// 编辑器模式下默认开启，非编辑器模式下默认关闭
    /// 可以运行时动态修改
    /// </summary>
    public static bool EnableLog { get; set; }

    /// <summary>
    /// 静态构造函数 - 初始化日志开关
    /// </summary>
    static LogUtlis()
    {
        #if UNITY_EDITOR
            EnableLog = true;
        #else
            EnableLog = false;
        #endif
    }

    public static void Info(string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Info)
        {
            Debuger.Info("", message);
        }
    }

    public static void NetSendLog(string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Info)
        {
            Debuger.InfoNetSend("Net", message);
        }
    }

    public static void NetReceiveLog(string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Info)
        {
            Debuger.InfoNetReceive("Net", message);
        }
    }

    public static void Info(this object obj, string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Info)
        {
            Debuger.Info(GetLogTag(obj), message);
        }
    }

    public static void Info(this object obj, string format, params object[] args)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Info)
        {
            string message = string.Format(format, args);
            Debuger.Info(GetLogTag(obj), message);
        }
    }

    public static void Warn(string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Warn)
        {
            Debuger.Warn("", message);
        }
    }

    public static void Warn(this object obj, string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Warn)
        {
            Debuger.Warn(GetLogTag(obj), message);
        }
    }

    public static void Warn(this object obj, string format, params object[] args)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Warn)
        {
            string message = string.Format(format, args);
            Debuger.Warn(GetLogTag(obj), message);
        }
    }

    public static void Error(string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Error)
        {
            Debuger.Error("", message);
        }
    }

    public static void Error(this object obj, string message)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Error)
        {
            Debuger.Error(GetLogTag(obj), message);
        }
    }

    public static void Error(this object obj, string format, params object[] args)
    {
        if (!EnableLog) return;
        if (Debuger.LogLevel >= LogLevel.Error)
        {
            string message = string.Format(format, args);
            Debuger.Error(GetLogTag(obj), message);
        }
    }

    /// <summary>
    /// 获取调用打印的类名称或者标记有TAGNAME的字段
    /// 有TAGNAME字段的，触发类名称用TAGNAME字段对应的赋值
    /// 没有用类的名称代替
    /// </summary>
    /// <param name="obj">触发Log对应的类</param>
    /// <returns></returns>
    private static string GetLogTag(object obj)
    {
        FieldInfo field = obj.GetType().GetField("TAGNAME");
        bool flag = field != null;
        string result;
        if (flag)
        {
            result = (string)field.GetValue(obj);
        }
        else
        {
            result = obj.GetType().Name;
        }
        return result;
    }
}
