# Unit tests

Test project:

```text
BattlegroundsGallery.UnitTests
```

Framework: MSTest 3.6.4 + Microsoft.Testing.Platform runner.

## Run

From the repository root:

```powershell
dotnet test tests\BattlegroundsGallery.UnitTests\BattlegroundsGallery.UnitTests.csproj
```

The ORM/repository tests connect to the local PostgreSQL database. Start it and
apply `db/schema.sql` + `db/seed.sql` first.

## Coverage

- Router GET/POST separation
- Path normalization
- MIME type mapping
- Template variables, HTML encoding, raw output
- Template if/else, foreach, nested loops and comparisons
- PBKDF2 password hashing and verification
- Card repository create/read/update/delete/search
- Feedback form validation

Current result: 14 tests, 14 passed.
