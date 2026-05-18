using Belumi.Core.Entities;

namespace Belumi.Core.Interfaces;

public interface ISkinAnalysisService
{
    Task<SkinAnalysis> AnalyzeAsync(Guid userId, string imageUrl, CancellationToken cancellationToken);
}
