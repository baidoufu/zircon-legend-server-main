using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Envir;
using Server.DBModels;
using Zircon.Server.Models;
using Library.SystemModels;
using Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Web.Pages
{
    [Authorize]
    public class ClassesModel : PageModel
    {
        public string? Message { get; set; }

        // 职业统计
        public int WarriorCount { get; set; }
        public int WizardCount { get; set; }
        public int TaoistCount { get; set; }
        public int AssassinCount { get; set; }
        public int TotalCharacters { get; set; }

        // 在线统计
        public int OnlineWarriorCount { get; set; }
        public int OnlineWizardCount { get; set; }
        public int OnlineTaoistCount { get; set; }
        public int OnlineAssassinCount { get; set; }

        // 职业设置
        public bool AllowWarrior => Config.AllowWarrior;
        public bool AllowWizard => Config.AllowWizard;
        public bool AllowTaoist => Config.AllowTaoist;
        public bool AllowAssassin => Config.AllowAssassin;

        // 角色列表
        public List<CharacterViewModel> Characters { get; set; } = new();
        public int CharacterTotalCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ClassFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 30;
        public int TotalPages => (CharacterTotalCount + PageSize - 1) / PageSize;

        // 基础属性列表
        public List<BaseStatViewModel> BaseStats { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatClassFilter { get; set; }

        public void OnGet()
        {
            LoadStatistics();
            LoadCharacters();
            LoadBaseStats();
        }

        private void LoadStatistics()
        {
            try
            {
                if (SEnvir.CharacterInfoList?.Binding == null) return;

                var characters = SEnvir.CharacterInfoList.Binding
                    .Where(c => !c.Deleted)
                    .ToList();

                TotalCharacters = characters.Count;
                WarriorCount = characters.Count(c => c.Class == MirClass.Warrior);
                WizardCount = characters.Count(c => c.Class == MirClass.Wizard);
                TaoistCount = characters.Count(c => c.Class == MirClass.Taoist);
                AssassinCount = characters.Count(c => c.Class == MirClass.Assassin);

                // 在线统计
                var onlinePlayers = SEnvir.Players?.ToList() ?? new List<PlayerObject>();
                OnlineWarriorCount = onlinePlayers.Count(p => p?.Character?.Class == MirClass.Warrior);
                OnlineWizardCount = onlinePlayers.Count(p => p?.Character?.Class == MirClass.Wizard);
                OnlineTaoistCount = onlinePlayers.Count(p => p?.Character?.Class == MirClass.Taoist);
                OnlineAssassinCount = onlinePlayers.Count(p => p?.Character?.Class == MirClass.Assassin);
            }
            catch
            {
                // Prevent enumeration errors
            }
        }

        private void LoadCharacters()
        {
            try
            {
                if (SEnvir.CharacterInfoList?.Binding == null) return;

                var query = SEnvir.CharacterInfoList.Binding
                    .Where(c => !c.Deleted)
                    .AsEnumerable();

                // 职业筛选
                if (!string.IsNullOrWhiteSpace(ClassFilter) && Enum.TryParse<MirClass>(ClassFilter, out var mirClass))
                {
                    query = query.Where(c => c.Class == mirClass);
                }

                // 关键字搜索
                if (!string.IsNullOrWhiteSpace(Keyword))
                {
                    query = query.Where(c =>
                        (c.CharacterName?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Account?.EMailAddress?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                CharacterTotalCount = query.Count();

                Characters = query
                    .OrderByDescending(c => c.Level)
                    .ThenByDescending(c => c.LastLogin)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .Select(c => new CharacterViewModel
                    {
                        Index = c.Index,
                        Name = c.CharacterName ?? "Unknown",
                        Class = c.Class.ToString(),
                        Gender = c.Gender.ToString(),
                        Level = c.Level,
                        AccountEmail = c.Account?.EMailAddress ?? "Unknown",
                        LastLogin = c.LastLogin,
                        IsOnline = SEnvir.Players?.Any(p => p?.Character?.Index == c.Index) ?? false
                    })
                    .ToList();
            }
            catch
            {
                // Prevent enumeration errors
            }
        }

        private void LoadBaseStats()
        {
            try
            {
                if (SEnvir.BaseStatList?.Binding == null) return;

                var query = SEnvir.BaseStatList.Binding.AsEnumerable();

                // 职业筛选
                if (!string.IsNullOrWhiteSpace(StatClassFilter) && Enum.TryParse<MirClass>(StatClassFilter, out var mirClass))
                {
                    query = query.Where(s => s.Class == mirClass);
                }

                BaseStats = query
                    .OrderBy(s => s.Class)
                    .ThenBy(s => s.Level)
                    .Take(100) // 限制显示数量
                    .Select(s => new BaseStatViewModel
                    {
                        Index = s.Index,
                        Class = s.Class.ToString(),
                        Level = s.Level,
                        Health = s.Health,
                        Mana = s.Mana,
                        BagWeight = s.BagWeight,
                        WearWeight = s.WearWeight,
                        HandWeight = s.HandWeight,
                        Accuracy = s.Accuracy,
                        Agility = s.Agility,
                        MinAC = s.MinAC,
                        MaxAC = s.MaxAC,
                        MinMR = s.MinMR,
                        MaxMR = s.MaxMR,
                        MinDC = s.MinDC,
                        MaxDC = s.MaxDC,
                        MinMC = s.MinMC,
                        MaxMC = s.MaxMC,
                        MinSC = s.MinSC,
                        MaxSC = s.MaxSC
                    })
                    .ToList();
            }
            catch
            {
                // Prevent enumeration errors
            }
        }

        public IActionResult OnPostToggleClass(string className, bool allow)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadStatistics();
                LoadCharacters();
                LoadBaseStats();
                return Page();
            }

            try
            {
                switch (className.ToLower())
                {
                    case "warrior":
                        Config.AllowWarrior = allow;
                        break;
                    case "wizard":
                        Config.AllowWizard = allow;
                        break;
                    case "taoist":
                        Config.AllowTaoist = allow;
                        break;
                    case "assassin":
                        Config.AllowAssassin = allow;
                        break;
                    default:
                        Message = $"未知职业: {className}";
                        LoadStatistics();
                        LoadCharacters();
                        LoadBaseStats();
                        return Page();
                }

                ConfigReader.Save();
                var classNameCn = GetClassNameCn(className);
                Message = $"职业 [{classNameCn}] 创建权限已{(allow ? "开启" : "关闭")}";
                SEnvir.Log($"[Admin] 职业创建权限: {classNameCn} = {(allow ? "开启" : "关闭")}");
            }
            catch (Exception ex)
            {
                Message = $"操作失败: {ex.Message}";
            }

            LoadStatistics();
            LoadCharacters();
            LoadBaseStats();
            return Page();
        }

        public IActionResult OnPostChangeClass(string playerName, string newClass)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadStatistics();
                LoadCharacters();
                LoadBaseStats();
                return Page();
            }

            try
            {
                var player = SEnvir.Players?.FirstOrDefault(p =>
                    p?.Character?.CharacterName?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

                if (player == null)
                {
                    Message = $"玩家 {playerName} 不在线";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                if (!Enum.TryParse<MirClass>(newClass, out var mirClass))
                {
                    Message = $"无效的职业: {newClass}";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                var oldClass = player.Character.Class;
                if (oldClass == mirClass)
                {
                    Message = $"玩家 {playerName} 已经是 {GetClassNameCn(newClass)} 职业";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                // 修改职业
                player.Character.Class = mirClass;

                // 清空技能（因为转职后技能不同）
                var magicsToRemove = player.Character.Magics.ToList();
                foreach (var magic in magicsToRemove)
                {
                    if (magic.Info?.Magic != null)
                    {
                        player.Magics.Remove(magic.Info.Magic);
                    }
                    player.Character.Magics.Remove(magic);
                    magic.Delete();
                }

                Message = $"已将玩家 {playerName} 的职业从 [{GetClassNameCn(oldClass.ToString())}] 修改为 [{GetClassNameCn(newClass)}]，技能已清空，玩家需重新登录生效";
                SEnvir.Log($"[Admin] 修改职业: {playerName} {oldClass} -> {mirClass}");
            }
            catch (Exception ex)
            {
                Message = $"操作失败: {ex.Message}";
            }

            LoadStatistics();
            LoadCharacters();
            LoadBaseStats();
            return Page();
        }

        public IActionResult OnPostChangeOfflineClass(int characterIndex, string newClass)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadStatistics();
                LoadCharacters();
                LoadBaseStats();
                return Page();
            }

            try
            {
                var character = SEnvir.CharacterInfoList?.Binding?.FirstOrDefault(c => c.Index == characterIndex);
                if (character == null)
                {
                    Message = $"角色 ID {characterIndex} 不存在";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                // 检查是否在线
                var isOnline = SEnvir.Players?.Any(p => p?.Character?.Index == characterIndex) ?? false;
                if (isOnline)
                {
                    Message = $"角色 {character.CharacterName} 当前在线，请使用在线修改功能";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                if (!Enum.TryParse<MirClass>(newClass, out var mirClass))
                {
                    Message = $"无效的职业: {newClass}";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                var oldClass = character.Class;
                if (oldClass == mirClass)
                {
                    Message = $"角色 {character.CharacterName} 已经是 {GetClassNameCn(newClass)} 职业";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                // 修改职业
                character.Class = mirClass;

                // 清空技能
                var magicsToRemove = character.Magics.ToList();
                foreach (var magic in magicsToRemove)
                {
                    character.Magics.Remove(magic);
                    magic.Delete();
                }

                Message = $"已将角色 {character.CharacterName} 的职业从 [{GetClassNameCn(oldClass.ToString())}] 修改为 [{GetClassNameCn(newClass)}]，技能已清空";
                SEnvir.Log($"[Admin] 修改离线角色职业: {character.CharacterName} {oldClass} -> {mirClass}");
            }
            catch (Exception ex)
            {
                Message = $"操作失败: {ex.Message}";
            }

            LoadStatistics();
            LoadCharacters();
            LoadBaseStats();
            return Page();
        }

        public IActionResult OnPostUpdateBaseStat(
            string statClass, int level,
            int health, int mana,
            int bagWeight, int wearWeight, int handWeight,
            int accuracy, int agility,
            int minAC, int maxAC,
            int minMR, int maxMR,
            int minDC, int maxDC,
            int minMC, int maxMC,
            int minSC, int maxSC)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadStatistics();
                LoadCharacters();
                LoadBaseStats();
                return Page();
            }

            try
            {
                if (!Enum.TryParse<MirClass>(statClass, out var mirClass))
                {
                    Message = $"无效的职业: {statClass}";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                var baseStat = SEnvir.BaseStatList?.Binding?.FirstOrDefault(s => s.Class == mirClass && s.Level == level);
                if (baseStat == null)
                {
                    Message = $"未找到 {GetClassNameCn(statClass)} 等级 {level} 的基础属性";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                // 更新属性
                baseStat.Health = health;
                baseStat.Mana = mana;
                baseStat.BagWeight = bagWeight;
                baseStat.WearWeight = wearWeight;
                baseStat.HandWeight = handWeight;
                baseStat.Accuracy = accuracy;
                baseStat.Agility = agility;
                baseStat.MinAC = minAC;
                baseStat.MaxAC = maxAC;
                baseStat.MinMR = minMR;
                baseStat.MaxMR = maxMR;
                baseStat.MinDC = minDC;
                baseStat.MaxDC = maxDC;
                baseStat.MinMC = minMC;
                baseStat.MaxMC = maxMC;
                baseStat.MinSC = minSC;
                baseStat.MaxSC = maxSC;

                Message = $"已更新 {GetClassNameCn(statClass)} 等级 {level} 的基础属性";
                SEnvir.Log($"[Admin] 更新基础属性: {statClass} Lv.{level}");
            }
            catch (Exception ex)
            {
                Message = $"更新失败: {ex.Message}";
            }

            LoadStatistics();
            LoadCharacters();
            LoadBaseStats();
            return Page();
        }

        public IActionResult OnPostCreateBaseStat(string statClass, int level)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadStatistics();
                LoadCharacters();
                LoadBaseStats();
                return Page();
            }

            try
            {
                if (!Enum.TryParse<MirClass>(statClass, out var mirClass))
                {
                    Message = $"无效的职业: {statClass}";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                // 检查是否已存在
                var existing = SEnvir.BaseStatList?.Binding?.FirstOrDefault(s => s.Class == mirClass && s.Level == level);
                if (existing != null)
                {
                    Message = $"{GetClassNameCn(statClass)} 等级 {level} 的基础属性已存在";
                    LoadStatistics();
                    LoadCharacters();
                    LoadBaseStats();
                    return Page();
                }

                // 创建新属性（使用上一级的数据作为基础）
                var prevLevel = SEnvir.BaseStatList?.Binding?
                    .Where(s => s.Class == mirClass && s.Level < level)
                    .OrderByDescending(s => s.Level)
                    .FirstOrDefault();

                var newStat = SEnvir.BaseStatList.CreateNewObject();
                newStat.Class = mirClass;
                newStat.Level = level;

                if (prevLevel != null)
                {
                    // 基于上一级的数据稍微增加
                    newStat.Health = prevLevel.Health + 10;
                    newStat.Mana = prevLevel.Mana + 5;
                    newStat.BagWeight = prevLevel.BagWeight + 1;
                    newStat.WearWeight = prevLevel.WearWeight + 1;
                    newStat.HandWeight = prevLevel.HandWeight + 1;
                    newStat.Accuracy = prevLevel.Accuracy;
                    newStat.Agility = prevLevel.Agility;
                    newStat.MinAC = prevLevel.MinAC;
                    newStat.MaxAC = prevLevel.MaxAC;
                    newStat.MinMR = prevLevel.MinMR;
                    newStat.MaxMR = prevLevel.MaxMR;
                    newStat.MinDC = prevLevel.MinDC;
                    newStat.MaxDC = prevLevel.MaxDC;
                    newStat.MinMC = prevLevel.MinMC;
                    newStat.MaxMC = prevLevel.MaxMC;
                    newStat.MinSC = prevLevel.MinSC;
                    newStat.MaxSC = prevLevel.MaxSC;
                }
                else
                {
                    // 默认值
                    newStat.Health = 50;
                    newStat.Mana = 20;
                    newStat.BagWeight = 30;
                    newStat.WearWeight = 10;
                    newStat.HandWeight = 5;
                }

                Message = $"已创建 {GetClassNameCn(statClass)} 等级 {level} 的基础属性";
                SEnvir.Log($"[Admin] 创建基础属性: {statClass} Lv.{level}");
            }
            catch (Exception ex)
            {
                Message = $"创建失败: {ex.Message}";
            }

            LoadStatistics();
            LoadCharacters();
            LoadBaseStats();
            return Page();
        }

        private string GetClassNameCn(string className)
        {
            return className.ToLower() switch
            {
                "warrior" => "战士",
                "wizard" => "法师",
                "taoist" => "道士",
                "assassin" => "刺客",
                _ => className
            };
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

    public class CharacterViewModel
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public string Gender { get; set; } = "";
        public int Level { get; set; }
        public string AccountEmail { get; set; } = "";
        public DateTime LastLogin { get; set; }
        public bool IsOnline { get; set; }
    }

    public class BaseStatViewModel
    {
        public int Index { get; set; }
        public string Class { get; set; } = "";
        public int Level { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
        public int BagWeight { get; set; }
        public int WearWeight { get; set; }
        public int HandWeight { get; set; }
        public int Accuracy { get; set; }
        public int Agility { get; set; }
        public int MinAC { get; set; }
        public int MaxAC { get; set; }
        public int MinMR { get; set; }
        public int MaxMR { get; set; }
        public int MinDC { get; set; }
        public int MaxDC { get; set; }
        public int MinMC { get; set; }
        public int MaxMC { get; set; }
        public int MinSC { get; set; }
        public int MaxSC { get; set; }
    }
}
