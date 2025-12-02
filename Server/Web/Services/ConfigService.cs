using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Envir;
using Library;

namespace Server.Web.Services
{
    /// <summary>
    /// 配置管理服务 - 提供配置的读取、验证和保存功能
    /// </summary>
    public class ConfigService
    {
        /// <summary>
        /// 配置项定义
        /// </summary>
        public class ConfigItem
        {
            public string Section { get; set; } = "";
            public string Key { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public Type ValueType { get; set; } = typeof(string);
            public object? Value { get; set; }
            public object? DefaultValue { get; set; }
            public PropertyInfo? PropertyInfo { get; set; }
            public bool IsReadOnly { get; set; } = false;
            public bool RequiresRestart { get; set; } = false;
        }

        /// <summary>
        /// 配置分组定义
        /// </summary>
        public class ConfigSectionInfo
        {
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Description { get; set; } = "";
            public string Icon { get; set; } = "bi-gear";
            public List<ConfigItem> Items { get; set; } = new();
        }

        // 配置分组显示名称和图标映射
        private static readonly Dictionary<string, (string DisplayName, string Description, string Icon)> SectionMeta = new()
        {
            ["Network"] = ("网络配置", "服务器IP、端口、连接限制等网络相关设置", "bi-ethernet"),
            ["System"] = ("系统配置", "数据库、路径、密码等系统核心设置", "bi-cpu"),
            ["Control"] = ("登录控制", "登录、注册、角色创建等权限控制", "bi-shield-lock"),
            ["Mail"] = ("邮件配置", "SMTP服务器、账户等邮件发送设置", "bi-envelope"),
            ["WebServer"] = ("Web服务器", "Web命令、支付等Web接口配置", "bi-globe"),
            ["Players"] = ("玩家配置", "等级上限、PK规则、转生等玩家相关设置", "bi-people"),
            ["Monsters"] = ("怪物配置", "怪物行为、掉落、宠物等怪物相关设置", "bi-bug"),
            ["Items"] = ("物品配置", "掉落、精炼、武器等物品相关设置", "bi-box-seam"),
            ["Rates"] = ("倍率配置", "经验、掉落、金币等各类倍率设置", "bi-graph-up"),
            ["Admin"] = ("管理后台", "后台端口、IP白名单等管理设置", "bi-gear-wide-connected")
        };

