using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;


namespace server.Core
{
    public static class MailService
    {
        private const string SmtpHost = "smtp.qq.com";
        private const int SmtpPort = 587;                          // QQ 的 STARTTLS 端口；SmtpClient 不支持 465
        private const string SmtpUser = "2169912271@qq.com";
        private const string SmtpPassword = "gfffkisbckgweagc";    // QQ 邮箱授权码
        private const string FromName = "Steam Support";
        private const int TimeoutMs = 20000;

        /// <summary>测试版固定验证码。以后要改成随机码，只改这一处。</summary>
        public const string TestResetCode = "88888";

        /// <summary>发送一封 HTML 邮件；失败会抛异常，由调用方提示。</summary>
        public static void Send(string to, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("收件人邮箱不能为空。", nameof(to));

            var from = new MailAddress(SmtpUser, FromName);
            var toAddress = new MailAddress(to);

            using var message = new MailMessage(from, toAddress)
            {
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = htmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };

            using var smtp = new SmtpClient(SmtpHost, SmtpPort)
            {
                EnableSsl = true,
                Timeout = TimeoutMs,
                Credentials = new NetworkCredential(SmtpUser, SmtpPassword),
            };

            smtp.Send(message);
        }

        /// <summary>发送"忘记密码"验证码邮件（测试版：验证码固定 88888）。</summary>
        public static void SendPasswordResetCode(string to, string code)
        {
            string body = @"
<div style=""font-family:Arial,'Microsoft YaHei',sans-serif;background:#171a21;color:#c6d4df;padding:28px;"">
  <h2 style=""margin:0 0 6px;color:#fff;font-size:20px;"">Steam 账号密码重置</h2>
  <p style=""margin:0 0 18px;font-size:13px;color:#8f98a0;"">您正在找回 Steam 账号密码，本次验证码如下：</p>

  <div style=""background:#1b2838;border-radius:4px;padding:18px;text-align:center;margin-bottom:18px;"">
    <span style=""font-size:34px;font-weight:bold;letter-spacing:8px;color:#66c0f4;"">" + code + @"</span>
  </div>

  <p style=""margin:0 0 6px;font-size:13px;"">请回到「找回密码」页面输入该验证码完成验证。</p>
  <p style=""margin:0;font-size:12px;color:#8f98a0;"">如果这不是您本人的操作，请忽略本邮件。</p>
</div>";

            Send(to, "【Steam】密码重置验证码", body);
        }
    }
}
