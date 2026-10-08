using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Infrastructure.Persistence;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly LoanDbContext _db;

    public ApplicationRepository(LoanDbContext db)
    {
        _db = db;
    }

    public async Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var app = await Query()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        DropGhosts(app);
        return app;
    }

    public async Task<LoanApplication?> GetByIdForUserAsync(Guid id, string userId, CancellationToken ct)
    {
        var app = await Query()
            .FirstOrDefaultAsync(a => a.Id == id && a.ApplicantUserId == userId, ct);
        DropGhosts(app);
        return app;
    }

    public async Task<IReadOnlyList<LoanApplication>> ListForUserAsync(string userId, int page, int pageSize, CancellationToken ct)
    {
        var items = await Query()
            .AsNoTracking()
            .Where(a => a.ApplicantUserId == userId)
            .ToListAsync(ct);

        foreach (var app in items)
        {
            DropGhosts(app);
        }

        return items
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public Task<int> CountForUserAsync(string userId, CancellationToken ct) =>
        _db.Applications.CountAsync(a => a.ApplicantUserId == userId, ct);

    public async Task<IReadOnlyList<LoanApplication>> ListAllAsync(int page, int pageSize, CancellationToken ct)
    {
        var items = await Query().AsNoTracking().ToListAsync(ct);
        foreach (var app in items)
        {
            DropGhosts(app);
        }

        return items
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public Task<int> CountAllAsync(CancellationToken ct) => _db.Applications.CountAsync(ct);

    public async Task AddAsync(LoanApplication application, CancellationToken ct) =>
        await _db.Applications.AddAsync(application, ct);

    public Task AddDocumentAsync(LoanDocument document, CancellationToken ct)
    {
        var entry = _db.Entry(document);
        if (entry.State == EntityState.Detached)
        {
            _db.Documents.Add(document);
        }
        else if (entry.State is not EntityState.Added)
        {
            entry.State = EntityState.Added;
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    private IQueryable<LoanApplication> Query() =>
        _db.Applications
            .AsSplitQuery()
            .Include(a => a.Documents)
            .Include(a => a.Findings);

    private void DropGhosts(LoanApplication? app)
    {
        if (app is null)
        {
            return;
        }

        foreach (var document in app.Documents.Where(d => d.Id == Guid.Empty).ToList())
        {
            app.Documents.Remove(document);
            _db.Entry(document).State = EntityState.Detached;
        }

        foreach (var finding in app.Findings.Where(f => f.Id == Guid.Empty).ToList())
        {
            app.Findings.Remove(finding);
            _db.Entry(finding).State = EntityState.Detached;
        }
    }
}