        // 配置项显示名称和描述映射
        private static readonly Dictionary<string, (string DisplayName, string Description, bool RequiresRestart)> ConfigMeta = new()
        {
            // Network
            ["IPAddress"] = ("监听地址", "服务器监听的IP地址，0.0.0.0表示所有网卡", true),
            ["Port"] = ("游戏端口", "游戏服务器端口号 (1-65535)", true),
            ["TimeOut"] = ("连接超时", "客户端连接超时时间", false),
            ["PingDelay"] = ("心跳间隔", "心跳包发送间隔", false),
            ["UserCountPort"] = ("用户统计端口", "用户统计服务端口", true),
            ["MaxPacket"] = ("最大包数/秒", "每秒最大数据包数量，防刷机制", false),
            ["PacketBanTime"] = ("超包封禁时长", "超过包数限制后的封禁时间", false),
            ["UseProxy"] = ("使用代理", "是否使用Nginx等反向代理", false),
            ["ConnectionLimit"] = ("最大连接数", "服务器最大并发连接数 (1-65533)", false),

            // System
            ["DBSaveDelay"] = ("数据保存间隔", "数据库自动保存间隔时间", false),
            ["MapPath"] = ("地图路径", "地图文件存放目录", true),
            ["MasterPassword"] = ("主密码", "GM命令验证密码（敏感）", false),
            ["ClientPath"] = ("客户端路径", "客户端更新文件目录", false),
            ["ReleaseDate"] = ("发布日期", "服务器发布日期", false),
            ["TestServer"] = ("测试服务器", "是否为测试服务器模式", false),
            ["StarterGuildName"] = ("新人行会名称", "新玩家自动加入的行会名称", false),
            ["EasterEventEnd"] = ("复活节活动结束", "复活节活动结束时间", false),
            ["HalloweenEventEnd"] = ("万圣节活动结束", "万圣节活动结束时间", false),
            ["ChristmasEventEnd"] = ("圣诞节活动结束", "圣诞节活动结束时间", false),
            ["UpgradeChunkSize"] = ("更新块大小", "客户端更新文件分块大小(字节)", false),
            ["WelcomeWordsFile"] = ("欢迎词文件", "玩家登录欢迎词配置文件路径", false),
            ["挖出的黑铁矿最小纯度"] = ("黑铁矿最小纯度", "采矿获得黑铁矿的最小纯度", false),
            ["挖出的黑铁矿最大纯度"] = ("黑铁矿最大纯度", "采矿获得黑铁矿的最大纯度", false),
            ["排名只显示前多少名"] = ("排名显示数量", "排行榜显示前N名，-1为全部", false),
            ["玩家数据备份间隔"] = ("数据备份间隔", "玩家数据自动备份间隔", false),
            ["单次请求排名拉取的数量不超过多少个"] = ("排名拉取数量", "单次请求最多获取的排名数量", false),
            ["地狱之门关联地图名称"] = ("地狱之门地图", "地狱之门传送的目标地图", false),
            ["异界之门关联地图名称"] = ("异界之门地图", "异界之门传送的目标地图", false),
            ["数据清理间隔分钟"] = ("数据清理间隔", "过期数据清理间隔(分钟)", false),
            ["判断敏感词最大跳几个字符"] = ("敏感词跳字", "敏感词检测跳字符数", false),

            // Control
            ["AllowLogin"] = ("允许登录", "是否允许普通玩家登录", false),
            ["AdminOnlyLogin"] = ("仅管理员登录", "是否仅允许管理员登录", false),
            ["AllowNewAccount"] = ("允许注册", "是否允许注册新账户", false),
            ["AllowChangePassword"] = ("允许修改密码", "是否允许用户修改密码", false),
            ["AllowRequestPasswordReset"] = ("允许请求重置密码", "是否允许请求重置密码", false),
            ["AllowWebResetPassword"] = ("允许Web重置密码", "是否允许通过Web重置密码", false),
            ["AllowManualResetPassword"] = ("允许手动重置密码", "是否允许手动重置密码", false),
            ["AllowDeleteAccount"] = ("允许删除账户", "是否允许删除账户", false),
            ["AllowManualActivation"] = ("允许手动激活", "是否允许手动激活账户", false),
            ["AllowWebActivation"] = ("允许Web激活", "是否允许Web激活账户", false),
            ["AllowRequestActivation"] = ("允许请求激活", "是否允许请求激活账户", false),
            ["AllowNewCharacter"] = ("允许创建角色", "是否允许创建新角色", false),
            ["AllowDeleteCharacter"] = ("允许删除角色", "是否允许删除角色", false),
            ["RelogDelay"] = ("重新登录延迟", "断线后重新登录等待时间", false),
            ["AllowWarrior"] = ("允许战士", "是否允许创建战士职业", false),
            ["AllowWizard"] = ("允许法师", "是否允许创建法师职业", false),
            ["AllowTaoist"] = ("允许道士", "是否允许创建道士职业", false),
            ["AllowAssassin"] = ("允许刺客", "是否允许创建刺客职业", false),
            ["AllowStartGame"] = ("允许进入游戏", "是否允许玩家进入游戏世界", false),

            // Mail
            ["MailServer"] = ("SMTP服务器", "邮件服务器地址", false),
            ["MailPort"] = ("SMTP端口", "邮件服务器端口", false),
            ["MailUseSSL"] = ("使用SSL", "是否使用SSL加密连接", false),
            ["MailAccount"] = ("邮件账户", "发送邮件的账户", false),
            ["MailPassword"] = ("邮件密码", "邮件账户密码（敏感）", false),
            ["MailFrom"] = ("发件人地址", "邮件发件人地址", false),
            ["MailDisplayName"] = ("发件人名称", "邮件发件人显示名称", false),

            // WebServer
            ["WebPrefix"] = ("Web命令前缀", "Web命令服务监听地址", true),
            ["WebCommandLink"] = ("Web命令链接", "Web命令外部访问地址", false),
            ["ActivationSuccessLink"] = ("激活成功链接", "账户激活成功跳转地址", false),
            ["ActivationFailLink"] = ("激活失败链接", "账户激活失败跳转地址", false),
            ["ResetSuccessLink"] = ("重置成功链接", "密码重置成功跳转地址", false),
            ["ResetFailLink"] = ("重置失败链接", "密码重置失败跳转地址", false),
            ["DeleteSuccessLink"] = ("删除成功链接", "账户删除成功跳转地址", false),
            ["DeleteFailLink"] = ("删除失败链接", "账户删除失败跳转地址", false),
            ["BuyPrefix"] = ("购买前缀", "购买服务监听地址", true),
            ["BuyAddress"] = ("购买地址", "购买服务外部访问地址", false),
            ["IPNPrefix"] = ("IPN前缀", "支付通知服务监听地址", true),
            ["ReceiverEMail"] = ("收款邮箱", "支付接收邮箱", false),
            ["ProcessGameGold"] = ("处理游戏币", "是否处理游戏币购买", false),
            ["AllowBuyGammeGold"] = ("允许购买游戏币", "是否允许购买游戏币", false),

            // Players
            ["MaxViewRange"] = ("最大视野", "玩家最大视野范围", false),
            ["ShoutDelay"] = ("喊话间隔", "喊话频率限制", false),
            ["GlobalDelay"] = ("全服发言间隔", "全服发言频率限制", false),
            ["MaxLevel"] = ("最高等级", "玩家最高等级上限", false),
            ["DayCycleCount"] = ("昼夜循环次数", "游戏内昼夜循环次数", false),
            ["技能初级阶段基础经验"] = ("技能基础经验", "技能初级阶段基础经验值", false),
            ["AllowObservation"] = ("允许观察", "是否允许观察模式", false),
            ["BrownDuration"] = ("灰名持续时间", "灰名状态持续时间", false),
            ["PKPointTickRate"] = ("PK点衰减间隔", "PK点数衰减计算间隔", false),
            ["PKPointRate"] = ("PK点衰减率", "每次衰减减少的PK点数", false),
            ["RedPoint"] = ("红名点数", "成为红名所需的PK点数", false),
            ["PvPCurseDuration"] = ("PK诅咒持续", "PK诅咒效果持续时间", false),
            ["PvPCurseRate"] = ("PK诅咒倍率", "PK诅咒效果倍率", false),
            ["AutoReviveDelay"] = ("自动复活延迟", "死亡后自动复活等待时间", false),
            ["最高转生次数"] = ("最高转生次数", "玩家最高转生次数上限", false),
            ["转生基础等级"] = ("转生基础等级", "转生所需的基础等级", false),
            ["技能最高等级"] = ("技能最高等级", "技能可达到的最高等级", false),
            ["转生标识设置文件"] = ("转生配置文件", "转生标识设置文件路径", false),

            // Monsters
            ["DeadDuration"] = ("死亡持续时间", "怪物尸体存在时间", false),
            ["HarvestDuration"] = ("采集持续时间", "怪物可采集时间", false),
            ["MysteryShipRegionIndex"] = ("神秘船区域索引", "神秘船区域索引值", false),
            ["不掉落低于本价格的普通药水"] = ("药水掉落价格门槛", "不掉落低于此价格的普通药水", false),
            ["DropNothingTypeCommonItem"] = ("掉落无类型普通物品", "是否掉落无类型的普通物品", false),
            ["DropLowestEquipmentsExcludeWeapon"] = ("最低装备掉落等级", "掉落装备的最低等级(不含武器)", false),
            ["DropLowestWeapon"] = ("最低武器掉落等级", "掉落武器的最低等级", false),
            ["SummonMonsterGrowUpFile"] = ("召唤怪物成长文件", "召唤怪物成长配置文件路径", false),
            ["道具呼唤的怪物存活分钟"] = ("召唤怪物存活时间", "道具召唤怪物的存活时间(分钟)", false),
            ["宠物不追击距离玩家多少格以外的敌人"] = ("宠物追击距离", "宠物停止追击敌人的距离(格)", false),

            // Items
            ["DropDuration"] = ("掉落存在时间", "地面物品存在时间", false),
            ["DropDistance"] = ("掉落距离", "物品掉落扩散距离", false),
            ["DropLayers"] = ("掉落层数", "同位置最大物品堆叠层数", false),
            ["TorchRate"] = ("火把效果", "火把照明效果强度", false),
            ["SpecialRepairDelay"] = ("特殊修理延迟", "特殊修理冷却时间", false),
            ["MaxLuck"] = ("最大幸运", "装备最大幸运值", false),
            ["MaxCurse"] = ("最大诅咒", "装备最大诅咒值", false),
            ["CurseRate"] = ("诅咒几率", "获得诅咒属性的几率(%)", false),
            ["LuckRate"] = ("幸运几率", "获得幸运属性的几率(%)", false),
            ["MaxStrength"] = ("最大强度", "装备最大强度值", false),
            ["StrengthAddRate"] = ("强度增加率", "强化增加强度的几率(%)", false),
            ["StrengthLossRate"] = ("强度损失率", "强化失败损失强度的几率(%)", false),
            ["MonsterDropGroupShare"] = ("怪物掉落组队分享", "是否开启组队分享掉落", false),
            ["CanSeeOthersDropped"] = ("可见他人掉落", "是否可以看到他人的掉落物品", false),
            ["MonsterDropProtectionDuration"] = ("掉落保护时间", "掉落物品的保护时间(秒)", false),
            ["武器最高精炼等级"] = ("武器最高精炼等级", "武器可达到的最高精炼等级", false),
            ["武器品质每低一档降低精炼上限"] = ("品质精炼惩罚", "武器品质每低一档降低的精炼上限", false),
            ["武器重置等待分钟"] = ("武器重置等待", "武器重置冷却时间(分钟)", false),
            ["武器精炼最大几率基数"] = ("精炼最大几率", "武器精炼成功最大几率基数", false),
            ["武器精炼几率基数"] = ("精炼几率基数", "武器精炼成功基础几率", false),
            ["武器重置冷却分钟"] = ("武器重置冷却", "武器重置冷却时间(分钟)", false),
            ["武器重置保留五分之一属性"] = ("重置保留属性", "武器重置是否保留五分之一属性", false),
            ["武器重置时每多少点属性保留一点"] = ("重置属性保留率", "武器重置时每N点属性保留1点", false),

            // Rates
            ["ExperienceRate"] = ("经验倍率", "经验获取倍率，0为默认", false),
            ["DropRate"] = ("掉落倍率", "物品掉落倍率，0为默认", false),
            ["GoldRate"] = ("金币倍率", "金币获取倍率，0为默认", false),
            ["技能低等级经验倍率"] = ("技能低级经验倍率", "低等级技能经验倍率", false),
            ["CompanionRate"] = ("同伴倍率", "同伴经验获取倍率", false),
            ["Boss掉落倍率"] = ("Boss掉落倍率", "Boss怪物掉落倍率", false),
            ["技能高等级经验倍率"] = ("技能高级经验倍率", "高等级技能经验倍率", false),

            // Admin
            ["AdminEnabled"] = ("启用管理后台", "是否启用Web管理后台", true),
            ["AdminPort"] = ("管理后台端口", "Web管理后台监听端口", true),
            ["AdminAllowedIPs"] = ("允许访问IP", "允许访问后台的IP白名单，逗号分隔，空为不限制", false)
        };

