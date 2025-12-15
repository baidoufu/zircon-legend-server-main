using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Envir;
using Server.DBModels;
using Library.SystemModels;
using Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Web.Pages
{
    [Authorize]
    public class SkillsModel : PageModel
    {
        public List<MagicViewModel> Magics { get; set; } = new();
        public List<CustomSummonViewModel> CustomSummonSkills { get; set; } = new();
        public List<MonsterListViewModel> MonsterList { get; set; } = new();
        public int TotalCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ClassFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SchoolFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 50;
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

        public string? Message { get; set; }

        public void OnGet()
        {
            LoadMagics();
            LoadCustomSummonSkills();
            LoadMonsterList();
        }

        private void LoadMagics()
        {
            try
            {
                if (SEnvir.MagicInfoList?.Binding == null) return;

                var query = SEnvir.MagicInfoList.Binding.AsEnumerable();

                // Keyword filter
                if (!string.IsNullOrWhiteSpace(Keyword))
                {
                    query = query.Where(m =>
                        (m.Name?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        m.Magic.ToString().Contains(Keyword, StringComparison.OrdinalIgnoreCase) ||
                        m.Index.ToString().Contains(Keyword));
                }

                // Class filter
                if (!string.IsNullOrWhiteSpace(ClassFilter) && Enum.TryParse<MirClass>(ClassFilter, out var mirClass))
                {
                    query = query.Where(m => m.Class == mirClass);
                }

                // School filter
                if (!string.IsNullOrWhiteSpace(SchoolFilter) && Enum.TryParse<MagicSchool>(SchoolFilter, out var school))
                {
                    query = query.Where(m => m.School == school);
                }

                TotalCount = query.Count();

                Magics = query
                    .OrderBy(m => m.Class)
                    .ThenBy(m => m.School)
                    .ThenBy(m => m.NeedLevel1)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .Select(m => new MagicViewModel
                    {
                        Index = m.Index,
                        Name = m.Name ?? "Unknown",
                        Magic = m.Magic.ToString(),
                        Class = m.Class.ToString(),
                        School = m.School.ToString(),
                        Mode = m.Mode.ToString(),
                        Icon = m.Icon,
                        MinBasePower = m.MinBasePower,
                        MaxBasePower = m.MaxBasePower,
                        MinLevelPower = m.MinLevelPower,
                        MaxLevelPower = m.MaxLevelPower,
                        BaseCost = m.BaseCost,
                        LevelCost = m.LevelCost,
                        NeedLevel1 = m.NeedLevel1,
                        NeedLevel2 = m.NeedLevel2,
                        NeedLevel3 = m.NeedLevel3,
                        Experience1 = m.Experience1,
                        Experience2 = m.Experience2,
                        Experience3 = m.Experience3,
                        Delay = m.Delay,
                        Description = m.Description ?? ""
                    })
                    .ToList();
            }
            catch
            {
                // Prevent enumeration errors
            }
        }

        private void LoadCustomSummonSkills()
        {
            try
            {
                if (SEnvir.MagicInfoList?.Binding == null) return;

                // 筛选 Magic == SummonSkeleton 且 SummonMonsterIndex > 0 的记录（自定义召唤技能）
                CustomSummonSkills = SEnvir.MagicInfoList.Binding
                    .Where(m => m.Magic == MagicType.SummonSkeleton && m.SummonMonsterIndex > 0)
                    .OrderBy(m => m.Index)
                    .Select(m => new CustomSummonViewModel
                    {
                        Index = m.Index,
                        Name = m.Name ?? "Unknown",
                        SummonMonsterIndex = m.SummonMonsterIndex,
                        MonsterName = SEnvir.MonsterInfoList?.Binding?.FirstOrDefault(x => x.Index == m.SummonMonsterIndex)?.MonsterName ?? "未知怪物",
                        MaxSummonCount = m.MaxSummonCount > 0 ? m.MaxSummonCount : 2,
                        AmuletCost = m.AmuletCost > 0 ? m.AmuletCost : 1,
                        Class = m.Class.ToString(),
                        NeedLevel1 = m.NeedLevel1,
                        Description = m.Description ?? "",
                        School = m.School.ToString()
                    })
                    .ToList();
            }
            catch
            {
                // Prevent enumeration errors
            }
        }

        private void LoadMonsterList()
        {
            try
            {
                if (SEnvir.MonsterInfoList?.Binding == null) return;

                MonsterList = SEnvir.MonsterInfoList.Binding
                    .OrderBy(m => m.Level)
                    .ThenBy(m => m.Index)
                    .Select(m => new MonsterListViewModel
                    {
                        Index = m.Index,
                        MonsterName = m.MonsterName ?? "Unknown",
                        Level = m.Level
                    })
                    .ToList();
            }
            catch
            {
                // Prevent enumeration errors
            }
        }

        public IActionResult OnPostGiveSkill(string playerName, int magicIndex, int level)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                LoadMagics();
                return Page();
            }

            try
            {
                var player = SEnvir.Players.FirstOrDefault(p =>
                    p?.Character?.CharacterName?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

                if (player == null)
                {
                    Message = $"玩家 {playerName} 不在线";
                    LoadMagics();
                    return Page();
                }

                var magicInfo = SEnvir.MagicInfoList?.Binding?.FirstOrDefault(m => m.Index == magicIndex);
                if (magicInfo == null)
                {
                    Message = $"技能 ID {magicIndex} 不存在";
                    LoadMagics();
                    return Page();
                }

                // Check if player already has this skill
                var existingMagic = player.Character.Magics.FirstOrDefault(m => m.Info?.Index == magicIndex);
                if (existingMagic != null)
                {
                    // Update existing skill level
                    existingMagic.Level = Math.Max(0, Math.Min(level, Config.技能最高等级));
                    existingMagic.Experience = 0;

                    // Notify player
                    player.Enqueue(new Library.Network.ServerPackets.MagicLeveled
                    {
                        InfoIndex = magicInfo.Index,
                        Level = existingMagic.Level,
                        Experience = existingMagic.Experience
                    });

                    Message = $"已更新玩家 {playerName} 的技能 [{magicInfo.Name}] 等级为 {existingMagic.Level}";
                    SEnvir.Log($"[Admin] 更新技能: {playerName} - {magicInfo.Name} Lv.{existingMagic.Level}");
                }
                else
                {
                    // Create new skill
                    var userMagic = SEnvir.UserMagicList.CreateNewObject();
                    userMagic.Character = player.Character;
                    userMagic.Info = magicInfo;
                    userMagic.Level = Math.Max(0, Math.Min(level, Config.技能最高等级));
                    userMagic.Experience = 0;

                    // Add to player's magic list (Character.Magics is a DBModel list, player.Magics is MagicType dictionary)
                    player.Character.Magics.Add(userMagic);
                    player.Magics[magicInfo.Magic] = userMagic;

                    // Notify player
                    player.Enqueue(new Library.Network.ServerPackets.NewMagic { Magic = userMagic.ToClientInfo() });

                    Message = $"已给玩家 {playerName} 添加技能 [{magicInfo.Name}] Lv.{userMagic.Level}";
                    SEnvir.Log($"[Admin] 添加技能: {playerName} - {magicInfo.Name} Lv.{userMagic.Level}");
                }
            }
            catch (Exception ex)
            {
                Message = $"操作失败: {ex.Message}";
            }

            LoadMagics();
            return Page();
        }

        public IActionResult OnPostRemoveSkill(string playerName, int magicIndex)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 权限";
                LoadMagics();
                return Page();
            }

            try
            {
                var player = SEnvir.Players.FirstOrDefault(p =>
                    p?.Character?.CharacterName?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

                if (player == null)
                {
                    Message = $"玩家 {playerName} 不在线";
                    LoadMagics();
                    return Page();
                }

                var userMagic = player.Character.Magics.FirstOrDefault(m => m.Info?.Index == magicIndex);
                if (userMagic == null)
                {
                    Message = $"玩家 {playerName} 没有该技能";
                    LoadMagics();
                    return Page();
                }

                var magicName = userMagic.Info?.Name ?? "Unknown";
                var magicType = userMagic.Info?.Magic;

                // Remove from player (player.Magics is Dictionary<MagicType, UserMagic>)
                if (magicType.HasValue)
                {
                    player.Magics.Remove(magicType.Value);
                }
                player.Character.Magics.Remove(userMagic);

                // Delete from database
                userMagic.Delete();

                // Note: No RemoveMagic packet available, player needs to relog to see changes
                Message = $"已移除玩家 {playerName} 的技能 [{magicName}]（玩家需重新登录生效）";
                SEnvir.Log($"[Admin] 移除技能: {playerName} - {magicName}");
            }
            catch (Exception ex)
            {
                Message = $"操作失败: {ex.Message}";
            }

            LoadMagics();
            return Page();
        }

        public IActionResult OnPostUpdateMagic(
            int index, string name, int icon,
            int minBasePower, int maxBasePower,
            int minLevelPower, int maxLevelPower,
            int baseCost, int levelCost,
            int needLevel1, int needLevel2, int needLevel3,
            int experience1, int experience2, int experience3,
            int delay, string description)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadMagics();
                return Page();
            }

            try
            {
                var magicInfo = SEnvir.MagicInfoList?.Binding?.FirstOrDefault(m => m.Index == index);
                if (magicInfo == null)
                {
                    Message = $"技能 ID {index} 不存在";
                    LoadMagics();
                    return Page();
                }

                // Update properties
                magicInfo.Name = name;
                magicInfo.Icon = icon;
                magicInfo.MinBasePower = minBasePower;
                magicInfo.MaxBasePower = maxBasePower;
                magicInfo.MinLevelPower = minLevelPower;
                magicInfo.MaxLevelPower = maxLevelPower;
                magicInfo.BaseCost = baseCost;
                magicInfo.LevelCost = levelCost;
                magicInfo.NeedLevel1 = needLevel1;
                magicInfo.NeedLevel2 = needLevel2;
                magicInfo.NeedLevel3 = needLevel3;
                magicInfo.Experience1 = experience1;
                magicInfo.Experience2 = experience2;
                magicInfo.Experience3 = experience3;
                magicInfo.Delay = delay;
                magicInfo.Description = description;

                Message = $"技能 [{name}] 已更新";
                SEnvir.Log($"[Admin] 更新技能属性: {name} (ID: {index})");
            }
            catch (Exception ex)
            {
                Message = $"更新失败: {ex.Message}";
            }

            LoadMagics();
            return Page();
        }

        public IActionResult OnPostGiveAllSkills(string playerName, int level)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadMagics();
                return Page();
            }

            try
            {
                var player = SEnvir.Players.FirstOrDefault(p =>
                    p?.Character?.CharacterName?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true);

                if (player == null)
                {
                    Message = $"玩家 {playerName} 不在线";
                    LoadMagics();
                    return Page();
                }

                var playerClass = player.Character.Class;
                int addedCount = 0;
                int updatedCount = 0;

                // Get all skills for player's class
                var classSkills = SEnvir.MagicInfoList?.Binding?
                    .Where(m => m.Class == playerClass)
                    .ToList() ?? new List<MagicInfo>();

                foreach (var magicInfo in classSkills)
                {
                    var existingMagic = player.Character.Magics.FirstOrDefault(m => m.Info?.Index == magicInfo.Index);

                    if (existingMagic != null)
                    {
                        // Update existing
                        existingMagic.Level = Math.Max(0, Math.Min(level, Config.技能最高等级));
                        existingMagic.Experience = 0;

                        player.Enqueue(new Library.Network.ServerPackets.MagicLeveled
                        {
                            InfoIndex = magicInfo.Index,
                            Level = existingMagic.Level,
                            Experience = existingMagic.Experience
                        });
                        updatedCount++;
                    }
                    else
                    {
                        // Create new
                        var userMagic = SEnvir.UserMagicList.CreateNewObject();
                        userMagic.Character = player.Character;
                        userMagic.Info = magicInfo;
                        userMagic.Level = Math.Max(0, Math.Min(level, Config.技能最高等级));
                        userMagic.Experience = 0;

                        player.Character.Magics.Add(userMagic);
                        player.Magics[magicInfo.Magic] = userMagic;

                        player.Enqueue(new Library.Network.ServerPackets.NewMagic { Magic = userMagic.ToClientInfo() });
                        addedCount++;
                    }
                }

                Message = $"已给玩家 {playerName} 添加 {addedCount} 个技能，更新 {updatedCount} 个技能到 Lv.{level}";
                SEnvir.Log($"[Admin] 全技能: {playerName} - 新增 {addedCount}, 更新 {updatedCount}, 等级 {level}");
            }
            catch (Exception ex)
            {
                Message = $"操作失败: {ex.Message}";
            }

            LoadMagics();
            return Page();
        }

        public IActionResult OnPostCreateCustomSummon(
            string name,
            string mirClass,
            int monsterIndex,
            int maxCount,
            int amuletCost,
            int needLevel,
            string description,
            string school)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadMagics();
                LoadCustomSummonSkills();
                LoadMonsterList();
                return Page();
            }

            try
            {
                // 验证怪物是否存在
                var monster = SEnvir.MonsterInfoList?.Binding?.FirstOrDefault(m => m.Index == monsterIndex);
                if (monster == null)
                {
                    Message = $"怪物索引 {monsterIndex} 不存在";
                    LoadMagics();
                    LoadCustomSummonSkills();
                    LoadMonsterList();
                    return Page();
                }

                // 解析职业
                if (!Enum.TryParse<MirClass>(mirClass, out var parsedClass))
                {
                    Message = $"无效的职业: {mirClass}";
                    LoadMagics();
                    LoadCustomSummonSkills();
                    LoadMonsterList();
                    return Page();
                }

                // 创建新的 MagicInfo 记录
                var newMagic = SEnvir.MagicInfoList.CreateNewObject();
                newMagic.Name = name;
                newMagic.Magic = MagicType.SummonSkeleton;  // 复用召唤骷髅类型
                newMagic.Class = parsedClass;
                newMagic.School = Enum.TryParse<MagicSchool>(school, out var parsedSchool) ? parsedSchool : MagicSchool.None;
                newMagic.Mode = MagicMode.Free;
                newMagic.SummonMonsterIndex = monsterIndex;
                newMagic.MaxSummonCount = maxCount > 0 ? maxCount : 2;
                newMagic.AmuletCost = amuletCost > 0 ? amuletCost : 1;
                newMagic.NeedLevel1 = needLevel > 0 ? needLevel : 1;
                newMagic.Description = description ?? "";

                // 复制召唤骷髅的其他默认属性
                var skeletonMagic = SEnvir.MagicInfoList.Binding.FirstOrDefault(m =>
                    m.Magic == MagicType.SummonSkeleton && m.SummonMonsterIndex == 0);
                if (skeletonMagic != null)
                {
                    newMagic.Icon = skeletonMagic.Icon;
                    newMagic.BaseCost = skeletonMagic.BaseCost;
                    newMagic.LevelCost = skeletonMagic.LevelCost;
                    newMagic.Delay = skeletonMagic.Delay;
                    // 复制升级所需经验值
                    newMagic.Experience1 = skeletonMagic.Experience1;
                    newMagic.Experience2 = skeletonMagic.Experience2;
                    newMagic.Experience3 = skeletonMagic.Experience3;
                    // 复制升级所需等级（NeedLevel1 由参数设置，复制2和3）
                    newMagic.NeedLevel2 = skeletonMagic.NeedLevel2;
                    newMagic.NeedLevel3 = skeletonMagic.NeedLevel3;
                }

                // 自动创建对应的技能书物品
                string skillBookMessage = "";
                var skillBook = SEnvir.ItemInfoList?.CreateNewObject();
                if (skillBook != null)
                {
                    skillBook.ItemName = $"{name}秘籍";
                    skillBook.ItemType = ItemType.Book;
                    skillBook.Shape = newMagic.Index;  // 关联技能

                    // 参考召唤骷髅技能书的参数
                    var skeletonBook = skeletonMagic != null
                        ? SEnvir.ItemInfoList.Binding.FirstOrDefault(
                            i => i.ItemType == ItemType.Book && i.Shape == skeletonMagic.Index)
                        : null;

                    if (skeletonBook != null)
                    {
                        // 复制召唤骷髅技能书的参数
                        skillBook.Price = skeletonBook.Price;
                        skillBook.Durability = skeletonBook.Durability;
                        skillBook.Image = skeletonBook.Image;
                        skillBook.Weight = skeletonBook.Weight;
                        skillBook.StackSize = skeletonBook.StackSize;
                        skillBook.Rarity = skeletonBook.Rarity;
                    }
                    else
                    {
                        // 默认值
                        skillBook.Price = 50000;
                        skillBook.Durability = 80;
                        skillBook.Image = 1;
                        skillBook.Weight = 10;
                        skillBook.StackSize = 1;
                    }

                    // 职业限制与技能一致
                    skillBook.RequiredClass = MirClassToRequiredClass(parsedClass);
                    skillBook.RequiredAmount = needLevel > 0 ? needLevel : 1;  // 学习等级要求

                    SEnvir.Log($"[Admin] 自动创建技能书: [{skillBook.Index}] {skillBook.ItemName}");
                    skillBookMessage = $"，技能书 [{skillBook.ItemName}] 已自动创建";
                }

                Message = $"自定义召唤技能 [{name}] 创建成功，召唤怪物: {monster.MonsterName}{skillBookMessage}";
                SEnvir.Log($"[Admin] 创建自定义召唤技能: {name} -> 召唤 {monster.MonsterName} (Index: {newMagic.Index})");
            }
            catch (Exception ex)
            {
                Message = $"创建失败: {ex.Message}";
            }

            LoadMagics();
            LoadCustomSummonSkills();
            LoadMonsterList();
            return Page();
        }

        public IActionResult OnPostUpdateCustomSummon(
            int magicIndex,
            string name,
            int monsterIndex,
            int maxCount,
            int amuletCost,
            int needLevel,
            string description,
            string school)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadMagics();
                LoadCustomSummonSkills();
                LoadMonsterList();
                return Page();
            }

            try
            {
                var magicInfo = SEnvir.MagicInfoList?.Binding?.FirstOrDefault(m => m.Index == magicIndex);
                if (magicInfo == null)
                {
                    Message = $"技能 ID {magicIndex} 不存在";
                    LoadMagics();
                    LoadCustomSummonSkills();
                    LoadMonsterList();
                    return Page();
                }

                // 验证是否为自定义召唤技能
                if (magicInfo.Magic != MagicType.SummonSkeleton || magicInfo.SummonMonsterIndex <= 0)
                {
                    Message = $"技能 ID {magicIndex} 不是自定义召唤技能";
                    LoadMagics();
                    LoadCustomSummonSkills();
                    LoadMonsterList();
                    return Page();
                }

                // 验证怪物是否存在
                var monster = SEnvir.MonsterInfoList?.Binding?.FirstOrDefault(m => m.Index == monsterIndex);
                if (monster == null)
                {
                    Message = $"怪物索引 {monsterIndex} 不存在";
                    LoadMagics();
                    LoadCustomSummonSkills();
                    LoadMonsterList();
                    return Page();
                }

                // 更新属性
                magicInfo.Name = name;
                magicInfo.SummonMonsterIndex = monsterIndex;
                magicInfo.MaxSummonCount = maxCount > 0 ? maxCount : 2;
                magicInfo.AmuletCost = amuletCost > 0 ? amuletCost : 1;
                magicInfo.NeedLevel1 = needLevel > 0 ? needLevel : 1;
                magicInfo.Description = description ?? "";
                if (Enum.TryParse<MagicSchool>(school, out var parsedSchool))
                {
                    magicInfo.School = parsedSchool;
                }

                Message = $"自定义召唤技能 [{name}] 更新成功";
                SEnvir.Log($"[Admin] 更新自定义召唤技能: {name} (ID: {magicIndex})");
            }
            catch (Exception ex)
            {
                Message = $"更新失败: {ex.Message}";
            }

            LoadMagics();
            LoadCustomSummonSkills();
            LoadMonsterList();
            return Page();
        }

        public IActionResult OnPostDeleteCustomSummon(int magicIndex)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                LoadMagics();
                LoadCustomSummonSkills();
                LoadMonsterList();
                return Page();
            }

            try
            {
                var magicInfo = SEnvir.MagicInfoList?.Binding?.FirstOrDefault(m => m.Index == magicIndex);
                if (magicInfo == null)
                {
                    Message = $"技能 ID {magicIndex} 不存在";
                    LoadMagics();
                    LoadCustomSummonSkills();
                    LoadMonsterList();
                    return Page();
                }

                // 验证是否为自定义召唤技能
                if (magicInfo.Magic != MagicType.SummonSkeleton || magicInfo.SummonMonsterIndex <= 0)
                {
                    Message = $"技能 ID {magicIndex} 不是自定义召唤技能，无法删除";
                    LoadMagics();
                    LoadCustomSummonSkills();
                    LoadMonsterList();
                    return Page();
                }

                var skillName = magicInfo.Name;

                // 删除技能前，先删除对应的技能书
                string skillBookMessage = "";
                var skillBook = SEnvir.ItemInfoList?.Binding?.FirstOrDefault(
                    i => i.ItemType == ItemType.Book && i.Shape == magicIndex);
                if (skillBook != null)
                {
                    var bookName = skillBook.ItemName;
                    SEnvir.Log($"[Admin] 删除关联技能书: [{skillBook.Index}] {bookName}");
                    skillBook.Delete();
                    skillBookMessage = $"，关联技能书 [{bookName}] 已删除";
                }

                // 从数据库删除技能
                magicInfo.Delete();

                Message = $"自定义召唤技能 [{skillName}] 已删除{skillBookMessage}";
                SEnvir.Log($"[Admin] 删除自定义召唤技能: {skillName} (ID: {magicIndex})");
            }
            catch (Exception ex)
            {
                Message = $"删除失败: {ex.Message}";
            }

            LoadMagics();
            LoadCustomSummonSkills();
            LoadMonsterList();
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

        private RequiredClass MirClassToRequiredClass(MirClass mirClass)
        {
            return mirClass switch
            {
                MirClass.Warrior => RequiredClass.Warrior,
                MirClass.Wizard => RequiredClass.Wizard,
                MirClass.Taoist => RequiredClass.Taoist,
                MirClass.Assassin => RequiredClass.Assassin,
                _ => RequiredClass.All
            };
        }
    }

    public class MagicViewModel
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Magic { get; set; } = "";
        public string Class { get; set; } = "";
        public string School { get; set; } = "";
        public string Mode { get; set; } = "";
        public int Icon { get; set; }
        public int MinBasePower { get; set; }
        public int MaxBasePower { get; set; }
        public int MinLevelPower { get; set; }
        public int MaxLevelPower { get; set; }
        public int BaseCost { get; set; }
        public int LevelCost { get; set; }
        public int NeedLevel1 { get; set; }
        public int NeedLevel2 { get; set; }
        public int NeedLevel3 { get; set; }
        public int Experience1 { get; set; }
        public int Experience2 { get; set; }
        public int Experience3 { get; set; }
        public int Delay { get; set; }
        public string Description { get; set; } = "";
    }

    public class CustomSummonViewModel
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public int SummonMonsterIndex { get; set; }
        public string MonsterName { get; set; } = "";
        public int MaxSummonCount { get; set; }
        public int AmuletCost { get; set; }
        public string Class { get; set; } = "";
        public int NeedLevel1 { get; set; }
        public string Description { get; set; } = "";
        public string School { get; set; } = "";
    }

    public class MonsterListViewModel
    {
        public int Index { get; set; }
        public string MonsterName { get; set; } = "";
        public int Level { get; set; }
    }
}
