using ResearchManagement.Domain.Entities;
using System.Collections.Generic;

namespace ResearchManagement.Web.Models.ViewModels
{
    public class TrackReviewersViewModel
    {
        public int TrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public List<TrackReviewer> CurrentReviewers { get; set; } = new();
        public List<Domain.Entities.User> AvailableReviewers { get; set; } = new();
    }

    public class AddTrackReviewerViewModel
    {
        public int TrackManagerId { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
    }
}