        /// <summary>
        /// 获取所有配置分组
        /// </summary>
        public List<ConfigSectionInfo> GetAllConfigSections()
        {
            var sections = new Dictionary<string, ConfigSectionInfo>();
            var configType = typeof(Config);

            // 当前 Section 名称（ConfigSection 特性标记后续属性直到下一个特性）
            string currentSectionName = "Unknown";

            // 获取所有公共静态属性（按声明顺序）
            foreach (var prop in configType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                // 检查是否有新的 ConfigSection 特性（使用 Library 中的特性）
                var sectionAttr = prop.GetCustomAttribute<Library.ConfigSection>();
                if (sectionAttr != null)
                {
                    currentSectionName = sectionAttr.Section;
                }

                // 创建或获取分组
                if (!sections.TryGetValue(currentSectionName, out var section))
                {
                    var (displayName, description, icon) = SectionMeta.GetValueOrDefault(
                        currentSectionName,
                        (currentSectionName, $"{currentSectionName}配置", "bi-gear")
                    );

                    section = new ConfigSectionInfo
                    {
                        Name = currentSectionName,
                        DisplayName = displayName,
                        Description = description,
                        Icon = icon
                    };
                    sections[currentSectionName] = section;
                }

                // 创建配置项
                var (itemDisplayName, itemDescription, requiresRestart) = ConfigMeta.GetValueOrDefault(
                    prop.Name,
                    (prop.Name, $"配置项: {prop.Name}", false)
                );

                var item = new ConfigItem
                {
                    Section = currentSectionName,
                    Key = prop.Name,
                    DisplayName = itemDisplayName,
                    Description = itemDescription,
                    ValueType = prop.PropertyType,
                    Value = prop.GetValue(null),
                    PropertyInfo = prop,
                    RequiresRestart = requiresRestart
                };

                section.Items.Add(item);
            }

            // 按预定义顺序排序
            var orderedSections = new List<ConfigSectionInfo>();
            foreach (var name in SectionMeta.Keys)
            {
                if (sections.TryGetValue(name, out var section))
                {
                    orderedSections.Add(section);
                }
            }

            // 添加未定义的分组
            foreach (var kvp in sections)
            {
                if (!SectionMeta.ContainsKey(kvp.Key))
                {
                    orderedSections.Add(kvp.Value);
                }
            }

            return orderedSections;
        }

