using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf;
using TowerDefense.Proto;
using UnityEngine;

public class LoginNet : GameSingleton<LoginNet>
{
    public override void Init()
    {
        base.Init();
        NetworkManager.Instance.AddNetEvent(ServerType.Game, Cmd.MsgLoginRsp, OnLoginResponse, typeof(LoginResponse));
    }

    /// <summary>
    /// 发送登录请求到游戏服（使用账号服返回的 token）
    /// </summary>
    public void SendLogin()
    {
        var token = AccountServiceManager.Instance.CurrentToken;
        if (string.IsNullOrEmpty(token))
        {
            LogUtlis.Error("[LoginNet] 无法登录游戏服：没有有效的 token");
            return;
        }

        LoginRequest loginRequest = new LoginRequest
        {
            Token = token,
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            Platform = Application.platform.ToString()
        };
        
        LogUtlis.Info("[LoginNet] 发送登录请求到游戏服");
        NetworkManager.Instance.SendMsg(ServerType.Game, Cmd.MsgLoginReq, loginRequest);
    }

    /// <summary>
    /// 游戏服登录响应
    /// </summary>
    private void OnLoginResponse(int code, IMessage message)
    {
        if (code != 0)
        {
            LogUtlis.Error($"[LoginNet] 游戏服登录失败，错误码: {code}");
            return;
        }

        var response = message as LoginResponse;
        if (response == null || !response.Success)
        {
            LogUtlis.Error($"[LoginNet] 游戏服登录失败: {response?.Message ?? "未知错误"}");
            return;
        }

        LogUtlis.Info($"[LoginNet] 游戏服登录成功! PlayerId={response.PlayerId}, PlayerName={response.PlayerName}");
        
        // TODO: 保存玩家信息，进入游戏主界面
        // PlayerManager.Instance.SetPlayerInfo(response.PlayerInfo);
    }
}
