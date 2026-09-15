// IRecommendationService.cs
namespace SmartFileOrganizer.Core.Interfaces
{
    using SmartFileOrganizer.Core.Models;
    using System.Threading.Tasks;

    public interface IRecommendationService
    {
        /// <summary>
        /// Applies scoring rules to a duplicate group and marks the recommended file.
        /// Returns the recommended <see cref="FileRecord"/>.
        /// </summary>
        Task<FileRecord> RecommendAsync(DuplicateGroup group);
    }
}
