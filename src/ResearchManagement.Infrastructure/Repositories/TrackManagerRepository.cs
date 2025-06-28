using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;
using ResearchManagement.Infrastructure.Data;

namespace ResearchManagement.Infrastructure.Repositories
{
    public class TrackManagerRepository : GenericRepository<TrackManager>, ITrackManagerRepository
    {
        public TrackManagerRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<TrackManager?> GetByUserIdAsync(string userId)
        {
            return await _context.TrackManagers
                .Include(tm => tm.User)
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.IsActive);
        }

        public async Task<TrackManager?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.TrackManagers
                .Include(tm => tm.User)
                .Include(tm => tm.ManagedResearches)
                .Include(tm => tm.TrackReviewers)
                    .ThenInclude(tr => tr.Reviewer)
                .FirstOrDefaultAsync(tm => tm.Id == id);
        }

        public async Task<TrackManager?> GetByTrackAsync(ResearchTrack track)
        {
            return await _context.TrackManagers
                .Include(tm => tm.User)
                .FirstOrDefaultAsync(tm => tm.Track == track && tm.IsActive);
        }

        public async Task<IEnumerable<TrackManager>> GetAllActiveAsync()
        {
            return await _context.TrackManagers
                .Include(tm => tm.User)
                .Where(tm => tm.IsActive)
                .OrderBy(tm => tm.Track)
                .ToListAsync();
        }

        public async Task<IEnumerable<Research>> GetManagedResearchesAsync(int trackManagerId)
        {
            var trackManager = await GetByIdAsync(trackManagerId);
            if (trackManager == null)
                return new List<Research>();

            return await _context.Researches
                .Include(r => r.SubmittedBy)
                .Include(r => r.Authors)
                .Include(r => r.Reviews)
                .Where(r => r.Track == trackManager.Track)
                .OrderByDescending(r => r.SubmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TrackReviewer>> GetTrackReviewersAsync(int trackManagerId)
        {
            return await _context.TrackReviewers
                .Include(tr => tr.Reviewer)
                .Where(tr => tr.TrackManagerId == trackManagerId && tr.IsActive)
                .OrderBy(tr => tr.Reviewer.FirstName)
                .ToListAsync();
        }

        public async Task<bool> IsUserTrackManagerAsync(string userId)
        {
            return await _context.TrackManagers
                .AnyAsync(tm => tm.UserId == userId && tm.IsActive);
        }

        public async Task<ResearchTrack?> GetUserTrackAsync(string userId)
        {
            var trackManager = await GetByUserIdAsync(userId);
            return trackManager?.Track;
        }

        public async Task<int> GetPendingAssignmentsCountAsync(int trackManagerId)
        {
            var trackManager = await GetByIdAsync(trackManagerId);
            if (trackManager == null)
                return 0;

            return await _context.Researches
                .CountAsync(r => r.Track == trackManager.Track && 
                            r.Status == ResearchStatus.Submitted);
        }

        public async Task<int> GetCompletedReviewsCountAsync(int trackManagerId)
        {
            var trackManager = await GetByIdAsync(trackManagerId);
            if (trackManager == null)
                return 0;

            var researchIds = await _context.Researches
                .Where(r => r.Track == trackManager.Track)
                .Select(r => r.Id)
                .ToListAsync();

            return await _context.Reviews
                .CountAsync(r => researchIds.Contains(r.ResearchId) && r.IsCompleted);
        }

        public async Task<IEnumerable<Review>> GetOverdueReviewsAsync(int trackManagerId)
        {
            var trackManager = await GetByIdAsync(trackManagerId);
            if (trackManager == null)
                return new List<Review>();

            var researchIds = await _context.Researches
                .Where(r => r.Track == trackManager.Track)
                .Select(r => r.Id)
                .ToListAsync();

            return await _context.Reviews
                .Include(r => r.Research)
                .Include(r => r.Reviewer)
                .Where(r => researchIds.Contains(r.ResearchId) && 
                       !r.IsCompleted && 
                       r.Deadline < DateTime.UtcNow)
                .OrderBy(r => r.Deadline)
                .ToListAsync();
        }

        public override async Task DeleteAsync(int id)
        {
            var trackManager = await GetByIdAsync(id);
            if (trackManager != null)
            {
                trackManager.IsDeleted = true;
                trackManager.IsActive = false;
                trackManager.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}