using System.Collections.Generic;

namespace Supermarket.Application.BlackBox.Interfaces
{
    public interface IBlackBoxMetadataSanitizer
    {
        string? Sanitize(Dictionary<string, object?>? metadata, out bool metadataTruncated);
    }
}
