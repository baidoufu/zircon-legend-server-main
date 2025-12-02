using Server.DBModels;
using Server.Envir;
using System;
using System.Security.Cryptography;
using System.Text;
using Library;

namespace Server.Web.Services
{
    /// <summary>
    /// 管理员认证服务
    /// </summary>
    public class AdminAuthService
    {
        // 与 SEnvir 保持一致的密码加密参数
        private const int Iterations = 1354;
        private const int SaltSize = 16;
        private const int HashSize = 20;

        /// <summary>
        /// 验证管理员登录
        /// </summary>
        /// <param name="email">账户邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>验证成功返回 AccountInfo，失败返回 null</returns>
        public AccountInfo? ValidateLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine($"[Admin Auth] 邮箱或密码为空");
                return null;
            }

            var account = GetAccountByEmail(email);
            if (account == null)
            {
                Console.WriteLine($"[Admin Auth] 账户不存在: {email}");
                return null;
            }

            Console.WriteLine($"[Admin Auth] 找到账户: {email}, 权限: {account.Identify}");

            // 验证密码 - 尝试多种方式
            bool passwordValid = false;

            // 方式1: 使用 RealPassword (新系统) - 使用 MD5(email-password) 作为输入
            if (!passwordValid && (account.RealPassword?.Length ?? 0) > 0)
            {
                var passwordInput = Functions.CalcMD5($"{email}-{password}");
                passwordValid = PasswordMatchPBKDF2(passwordInput, account.RealPassword);
                Console.WriteLine($"[Admin Auth] RealPassword 验证: {passwordValid}");
            }

            // 方式2: 使用旧的 Password 字段 - 直接使用密码
            if (!passwordValid && (account.Password?.Length ?? 0) > 0)
            {
                passwordValid = PasswordMatchPBKDF2(password, account.Password);
                Console.WriteLine($"[Admin Auth] 旧Password 验证: {passwordValid}");
            }

            if (!passwordValid)
            {
                Console.WriteLine($"[Admin Auth] 密码验证失败");
                return null;
            }

            // 验证权限 - 必须是 Supervisor 或以上
            if (!HasAdminAccess(account))
            {
                Console.WriteLine($"[Admin Auth] 权限不足: {account.Identify} < Supervisor");
                return null;
            }

            Console.WriteLine($"[Admin Auth] 登录成功: {email}");
            return account;
        }

        /// <summary>
        /// PBKDF2 密码验证 - 与 SEnvir.PasswordMatch 保持一致
        /// </summary>
        private bool PasswordMatchPBKDF2(string password, byte[]? totalHash)
        {
            if (totalHash == null || totalHash.Length < SaltSize + HashSize)
                return false;

            // 从存储的哈希中提取盐值
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(totalHash, 0, salt, 0, SaltSize);

            // 使用相同的盐值和迭代次数计算哈希
            using (var rfc = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                byte[] hash = rfc.GetBytes(HashSize);

                // 比较哈希部分 (从 SaltSize 位置开始)
                return Functions.IsMatch(totalHash, hash, SaltSize);
            }
        }

        /// <summary>
        /// 检查账户是否有管理后台访问权限
        /// </summary>
        public bool HasAdminAccess(AccountInfo? account)
        {
            if (account == null) return false;
            return account.Identify >= AccountIdentity.Supervisor;
        }

        /// <summary>
        /// 检查是否有指定权限
        /// </summary>
        public bool HasPermission(AccountInfo? account, AccountIdentity required)
        {
            if (account == null) return false;
            return account.Identify >= required;
        }

        /// <summary>
        /// 根据邮箱获取账户
        /// </summary>
        public AccountInfo? GetAccountByEmail(string email)
        {
            try
            {
                if (SEnvir.AccountInfoList?.Binding == null) return null;

                for (int i = 0; i < SEnvir.AccountInfoList.Count; i++)
                {
                    var account = SEnvir.AccountInfoList[i];
                    if (string.Compare(account.EMailAddress, email, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return account;
                    }
                }
            }
            catch
            {
                // 防止遍历异常
            }
            return null;
        }

        /// <summary>
        /// 检查默认超管账户是否存在
        /// </summary>
        public bool DefaultSuperAdminExists()
        {
            return GetAccountByEmail(SEnvir.SuperAdmin) != null;
        }

        /// <summary>
        /// 创建默认超管账户
        /// </summary>
        /// <param name="password">密码，默认为 123456</param>
        /// <returns>创建是否成功</returns>
        public (bool Success, string Message) CreateDefaultSuperAdmin(string password = "123456")
        {
            try
            {
                if (SEnvir.AccountInfoList?.Binding == null)
                {
                    return (false, "数据库未初始化");
                }

                // 检查是否已存在
                if (DefaultSuperAdminExists())
                {
                    return (false, "默认超管账户已存在");
                }

                // 创建新账户
                var account = SEnvir.AccountInfoList.CreateNewObject();
                account.EMailAddress = SEnvir.SuperAdmin;
                account.Password = SEnvir.CreateHash(password);
                account.RealPassword = SEnvir.CreateHash(Functions.CalcMD5($"{account.EMailAddress}-{password}"));
                account.RealName = "SuperAdmin";
                account.BirthDate = DateTime.Now;
                account.CreationIP = "127.0.0.1";
                account.CreationDate = SEnvir.Now;
                account.Activated = true;
                account.Identify = AccountIdentity.SuperAdmin;

                SEnvir.Log($"[Admin] 创建默认超管账户: {SEnvir.SuperAdmin}");
                return (true, $"默认超管账户创建成功: {SEnvir.SuperAdmin}");
            }
            catch (Exception ex)
            {
                SEnvir.Log($"[Admin] 创建超管账户失败: {ex.Message}");
                return (false, $"创建失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 修改账户权限等级
        /// </summary>
        public (bool Success, string Message) SetAccountPermission(string email, AccountIdentity newPermission)
        {
            try
            {
                var account = GetAccountByEmail(email);
                if (account == null)
                {
                    return (false, $"账户 {email} 不存在");
                }

                var oldPermission = account.Identify;
                account.Identify = newPermission;

                SEnvir.Log($"[Admin] 修改账户权限: {email}, {oldPermission} -> {newPermission}");
                return (true, $"账户 {email} 权限已修改为 {newPermission}");
            }
            catch (Exception ex)
            {
                return (false, $"修改失败: {ex.Message}");
            }
        }

    }
}
