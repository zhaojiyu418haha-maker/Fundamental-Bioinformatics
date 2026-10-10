using Npgsql;

namespace server.Core
{
    public class Database
    {
        // ⚠️ 注意修改这里的连接信息，改成你自己的密码！
        private static readonly string ConnString = 
            "Host=localhost;Port=5432;Database=retail;Username=postgres;Password=mypassword";

        // 获取数据库连接对象
        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(ConnString);
        }
    }
}