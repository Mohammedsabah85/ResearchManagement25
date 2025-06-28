using System.Collections.Generic;

namespace ResearchManagement.Web.Models.ViewModels
{
    public class TrackReportsViewModel
    {
        public int TrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public int TotalResearches { get; set; }
        public int SubmittedResearches { get; set; }
        public int UnderReviewResearches { get; set; }
        public int UnderEvaluationResearches { get; set; }
        public int AcceptedResearches { get; set; }
        public int RejectedResearches { get; set; }
        public Dictionary<string, int> ResearchesByMonth { get; set; } = new();
        public Dictionary<string, int> ResearchesByType { get; set; } = new();
    }
}