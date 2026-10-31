using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PersonalGoalTracker.Core;

namespace PersonalGoalTracker.Server
{
    class Program
    {
        static GoalService _service = new GoalService();

        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            Console.WriteLine("🚀 服务器已启动！请在浏览器打开: http://localhost:8080/goals");

            while (true)
            {
                var context = listener.GetContext();
                Task.Run(() => HandleRequest(context));
            }
        }

        static void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;
            string path = req.Url.AbsolutePath;
            string method = req.HttpMethod;

            try
            {
                //路由get
                // ================= 路由 1: GET /goals (列表与过滤) =================
                if (method == "GET" && path == "/goals")
                {
                    string period = req.QueryString["period"];
                    var goals = _service.GetAll(period);

                    string html = "<html><head><meta charset='utf-8'><title>目标追踪器</title></head><body>";
                    html += "<h1>目标列表</h1>";

                    // 提供一个简单的表单用于提交（测试 POST）
                    html += @"<form action='/goals/create' method='POST'>
                                <input type='text' name='title' placeholder='输入新目标(5-100字)' required>
                                <select name='period'>
                                    <option value='week'>周</option>
                                    <option value='month'>月</option>
                                    <option value='year'>年</option>
                                </select>
                                <button type='submit'>添加目标</button>
                             </form><hr>";

                    html += "<ul>";
                    foreach (var g in goals)
                    {
                        // 【防 XSS】一定要处理
                        string safeTitle = WebUtility.HtmlEncode(g.Title);
                        html += $"<li>[{g.Period}] {safeTitle} - 状态: {g.Status} <a href='/goals/details?id={g.Id}'>查看详情</a></li>";
                    }
                    html += "</ul>";

                    // 附加任务：统计信息
                    html += $"<h3>统计信息</h3><pre>{_service.GetStats()}</pre>";
                    html += "</body></html>";

                    SendResponse(res, 200, html);
                }
                else if (method == "GET" && path == "/goals/details")
                {
                    string idStr = req.QueryString["id"];
                    if (!int.TryParse(idStr, out int id))
                    {
                        SendResponse(res, 400, "<h1>400 - 参数错误</h1>");
                        return;
                    }

                    var goal = _service.GetById(id);
                    if (goal == null)
                    {
                        SendResponse(res, 404, "<h1>404 - 找不到该目标</h1>");
                        return;
                    }

                    string safeTitle = WebUtility.HtmlEncode(goal.Title);
                    string html = $"<html><body><h1>目标详情</h1><p>ID: {goal.Id}</p><p>标题: {safeTitle}</p><p>周期: {goal.Period}</p><p>状态: {goal.Status}</p><a href='/goals'>返回列表</a></body></html>";
                    SendResponse(res, 200, html);
                }
                // ================= 路由 3: POST /goals/create (创建) =================
                else if (method == "POST" && path == "/goals/create")
                {
                    var form = ParseFormData(req);
                    string title = form.ContainsKey("title") ? form["title"] : "";
                    string period = form.ContainsKey("period") ? form["period"] : "";

                    if (_service.Create(title, period, out string error))
                    {
                        // 成功：303 See Other 重定向
                        res.StatusCode = 303;
                        res.RedirectLocation = "/goals";
                        res.Close();
                    }
                    else
                    {
                        // 失败：400 Bad Request
                        SendResponse(res, 400, $"<h1>400 - 数据不合法</h1><p>{error}</p>");
                    }
                }
                // ================= 路由 4: PUT /goals/update?id=1 (更新) =================
                else if (method == "PUT" && path == "/goals/update")
                {
                    string idStr = req.QueryString["id"];
                    if (!int.TryParse(idStr, out int id))
                    {
                        SendResponse(res, 400, "<h1>400 - 缺少或错误ID</h1>");
                        return;
                    }

                    var form = ParseFormData(req);
                    string title = form.ContainsKey("title") ? form["title"] : "";
                    string period = form.ContainsKey("period") ? form["period"] : "";
                    string status = form.ContainsKey("status") ? form["status"] : "";

                    if (_service.Update(id, title, period, status, out string error))
                    {
                        // 成功：204 No Content
                        res.StatusCode = 204;
                        res.Close();
                    }
                    else
                    {
                        // 找不到返回404，数据错误返回400
                        if (error.Contains("找不到")) SendResponse(res, 404, "<h1>404 - 找不到目标</h1>");
                        else SendResponse(res, 400, $"<h1>400 - 数据不合法</h1><p>{error}</p>");
                    }
                }
                else
                {
                    SendResponse(res, 404, "<h1>404 - 路由不存在</h1>");
                }
            }
            catch (Exception ex)
            {
                SendResponse(res, 500, $"<h1>500 - 服务器内部错误</h1><p>{ex.Message}</p>");
            }
        }

        // 辅助方法：解析 x-www-form-urlencoded 格式的请求体
        static Dictionary<string, string> ParseFormData(HttpListenerRequest request)
        {
            var result = new Dictionary<string, string>();
            if (!request.HasEntityBody) return result;

            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                foreach (var pair in body.Split('&'))
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2)
                    {
                        result[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
                    }
                }
            }
            return result;
        }

        // 辅助方法：统一发送 HTML 响应
        static void SendResponse(HttpListenerResponse res, int statusCode, string html)
        {
            res.StatusCode = statusCode;
            res.ContentType = "text/html; charset=utf-8";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            res.ContentLength64 = buffer.Length;
            res.OutputStream.Write(buffer, 0, buffer.Length);
            res.Close();
        }
    }
}