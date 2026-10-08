using LoanApp.Application.Domain;
using LoanApp.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Infrastructure.Persistence;

public static class UserSeeder
{
    public static async Task SeedAsync(LoanDbContext db, CancellationToken ct = default)
    {
        await EnsureUsersTableAsync(db, ct);
        await EnsureUserAsync(db, "applicant-1", "applicant@loan.local", "Asha Applicant", "LoanPortal!2026", UserRole.Applicant, ct);
        await EnsureUserAsync(db, "reviewer-1", "reviewer@loan.local", "Review Officer", "Reviewer!2026", UserRole.Reviewer, ct);
        await EnsureUserAsync(db, "admin-1", "admin@loan.local", "Super Admin", "Admin!2026", UserRole.SuperAdmin, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureUserAsync(
        LoanDbContext db,
        string id,
        string email,
        string displayName,
        string password,
        UserRole role,
        CancellationToken ct)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is not null)
        {
            return;
        }

        var (salt, hash) = PasswordHasher.Hash(password);
        db.Users.Add(new AppUser
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            PasswordSalt = salt,
            PasswordHash = hash,
            Role = role
        });
    }

    private static async Task EnsureUsersTableAsync(LoanDbContext db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        try
        {
            await using var check = connection.CreateCommand();
            check.CommandText = connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'Users'"
                : "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'";
            var exists = await check.ExecuteScalarAsync(ct) is not null;
            if (exists)
            {
                return;
            }

            var sqlite = connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
            foreach (var sql in sqlite
                         ? new[]
                         {
                             """
                             CREATE TABLE "Users" (
                               "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                               "Email" TEXT NOT NULL,
                               "DisplayName" TEXT NOT NULL,
                               "PasswordSalt" TEXT NOT NULL,
                               "PasswordHash" TEXT NOT NULL,
                               "Role" INTEGER NOT NULL
                             );
                             """,
                             """CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");"""
                         }
                         : new[]
                         {
                             """
                             CREATE TABLE [Users] (
                               [Id] nvarchar(64) NOT NULL CONSTRAINT [PK_Users] PRIMARY KEY,
                               [Email] nvarchar(256) NOT NULL,
                               [DisplayName] nvarchar(200) NOT NULL,
                               [PasswordSalt] nvarchar(128) NOT NULL,
                               [PasswordHash] nvarchar(128) NOT NULL,
                               [Role] int NOT NULL
                             );
                             """,
                             """CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);"""
                         })
            {
                await using var create = connection.CreateCommand();
                create.CommandText = sql;
                await create.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
