using server.Core;
using Npgsql;

[HttpController]
public class Account
{
    [HttpPost]
    public string Register(string fullname, string email, string password)
    {
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();

            // 先检查邮箱是否已被注册
            using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email = @e", conn);
            checkCmd.Parameters.AddWithValue("e", email);
            long count = (long)checkCmd.ExecuteScalar();

            if (count > 0)
                return "<h1>注册失败</h1><p>该邮箱已被注册</p><a href='/register.html'>返回</a>";

            // 插入新用户
            using var cmd = new NpgsqlCommand(
                "INSERT INTO users (username, email, password) VALUES (@u, @e, @p)", conn);
            cmd.Parameters.AddWithValue("u", fullname);
            cmd.Parameters.AddWithValue("e", email);
            cmd.Parameters.AddWithValue("p", password); // 真实项目要哈希加密！
            cmd.ExecuteNonQuery();

            return "<h1>注册成功！</h1><a href='/web.html'>去登录</a>";
        }
        catch (Exception ex)
        {
            return $"<h1>服务器错误</h1><p>{ex.Message}</p>";
        }
    }

    [HttpPost]
    public string Login(string username, string password)
    {
        try
        {
            using var conn = Database.GetConnection();
            conn.Open();

            // 查数据库匹配账号密码
            using var cmd = new NpgsqlCommand(
                "SELECT username FROM users WHERE email = @e AND password = @p", conn);
            cmd.Parameters.AddWithValue("u", username);
            cmd.Parameters.AddWithValue("p", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string usernamer = reader.GetString(0);
                return $"<h1>登录成功！</h1><p>欢迎回来，{usernamer}</p><a href='/web.html'>返回</a>";
            }

            return "<h1>登录失败</h1><p>邮箱或密码错误</p><a href='/web.html'>重试</a>";
        }
        catch (Exception ex)
        {
            return $"<h1>服务器错误</h1><p>{ex.Message}</p>";
        }
    }

    [HttpPost]
    public string Forget(string email)
    {
        try
        {

            email = (email ?? "").Trim();

            if (email.Length == 0)
                return "<h1>找回失败</h1><p>请先填写邮箱地址</p><a href='/forget.html'>返回</a>";

            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email = @e", conn);
            cmd.Parameters.AddWithValue("e", email);
            long count = Convert.ToInt64(cmd.ExecuteScalar());

            if (count == 0)
                return "<h1>找回失败</h1><p>该邮箱未注册</p><a href='/forget.html'>返回</a>";

            // 2) 发验证码邮件（测试版：验证码固定 88888）
            MailService.SendPasswordResetCode(email, MailService.TestResetCode);
            Console.WriteLine($"[找回密码] 已向 {email} 发送验证码 {MailService.TestResetCode}（测试版固定）");

            // 3) 提示用户去收信
            return "<h1>验证码已发送</h1>"
                 + "<p>我们已向 " + System.Net.WebUtility.HtmlEncode(email) + " 发送了包含验证码的邮件，请查收（没收到记得看垃圾邮件）。</p>"
                 + "<a href='/forget.html'>返回</a>";
        }
        catch (Exception ex)
        {
            return $"<h1>服务器错误</h1><p>{ex.Message}</p>";
        }
    }
}