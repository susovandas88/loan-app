using System.Collections.Concurrent;
using LoanApp.Application.Contracts;

namespace LoanApp.Infrastructure.Storage;

public sealed class LocalUploadTicket
{
    public required string BlobPath { get; init; }
    public required string ContentType { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed class LocalUploadTicketStore
{
    private readonly ConcurrentDictionary<string, LocalUploadTicket> _tickets = new();

    public string Issue(LocalUploadTicket ticket)
    {
        var token = Guid.NewGuid().ToString("N");
        _tickets[token] = ticket;
        return token;
    }

    public bool TryGet(string token, out LocalUploadTicket ticket) =>
        _tickets.TryGetValue(token, out ticket!);

    public void Complete(string token) => _tickets.TryRemove(token, out _);
}

public sealed class LocalBlobStorageService : IBlobStorageService
{
    private readonly string _root;
    private readonly string _publicBaseUrl;
    private readonly LocalUploadTicketStore _tickets;
    private readonly IClock _clock;

    public LocalBlobStorageService(string root, string publicBaseUrl, LocalUploadTicketStore tickets, IClock clock)
    {
        _root = root;
        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
        _tickets = tickets;
        _clock = clock;
        Directory.CreateDirectory(_root);
    }

    public Task<(string BlobPath, string UploadUrl, DateTimeOffset ExpiresAt)> CreateWriteUrlAsync(
        Guid applicationId,
        Guid documentId,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        var blobPath = $"{applicationId:N}/{documentId:N}/{fileName}";
        var expires = _clock.UtcNow.AddMinutes(5);
        var token = _tickets.Issue(new LocalUploadTicket
        {
            BlobPath = blobPath,
            ContentType = contentType,
            ExpiresAt = expires
        });
        var url = $"{_publicBaseUrl}/api/v1/dev/uploads/{token}";
        return Task.FromResult((blobPath, url, expires));
    }

    public Task<bool> ExistsAsync(string blobPath, CancellationToken ct) =>
        Task.FromResult(File.Exists(Physical(blobPath)));

    public Task<Stream> OpenReadAsync(string blobPath, CancellationToken ct)
    {
        var path = Physical(blobPath);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public async Task SaveAsync(string blobPath, Stream content, CancellationToken ct)
    {
        var path = Physical(blobPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
    }

    private string Physical(string blobPath) => Path.Combine(_root, blobPath.Replace('/', Path.DirectorySeparatorChar));
}
