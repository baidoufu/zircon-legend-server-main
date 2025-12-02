using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Envir;
using Server.Web.Services;
using System.Security.Claims;

namespace Server.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AdminAuthService _authService;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // 是否显示创建默认超管按钮
        public bool ShowCreateSuperAdmin { get; set; }

        // 默认超管邮箱
        public string DefaultSuperAdminEmail => SEnvir.SuperAdmin;

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public LoginModel(AdminAuthService authService)
        {
            _authService = authService;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Dashboard");
            }

            // 检查默认超管账户是否存在
            ShowCreateSuperAdmin = !_authService.DefaultSuperAdminExists();

            return Page();
        }

        /// <summary>
        /// 创建默认超管账户
        /// </summary>
        public IActionResult OnPostCreateSuperAdmin()
        {
            var (success, message) = _authService.CreateDefaultSuperAdmin("123456");

            if (success)
            {
                SuccessMessage = $"{message}，默认密码为 123456，请登录后及时修改密码！";
                ShowCreateSuperAdmin = false;
            }
            else
            {
                ErrorMessage = message;
                ShowCreateSuperAdmin = !_authService.DefaultSuperAdminExists();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 检查默认超管账户状态
            ShowCreateSuperAdmin = !_authService.DefaultSuperAdminExists();

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入邮箱和密码";
                return Page();
            }

            var account = _authService.ValidateLogin(Email, Password);
            if (account == null)
            {
                ErrorMessage = "登录失败：账户不存在、密码错误或权限不足";
                return Page();
            }

            // 创建 Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, account.EMailAddress ?? ""),
                new Claim(ClaimTypes.Email, account.EMailAddress ?? ""),
                new Claim(ClaimTypes.Role, account.Identify.ToString()),
                new Claim("Permission", ((int)account.Identify).ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            return RedirectToPage("/Dashboard");
        }
    }
}
