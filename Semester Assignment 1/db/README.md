# Database setup

The project uses PostgreSQL 18 with a custom ORM.

## Local connection

```text
Host=127.0.0.1
Port=5432
Database=battlegrounds_gallery
Username=bg_app
Password=bg_dev_password
```

The connection string is in:

```text
src/BattlegroundsGallery.Data/DatabaseOptions.cs
```

It can be overridden with the `BATTLEGROUNDS_DB` environment variable.

## Create and seed the database

From a normal PowerShell window:

```powershell
.\scripts\Setup-Database.ps1
```

The script asks for the password of the PostgreSQL `postgres` user. It:

1. Creates the application role `bg_app` if needed.
2. Creates the `battlegrounds_gallery` database if needed.
3. Runs `schema.sql`.
4. Runs `seed.sql`.

## Apply manually

```powershell
$env:PGPASSWORD = "bg_dev_password"

psql -h 127.0.0.1 -U bg_app -d battlegrounds_gallery -f db/schema.sql
psql -h 127.0.0.1 -U bg_app -d battlegrounds_gallery -f db/seed.sql
```

## Tables

- `users` — administrator/user accounts
- `cards` — 214 minions
- `tribes` — 10 Battlegrounds tribes
- `cards_tribes` — many-to-many card/tribe relation
- `feedback` — user feedback form data

Seed data includes an administrator account:

```text
username: admin
password: admin123
role: admin
```
