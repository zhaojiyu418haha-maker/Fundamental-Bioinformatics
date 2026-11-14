
using System;
using System.Net;
using System.Net.Mail;



namespace server.Core
{
    [HttpController]
    public class SteamController
    {
        //Get 
        [HttpPost]
        public string Send( string to,string hero)
        {
            try
            {
                // отправитель - устанавливаем адрес и отображаемое в письме имя
                MailAddress from = new MailAddress("2169912271@qq.com", "zhao");
                // кому отправляем
                MailAddress toAddres = new MailAddress(to);
                // создаем объект сообщения
                MailMessage m = new MailMessage(from, toAddres)

                {
                    Subject = "тест",
                    Body = "<h2>Письмо-тест работы smtp-клиента</h2>",
                    IsBodyHtml = true,
                };
                // QQ 邮箱 SMTP
                using var smtp = new SmtpClient("smtp.qq.com", 587);
                {
                    smtp.Credentials = new NetworkCredential("2169912271@qq.com", "gfffkisbckgweagc");
                    smtp.EnableSsl = true;
                }
                

                smtp.Send(m);

                return "<h1>邮件已发送</h1><a href='/mailtest.html'>返回</a>";
            }
            catch (Exception ex)
            {
                return $"<h1>发送失败</h1><p>{ex.Message}</p>";
            }
            
        }
    }
}
