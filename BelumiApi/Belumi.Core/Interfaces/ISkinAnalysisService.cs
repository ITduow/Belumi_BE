using Belumi.Core.DTOs.Gemini;
using System.Threading.Tasks;

namespace Belumi.Core.Interfaces;

public interface ISkinAnalysisService
{
    Task<AnalysisResponse> AnalyzeAsync(byte[] imageBytes, string skinType);
}
