using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PersonalGoalTracker.Core;

namespace PersonalGoalTracker.Core
{
    public class GoalService
    {
        private readonly List<Goal> _goals = new List<Goal>();
        private int _nextId = 1;
        private readonly object _lock = new object();
        public bool Create(string title, string period, out string error)
        {
            error = null;
            // 题目要求：标题 5-100 字符
            if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 100)
            {
                error = "标题长度必须在 5 到 100 个字符之间";
                return false;
            }
            // 题目要求：枚举值验证
            if (period != "week" && period != "month" && period != "year")
            {
                error = "周期只能是 week, month 或 year";
                return false;
            }

            lock (_lock)
            {
                _goals.Add(new Goal { Id = _nextId++, Title = title, Period = period, Status = "active" });
            }
            return true;
        }
        public List<Goal> GetAll(string filterPeriod)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(filterPeriod))
                    return _goals.ToList();
                return _goals.Where(g => g.Period == filterPeriod).ToList();
            }
        }

        // 3. 获取单个 (找不到返回 null)
        public Goal GetById(int id)
        {
            lock (_lock) { return _goals.FirstOrDefault(g => g.Id == id); }
        }

        // 4. 更新 (验证数据是否存在及合法)
        public bool Update(int id, string title, string period, string status, out string error)
        {
            error = null;
            var goal = GetById(id);
            if (goal == null) { error = "找不到该目标"; return false; }

            if (title.Length < 5 || title.Length > 100) { error = "标题长度不对"; return false; }
            if (period != "week" && period != "month" && period != "year") { error = "周期不对"; return false; }
            if (status != "active" && status != "completed") { error = "状态不对"; return false; }

            lock (_lock)
            {
                goal.Title = title;
                goal.Period = period;
                goal.Status = status;
            }
            return true;
        }
        public string GetStats()
        {
            lock (_lock)
            {
                // 简单拼一段字符串，你后面可以在 HTML 里展示
                var stats = "统计信息：\n";
                foreach (var p in new[] { "week", "month", "year" })
                {
                    int active = _goals.Count(g => g.Period == p && g.Status == "active");
                    int completed = _goals.Count(g => g.Period == p && g.Status == "completed");
                    stats += $"[{p}] 未完成: {active}, 已完成: {completed}\n";
                }
                return stats;
            }
        }
    }
}
