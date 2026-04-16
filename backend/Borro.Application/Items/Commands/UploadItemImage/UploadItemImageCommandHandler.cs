using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Items.Commands.UploadItemImage;

public class UploadItemImageCommandHandler : IRequestHandler<UploadItemImageCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IStorageService _storage;

    public UploadItemImageCommandHandler(IApplicationDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<string> Handle(UploadItemImageCommand cmd, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == cmd.ItemId, ct)
            ?? throw new InvalidOperationException($"Item {cmd.ItemId} not found.");

        if (item.OwnerId != cmd.RequestingUserId)
            throw new UnauthorizedAccessException("Only the item owner can upload images.");

        var uniqueFileName = $"items/{cmd.ItemId}/{Guid.NewGuid()}_{cmd.FileName}";
        var url = await _storage.UploadFileAsync(cmd.FileStream, uniqueFileName, cmd.ContentType, ct);

        item.ImageUrls.Add(url);
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return url;
    }
}
