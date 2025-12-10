#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.0"

using Npgsql;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

var connectionString = "Host=mainline.proxy.rlwy.net;Port=15160;Database=railway;Username=postgres;Password=odJhlhNXHtEkBwGJkPYdLxJLbBjNqMrC;SSL Mode=Require;Trust Server Certificate=true";

using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

Console.WriteLine("Connected to Railway PostgreSQL!");

// Create admin user with ASP.NET Identity compatible password hash
var userId = Guid.NewGuid().ToString();
var email = "admin@poms.lk";
var normalizedEmail = email.ToUpper();
var password = "Admin@123";

// Generate password hash using PBKDF2 (ASP.NET Identity v3 format)
byte[] salt = new byte[128 / 8];
using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(salt);
}

string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
    password: password,
    salt: salt,
    prf: KeyDerivationPrf.HMACSHA256,
    iterationCount: 10000,
    numBytesRequested: 256 / 8));

var passwordHash = $"AQAAAAEAACcQAAAAE{Convert.ToBase64String(salt)}{hashed}";

// Insert user
var insertUserSql = @"
INSERT INTO ""AspNetUsers"" (""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"", ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"", ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnabled"", ""AccessFailedCount"")
VALUES (@id, @email, @normalizedEmail, @email, @normalizedEmail, true, @passwordHash, @securityStamp, @concurrencyStamp, false, false, false, 0)
ON CONFLICT (""Id"") DO NOTHING";

using var cmd = new NpgsqlCommand(insertUserSql, conn);
cmd.Parameters.AddWithValue("id", userId);
cmd.Parameters.AddWithValue("email", email);
cmd.Parameters.AddWithValue("normalizedEmail", normalizedEmail);
cmd.Parameters.AddWithValue("passwordHash", passwordHash);
cmd.Parameters.AddWithValue("securityStamp", Guid.NewGuid().ToString());
cmd.Parameters.AddWithValue("concurrencyStamp", Guid.NewGuid().ToString());

await cmd.ExecuteNonQueryAsync();

Console.WriteLine($"User created: {email}");

// Create ADMIN role if it doesn't exist
var roleId = Guid.NewGuid().ToString();
var insertRoleSql = @"
INSERT INTO ""AspNetRoles"" (""Id"", ""Name"", ""NormalizedName"", ""ConcurrencyStamp"")
VALUES (@id, 'ADMIN', 'ADMIN', @concurrencyStamp)
ON CONFLICT DO NOTHING";

using var roleCmd = new NpgsqlCommand(insertRoleSql, conn);
roleCmd.Parameters.AddWithValue("id", roleId);
roleCmd.Parameters.AddWithValue("concurrencyStamp", Guid.NewGuid().ToString());

await roleCmd.ExecuteNonQueryAsync();

Console.WriteLine("ADMIN role created");

// Get the role ID
var getRoleIdSql = @"SELECT ""Id"" FROM ""AspNetRoles"" WHERE ""Name"" = 'ADMIN' LIMIT 1";
using var getRoleCmd = new NpgsqlCommand(getRoleIdSql, conn);
var adminRoleId = (string)(await getRoleCmd.ExecuteScalarAsync());

// Assign user to ADMIN role
var insertUserRoleSql = @"
INSERT INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"")
VALUES (@userId, @roleId)
ON CONFLICT DO NOTHING";

using var userRoleCmd = new NpgsqlCommand(insertUserRoleSql, conn);
userRoleCmd.Parameters.AddWithValue("userId", userId);
userRoleCmd.Parameters.AddWithValue("roleId", adminRoleId);

await userRoleCmd.ExecuteNonQueryAsync();

Console.WriteLine($"User assigned to ADMIN role");
Console.WriteLine("\n✅ Admin user created successfully!");
Console.WriteLine($"Email: {email}");
Console.WriteLine($"Password: {password}");
