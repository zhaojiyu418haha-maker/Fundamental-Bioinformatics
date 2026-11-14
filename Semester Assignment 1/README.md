# Каталог существ Battlegrounds · Семестровая работа по веб-разработке

Учебный проект — каталог существ в стиле Hearthstone Battlegrounds. Используются собственный HTTP-сервер на `HttpListener`, шаблонизатор, ORM и PostgreSQL.

## Текущее состояние

- 214 карт существ сохранены в PostgreSQL
- Список карт, фильтрация по уровню и племени, AJAX-поиск и страница карты
- Реализованы HTTP-сервер на `HttpListener`, маршрутизация, POST-формы, Cookie и Session
- Реализован собственный шаблонизатор: переменные, циклы, условия, вложенность
- Реализованы собственная ORM и Repository: CRUD, транзакции, параметризованные запросы
- Вход администратора, проверка прав, добавление/редактирование/удаление карт
- Форма обратной связи, клиентская и серверная валидация
- 14 модульных тестов MSTest
- Файлы Docker добавлены, но Docker на локальной машине не установлен, запуск контейнера не проверялся

## Требования к окружению

- Windows 10/11
- .NET 9 SDK
- Visual Studio 2022 версии 17.12 или выше (с поддержкой .NET 9)
- ASP.NET Core MVC не используется

## Запуск сервера

1. Откройте решение в Visual Studio 2022:

   ```text
   BattlegroundsGallery.sln
   ```

2. В обозревателе решений щёлкните правой кнопкой по `BattlegroundsGallery.Web` и выберите **Назначить запускаемым проектом**.

3. При первом запуске нужно зарегистрировать локальный адрес `HttpListener` для текущего пользователя Windows.  
   Откройте PowerShell от имени администратора и выполните в корне проекта:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\Setup-UrlAcl.ps1
   ```

   Скрипт нужно запустить только один раз. Он выполняет:

   ```text
   netsh http add urlacl url=http://localhost:5080/ user=ИМЯ_ПОЛЬЗОВАТЕЛЯ
   ```

4. Нажмите `F5` и откройте в браузере:

   ```text
   http://localhost:5080/
   ```

   Доступные endpoints:

   ```text
   GET /health
   GET /api/status
   ```

Если не хотите настраивать URL ACL, можно запустить Visual Studio от имени администратора, но рекомендуется один раз выполнить скрипт регистрации URL.

### Режим совместимости на текущей машине

В некоторых установках Windows `HttpListener.IsSupported` возвращает `false`, и тогда `HttpListener.Start()` выбрасывает ошибку «неверный дескриптор». Программа обнаруживает это и автоматически запускает совместимый сервер на `TcpListener`, который отдаёт ту же страницу каталога:

```text
HttpListener.IsSupported = false on this Windows installation.
Starting the TcpListener compatibility server instead.
```

Режим совместимости предназначен только для локальной отладки и демонстрации. Основная реализация сервера — `HttpListener` в `BattlegroundsGallery.Server/HttpServer.cs`; на машинах с поддержкой `HttpListener` используется именно она.

### Запуск из командной строки

```powershell
dotnet build BattlegroundsGallery.sln
dotnet run --project src\BattlegroundsGallery.Web\BattlegroundsGallery.Web.csproj
```

Порт по умолчанию — `5080`. Его можно изменить параметром:

```powershell
dotnet run --project src\BattlegroundsGallery.Web\BattlegroundsGallery.Web.csproj -- --port 5090
```

## База данных

Проект использует локальный PostgreSQL 18, база данных `battlegrounds_gallery`, учётная запись приложения:

```text
username: bg_app
password: bg_dev_password
```

Первое создание базы данных:

```powershell
.\scripts\Setup-Database.ps1
```

Скрипт запросит пароль пользователя `postgres` в PostgreSQL, затем создаст учётную запись, базу данных, таблицы и импортирует 214 карт.

Можно выполнить вручную:

```powershell
$env:PGPASSWORD = "bg_dev_password"
psql -h 127.0.0.1 -U bg_app -d battlegrounds_gallery -f db/schema.sql
psql -h 127.0.0.1 -U bg_app -d battlegrounds_gallery -f db/seed.sql
```

Строка подключения к базе данных находится в:

```text
src/BattlegroundsGallery.Data/DatabaseOptions.cs
```

Её можно переопределить переменной окружения `BATTLEGROUNDS_DB`.

Начальная учётная запись администратора:

```text
username: admin
password: admin123
```

## Тестирование

Тестовый проект:

```text
tests/BattlegroundsGallery.UnitTests
```

Запуск:

```powershell
dotnet test tests\BattlegroundsGallery.UnitTests\BattlegroundsGallery.UnitTests.csproj
```

Сейчас покрыто 14 тестов: маршрутизация, шаблонизатор, хеширование паролей, CRUD в Repository и валидация форм.

## Docker

Требуется установленный Docker Desktop с **Linux-контейнерами**.

В корне проекта выполните:

```powershell
docker compose up --build
```

После запуска откройте:

```text
http://localhost:5080/
http://localhost:5080/cards
http://localhost:5080/feedback
```

Compose запускает:

- `db`: PostgreSQL 18
- `web`: приложение .NET 9

При первом запуске базы данных автоматически выполняются:

```text
db/schema.sql
db/seed.sql
```

Остановка контейнеров:

```powershell
docker compose down
```

Если нужно также удалить том с данными базы:

```powershell
docker compose down -v
```

> В Docker используется Linux-контейнер и кроссплатформенная реализация `HttpListener` из .NET; при локальном запуске в Windows используется Windows HTTP.sys.

## Структура проекта

```text
BattlegroundsGallery.sln
src/
  BattlegroundsGallery.Server/    HttpListener, маршрутизация, статические файлы, обработка исключений
  BattlegroundsGallery.Mvc/       Controller / ActionResult
  BattlegroundsGallery.Templating/Собственный шаблонизатор
  BattlegroundsGallery.Orm/       Собственная ORM: отображение, CRUD, транзакции, параметризация
  BattlegroundsGallery.Data/      Модели данных, Repository, конфигурация БД
  BattlegroundsGallery.Web/       Запускаемый проект, контроллеры, представления, wwwroot
    wwwroot/                      Страницы каталога и 214 изображений карт
    Views/                        Представления, рендерируемые сервером
