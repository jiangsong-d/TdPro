using System;
using UnityEngine;

/// <summary>
/// 时间格式化工具
/// </summary>
public static class TimeUtil
{
    private const int HOUR_OF_DAY = 24;
    private const int SECOND_OF_MINUTE = 60;
    private const int SECOND_OF_HOUR = 3600;
    private const int SECOND_OF_DAY = HOUR_OF_DAY * SECOND_OF_HOUR;
    private static bool _updateFunctionRunning = false;
    private static Timer _secondsTimer = null;

    /// <summary>
    /// 时间差(秒)
    /// </summary>
    private static long TimeSecOffset = 0;
    /// <summary>
    /// 时间差(毫秒)
    /// </summary>
    private static long TimeMsecOffset = 0;
    /// <summary>
    /// 服务器时区
    /// </summary>
    private const int TimeZoneServer = 8;
    /// <summary>
    /// 客户端时区
    /// </summary>
    private static int TimeZoneClient = TimeZoneInfo.Local.BaseUtcOffset.Hours;
    /// <summary>
    /// 服务器时区和客户端时区的差值
    /// </summary>
    private static int TimeZoneOff = TimeZoneServer - TimeZoneClient;
    /// <summary>
    /// 开服时间
    /// </summary>
    private static long OpenServerTime = 0;

    private static long LocalTimeSec()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static long LocalTimeMsec()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 更新服务器时间
    /// </summary>
    /// <param name="serverTime">服务器时间戳(毫秒)</param>
    /// <param name="serverTimeSec">服务器时间戳(秒)</param>
    public static void UpdateServerTimeNow(long serverTime, long serverTimeSec)
    {
        if (!_updateFunctionRunning)
        {
            _updateFunctionRunning = true;
            if (_secondsTimer != null)
            {
                _secondsTimer.Stop();
            }
            _secondsTimer = TimerManager.Instance.GetTimer("UpdateServerTimeNow",1, _UpdateSeconds, false);
            _secondsTimer.Start();
        }

        TimeMsecOffset = LocalTimeMsec() - serverTime;
        TimeSecOffset = LocalTimeSec() - serverTimeSec;
    }

    private static void _UpdateSeconds()
    {
       // EngineEventManager.Instance.DispatchEvent(new EngineEvent(EventID.SecondsUpdateMsg, null));
    }

    /// <summary>
    /// 切后台回来调时间刷新
    /// </summary>
    public static void OnApplicationFocusHandle()
    {
        _UpdateSeconds();
    }

    /// <summary>
    /// 设置开服时间
    /// </summary>
    /// <param name="time">开服时间戳</param>
    public static void SetOpenServerTime(long time)
    {
        OpenServerTime = time;
    }

    /// <summary>
    /// 获取开服时间
    /// </summary>
    /// <returns>开服时间戳</returns>
    public static long GetOpenServerTime()
    {
        return OpenServerTime;
    }

    /// <summary>
    /// 获取当前时间戳(秒)
    /// </summary>
    /// <returns>当前时间戳(秒)</returns>
    public static long GetSecTime()
    {
        return LocalTimeSec() - TimeSecOffset;
    }

    /// <summary>
    /// 获取当前时间戳(毫秒)
    /// </summary>
    /// <returns>当前时间戳(毫秒)</returns>
    public static long GetMSecTime()
    {
        return LocalTimeMsec() - TimeMsecOffset;
    }

    private static long _beginUnityTimeMsec = 0;
    private static long _beginLocalTimeMsec = 0;

    /// <summary>
    /// 获取真实毫秒时间
    /// </summary>
    /// <returns>真实毫秒时间</returns>
    public static long GetRealMSecTime()
    {
        if (_beginUnityTimeMsec > 0)
        {
            long offsetMsec = Mathf.FloorToInt((Time.time - _beginUnityTimeMsec) * 1000);
            return _beginLocalTimeMsec + offsetMsec;
        }
        else
        {
            _beginLocalTimeMsec = GetMSecTime();
            _beginUnityTimeMsec = Mathf.FloorToInt(Time.time);
            return _beginLocalTimeMsec;
        }
    }

