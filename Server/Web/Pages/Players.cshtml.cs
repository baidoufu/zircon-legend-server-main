using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Envir;
using Server.Web.Services;
using System.Collections.Generic;
using System.Linq;
using Library;
using Library.Network.GeneralPackets;

namespace Server.Web.Pages
{
    [Authorize]
    public class PlayersModel : PageModel
    {
        private readonly PlayerService _playerService;

        public PlayersModel(PlayerService playerService)
        {
            _playerService = playerService;
        }

        public List<PlayerViewModel> Players { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        public string? Message { get; set; }

        public void OnGet()
        {
            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                Players = _playerService.SearchPlayers(Keyword);
            }
            else
            {
                Players = _playerService.GetOnlinePlayers();
            }
        }

        public IActionResult OnPostRecall(string playerName)
        {
            try
            {
                var targetPlayer = _playerService.GetOnlinePlayer(playerName);
                if (targetPlayer == null)
                {
                    Message = $"玩家 {playerName} 不在线";
                }
                else
                {
                    // 获取当前管理员的角色（如果在线）
                    var adminEmail = User.Identity?.Name;
                    var adminPlayer = SEnvir.Players.FirstOrDefault(p =>
                        p?.Character?.Account?.EMailAddress?.Equals(adminEmail, System.StringComparison.OrdinalIgnoreCase) == true);

                    if (adminPlayer != null)
                    {
                        // 召唤到管理员身边
                        targetPlayer.Teleport(adminPlayer.CurrentMap, adminPlayer.CurrentLocation);
                        Message = $"已将 {playerName} 召唤到您身边";
                    }
                    else
                    {
                        Message = "您当前不在线，无法召唤玩家";
                    }
                }
            }
            catch (System.Exception ex)
            {
                Message = $"召唤失败: {ex.Message}";
            }

            // 重新加载玩家列表
            OnGet();
            return Page();
        }

        public IActionResult OnPostKick(string playerName)
        {
            try
            {
                var targetPlayer = _playerService.GetOnlinePlayer(playerName);
                if (targetPlayer == null)
                {
                    Message = $"玩家 {playerName} 不在线";
                }
                else
                {
                    // 使用 AnotherUserAdmin 作为踢下线原因
                    targetPlayer.Connection?.TrySendDisconnect(new Disconnect
                    {
                        Reason = DisconnectReason.AnotherUserAdmin
                    });
                    Message = $"已将 {playerName} 踢下线";
                }
            }
            catch (System.Exception ex)
            {
                Message = $"踢出失败: {ex.Message}";
            }

            // 重新加载玩家列表
            OnGet();
            return Page();
        }
    }
}
