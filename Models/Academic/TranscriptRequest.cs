using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PWCEPortal.CommonEntity;
using PWCEPortal.Data;
using PWCEPortal.Models.StudentInfo;

namespace PWCEPortal.Models.Academic;

public enum TranscriptType
{
    Unofficial = 0,
    Official = 1
}

/// <summary>
/// Audit log of all transcript generation requests.
/// </summary>
public class TranscriptRequest : EntityHelper
{
    [ForeignKey("Student")]
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public TranscriptType TranscriptType { get; set; } = TranscriptType.Unofficial;

    public string RequestedById { get; set; } = string.Empty;
    [ForeignKey("RequestedById")]
    public ApplicationUser? RequestedBy { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public string? Purpose { get; set; }

    public string? GeneratedFilePath { get; set; }
}
