using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Application.Interfaces
{
    public interface ITrackManagerRepository : IGenericRepository<TrackManager>
    {
       

        Task<TrackManager?> GetByUserIdAsync(string userId);
        Task<TrackManager?> GetByIdWithDetailsAsync(int id);
        Task<TrackManager?> GetByTrackAsync(ResearchTrack track);
        Task<IEnumerable<TrackManager>> GetAllActiveAsync();
        Task<IEnumerable<Research>> GetManagedResearchesAsync(int trackManagerId);
        Task<IEnumerable<TrackReviewer>> GetTrackReviewersAsync(int trackManagerId);
        Task<bool> IsUserTrackManagerAsync(string userId);
        Task<ResearchTrack?> GetUserTrackAsync(string userId);
        Task<int> GetPendingAssignmentsCountAsync(int trackManagerId);
        Task<int> GetCompletedReviewsCountAsync(int trackManagerId);
        Task<IEnumerable<Review>> GetOverdueReviewsAsync(int trackManagerId);
    }
}