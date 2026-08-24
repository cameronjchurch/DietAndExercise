Resume checklist — DietAndExercise migration (short)

1. Confirm Postgres admin access and network reachability to 12.10.83.6 (Test-NetConnection -Port 5432).
2. Run DB creation script as admin:
   pwsh DietAndExercise\Scripts\Create-Postgres-DB.ps1
3. Apply EF migrations (set connection securely):
   $env:DIET_DB_CONN='Host=12.10.83.6;Database=diet_and_exercise;Username=<user>;Password=<pass>'
   dotnet ef database update --project DietAndExercise\DietAndExercise.csproj --context DietAndExerciseDbContext
4. Run importer (non-dry-run) to persist and move markdown to backup:
   dotnet run --project ImporterRunner (or call DataImporter via app) — ensure DIET_DB_CONN set.
5. Verify UI and data; take DB backup if expected. If any errors, restore from backup and debug importer logs.
6. Commit and push any local changes.

Notes:
- Dry-run CSV already created: C:\temp\diet_import_report.csv
- Migration SQL: Data/Migrations/InitialCreate_postgres.sql
- Scripts: DietAndExercise\Scripts\Create-Postgres-DB.ps1

Contact: run these on a trusted admin workstation; do not paste secrets into chat or commit them to repo.
