using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ResearchManagement.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        //IResearchRepository Research { get; }
        //IGenericRepository<ResearchAuthor> ResearchAuthors { get; }
        //IGenericRepository<ResearchFile> ResearchFiles { get; }
        //IReviewRepository Reviews { get; }
        //IResearchStatusHistoryRepository StatusHistory { get; }
        //IUserRepository Users { get; }
        //ITrackManagerRepository TrackManagers { get; }
        //IGenericRepository<TrackReviewer> TrackReviewers { get; }
        //IGenericRepository<ResearchTrackHistory> ResearchTrackHistories { get; }
        ////IGenericRepository<ResearchTrack> ResearchTracks { get; }
        //INotificationService NotificationService { get; }
        IResearchRepository Research { get; }
        IGenericRepository<ResearchAuthor> ResearchAuthors { get; }
        IGenericRepository<ResearchFile> ResearchFiles { get; }
        IReviewRepository Reviews { get; }
        IResearchStatusHistoryRepository StatusHistory { get; }
        IGenericRepository<ResearchTrackHistory> ResearchTrackHistories { get; }
        IUserRepository Users { get; }
        ITrackManagerRepository TrackManagers { get; }
        IGenericRepository<TrackReviewer> TrackReviewers { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