        /// <summary>
        /// 更新单个配置项
        /// </summary>
        public (bool Success, string Message) UpdateConfig(string key, string value)
        {
            try
            {
                var configType = typeof(Config);
                var prop = configType.GetProperty(key, BindingFlags.Public | BindingFlags.Static);

                if (prop == null)
                {
                    return (false, $"配置项 '{key}' 不存在");
                }

                // 类型转换
                object? convertedValue = ConvertValue(value, prop.PropertyType);
                if (convertedValue == null && prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null)
                {
                    return (false, $"无法将 '{value}' 转换为 {prop.PropertyType.Name} 类型");
                }

                // 设置值
                prop.SetValue(null, convertedValue);
                return (true, "配置已更新");
            }
            catch (Exception ex)
            {
                return (false, $"更新配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新配置
        /// </summary>
        public (bool Success, string Message, int UpdatedCount) UpdateConfigs(Dictionary<string, string> configs)
        {
            int updatedCount = 0;
            var errors = new List<string>();

            foreach (var kvp in configs)
            {
                var (success, message) = UpdateConfig(kvp.Key, kvp.Value);
                if (success)
                {
                    updatedCount++;
                }
                else
                {
                    errors.Add($"{kvp.Key}: {message}");
                }
            }

            if (errors.Count > 0)
            {
                return (false, $"部分配置更新失败:\n{string.Join("\n", errors)}", updatedCount);
            }

            return (true, $"成功更新 {updatedCount} 项配置", updatedCount);
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public (bool Success, string Message) SaveToFile()
        {
            try
            {
                ConfigReader.Save();
                SEnvir.Log("[Admin] 配置已保存到 Server.ini");
                return (true, "配置已保存到 Server.ini");
            }
            catch (Exception ex)
            {
                SEnvir.Log($"[Admin] 保存配置失败: {ex.Message}");
                return (false, $"保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public (bool Success, string Message) ReloadFromFile()
        {
            try
            {
                ConfigReader.Load();
                SEnvir.Log("[Admin] 配置已从 Server.ini 重新加载");
                return (true, "配置已重新加载");
            }
            catch (Exception ex)
            {
                SEnvir.Log($"[Admin] 加载配置失败: {ex.Message}");
                return (false, $"加载配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 值类型转换
        /// </summary>
        private object? ConvertValue(string value, Type targetType)
        {
            try
            {
                if (targetType == typeof(string))
                    return value;

                if (targetType == typeof(bool))
                    return value.ToLower() == "true" || value == "1" || value.ToLower() == "on";

                if (targetType == typeof(int))
                    return int.Parse(value);

                if (targetType == typeof(uint))
                    return uint.Parse(value);

                if (targetType == typeof(long))
                    return long.Parse(value);

                if (targetType == typeof(short))
                    return short.Parse(value);

                if (targetType == typeof(ushort))
                    return ushort.Parse(value);

                if (targetType == typeof(byte))
                    return byte.Parse(value);

                if (targetType == typeof(float))
                    return float.Parse(value);

                if (targetType == typeof(double))
                    return double.Parse(value);

                if (targetType == typeof(decimal))
                    return decimal.Parse(value);

                if (targetType == typeof(TimeSpan))
                {
                    // 支持多种格式：分钟数字、HH:mm:ss、d.HH:mm:ss
                    if (int.TryParse(value, out int minutes))
                        return TimeSpan.FromMinutes(minutes);
                    return TimeSpan.Parse(value);
                }

                if (targetType == typeof(DateTime))
                    return DateTime.Parse(value);

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, value, true);

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 格式化配置值用于显示
        /// </summary>
        public string FormatValue(object? value, Type type)
        {
            if (value == null) return "";

            if (type == typeof(TimeSpan))
            {
                var ts = (TimeSpan)value;
                if (ts.TotalDays >= 1)
                    return $"{ts.Days}天{ts.Hours}小时{ts.Minutes}分钟";
                if (ts.TotalHours >= 1)
                    return $"{ts.Hours}小时{ts.Minutes}分钟{ts.Seconds}秒";
                if (ts.TotalMinutes >= 1)
                    return $"{ts.Minutes}分钟{ts.Seconds}秒";
                return $"{ts.Seconds}秒";
            }

            if (type == typeof(DateTime))
            {
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (type == typeof(bool))
            {
                return (bool)value ? "是" : "否";
            }

            return value.ToString() ?? "";
        }

        /// <summary>
        /// 获取输入框类型
        /// </summary>
        public string GetInputType(Type type)
        {
            if (type == typeof(bool)) return "checkbox";
            if (type == typeof(int) || type == typeof(uint) || type == typeof(long) ||
                type == typeof(short) || type == typeof(ushort) || type == typeof(byte))
                return "number";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(DateTime)) return "datetime-local";
            if (type == typeof(TimeSpan)) return "text";
            return "text";
        }

        /// <summary>
        /// 获取配置项的原始值（用于表单）
        /// </summary>
        public string GetRawValue(object? value, Type type)
        {
            if (value == null) return "";

            if (type == typeof(TimeSpan))
            {
                var ts = (TimeSpan)value;
                return ts.ToString(@"hh\:mm\:ss");
            }

            if (type == typeof(DateTime))
            {
                return ((DateTime)value).ToString("yyyy-MM-ddTHH:mm");
            }

            if (type == typeof(bool))
            {
                return (bool)value ? "true" : "false";
            }

            return value.ToString() ?? "";
        }
    }
}
