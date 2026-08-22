CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822111902_InitialCreate') THEN
    CREATE TABLE "DayRecords" (
        "Id" INTEGER NOT NULL,
        "Date" date NOT NULL,
        "CaloriesConsumed" INTEGER NOT NULL,
        "CaloriesBurned" INTEGER NOT NULL,
        "WeightLb" REAL NOT NULL,
        "FoodIntakeNotes" TEXT NOT NULL,
        "ExerciseNotes" TEXT NOT NULL,
        CONSTRAINT "PK_DayRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822111902_InitialCreate') THEN
    CREATE TABLE "ExerciseEntries" (
        "Id" INTEGER NOT NULL,
        "DayRecordId" INTEGER NOT NULL,
        "Name" TEXT NOT NULL,
        "Note" TEXT NOT NULL,
        CONSTRAINT "PK_ExerciseEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ExerciseEntries_DayRecords_DayRecordId" FOREIGN KEY ("DayRecordId") REFERENCES "DayRecords" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822111902_InitialCreate') THEN
    CREATE TABLE "FoodEntries" (
        "Id" INTEGER NOT NULL,
        "DayRecordId" INTEGER NOT NULL,
        "Category" TEXT NOT NULL,
        "Note" TEXT NOT NULL,
        CONSTRAINT "PK_FoodEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FoodEntries_DayRecords_DayRecordId" FOREIGN KEY ("DayRecordId") REFERENCES "DayRecords" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822111902_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_DayRecords_Date" ON "DayRecords" ("Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822111902_InitialCreate') THEN
    CREATE INDEX "IX_ExerciseEntries_DayRecordId" ON "ExerciseEntries" ("DayRecordId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822111902_InitialCreate') THEN
    CREATE INDEX "IX_FoodEntries_DayRecordId" ON "FoodEntries" ("DayRecordId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822111902_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260822111902_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

