using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

public class WWCharTestResultAttachment : CompanyScopedEntity
{
    public Guid WWCharId { get; set; }
    public DateTime TestDate { get; set; }
    public string FileStoragePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }

    public WWChar? WWChar { get; set; }
}

