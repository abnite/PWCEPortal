using PWCEPortal.Models.Academic;

namespace PWCEPortal.Interfaces;

public interface IAssessmentStructureService
{
    // Assessment Structures
    Task<List<AssessmentStructure>> GetAllStructuresAsync();
    Task<AssessmentStructure?> GetStructureByIdAsync(Guid id);
    Task<bool> CreateStructureAsync(AssessmentStructure structure);
    Task<bool> UpdateStructureAsync(AssessmentStructure structure);
    Task<bool> DeleteStructureAsync(Guid id);

    // Assessment Components
    Task<List<AssessmentComponent>> GetComponentsByStructureIdAsync(Guid structureId);
    Task<AssessmentComponent?> GetComponentByIdAsync(Guid id);
    Task<bool> CreateComponentAsync(AssessmentComponent component);
    Task<bool> UpdateComponentAsync(AssessmentComponent component);
    Task<bool> DeleteComponentAsync(Guid id);
    Task<decimal> GetTotalWeightForStructureAsync(Guid structureId);

    // Grading Scales
    Task<List<GradingScale>> GetAllGradingScalesAsync();
    Task<GradingScale?> GetDefaultGradingScaleAsync();
    Task<GradingScale?> GetGradingScaleByIdAsync(Guid id);
    Task<bool> CreateGradingScaleAsync(GradingScale scale, List<GradeDefinition> grades);
    Task<bool> UpdateGradingScaleAsync(GradingScale scale, List<GradeDefinition> grades);
    Task<bool> DeleteGradingScaleAsync(Guid id);
    Task<bool> SetDefaultGradingScaleAsync(Guid id);
    Task<GradeDefinition?> GetGradeForScoreAsync(Guid gradingScaleId, decimal score);
}
