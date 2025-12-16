using cfg;
using Luban;
using System.Collections.Generic;
using UnityEngine;

public class ConfigManager : GameSingleton<ConfigManager>
{
    public Tables Cfgs;

    private static string DirPath = "Config/";
    readonly Dictionary<string, IVOFun> tables = new Dictionary<string, IVOFun>();

    private TextAsset ReadData(string path)
    {
        TextAsset asset = LoadManager.Instance.SyncLoadAsset<TextAsset>(path);
        return asset;
    }
    public T GetVOData<T>(string fileName) where T : IVOFun, new()
    {
        var path = DirPath + fileName + ".bytes";
        if (tables.ContainsKey(fileName))
        {
            return (T)tables[fileName];
        }
        else
        {
            var data = new T();
            TextAsset text = ReadData(path);
            data._LoadData(new ByteBuf(text.bytes));
            tables.Add(fileName, data);
            return data;
        }
    }
    public void RemoveConfigData<T>(string fileName)
    {
        if (tables.ContainsKey(fileName))
        {
            tables.Remove(fileName);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Cfgs = null;
    }

}