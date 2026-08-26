using LimsControlLab.Domain.Entities;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Repositories;

public interface IAnalysisRepository
{
    Task<Analysis?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Sample?> GetSampleByIdAsync(int id, CancellationToken ct = default);
    Task<AnalysisTemplate?> GetTemplateByIdAsync(int id, CancellationToken ct = default);
    Task AddReadingAsync(Reading reading, CancellationToken ct = default);
    Task AddExceptionAsync(ExceptionRecord exception, CancellationToken ct = default);
    Task AddSampleTransferAsync(SampleTransfer transfer, CancellationToken ct = default);
    Task<bool> TryAddSampleTransferAsync(SampleTransfer transfer, Sample sample, byte[] expectedRowVersion, CancellationToken ct = default);
    Task<ExceptionRecord?> GetExceptionByIdAsync(int id, CancellationToken ct = default);
    Task<bool> TryUpdateAnalysisWithConcurrencyCheckAsync(Analysis analysis, byte[] expectedRowVersion, CancellationToken ct = default);
    Task<bool> TryUpdateExceptionWithConcurrencyCheckAsync(ExceptionRecord exception, byte[] expectedRowVersion, CancellationToken ct = default);
    Task<IEnumerable<Analysis>> GetAnalysesWithExceptionsAsync(CancellationToken ct = default);
    Task<IEnumerable<Analysis>> GetAnalysesWithExceptionsBySiteAsync(Site site, CancellationToken ct = default);
}
