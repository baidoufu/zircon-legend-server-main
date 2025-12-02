using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Envir;
using Library;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Web.Pages
{
    [Authorize]
    public class SystemModel : PageModel
    {
        public string? Message { get; set; }

        // Server Status
        public bool ServerStarted => SEnvir.Started;
        public int OnlineCount => SEnvir.Players?.Count ?? 0;
        public int ConnectionCount => SEnvir.Connections?.Count ?? 0;
        public int MapCount => SEnvir.Maps?.Count ?? 0;
        public int AccountCount => SEnvir.AccountInfoList?.Count ?? 0;
        public int CharacterCount => SEnvir.CharacterInfoList?.Count ?? 0;
        public int ItemInfoCount => SEnvir.ItemInfoList?.Count ?? 0;
        public int MonsterInfoCount => SEnvir.MonsterInfoList?.Count ?? 0;

        // Config Values (editable)
        public string ServerIP => Config.IPAddress;
        public int ServerPort => Config.Port;
        public int AdminPort => Config.AdminPort;
        public bool AdminEnabled => Config.AdminEnabled;
        public int MaxLevel => Config.MaxLevel;
        public int ExperienceRate => Config.ExperienceRate;
        public int DropRate => Config.DropRate;
        public int GoldRate => Config.GoldRate;
        public int MaxRebirth => Config.最高转生次数;
        public bool AllowNewAccount => Config.AllowNewAccount;
        public bool AllowLogin => Config.AllowLogin;
        public bool AllowStartGame => Config.AllowStartGame;
        public bool AllowNewCharacter => Config.AllowNewCharacter;
        public bool TestServer => Config.TestServer;

        // Memory Info
        public string MemoryUsage
        {
            get
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var mb = process.WorkingSet64 / 1024.0 / 1024.0;
                return $"{mb:F1} MB";
            }
        }

        public string Uptime
        {
            get
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var uptime = DateTime.Now - process.StartTime;
                if (uptime.TotalDays >= 1)
                    return $"{uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分钟";
                if (uptime.TotalHours >= 1)
                    return $"{uptime.Hours}小时 {uptime.Minutes}分钟";
                return $"{uptime.Minutes}分钟 {uptime.Seconds}秒";
            }
        }

        public void OnGet()
        {
        }

        public IActionResult OnPostSaveData()
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                return Page();
            }

            try
            {
                SEnvir.SaveUserDatas();
                Message = "用户数据已保存";
            }
            catch (Exception ex)
            {
                Message = $"保存失败: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnPostSaveSystem()
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                return Page();
            }

            try
            {
                SEnvir.SaveSystem();
                Message = "系统数据已保存";
            }
            catch (Exception ex)
            {
                Message = $"保存失败: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnPostGC()
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                return Page();
            }

            try
            {
                var before = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var after = GC.GetTotalMemory(true) / 1024.0 / 1024.0;
                Message = $"垃圾回收完成，内存 {before:F1}MB -> {after:F1}MB，释放 {before - after:F1}MB";
            }
            catch (Exception ex)
            {
                Message = $"GC失败: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnPostUpdateConfig(int port, int maxLevel, int expRate, int dropRate, int goldRate, int maxRebirth)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                return Page();
            }

            try
            {
                // Validate values
                if (port < 1 || port > 65535)
                {
                    Message = "端口号必须在 1-65535 之间";
                    return Page();
                }

                if (maxLevel < 1 || maxLevel > 255)
                {
                    Message = "最高等级必须在 1-255 之间";
                    return Page();
                }

                // Update config values
                Config.Port = (ushort)port;
                Config.MaxLevel = maxLevel;
                Config.ExperienceRate = expRate;
                Config.DropRate = dropRate;
                Config.GoldRate = goldRate;
                Config.最高转生次数 = maxRebirth;

                // 持久化保存到文件
                ConfigReader.Save();

                Message = $"配置已更新并保存：端口={port}, 最高等级={maxLevel}, 经验倍率={expRate}x, 掉落倍率={dropRate}x, 金币倍率={goldRate}x, 最高转生={maxRebirth}";

                SEnvir.Log($"[Admin] 配置已更新并保存到文件: Port={port}, MaxLevel={maxLevel}, ExpRate={expRate}, DropRate={dropRate}, GoldRate={goldRate}, MaxRebirth={maxRebirth}");
            }
            catch (Exception ex)
            {
                Message = $"更新配置失败: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnPostToggleLogin(bool allow)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                return Page();
            }

            Config.AllowLogin = allow;
            ConfigReader.Save();
            Message = allow ? "已开启登录功能" : "已关闭登录功能（维护模式）";
            SEnvir.Log($"[Admin] 登录功能: {(allow ? "开启" : "关闭")}（已保存）");

            return Page();
        }

        public IActionResult OnPostToggleRegister(bool allow)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                return Page();
            }

            Config.AllowNewAccount = allow;
            ConfigReader.Save();
            Message = allow ? "已开启注册功能" : "已关闭注册功能";
            SEnvir.Log($"[Admin] 注册功能: {(allow ? "开启" : "关闭")}（已保存）");

            return Page();
        }

        public IActionResult OnPostToggleStartGame(bool allow)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                return Page();
            }

            Config.AllowStartGame = allow;
            ConfigReader.Save();
            Message = allow ? "已开启进入游戏功能" : "已关闭进入游戏功能（维护模式）";
            SEnvir.Log($"[Admin] 进入游戏: {(allow ? "开启" : "关闭")}（已保存）");

            // 如果关闭，广播维护通知
            if (!allow)
            {
                foreach (var player in SEnvir.Players.ToList())
                {
                    player?.Connection?.ReceiveChat("[系统公告] 服务器进入维护模式，新玩家暂时无法进入游戏", MessageType.Announcement);
                }
            }

            return Page();
        }

        public IActionResult OnPostToggleNewCharacter(bool allow)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                return Page();
            }

            Config.AllowNewCharacter = allow;
            ConfigReader.Save();
            Message = allow ? "已开启创建角色功能" : "已关闭创建角色功能";
            SEnvir.Log($"[Admin] 创建角色: {(allow ? "开启" : "关闭")}（已保存）");

            return Page();
        }

        public IActionResult OnPostKickAll()
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                return Page();
            }

            try
            {
                int count = 0;
                foreach (var player in SEnvir.Players.ToList())
                {
                    player?.Connection?.TrySendDisconnect(new Library.Network.GeneralPackets.Disconnect
                    {
                        Reason = DisconnectReason.ServerClosing
                    });
                    count++;
                }
                Message = $"已踢出 {count} 名玩家";
                SEnvir.Log($"[Admin] 踢出所有玩家: {count}人");
            }
            catch (Exception ex)
            {
                Message = $"操作失败: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnPostRestartServer(int delay)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 或更高权限";
                return Page();
            }

            try
            {
                if (delay < 0) delay = 0;
                if (delay > 300) delay = 300;

                // 获取操作者信息
                var operatorName = User.Identity?.Name ?? "Unknown";
                SEnvir.Log($"[Admin] 管理员 {operatorName} 发起服务器重启，延迟 {delay} 秒");

                // Broadcast restart message
                var msg = delay > 0 ? $"服务器将在 {delay} 秒后重启，请及时下线保存数据！" : "服务器正在重启...";
                foreach (var player in SEnvir.Players.ToList())
                {
                    player?.Connection?.ReceiveChat($"[系统公告] {msg}", MessageType.Announcement);
                }

                Message = $"服务器将在 {delay} 秒后重启（请确保 Docker 配置了 restart 策略）";
                SEnvir.Log($"[Admin] 服务器重启计划: {delay}秒后");

                // Schedule restart
                Task.Run(async () =>
                {
                    try
                    {
                        if (delay > 0)
                        {
                            await Task.Delay(delay * 1000);
                        }

                        // Kick all players first
                        SEnvir.Log("[Admin] 正在断开所有玩家连接...");
                        foreach (var player in SEnvir.Players.ToList())
                        {
                            try
                            {
                                player?.Connection?.TrySendDisconnect(new Library.Network.GeneralPackets.Disconnect
                                {
                                    Reason = DisconnectReason.ServerClosing
                                });
                            }
                            catch { }
                        }

                        // Wait for disconnects and auto-save
                        SEnvir.Log("[Admin] 等待数据自动保存...");
                        await Task.Delay(3000);

                        // Try to save data (with error handling)
                        try
                        {
                            SEnvir.Log("[Admin] 尝试保存数据...");
                            SEnvir.SaveUserDatas();
                            SEnvir.Log("[Admin] 用户数据保存完成");
                        }
                        catch (Exception saveEx)
                        {
                            SEnvir.Log($"[Admin] 保存用户数据时出错（可忽略）: {saveEx.Message}");
                        }

                        try
                        {
                            SEnvir.SaveSystem();
                            SEnvir.Log("[Admin] 系统数据保存完成");
                        }
                        catch (Exception saveEx)
                        {
                            SEnvir.Log($"[Admin] 保存系统数据时出错（可忽略）: {saveEx.Message}");
                        }

                        // Exit process - the service manager should restart it
                        SEnvir.Log("[Admin] 服务器正在重启...");
                        await Task.Delay(1000);
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        SEnvir.Log($"[Admin] 重启过程出错: {ex.Message}");
                        // Still try to exit
                        try
                        {
                            SEnvir.Log("[Admin] 强制退出...");
                            Environment.Exit(1);
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Message = $"重启失败: {ex.Message}";
                SEnvir.Log($"[Admin] 重启失败: {ex.Message}");
            }

            return Page();
        }

        private bool HasPermission(AccountIdentity required)
        {
            var permissionClaim = User.FindFirst("Permission")?.Value;
            if (string.IsNullOrEmpty(permissionClaim)) return false;

            if (int.TryParse(permissionClaim, out int permValue))
            {
                return permValue >= (int)required;
            }
            return false;
        }
    }
}
