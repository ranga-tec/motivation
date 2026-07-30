using Microsoft.EntityFrameworkCore;

namespace Poms.Infrastructure.Data;

public static class PostgresSchemaUpgrader
{
    public static async Task ApplyAsync(PomsDbContext context)
    {
        if (!(context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false))
            return;

        await context.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Episodes" ADD COLUMN IF NOT EXISTS "IsRestricted" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Assessments" ADD COLUMN IF NOT EXISTS "IsRestricted" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Fittings" ADD COLUMN IF NOT EXISTS "IsRestricted" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "IsRestricted" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "FollowUps" ADD COLUMN IF NOT EXISTS "IsRestricted" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "PatientDocuments" ADD COLUMN IF NOT EXISTS "IsRestricted" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "EpisodeDocuments" ADD COLUMN IF NOT EXISTS "IsRestricted" boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "AssignedClinicianUserId" text;
            ALTER TABLE "Appointments" ADD COLUMN IF NOT EXISTS "AssignedClinicianName" character varying(200);

            CREATE TABLE IF NOT EXISTS "EmployeeProfiles" (
                "Id" uuid NOT NULL,
                "UserId" text NOT NULL,
                "EmployeeNumber" character varying(50) NOT NULL,
                "FullName" character varying(200) NOT NULL,
                "Designation" character varying(150) NOT NULL,
                "Department" character varying(150),
                "MobileNumber" character varying(30) NOT NULL,
                "WorkPhoneNumber" character varying(30),
                "CanAccessRestrictedClinicalData" boolean NOT NULL DEFAULT FALSE,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" text,
                "UpdatedAt" timestamp with time zone,
                "UpdatedBy" text,
                CONSTRAINT "PK_EmployeeProfiles" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_EmployeeProfiles_AspNetUsers_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_EmployeeProfiles_UserId"
                ON "EmployeeProfiles" ("UserId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_EmployeeProfiles_EmployeeNumber"
                ON "EmployeeProfiles" ("EmployeeNumber");
            CREATE INDEX IF NOT EXISTS "IX_Episodes_IsRestricted" ON "Episodes" ("IsRestricted");
            CREATE INDEX IF NOT EXISTS "IX_Assessments_IsRestricted" ON "Assessments" ("IsRestricted");
            CREATE INDEX IF NOT EXISTS "IX_Fittings_IsRestricted" ON "Fittings" ("IsRestricted");
            CREATE INDEX IF NOT EXISTS "IX_Deliveries_IsRestricted" ON "Deliveries" ("IsRestricted");
            CREATE INDEX IF NOT EXISTS "IX_FollowUps_IsRestricted" ON "FollowUps" ("IsRestricted");
            CREATE INDEX IF NOT EXISTS "IX_PatientDocuments_IsRestricted" ON "PatientDocuments" ("IsRestricted");
            CREATE INDEX IF NOT EXISTS "IX_EpisodeDocuments_IsRestricted" ON "EpisodeDocuments" ("IsRestricted");
            CREATE INDEX IF NOT EXISTS "IX_Appointments_AssignedClinicianUserId"
                ON "Appointments" ("AssignedClinicianUserId");

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'FK_Appointments_AspNetUsers_AssignedClinicianUserId'
                ) THEN
                    ALTER TABLE "Appointments"
                        ADD CONSTRAINT "FK_Appointments_AspNetUsers_AssignedClinicianUserId"
                        FOREIGN KEY ("AssignedClinicianUserId")
                        REFERENCES "AspNetUsers" ("Id")
                        ON DELETE SET NULL;
                END IF;
            END
            $$;
            """);
    }
}
