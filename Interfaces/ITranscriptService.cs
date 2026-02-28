using PWCEPortal.Models.Academic;

namespace PWCEPortal.Interfaces;

public interface ITranscriptService
{
    Task<byte[]> GenerateTranscriptPdfAsync(Guid studentId, TranscriptType type, string requestedByUserId, string? purpose = null);
    Task<List<TranscriptRequest>> GetTranscriptLogsAsync(Guid? studentId = null);
    Task<TranscriptRequest?> GetRequestByIdAsync(Guid id);
}
