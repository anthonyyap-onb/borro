using MediatR;

namespace Borro.Application.Items.Commands.UploadItemImage;

public record UploadItemImageCommand(
    Guid ItemId,
    Guid RequestingUserId,
    Stream FileStream,
    string FileName,
    string ContentType
) : IRequest<string>;
