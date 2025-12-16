using UnityEngine;

/// <summary>
/// 本地数据持久化读写
/// 使用 Unity 的 `PlayerPrefs` 类可以方便地实现数据的本地存储和读取。
/// SaveString(string key, string value)`：保存字符串数据到 `PlayerPrefs`。
/// LoadString(string key)`：从 `PlayerPrefs` 读取字符串数据。
/// SaveInt(string key, int value)`：保存整数数据到 `PlayerPrefs`。
/// LoadInt(string key)`：从 `PlayerPrefs` 读取整数数据。
/// SaveFloat(string key, float value)`：保存浮点数数据到 `PlayerPrefs`。
/// LoadFloat(string key)`：从 `PlayerPrefs` 读取浮点数数据。
/// DeleteData(string key)`：删除指定键的数据。
/// ClearAllData()`：清除所有数据。
/// </summary>
public class PlayerPrefsStorage:MonoSingleton<PlayerPrefsStorage>
{
    /// <summary>
    /// 保存字符串数据到 PlayerPrefs
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void SaveString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
        LogUtlis.Info($"字符串数据已保存: {key} = {value}");
    }

    /// <summary>
    /// 读取字符串数据从 PlayerPrefs
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>值</returns>
    public string LoadString(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            string value = PlayerPrefs.GetString(key);
            LogUtlis.Info($"字符串数据已加载: {key} = {value}");
            return value;
        }
        else
        {
            LogUtlis.Info($"键 {key} 不存在");
            return null;
        }
    }

    /// <summary>
    /// 保存整数数据到 PlayerPrefs
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        LogUtlis.Info($"整数数据已保存: {key} = {value}");
    }

    /// <summary>
    /// 读取整数数据从 PlayerPrefs
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>值</returns>
    public int LoadInt(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            int value = PlayerPrefs.GetInt(key);
            LogUtlis.Info($"整数数据已加载: {key} = {value}");
            return value;
        }
        else
        {
            LogUtlis.Info($"键 {key} 不存在");
            return 0;
        }
    }

    /// <summary>
    /// 保存浮点数数据到 PlayerPrefs
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        LogUtlis.Info($"浮点数数据已保存: {key} = {value}");
    }

    /// <summary>
    /// 读取浮点数数据从 PlayerPrefs
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>值</returns>
    public float LoadFloat(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            float value = PlayerPrefs.GetFloat(key);
            LogUtlis.Info($"浮点数数据已加载: {key} = {value}");
            return value;
        }
        else
        {
            LogUtlis.Info($"键 {key} 不存在");
            return 0f;
        }
    }

    /// <summary>
    /// 删除指定键的数据
    /// </summary>
    /// <param name="key">键</param>
    public void DeleteData(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            LogUtlis.Info($"数据已删除: {key}");
        }
        else
        {
            LogUtlis.Info($"键 {key} 不存在");
        }
    }

    /// <summary>
    /// 清除所有数据
    /// </summary>
    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        LogUtlis.Info("所有数据已清除");
    }
}