scripts/
  Setup-UrlAcl.ps1                Регистрация локального адреса HttpListener
  Setup-Database.ps1              Создание БД и импорт данных
db/
  schema.sql                      Таблицы, первичные и внешние ключи, индексы
  seed.sql                        10 племён, 214 карт, учётная запись администратора
tests/
  BattlegroundsGallery.UnitTests/ Модульные тесты MSTest
Dockerfile                        Образ приложения
docker-compose.yml                Приложение + PostgreSQL
README.md
```

## Синтаксис шаблонизатора

Сейчас `BattlegroundsGallery.Templating` поддерживает переменные, циклы и условия.

```html
<h1>{{ model.Title }}</h1>

@if (model.Cards.Count > 0)
  <p>Всего карт: {{ model.Cards.Count }}</p>
@else
  <p>Карт нет</p>
@endif

@foreach (var card in model.Cards)
  <li>{{ card.Name }} / {{ card.Attack }}</li>
@endforeach
```

- `{{ value }}` выводит значение с автоматическим HTML-экранированием
- `{{{ value }}}` выводит исходный HTML
- `@if (...) ... @else ... @endif`
- `@foreach (var item in collection) ... @endforeach`
- Поддерживаются вложенные циклы и условия
- В условиях поддерживаются `!`, `==`, `!=`, `>`, `>=`, `<`, `<=`
- В выражениях можно обращаться к свойствам, например `{{ card.Name }}`

## Дальнейшие шаги

1. Подключить Quill WYSIWYG для описания карт в административной панели.
2. Добавить уведомления об операциях и подтверждение удаления.
3. Провести финальное приёмочное тестирование.
4. Инициализировать Git-репозиторий и зафиксировать изменения.
5. При наличии окружения проверить запуск в Docker.

> Названия карт, характеристики и изображения используются только для учебной демонстрации; права принадлежат Blizzard Entertainment.