    /// <summary>
    /// 通过秒时间戳返回描述
    /// </summary>
    /// <param name="format">时间格式:%H:%M:%S or *t</param>
    /// <param name="timeSec">时间戳(秒)</param>
    /// <returns>时间描述</returns>
    public static string GetDateBySec(string format, long timeSec)
    {
        timeSec = timeSec == 0 ? GetSecTime() : timeSec;
        return DateTimeOffset.FromUnixTimeSeconds(timeSec + TimeZoneOff * 3600).ToString(format);
    }

    /// <summary>
    /// 通过毫秒时间戳返回描述
    /// </summary>
    /// <param name="format">时间格式:%H:%M:%S or *t</param>
    /// <param name="timeMsec">时间戳(毫秒)</param>
    /// <returns>时间描述</returns>
    public static string GetDateByMsec(string format, long timeMsec)
    {
        timeMsec = timeMsec == 0 ? GetMSecTime() : timeMsec;
        return DateTimeOffset.FromUnixTimeMilliseconds(timeMsec + TimeZoneOff * 3600 * 1000).ToString(format);
    }

    /// <summary>
    /// 通过TimeTab返回秒时间戳
    /// </summary>
    /// <param name="timeTab">TimeTab</param>
    /// <returns>时间戳(秒)</returns>
    public static long GetSecTimeByTab(DateTime timeTab)
    {
        return new DateTimeOffset(timeTab).ToUnixTimeSeconds() - TimeZoneOff * 3600;
    }

    /// <summary>
    /// 通过TimeTab返回毫秒时间戳
    /// </summary>
    /// <param name="timeTab">TimeTab</param>
    /// <returns>时间戳(毫秒)</returns>
    public static long GetMsecTimeByTab(DateTime timeTab)
    {
        return GetSecTimeByTab(timeTab) * 1000;
    }

    /// <summary>
    /// 设置指定时间，返回时间戳
    /// </summary>
    /// <param name="year">年(4位数)</param>
    /// <param name="month">月(1-12)</param>
    /// <param name="day">日(1-31)</param>
    /// <param name="hour">时(0-23)</param>
    /// <param name="min">分(0-59)</param>
    /// <param name="sec">秒(0-59)</param>
    /// <returns>时间戳</returns>
    public static long SetTime(int year, int month, int day, int hour, int min, int sec)
    {
        DateTime dateTime = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 根据时间戳获取几年
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>几年</returns>
    public static int GetYear(long timeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timeStamp).Year;
    }

