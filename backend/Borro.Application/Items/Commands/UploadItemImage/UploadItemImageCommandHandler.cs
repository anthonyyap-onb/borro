using Borro.Application.Common.Interfaces;
using Borro.Application.Common.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Borro.Application.Items.Commands.UploadItemImage;

public class UploadItemImageCommandHandler : IRequestHandler<UploadItemImageCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IStorageService _storage;
    private readonly StorageSettings _storageSettings;

    public UploadItemImageCommandHandler(
        IApplicationDbContext db,
        IStorageService storage,
        IOptions<StorageSettings> storageSettings)
    {
        _db = db;
        _storage = storage;
        _storageSettings = storageSettings.Value;
    }

    public async Task<string> Handle(UploadItemImageCommand cmd, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == cmd.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {cmd.ItemId} not found.");

        if (item.OwnerId != cmd.RequestingUserId)
            throw new UnauthorizedAccessException("Only the item owner can upload images.");

        var extension = Path.GetExtension(cmd.FileName);
        var objectKey = $"items/{cmd.ItemId}/{Guid.NewGuid()}{extension}";

        var url = await _storage.UploadAsync(
            _storageSettings.ItemImagesBucket,
            objectKey,
            cmd.FileStream,
            cmd.ContentType,
            ct);

        item.AddImageUrl(url);
        await _db.SaveChangesAsync(ct);

        return url;
    }
}
