using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly LoanDbContext _db;

    public UserRepository(LoanDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
    }

    public Task<AppUser?> FindByIdAsync(string id, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<AppUser>> ListApplicantsAsync(CancellationToken ct) =>
        await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Applicant)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(ct);
}
