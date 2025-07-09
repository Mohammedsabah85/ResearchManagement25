using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Web.Models.ViewModels.Dashboard
{
    // تحديث ConferenceManagerDashboardViewModel في ملف ViewModels/Dashboard

    public class ConferenceManagerDashboardViewModel
    {
        public int TotalResearches { get; set; }
        public int AcceptedResearches { get; set; }
        public int PendingResearches { get; set; }
        public int RejectedResearches { get; set; }
        public int SubmittedResearches { get; set; }

        // إضافة الخصائص الجديدة
        public int PendingTrackAssignments { get; set; }
        public int UrgentPendingAssignments { get; set; }

        public int TotalUsers { get; set; }
        public int TotalReviewers { get; set; }
        public int CompletedReviews { get; set; }
        public double AverageReviewTime { get; set; }
        public List<Domain.Entities.Research> RecentSubmissions { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
        public List<TrackStatistic> TrackStatistics { get; set; } = new();

        // خصائص محسوبة للإحصائيات
        public double AcceptanceRate => TotalResearches > 0 ? (double)AcceptedResearches / TotalResearches * 100 : 0;
        public double RejectionRate => TotalResearches > 0 ? (double)RejectedResearches / TotalResearches * 100 : 0;
        public bool HasUrgentAssignments => UrgentPendingAssignments > 0;
        public bool HasPendingAssignments => PendingTrackAssignments > 0;

        // نسبة البحوث المعلقة لتحديد المسار
        public double PendingAssignmentRate => TotalResearches > 0 ? (double)PendingTrackAssignments / TotalResearches * 100 : 0;
    }

    public class TrackStatistic
    {
        public string TrackName { get; set; } = string.Empty;
        public int ResearchCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }

        // إضافة إحصائيات التعيين المعلق
        public int PendingAssignmentCount { get; set; }

        // خصائص محسوبة
        public double AcceptanceRate => ResearchCount > 0 ? (double)AcceptedCount / ResearchCount * 100 : 0;
        public double RejectionRate => ResearchCount > 0 ? (double)RejectedCount / ResearchCount * 100 : 0;
        public double PendingAssignmentRate => ResearchCount > 0 ? (double)PendingAssignmentCount / ResearchCount * 100 : 0;
        public bool HasPendingAssignments => PendingAssignmentCount > 0;
    }
}
