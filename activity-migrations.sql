CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623080711_InitialActivityCreate') THEN
    CREATE TABLE "ActivityLogs" (
        "Id" uuid NOT NULL,
        "WorkspaceId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ActionType" character varying(50) NOT NULL,
        "EntityType" character varying(100) NOT NULL,
        "EntityId" uuid NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "Metadata" jsonb,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ActivityLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623080711_InitialActivityCreate') THEN
    CREATE INDEX "IX_ActivityLogs_ActionType" ON "ActivityLogs" ("ActionType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623080711_InitialActivityCreate') THEN
    CREATE INDEX "IX_ActivityLogs_CreatedAt" ON "ActivityLogs" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623080711_InitialActivityCreate') THEN
    CREATE INDEX "IX_ActivityLogs_UserId" ON "ActivityLogs" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623080711_InitialActivityCreate') THEN
    CREATE INDEX "IX_ActivityLogs_WorkspaceId" ON "ActivityLogs" ("WorkspaceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623080711_InitialActivityCreate') THEN
    CREATE INDEX "IX_ActivityLogs_WorkspaceId_CreatedAt" ON "ActivityLogs" ("WorkspaceId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623080711_InitialActivityCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260623080711_InitialActivityCreate', '10.0.9');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623105204_MakeActivityWorkspaceNullable') THEN
    ALTER TABLE "ActivityLogs" ALTER COLUMN "WorkspaceId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623105204_MakeActivityWorkspaceNullable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260623105204_MakeActivityWorkspaceNullable', '10.0.9');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623105744_AddActivityProcessedEvents') THEN
    CREATE TABLE "ProcessedEvents" (
        "EventId" uuid NOT NULL,
        "EventType" character varying(200) NOT NULL,
        "ProcessedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ProcessedEvents" PRIMARY KEY ("EventId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623105744_AddActivityProcessedEvents') THEN
    CREATE INDEX "IX_ProcessedEvents_EventType" ON "ProcessedEvents" ("EventType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623105744_AddActivityProcessedEvents') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260623105744_AddActivityProcessedEvents', '10.0.9');
    END IF;
END $EF$;
COMMIT;

