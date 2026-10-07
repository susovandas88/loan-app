using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using LoanApp.Application.Contracts;

namespace LoanApp.Infrastructure.Storage;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _service;
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(BlobServiceClient serviceClient, string containerName)
    {
        _service = serviceClient;
        _container = serviceClient.GetBlobContainerClient(containerName);
    }

    public async Task<(string BlobPath, string UploadUrl, DateTimeOffset ExpiresAt)> CreateWriteUrlAsync(
        Guid applicationId,
        Guid documentId,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blobPath = $"{applicationId:N}/{documentId:N}/{fileName}";
        var blob = _container.GetBlobClient(blobPath);
        var expires = DateTimeOffset.UtcNow.AddMinutes(5);

        if (blob.CanGenerateSasUri)
        {
            var sas = blob.GenerateSasUri(new BlobSasBuilder(BlobSasPermissions.Write | BlobSasPermissions.Create, expires)
            {
                ContentType = contentType
            });
            return (blobPath, sas.ToString(), expires);
        }

        var starts = DateTimeOffset.UtcNow.AddMinutes(-5);
        var key = await _service.GetUserDelegationKeyAsync(starts, expires, ct);
        var builder = new BlobSasBuilder(BlobSasPermissions.Write | BlobSasPermissions.Create, expires)
        {
            BlobContainerName = _container.Name,
            BlobName = blobPath,
            Resource = "b",
            ContentType = contentType
        };
        var query = builder.ToSasQueryParameters(key.Value, _service.AccountName);
        return (blobPath, $"{blob.Uri}?{query}", expires);
    }

    public async Task<bool> ExistsAsync(string blobPath, CancellationToken ct)
    {
        var blob = _container.GetBlobClient(blobPath);
        return await blob.ExistsAsync(ct);
    }

    public async Task<Stream> OpenReadAsync(string blobPath, CancellationToken ct)
    {
        var blob = _container.GetBlobClient(blobPath);
        var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }
}
