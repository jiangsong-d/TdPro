using System.Collections.Generic;

namespace Launch
{
    public static class ConfirmConfig
    {
        private static Dictionary<int, ConfirmConfigInfo> ConfirmConfigs = new Dictionary<int, ConfirmConfigInfo>()
        {
            {1, new ConfirmConfigInfo()
                {
                    Title = "没有网络",
                    Content = "当前所处网络环境异常，请确认网络可用。",
                    ShowTip = true,
                    ShowRect = 1
                }
            },
            {2, new ConfirmConfigInfo()
                {
                    Title = "登录状态",
                    Content = "检测网络环境中",
                    ShowTip = false,
                    ShowRect = 2
                }
            },
            {3, new ConfirmConfigInfo()
                {
                    Title = "登录成功",
                    Content = "点击任意区域进入。",
                    ShowTip = false,
                    ShowRect = 2
                }
            },
            {4, new ConfirmConfigInfo()
                {
                    Title = "登录中",
                    Content = "正在登录中...",
                    ShowTip = false,
                    ShowRect = 2
                }
            },
            {5, new ConfirmConfigInfo()
                {
                    Title = "没有网络",
                    Content = "当前所处网络环境异常，登录异常",
                    ShowTip = true,
                    ShowRect = 2
                }
            },
            {6, new ConfirmConfigInfo()
                {
                    Title = "下载失败",
                    Content = "当前网络环境异常导致下载失败，请尝试切换其他wifi或者移动网络进行下载",
                    ShowTip = true,
                    ShowRect = 2
                }
            },
            {7, new ConfirmConfigInfo()
                {
                    Title = "更新失败",
                    Content = "更新失败，请确保网络环境正常后重试。（若多次失败可以尝试‘修复模式’）",
                    ShowTip = true,
                    ShowRect = 2
                }
            },
        };
        
        public static ConfirmConfigInfo GetConfig(int id)
        {
            if (ConfirmConfigs.ContainsKey(id))
            {
                return ConfirmConfigs[id];
            }
            return default;
        }
    }

    public struct ConfirmConfigInfo
    {
        /// <summary>
        /// 显示标题
        /// </summary>
        public string Title;
        /// <summary>
        /// 显示内容
        /// </summary>
        public string Content;
        /// <summary>
        /// 是否显示提示
        /// </summary>
        public bool ShowTip;
        /// <summary>
        /// 显示区域
        /// </summary>
        public int ShowRect;
    }
}