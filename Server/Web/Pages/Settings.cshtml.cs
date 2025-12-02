using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Envir;
using Server.Web.Services;
using Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Web.Pages
{
    /// <summary>
    /// 配置管理页面 - 支持所有 Server.ini 配置项的查看和修改
    /// </summary>
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly ConfigService _configService;

        public SettingsModel(ConfigService configService)
        {
            _configService = configService;
        }

        // 消息提示
        public string? Message { get; set; }
        public string? MessageType { get; set; } = "info";

        // 配置数据
        public List<ConfigService.ConfigSectionInfo> ConfigSections { get; set; } = new();

        // 当前选中的Tab
        public string ActiveTab { get; set; } = "Network";

        // 服务
        public ConfigService ConfigServiceInstance => _configService;

        public void OnGet(string? tab)
        {
            if (!HasPermission(AccountIdentity.Admin))
            {
                Message = "权限不足，需要 Admin 或更高权限才能访问配置管理";
                MessageType = "danger";
                return;
            }

            // 设置当前Tab
            if (!string.IsNullOrEmpty(tab))
            {
                ActiveTab = tab;
            }

            // 加载所有配置
            ConfigSections = _configService.GetAllConfigSections();
        }

        /// <summary>
        /// 保存单个分组的配置
        /// </summary>
        public IActionResult OnPostSaveSection(string section, string? returnTab)
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限才能修改配置";
                MessageType = "danger";
                ConfigSections = _configService.GetAllConfigSections();
                return Page();
            }

            try
            {
                // 获取表单数据
                var configs = new Dictionary<string, string>();
                var configSections = _configService.GetAllConfigSections();
                var targetSection = configSections.FirstOrDefault(s => s.Name == section);

                if (targetSection == null)
                {
                    Message = $"配置分组 '{section}' 不存在";
                    MessageType = "danger";
                    ConfigSections = configSections;
                    return Page();
                }

                foreach (var item in targetSection.Items)
                {
                    string formKey = $"config_{item.Key}";
                    string? formValue;

                    // 布尔类型特殊处理（checkbox）
                    if (item.ValueType == typeof(bool))
                    {
                        formValue = Request.Form.ContainsKey(formKey) ? "true" : "false";
                    }
                    else
                    {
                        formValue = Request.Form[formKey].FirstOrDefault();
                    }

                    if (formValue != null)
                    {
                        configs[item.Key] = formValue;
                    }
                }

                // 批量更新
                var (success, message, count) = _configService.UpdateConfigs(configs);

                if (success)
                {
                    // 保存到文件
                    var (saveSuccess, saveMessage) = _configService.SaveToFile();
                    if (saveSuccess)
                    {
                        Message = $"成功保存 {count} 项配置到 Server.ini";
                        MessageType = "success";

                        // 记录日志
                        var operatorName = User.Identity?.Name ?? "Unknown";
                        SEnvir.Log($"[Admin] {operatorName} 更新了 [{section}] 分组的 {count} 项配置");
                    }
                    else
                    {
                        Message = $"配置已更新但保存文件失败: {saveMessage}";
                        MessageType = "warning";
                    }
                }
                else
                {
                    Message = message;
                    MessageType = "danger";
                }
            }
            catch (Exception ex)
            {
                Message = $"保存配置时发生错误: {ex.Message}";
                MessageType = "danger";
                SEnvir.Log($"[Admin] 保存配置异常: {ex}");
            }

            // 重新加载配置
            ActiveTab = returnTab ?? section;
            ConfigSections = _configService.GetAllConfigSections();
            return Page();
        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        public IActionResult OnPostSaveAll()
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                MessageType = "danger";
                ConfigSections = _configService.GetAllConfigSections();
                return Page();
            }

            try
            {
                var configs = new Dictionary<string, string>();
                var configSections = _configService.GetAllConfigSections();

                foreach (var section in configSections)
                {
                    foreach (var item in section.Items)
                    {
                        string formKey = $"config_{item.Key}";
                        string? formValue;

                        if (item.ValueType == typeof(bool))
                        {
                            formValue = Request.Form.ContainsKey(formKey) ? "true" : "false";
                        }
                        else
                        {
                            formValue = Request.Form[formKey].FirstOrDefault();
                        }

                        if (formValue != null)
                        {
                            configs[item.Key] = formValue;
                        }
                    }
                }

                var (success, message, count) = _configService.UpdateConfigs(configs);

                if (success || count > 0)
                {
                    var (saveSuccess, saveMessage) = _configService.SaveToFile();
                    if (saveSuccess)
                    {
                        Message = $"成功保存 {count} 项配置";
                        MessageType = "success";

                        var operatorName = User.Identity?.Name ?? "Unknown";
                        SEnvir.Log($"[Admin] {operatorName} 保存了所有配置 ({count} 项)");
                    }
                    else
                    {
                        Message = $"更新了 {count} 项配置，但保存文件失败: {saveMessage}";
                        MessageType = "warning";
                    }
                }
                else
                {
                    Message = message;
                    MessageType = "danger";
                }
            }
            catch (Exception ex)
            {
                Message = $"保存配置时发生错误: {ex.Message}";
                MessageType = "danger";
                SEnvir.Log($"[Admin] 保存所有配置异常: {ex}");
            }

            ConfigSections = _configService.GetAllConfigSections();
            return Page();
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public IActionResult OnPostReload()
        {
            if (!HasPermission(AccountIdentity.SuperAdmin))
            {
                Message = "权限不足，需要 SuperAdmin 权限";
                MessageType = "danger";
                ConfigSections = _configService.GetAllConfigSections();
                return Page();
            }

            var (success, message) = _configService.ReloadFromFile();
            Message = message;
            MessageType = success ? "success" : "danger";

            if (success)
            {
                var operatorName = User.Identity?.Name ?? "Unknown";
                SEnvir.Log($"[Admin] {operatorName} 重新加载了配置文件");
            }

            ConfigSections = _configService.GetAllConfigSections();
            return Page();
        }

        /// <summary>
        /// 权限检查
        /// </summary>
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
