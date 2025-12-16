using System;
using UnityEngine;
using TowerDefense.Network;

namespace TowerDefense.Example
{
    /// <summary>
    /// 网络登录示例 - 账号服 + 区服 两阶段登录
    /// </summary>
    public class NetworkLoginExample : MonoBehaviour
    {
        [Header("账号信息")]
        public string username = "testuser";
        public string password = "123456";
        
        [Header("UI引用")]
        public GameObject loginPanel;
        public GameObject serverListPanel;
        public GameObject gamePanel;
        
        private void Start()
        {
            // 显示登录面板
            ShowPanel(loginPanel);
        }
        
        #region 第一阶段：登录账号服
        
        /// <summary>
        /// 点击登录按钮
        /// </summary>
        public void OnLoginButtonClick()
        {
            LogUtlis.Info("开始登录账号服...");
            
            AccountService.Instance.Login(username, password, OnLoginComplete);
        }
        
        /// <summary>
        /// 账号登录完成回调
        /// </summary>
        private void OnLoginComplete(bool success, string message, LoginData data)
        {
            if (success)
            {
                LogUtlis.Info($"账号登录成功！Token: {data.token}");
                
                // 登录成功后，获取区服列表
                GetServerList();
            }
            else
            {
                LogUtlis.Error($"账号登录失败: {message}");
                // TODO: 显示错误提示
            }
        }
        
        #endregion
        
        #region 第二阶段：获取区服列表
        
        /// <summary>
        /// 获取区服列表
        /// </summary>
        private void GetServerList()
        {
            LogUtlis.Info("获取区服列表...");
            
            AccountService.Instance.GetServerList(OnGetServerListComplete);
        }
        
        /// <summary>
        /// 获取区服列表完成回调
        /// </summary>
        private void OnGetServerListComplete(bool success, string message, System.Collections.Generic.List<GameServerInfo> servers)
        {
            if (success && servers != null)
            {
                LogUtlis.Info($"获取到 {servers.Count} 个区服");
                
                // 显示区服列表面板
                ShowPanel(serverListPanel);
                
                // TODO: 在UI上显示区服列表
                // 示例：自动选择第一个可用区服
                foreach (var server in servers)
                {
                    LogUtlis.Info($"区服 {server.server_id}: {server.server_name} - {server.GetStatusText()} ({server.online_num}/{server.max_player})");
                    
                    if (server.CanConnect())
                    {
                        // 自动连接第一个可用区服（实际应该让玩家选择）
                        OnSelectServer(server);
                        break;
                    }
                }
            }
            else
            {
                LogUtlis.Error($"获取区服列表失败: {message}");
            }
        }
        
        #endregion
        
        #region 第三阶段：连接游戏区服
        
        /// <summary>
        /// 选择区服
        /// </summary>
        public void OnSelectServer(GameServerInfo server)
        {
            if (!server.CanConnect())
            {
                LogUtlis.Warn($"区服 {server.server_name} 当前无法连接");
                return;
            }
            
            LogUtlis.Info($"连接到区服: {server.server_name}");
            
            // 使用token连接到游戏区服
            string token = AccountService.Instance.Token;
            TDNetworkManager.Instance.ConnectToGameServer(server.host, server.port, token);
            
            // 监听游戏服连接事件
            EngineEventManager.Instance.AddListener(EventID.NETWORK_CONNECTED, OnGameServerConnected);
            EngineEventManager.Instance.AddListener(EventID.NETWORK_LOGIN_SUCCESS, OnGameLoginSuccess);
        }
        
        /// <summary>
        /// 游戏服连接成功
        /// </summary>
        private void OnGameServerConnected(params object[] args)
        {
            LogUtlis.Info("WebSocket连接成功，发送登录请求...");
            
            // 发送游戏登录请求（带token）
            var request = new LoginRequest
            {
                Token = AccountService.Instance.Token,
                PlayerId = AccountService.Instance.PlayerId,
                PlayerName = AccountService.Instance.Username
            };
            
            TDNetworkManager.Instance.SendLoginRequest(request);
        }
        
        /// <summary>
        /// 游戏登录成功
        /// </summary>
        private void OnGameLoginSuccess(params object[] args)
        {
            LogUtlis.Info("游戏登录成功，进入游戏！");
            
            // 显示游戏面板
            ShowPanel(gamePanel);
            
            // TODO: 加载游戏场景或初始化游戏逻辑
        }
        
        #endregion
        
        #region UI辅助
        
        private void ShowPanel(GameObject panel)
        {
            if (loginPanel) loginPanel.SetActive(false);
            if (serverListPanel) serverListPanel.SetActive(false);
            if (gamePanel) gamePanel.SetActive(false);
            
            if (panel) panel.SetActive(true);
        }
        
        #endregion
        
        #region 测试按钮
        
        [ContextMenu("测试完整登录流程")]
        public void TestFullLoginFlow()
        {
            OnLoginButtonClick();
        }
        
        [ContextMenu("测试获取区服列表")]
        public void TestGetServerList()
        {
            if (AccountService.Instance.IsLoggedIn)
            {
                GetServerList();
            }
            else
            {
                LogUtlis.Error("请先登录账号服");
            }
        }
        
        #endregion
    }
}
