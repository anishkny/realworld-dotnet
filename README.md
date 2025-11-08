# Migrations

## Creating and Applying Migrations

After adding or modifying models, create a new migration:

```bash
dotnet ef migrations add <MigrationName> -o migrations/
```

Replace `<MigrationName>` with a descriptive name (e.g., `AddUserTable`).

Migrations will be automatically applied before starting the server, so you do not need to run them manually.
