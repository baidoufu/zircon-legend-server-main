using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Envir;
using Server.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Web.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly PlayerService _playerService;
        private readonly AccountService _accountService;

        public DashboardModel(PlayerService playerService, AccountService accountService)
        {
            _playerService = playerService;
            _accountService = accountService;
        }

        // 统计数据
        public int OnlineCount { get; set; }
        public int TotalAccounts { get; set; }
        public int MapCount { get; set; }
        public string ServerStatus { get; set; } = "运行中";

        // 服务器信息
        public DateTime ServerTime { get; set; }
        public string UpTime { get; set; } = "";
        public int GamePort { get; set; }
        public int AdminPort { get; set; }
        public bool IsTestServer { get; set; }
        public bool AllowLogin { get; set; }

        // 倍率设置
        public int ExpRate { get; set; }
        public int DropRate { get; set; }
        public int GoldRate { get; set; }
        public int MaxLevel { get; set; }
        public int MaxRebirth { get; set; }

        // 在线玩家预览
        public List<PlayerViewModel> RecentPlayers { get; set; } = new();

        public void OnGet()
        {
            // 基础统计
            OnlineCount = _playerService.GetOnlineCount();
            TotalAccounts = _accountService.GetAccountCount();

            // 地图数量
            try
            {
                MapCount = SEnvir.Maps?.Count ?? 0;
            }
            catch
            {
                MapCount = 0;
            }

            // 服务器信息
            ServerTime = DateTime.Now;
            GamePort = Config.Port;
            AdminPort = Config.AdminPort;
            IsTestServer = Config.TestServer;
            AllowLogin = Config.AllowLogin;

            // 计算运行时长
            try
            {
                var startTime = SEnvir.StartTime;
                var uptime = DateTime.Now - startTime;
                if (uptime.TotalDays >= 1)
                    UpTime = $"{(int)uptime.TotalDays}天 {uptime.Hours}时 {uptime.Minutes}分";
                else if (uptime.TotalHours >= 1)
                    UpTime = $"{uptime.Hours}时 {uptime.Minutes}分 {uptime.Seconds}秒";
                else
                    UpTime = $"{uptime.Minutes}分 {uptime.Seconds}秒";
            }
            catch
            {
                UpTime = "未知";
            }

            // 倍率设置
            ExpRate = Config.ExperienceRate;
            DropRate = Config.DropRate;
            GoldRate = Config.GoldRate;
            MaxLevel = Config.MaxLevel;
            MaxRebirth = Config.最高转生次数;

            // 最近在线玩家
            RecentPlayers = _playerService.GetOnlinePlayers().Take(10).ToList();
        }
    }
}
