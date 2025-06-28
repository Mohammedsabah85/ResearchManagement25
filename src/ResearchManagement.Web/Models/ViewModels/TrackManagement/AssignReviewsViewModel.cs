using ResearchManagement.Domain.Entities;
using System.Collections.Generic;

namespace ResearchManagement.Web.Models.ViewModels
{
    public class AssignReviewsViewModel
    {
        public int TrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public List<Research> PendingResearches { get; set; } = new();
        public List<User> Reviewers { get; set; } = new();
    }

    public class AssignReviewViewModel
    {
        public int ResearchId { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public int DeadlineDays { get; set; } = 14; // Default to 14 days
    }
}