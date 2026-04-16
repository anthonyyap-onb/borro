using MediatR;

namespace Borro.Application.Items.Commands;

/// <summary>
/// Uploads an image to object storage and appends the resulting URL to the item's ImageUrls list.
/// Returns the public URL of the uploaded image.
/// </summary>
public sealed record UploadItemImageCommand(
    Guid ItemId,
    Guid RequestingUserId,
    Stream FileStream,
    string FileName,
    string ContentType
) : IRequest<string?>;
