# 酒馆战棋随从图鉴 · Web 开发技术学期作业

一个模仿《炉石传说》酒馆战棋图鉴的课程项目。使用自研 HttpListener 服务器、模板引擎、ORM 和 PostgreSQL。

## 当前状态

- 214 张随从卡牌已存入 PostgreSQL
- 卡牌列表、等级/种族筛选、AJAX 关键词搜索和卡牌详情页
- 已完成 HttpListener 服务器、路由、POST 表单、Cookie 和 Session
- 已完成自研模板引擎：变量、循环、条件、嵌套
- 已完成自研 ORM 和 Repository：CRUD、事务、参数化查询
- 管理员登录、权限检查、卡牌新增/编辑/删除
- 留言反馈表单、客户端/服务端校验
- 14 个 MSTest 单元测试
- Docker 文件已提供，但本机未安装 Docker，未做容器运行验证

## 环境要求

- Windows 10/11
- .NET 9 SDK
- Visual Studio 2022 17.12 或更高版本（支持 .NET 9）
- 不使用 ASP.NET Core MVC

## 启动服务器

1. 用 Visual Studio 2022 打开解决方案：

   ```text
   BattlegroundsGallery.sln
   ```

2. 在解决方案资源管理器中右键 `BattlegroundsGallery.Web`，选择 **设为启动项目**。

3. 第一次运行时，需要给当前 Windows 用户注册本地 HttpListener 地址。  
   以管理员身份打开 PowerShell，在项目根目录运行：

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\Setup-UrlAcl.ps1
   ```

   这个脚本只需要运行一次。它执行的是：

   ```text
   netsh http add urlacl url=http://localhost:5080/ user=当前用户
   ```

4. 按 `F5` 启动，浏览器打开：

   ```text
   http://localhost:5080/
   ```

   可用接口：

   ```text
   GET /health
   GET /api/status
   ```

如果不想配置 URL ACL，也可以直接以管理员身份运行 Visual Studio，但推荐使用一次性的 URL 注册脚本。

### 关于当前机器的兼容模式

有些 Windows 安装会把 `HttpListener.IsSupported` 报告为 `false`，此时 `HttpListener.Start()` 会直接抛出“句柄无效”。程序检测到这种情况后，会自动启动 `TcpListener` 兼容服务器，仍然能打开同一个图鉴页面：

```text
HttpListener.IsSupported = false on this Windows installation.
Starting the TcpListener compatibility server instead.
```

兼容模式只用于本地调试和展示。项目的正式服务器实现仍然是 `BattlegroundsGallery.Server/HttpServer.cs` 中的 HttpListener；在支持 HttpListener 的机器上会自动使用它。

### 命令行运行

```powershell
dotnet build BattlegroundsGallery.sln
dotnet run --project src\BattlegroundsGallery.Web\BattlegroundsGallery.Web.csproj
```

默认端口是 `5080`。可以通过参数修改：

```powershell
dotnet run --project src\BattlegroundsGallery.Web\BattlegroundsGallery.Web.csproj -- --port 5090
```

## 数据库

项目使用本机 PostgreSQL 18，数据库名 `battlegrounds_gallery`，应用账号：

```text
username: bg_app
password: bg_dev_password
```

首次创建数据库：

```powershell
.\scripts\Setup-Database.ps1
```

脚本会提示输入 PostgreSQL 的 `postgres` 密码，然后自动创建账号、数据库、表结构并导入 214 张卡牌。

也可以手动执行：

```powershell
$env:PGPASSWORD = "bg_dev_password"
psql -h 127.0.0.1 -U bg_app -d battlegrounds_gallery -f db/schema.sql
psql -h 127.0.0.1 -U bg_app -d battlegrounds_gallery -f db/seed.sql
```

数据库连接字符串位于：

```text
src/BattlegroundsGallery.Data/DatabaseOptions.cs
```

可通过环境变量 `BATTLEGROUNDS_DB` 覆盖。

种子管理员账号：

```text
username: admin
password: admin123
```

## 测试

测试项目：

```text
tests/BattlegroundsGallery.UnitTests
```

运行：

```powershell
dotnet test tests\BattlegroundsGallery.UnitTests\BattlegroundsGallery.UnitTests.csproj
```

当前覆盖 14 个测试，包括路由、模板引擎、密码哈希、Repository CRUD 和表单校验。

## Docker

需要安装 Docker Desktop，并使用 **Linux containers**。

在项目根目录执行：

```powershell
docker compose up --build
```

启动后访问：

```text
http://localhost:5080/
http://localhost:5080/cards
http://localhost:5080/feedback
```

Compose 会启动：

- `db`：PostgreSQL 18
- `web`：.NET 9 应用

数据库首次启动时会自动执行：

```text
db/schema.sql
db/seed.sql
```

停止容器：

```powershell
docker compose down
```

如果需要同时删除数据库数据卷：

```powershell
docker compose down -v
```

> Docker 运行使用 Linux 容器和 .NET 的跨平台 HttpListener 实现；Windows 本机运行仍使用 Windows HTTP.sys。

## 项目结构

```text
BattlegroundsGallery.sln
src/
  BattlegroundsGallery.Server/    HttpListener、路由、静态文件、异常处理
  BattlegroundsGallery.Mvc/       Controller / ActionResult 骨架
  BattlegroundsGallery.Templating/自研模板引擎
  BattlegroundsGallery.Orm/       自研 ORM：映射、CRUD、事务、参数化
  BattlegroundsGallery.Data/      数据模型、Repository、数据库配置
  BattlegroundsGallery.Web/       启动项目、控制器、视图、wwwroot
    wwwroot/                      现有酒馆战棋图鉴页面和 214 张卡图
    Views/                        服务端渲染视图
scripts/
  Setup-UrlAcl.ps1                本地 HttpListener 地址注册
  Setup-Database.ps1              创建数据库并导入数据
db/
  schema.sql                      数据库表、主键、外键、索引
  seed.sql                        10 个种族、214 张卡、管理员账号
tests/
  BattlegroundsGallery.UnitTests/ MSTest 单元测试
Dockerfile                        应用镜像
docker-compose.yml                应用 + PostgreSQL
README.md
```

## 模板引擎语法

目前 `BattlegroundsGallery.Templating` 已支持变量、循环和条件。

```html
<h1>{{ model.Title }}</h1>

@if (model.Cards.Count > 0)
  <p>共有 {{ model.Cards.Count }} 张卡牌</p>
@else
  <p>没有卡牌</p>
@endif

@foreach (var card in model.Cards)
  <li>{{ card.Name }} / {{ card.Attack }}</li>
@endforeach
```

- `{{ value }}` 输出并自动进行 HTML 转义
- `{{{ value }}}` 输出原始 HTML
- `@if (...) ... @else ... @endif`
- `@foreach (var item in collection) ... @endforeach`
- 支持嵌套循环和条件
- 条件支持 `!`、`==`、`!=`、`>`、`>=`、`<`、`<=`
- 输出表达式支持属性路径，例如 `{{ card.Name }}`

## 下一步

1. 后台卡牌描述接入 Quill WYSIWYG。
2. 补充后台操作提示和删除确认。
3. 进行最终验收测试。
4. 初始化 Git 仓库并提交。
5. 如环境允许，补做 Docker 容器运行验证。

> 卡牌名称、数值和图片素材仅用于课堂练习与界面演示，版权归暴雪娱乐所有。