    /// <summary>
    /// 根据时间戳获取几月
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>几月</returns>
    public static int GetMonth(long timeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timeStamp).Month;
    }

    /// <summary>
    /// 根据时间戳获取几号
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>几号</returns>
    public static int GetMonthDay(long timeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timeStamp).Day;
    }

    /// <summary>
    /// 根据时间戳获取年月日字符串(日月前补0)
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>年月日</returns>
    public static (string, string, string) GetYMDStr(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return (dateTime.ToString("yyyy"), dateTime.ToString("MM"), dateTime.ToString("dd"));
    }

    /// <summary>
    /// 根据时间戳获取年月日(日月前不补0)
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>年月日</returns>
    public static (int, int, int) GetYMD(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return (dateTime.Year, dateTime.Month, dateTime.Day);
    }

    /// <summary>
    /// 根据时间戳获取年月日描述：XXXX年XXXX月XX日XX:XX:XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>年月日描述</returns>
    public static string GetYMDDesc(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:yyyy年MM月dd日HH:mm:ss秒}";
    }

    /// <summary>
    /// 根据时间戳获取年月日描述：XXXX年XXXX月XX日XX:XX:XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>年月日描述</returns>
    public static string GetYMDDesc2(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:yyyy MM/dd HH:mm}";
    }

    /// <summary>
    /// 根据时间戳获取年月日描述：XXXX/XX/XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>年月日描述</returns>
    public static string GetYMDDesc3(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:yyyy/MM/dd}";
    }

    /// <summary>
    /// 根据时间戳获取年月日描述：XXXX.XXXX.XX XX:XX:XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>年月日描述</returns>
    public static string GetYMDDesc4(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:yyyy.MM.dd HH:mm:ss}";
    }

    /// <summary>
    /// 根据时间戳获取月日描述：XXXX月XX日XX:XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>月日描述</returns>
    public static string GetMDDesc(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:MM月dd日 HH:mm}";
    }

    /// <summary>
    /// 根据时间戳获取月日描述：XXXX/XX XX:XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>月日描述</returns>
    public static string GetMDDesc2(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:MM/dd HH:mm}";
    }

    /// <summary>
    /// 根据时间戳获取月日描述： XX:XX:XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>月日描述</returns>
    public static string GetMDDesc3(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:HH:mm:ss}";
    }

    /// <summary>
    /// 根据时间戳获取月日描述：XXXX月XX日
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>月日描述</returns>
    public static string GetMDDesc4(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:MM月dd日}";
    }

    /// <summary>
    /// 根据时间戳获取时间描述：MM:HH
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>时间描述</returns>
    public static string GetMDDesc5(long timeStamp)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
        return $"{dateTime:HH:mm}";
    }

    /// <summary>
    /// 获取当前时间对应月份总共天数
    /// </summary>
    /// <param name="nowTime">当前时间</param>
    /// <returns>月份总天数</returns>
    public static int GetMonthForDay(long nowTime)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(nowTime);
        return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
    }

    /// <summary>
    /// 获取星期（1-7， 周日返回7）
    /// </summary>
    /// <param name="timeSec">时间戳(秒)</param>
    /// <returns>星期</returns>
    public static int GetWeek(long timeSec)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timeSec).DateTime;
        int week = (int)date.DayOfWeek;
        return week == 0 ? 7 : week;
    }

    /// <summary>
    /// 获取周一零点的时间戳
    /// </summary>
    /// <param name="nowTime">当前时间</param>
    /// <returns>周一零点的时间戳</returns>
    public static long GetWeekStart(long nowTime)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(nowTime).DateTime;
        int week = (int)date.DayOfWeek;
        week = week == 0 ? 7 : week;
        return GetDayBegin(nowTime) - ((week - 1) * SECOND_OF_DAY);
    }

    /// <summary>
    /// 获取周一零点的时间戳
    /// </summary>
    /// <param name="nowTime">当前时间</param>
    /// <returns>周一零点的时间戳</returns>
    public static long GetNextWeekStart(long nowTime)
    {
        return GetWeekStart(nowTime) + 7 * SECOND_OF_DAY;
    }

    /// <summary>
    /// 当前是一周的第几秒
    /// </summary>
    /// <returns>秒数</returns>
    public static long GetSecondsOfWeek()
    {
        return GetSecondsOfDay() + (GetWeek(GetSecTime()) - 1) * SECOND_OF_DAY;
    }

    /// <summary>
    /// 当前是一天的第几秒
    /// </summary>
    /// <param name="now">当前时间</param>
    /// <returns>秒数</returns>
    public static long GetSecondsOfDay(long now = 0)
    {
        now = now == 0 ? GetSecTime() : now;
        long zero = GetDayBegin(now);
        return now - zero;
    }

    /// <summary>
    /// 往前追朔到某一天的零点, 单位秒数
    /// </summary>
    /// <param name="time">时间戳 单位秒</param>
    /// <param name="useTimeZone">是否计算时区，默认不计算</param>
    /// <returns>零点时间戳</returns>
    public static long GetDayBegin(long time, bool useTimeZone = false)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds(time).DateTime;
        dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Utc);
        if (useTimeZone)
        {
            dateTime = dateTime.AddHours(GetTimeZone());
        }
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }
    
    /// <summary>
    /// 计算出服务器时间的时区
    /// </summary>
    /// <returns>时区</returns>
    public static int GetTimeZone()
    {
        long time = GetSecTime();
        DateTime utcTime = DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime;
        DateTime localTime = DateTimeOffset.FromUnixTimeSeconds(time).DateTime;

        int timeZone = (localTime.Hour - utcTime.Hour) * 3600 + (localTime.Minute - utcTime.Minute) * 60;
        timeZone = timeZone / 3600;
        return timeZone;
    }
    /// <summary>
    /// 获得一个时间 time 距离时间 ref_time 的相对时间 比如 1小时前 2天后 ...
    /// </summary>
    /// <param name="time">和参考时间比较的时间</param>
    /// <param name="ref_time">参考时间</param>
    /// <returns>相对时间描述</returns>
    public static string RelativeTime(long time, long ref_time)
    {
        int SECOND_OF_YEAR = SECOND_OF_DAY * 365;
        int SECOND_OF_MONTH = SECOND_OF_DAY * 30;
        long timeInSec = time - ref_time;
        string relative = "后";
        if (timeInSec < 0)
        {
            timeInSec = -timeInSec;
            relative = "前";
        }

        string result = "";

        if (timeInSec < SECOND_OF_YEAR)
        {
            if (timeInSec < SECOND_OF_MONTH)
            {
                if (timeInSec < SECOND_OF_DAY)
                {
                    if (timeInSec < SECOND_OF_HOUR)
                    {
                        if (timeInSec < SECOND_OF_MINUTE)
                        {
                            result = $"{timeInSec}秒";
                        }
                        else
                        {
                            result = $"{timeInSec / SECOND_OF_MINUTE}分钟";
                        }
                    }
                    else
                    {
                        result = $"{timeInSec / SECOND_OF_HOUR}小时";
                    }
                }
                else
                {
                    result = $"{timeInSec / SECOND_OF_DAY}天";
                }
            }
            else
            {
                result = $"{timeInSec / SECOND_OF_MONTH}个月";
            }
        }
        else
        {
            result = $"{timeInSec / SECOND_OF_YEAR}年";
        }

        return result + relative;
    }
     /// <summary>
    /// 天%s小时%s分%s秒
    /// </summary>
    /// <param name="nowTime">当前时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string GetDateTimeStr(long nowTime)
    {
        string str = "";
        if (nowTime > 86400)
        {
            str = string.Format("{0}天{1}小时{2}分{3}秒", 
                Math.Floor((double)nowTime / 86400), 
                Math.Floor(nowTime % 86400 / 3600.0), 
                Math.Floor(nowTime % 86400 % 3600 / 60.0), 
                Math.Floor(nowTime % 86400 % 3600 % 60.0));
        }
        else if (nowTime > 3600)
        {
            str = string.Format("{0}小时{1}分{2}秒", 
                Math.Floor(nowTime % 86400 / 3600.0), 
                Math.Floor(nowTime % 86400 % 3600 / 60.0), 
                Math.Floor(nowTime % 86400 % 3600 % 60.0));
        }
        else if (nowTime > 60)
        {
            str = string.Format("{0}分{1}秒", 
                Math.Floor(nowTime % 86400 % 3600 / 60.0), 
                Math.Floor(nowTime % 86400 % 3600 % 60.0));
        }
        else
        {
            str = string.Format("{0}秒", Math.Floor((double)nowTime % 86400));
        }
        return str;
    }

    /// <summary>
    /// 只显示最大单位时间，小于1分钟显示1分钟
    /// </summary>
    /// <param name="nowTime">当前时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string GetDateTimeStr1(long nowTime)
    {
        string str = "";
        if (nowTime > 86400)
        {
            str = string.Format("{0}天", Math.Floor((double)nowTime / 86400));
        }
        else if (nowTime > 3600)
        {
            str = string.Format("{0}小时", Math.Floor(nowTime % 86400 / 3600.0));
        }
        else if (nowTime > 60)
        {
            str = string.Format("{0}分钟", Math.Floor(nowTime % 86400 % 3600 / 60.0));
        }
        else
        {
            str = "1分钟";
        }
        return str;
    }

    /// <summary>
    /// 剩余时间
    /// </summary>
    /// <param name="_time">时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string FormatUnixTime(long _time)
    {
        if (_time >= 0)
        {
            long day = _time / 86400;
            long hour = _time / 3600 % 24;
            long minute = _time / 60 % 60;
            long second = _time % 60;
            string dayStr = day > 0 ? string.Format("{0}天", day) : "";
            string hourStr = hour > 0 ? string.Format("{0}小时", hour) : "";
            string minuteStr = minute > 0 ? string.Format("{0}分钟", minute) : "";
            string secondStr = second > 0 ? string.Format("{0}秒", second) : "";
            return dayStr + hourStr + minuteStr + secondStr;
        }
        return "";
    }

    /// <summary>
    /// 剩余时间
    /// </summary>
    /// <param name="_time">时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string FormatUnixTime2(long _time)
    {
        if (_time >= 0)
        {
            long day = _time / 86400;
            long hour = _time / 3600 % 24;
            long minute = _time / 60 % 60;
            long second = _time % 60;
            string dayStr = day > 0 ? string.Format("{0}天", day) : "";
            string hourStr = hour > 0 ? string.Format("{0}小时", hour) : "";
            string minuteStr = minute > 0 ? string.Format("{0}分钟", minute) : "";
            string secondStr = second > 0 ? string.Format("{0}秒", second) : "";
            if (day > 0)
            {
                return dayStr + hourStr;
            }
            else if (hour > 0)
            {
                return hourStr + minuteStr;
            }
            else if (minute > 0)
            {
                return minuteStr + secondStr;
            }
            else if (second > 0)
            {
                return secondStr;
            }
        }
        return "";
    }

    /// <summary>
    /// 剩余时间(超过1小时则按小时显示，超过24小时按天显示，超过365天按年显示)
    /// </summary>
    /// <param name="_time">时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string FormatUnixTime3(long _time)
    {
        if (_time >= 0)
        {
            long year = _time / (86400 * 365);
            long day = _time / 86400;
            long hour = _time / 3600 % 24;
            string yearStr = year > 0 ? string.Format("{0}年", year) : "";
            string dayStr = day > 0 ? string.Format("{0}天", day) : "";
            string hourStr = hour > 0 ? string.Format("{0}小时", hour) : "";
            if (year > 0)
            {
                return yearStr;
            }
            else if (day > 0)
            {
                return dayStr;
            }
            else if (hour > 0)
            {
                return hourStr;
            }
        }
        return "";
    }

    /// <summary>
    /// 剩余时间 xx天xx:xx:xx
    /// </summary>
    /// <param name="_time">时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string FormatUnixTime4(long _time)
    {
        if (_time >= 0)
        {
            long day = _time / 86400;
            _time = _time - day * 86400;
            string dayStr = day > 0 ? string.Format("{0}天", day) : "";
            return dayStr + TimeToString(_time);
        }
        return "";
    }

    /// <summary>
    /// 剩余时间(超过一年显示年，超过一月显示月，超过一天显示天，超过一小时显示小时，超过一分钟显示分钟，不足一分显示秒)
    /// </summary>
    /// <param name="_time">时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string FormatUnixTime5(long _time)
    {
        if (_time >= 0)
        {
            long year = _time / (86400 * 365);
            long day = _time / 86400;
            long hour = _time / 3600 % 24;
            long minute = _time / 60 % 60;
            long second = _time % 60;
            string yearStr = year > 0 ? string.Format("{0}年", year) : "";
            string dayStr = day > 0 ? string.Format("{0}天", day) : "";
            string hourStr = hour > 0 ? string.Format("{0}小时", hour) : "";

            if (year > 0)
            {
                return yearStr;
            }
            else if (day > 0)
            {
                return dayStr;
            }
            else if (hour > 0)
            {
                return hourStr;
            }
            else if (minute > 0)
            {
                return string.Format("{0:D2}:{1:D2}", minute, second);
            }
            else if (second > 0)
            {
                return string.Format("{0:D2}:{1:D2}", minute, second);
            }
        }
        return "";
    }

    /// <summary>
    /// 剩余时间
    /// 小于1小时 XX分:xx秒
    /// 小于1天 XX小时:XX分
    /// 大于一天 XX天:XX小时
    /// </summary>
    /// <param name="_time">时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string FormatUnixTime6(long _time)
    {
        long sec = _time % SECOND_OF_MINUTE;
        long min = _time / 60 % 60;
        long hour = _time / SECOND_OF_HOUR % 24;
        long day = _time / SECOND_OF_HOUR / 24;
        if (_time < 3600)
        {
            return string.Format("{0:D2}:{1:D2}", min, sec);
        }
        else if (_time < 86400)
        {
            return string.Format("{0:D2}:{1:D2}", hour, min);
        }
        else
        {
            return string.Format("{0:D2}:{1:D2}", day, hour);
        }
    }

    /// <summary>
    /// 剩余时间
    /// XX：XX；若是满足1小时，其中第一个XX为时钟，后一个XX为分钟；若是不满足1小时，则第一个XX为分钟，第二个XX为秒钟
    /// </summary>
    /// <param name="_time">时间</param>
    /// <returns>格式化时间字符串</returns>
    public static string FormatUnixTime6_1(long _time)
    {
        long sec = _time % SECOND_OF_MINUTE;
        long min = _time / 60 % 60;
        long hour = _time / SECOND_OF_HOUR % 24;
        long day = _time / SECOND_OF_HOUR / 24;
        if (_time < 3600)
        {
            return string.Format("{0:D2}:{1:D2}", min, sec);
        }
        else if (_time < 86400)
        {
            return string.Format("{0:D2}:{1:D2}", hour, min);
        }
        else
        {
            return string.Format("{0:D2}:{1:D2}", day, hour);
        }
    }

    /// <summary>
    /// 将秒数转换为：00:00:00
    /// </summary>
    /// <param name="time">时间戳秒数</param>
    /// <param name="flag">nil为-->时:分:秒; 1为-->时:分 2为-->分:秒</param>
    /// <param name="useTimeZone">是否使用时区 默认不使用</param>
    /// <returns>格式化时间字符串</returns>
    public static string TimeToString(long time, int flag = 0, bool useTimeZone = false)
    {
        long sec = time % SECOND_OF_MINUTE;
        long min = time / 60 % 60;
        long hour = time / SECOND_OF_HOUR;
        if (useTimeZone)
        {
            hour += GetTimeZone();
        }
        if (flag == 1)
        {
            return string.Format("{0:D2}:{1:D2}", hour, min);
        }
        else if (flag == 2)
        {
            return string.Format("{0:D2}:{1:D2}", min, sec);
        }
        else
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, min, sec);
        }
    }

    /// <summary>
    /// 将秒数转换为 MM:SS
    /// </summary>
    /// <param name="time">时间戳秒数</param>
    /// <returns>格式化时间字符串</returns>
    public static string TimeToMinString(long time)
    {
        long sec = time % SECOND_OF_MINUTE;
        long min = time / SECOND_OF_MINUTE;
        return string.Format("{0}:{1:D2}", min, sec);
    }

    /// <summary>
    /// 将秒数转换为 HH:MM:SS
    /// </summary>
    /// <param name="time">时间戳秒数</param>
    /// <returns>格式化时间字符串</returns>
    public static string TimeToHourString(long time)
    {
        long sec = time % SECOND_OF_MINUTE;
        long min = time / 60 % 60;
        long hour = time / SECOND_OF_HOUR;
        return string.Format("{0}:{1:D2}:{2:D2}", hour, min, sec);
    }

    /// <summary>
    /// 将时间戳的秒数转换为今日的：00:00:00
    /// </summary>
    /// <param name="time">时间戳秒数</param>
    /// <param name="flag">nil为 时:分:秒 2为 时:分</param>
    /// <param name="useTimeZone">是否使用时区 默认不使用</param>
    /// <returns>格式化时间字符串</returns>
    public static string TimeStampToString(long time, int flag = 0, bool useTimeZone = false)
    {
        time = time % SECOND_OF_DAY;
        long sec = time % SECOND_OF_MINUTE;
        long min = time / 60 % 60;
        long hour = time / SECOND_OF_HOUR;
        if (useTimeZone)
        {
            hour += GetTimeZone();
        }
        if (flag == 2)
        {
            return string.Format("{0:D2}:{1:D2}", hour, min);
        }
        else
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, min, sec);
        }
    }


    #region 另加方法        有问题随时删

    /// <summary>
    /// 接收一个时间戳根据当前时间返回一个描述  刚刚、分钟、小时...   (以当前服务器时间为基准)
    /// </summary>
    /// <param name="pastTimestampMesc"></param>
    /// <returns></returns>
    public static string FormatElapsedTime(long pastTimestampMesc)
    {
        long nowMesc = GetMSecTime();
        
        long elapsedSeconds = (nowMesc - pastTimestampMesc) / 1000;

        
        if (elapsedSeconds < 60) return LanguageManager.GetLangVal("381");
        TimeSpan ts = TimeSpan.FromSeconds(elapsedSeconds);
        if (ts.TotalDays >= 1) return string.Format(LanguageManager.GetLangVal("382"), (int)ts.TotalDays);
        if (ts.TotalHours >= 1) return string.Format(LanguageManager.GetLangVal("383"), (int)ts.TotalHours);
        if (ts.TotalMinutes >= 1) return string.Format(LanguageManager.GetLangVal("384"), (int)ts.TotalMinutes);

        return $"{ts.Seconds}秒前";
    }
    
    
    /// <summary>
    /// 根据时间戳获取年月日描述：XXXX-XX-XX
    /// </summary>
    /// <param name="timeStamp">时间戳</param>
    /// <returns>年月日描述</returns>
    public static string GetYMDDesc5(long timeStamp)
    {
        
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
        return $"{dateTime:yyyy-MM-dd}";
        
    }

    #endregion
}
        