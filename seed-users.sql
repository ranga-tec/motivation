-- Create ADMIN role
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES ('admin-role-id', 'ADMIN', 'ADMIN', 'admin-stamp')
ON CONFLICT DO NOTHING;

-- Create admin user
-- Password: Admin@123
-- This is the ASP.NET Identity password hash for 'Admin@123'
INSERT INTO "AspNetUsers" (
    "Id",
    "UserName",
    "NormalizedUserName",
    "Email",
    "NormalizedEmail",
    "EmailConfirmed",
    "PasswordHash",
    "SecurityStamp",
    "ConcurrencyStamp",
    "PhoneNumberConfirmed",
    "TwoFactorEnabled",
    "LockoutEnabled",
    "AccessFailedCount"
)
VALUES (
    'admin-user-id',
    'admin@poms.lk',
    'ADMIN@POMS.LK',
    'admin@poms.lk',
    'ADMIN@POMS.LK',
    true,
    'AQAAAAIAAYagAAAAEH8qVvNqPp0qvqnY8RLHqH6XJKqxJKZ5fKCm0V3xE1wZrH9R0qJLmK4xG8RqvL5VCg==',
    'SECURITYSTAMP123',
    'CONCURRENCYSTAMP123',
    false,
    false,
    false,
    0
)
ON CONFLICT ("Id") DO NOTHING;

-- Assign admin user to ADMIN role
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES ('admin-user-id', 'admin-role-id')
ON CONFLICT DO NOTHING;
