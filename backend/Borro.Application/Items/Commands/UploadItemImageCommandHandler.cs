using Borro.Application.Common.Interfaces;
using Borro.Application.Common.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Borro.Application.Items.Commands;

public sealed class UploadItemImageCommandHandler : IRequestHandler<UploadItemImageCommand, string?>
{
    private readonly IBorroDbContext _context;
    private readonly IStorageService _storage;
    private readonly StorageSettings _storageSettings;

    public UploadItemImageCommandHandler(
        IBorroDbContext context,
        IStorageService storage,
        IOptions<StorageSettings> storageSettings)
    {
        _context = context;
        _storage = storage;
        _storageSettings = storageSettings.Value;
    }

    public async Task<string?> Handle(UploadItemImageCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken);

        if (item is null)
            return null;

        if (item.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException($"User {request.RequestingUserId} does not own item {request.ItemId}.");

        var extension = Path.GetExtension(request.FileName);
        var objectKey = $"items/{request.ItemId}/{Guid.NewGuid()}{extension}";

        var url = await _storage.UploadAsync(
            _storageSettings.ItemImagesBucket,
            objectKey,
            request.FileStream,
            request.ContentType,
            cancellationToken);

        item.AddImageUrl(url);
        await _context.SaveChangesAsync(cancellationToken);

        return url;
    }
}
