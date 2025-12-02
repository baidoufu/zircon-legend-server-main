using Server.Envir;
using Zircon.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Library;

namespace Server.Web.Services
{
    /// <summary>
    /// 玩家管理服务
    /// </summary>
    public class PlayerService
    {
        /// <summary>
        /// 获取在线玩家列表
        /// </summary>
        public List<PlayerViewModel> GetOnlinePlayers()
        {
            var players = new List<PlayerViewModel>();

            try
            {
                foreach (var player in SEnvir.Players.ToList())
                {
                    if (player?.Character == null) continue;

                    players.Add(new PlayerViewModel
                    {
                        Name = player.Character.CharacterName ?? "Unknown",
                        Level = player.Level,
                        Class = player.Class.ToString(),
                        Gender = player.Gender.ToString(),
                        MapName = player.CurrentMap?.Info?.Description ?? "Unknown",
                        LocationX = player.CurrentLocation.X,
                        LocationY = player.CurrentLocation.Y,
                        Gold = player.Gold,
                        GameGold = player.Character.Account?.GameGold ?? 0,
                        AccountEmail = player.Character.Account?.EMailAddress ?? "",
                        PKPoints = player.Stats[Stat.PKPoint],
                        Online = true
                    });
                }
            }
            catch
            {
                // 防止遍历时集合变化导致异常
            }

            return players.OrderByDescending(p => p.Level).ToList();
        }

        /// <summary>
        /// 获取在线玩家数量
        /// </summary>
        public int GetOnlineCount()
        {
            try
            {
                return SEnvir.Players.Count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 根据名称查找在线玩家
        /// </summary>
        public PlayerObject? GetOnlinePlayer(string name)
        {
            try
            {
                return SEnvir.Players.FirstOrDefault(p =>
                    p?.Character?.CharacterName?.Equals(name, System.StringComparison.OrdinalIgnoreCase) == true);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 根据名称搜索在线玩家
        /// </summary>
        public List<PlayerViewModel> SearchPlayers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return GetOnlinePlayers();

            return GetOnlinePlayers()
                .Where(p => p.Name.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ||
                           p.AccountEmail.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// 玩家视图模型
    /// </summary>
    public class PlayerViewModel
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string Class { get; set; } = "";
        public string Gender { get; set; } = "";
        public string MapName { get; set; } = "";
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public long Gold { get; set; }
        public int GameGold { get; set; }
        public string AccountEmail { get; set; } = "";
        public int PKPoints { get; set; }
        public bool Online { get; set; }
    }
}
