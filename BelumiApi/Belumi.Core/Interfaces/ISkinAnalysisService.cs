using Belumi.Core.Entities;
using Belumi.Core.DTOs;

namespace Belumi.Core.Interfaces;

public interface ISkinAnalysisService
{
    Task<SkinAnalysis> AnalyzeAsync(Guid userId, SkinAnalysisRequest request, CancellationToken cancellationToken);
}
