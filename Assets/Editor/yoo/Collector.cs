using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YooAsset.Editor;

public class CollectSpine : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }

    public bool IsCollectAsset(FilterRuleData data)
    {
        return Path.GetExtension(data.AssetPath) == ".asset";
    }
}
public class CollectAsset : IFilterRule
{
        public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }


    public bool IsCollectAsset(FilterRuleData data)
    {
        return Path.GetExtension(data.AssetPath) == ".asset";
    }
}
public class CollectXml : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }
    public bool IsCollectAsset(FilterRuleData data)
    {
        return Path.GetExtension(data.AssetPath) == ".xml";
    }
}

public class CollectMapFloor : IFilterRule
{
    public string FindAssetType
    {
        get { return EAssetSearchType.All.ToString(); }
    }
    public bool IsCollectAsset(FilterRuleData data)
    {
        return Path.GetFileNameWithoutExtension(data.AssetPath).Contains("dimian");
    }
}

