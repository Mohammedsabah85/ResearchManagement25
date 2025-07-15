using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ResearchManagement.Web.Models.ViewModels.User
{
    public class UserListViewModel
    {
        public IEnumerable<Domain.Entities.User> Users { get; set; } = new List<Domain.Entities.User>();
        public string? SearchTerm { get; set; }
        public UserRole? SelectedRole { get; set; }
        public bool? SelectedIsActive { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public UserStatisticsViewModel Statistics { get; set; } = new();

        // Helper properties for pagination
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class UserStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int EmailConfirmedUsers { get; set; }
        public int ResearchersCount { get; set; }
        public int ReviewersCount { get; set; }
        public int TrackManagersCount { get; set; }
        public int ConferenceManagersCount { get; set; }
        public int SystemAdminsCount { get; set; }
        public int RecentRegistrations { get; set; }
    }

    public class UserDetailsViewModel
    {
        public ResearchManagement.Application.DTOs.UserDto User { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public DateTime? LastLoginDate { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }

        // إضافة الخاصيات المفقودة
        public int TotalResearches { get; set; } = 0;
        public int CompletedReviews { get; set; } = 0;
        public int PendingReviews { get; set; } = 0;
        public double AverageReviewScore { get; set; } = 0.0;

        // خاصية لحساب عمر الحساب
        public string AccountAgeText
        {
            get
            {
                var timeSpan = DateTime.UtcNow - RegistrationDate;
                if (timeSpan.Days > 30)
                {
                    int months = (int)(timeSpan.Days / 30);
                    return $"{months} شهر";
                }
                else if (timeSpan.Days > 0)
                {
                    return $"{timeSpan.Days} يوم";
                }
                else if (timeSpan.Hours > 0)
                {
                    return $"{timeSpan.Hours} ساعة";
                }
                else
                {
                    return "اليوم";
                }
            }
        }
    }

    public class UserActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ActionUrl { get; set; }
        public string StatusClass { get; set; } = "primary";
        public string Icon { get; set; } = "fas fa-info-circle";
    }

    public class UserActivityStatistics
    {
        public int TotalResearches { get; set; }
        public int AcceptedResearches { get; set; }
        public int RejectedResearches { get; set; }
        public int PendingResearches { get; set; }
        public int TotalReviews { get; set; }
        public int CompletedReviews { get; set; }
        public int PendingReviews { get; set; }
        public int OverdueReviews { get; set; }
        public double AverageReviewScore { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    public class UserActivityListViewModel
    {
        public List<UserActivityViewModel> Activities { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalActivities { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class SecuritySettingsViewModel
    {
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LastPasswordChange { get; set; }
        public List<LoginAttemptViewModel> LoginAttempts { get; set; } = new();
    }

    public class LoginAttemptViewModel
    {
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public bool IsSuccessful { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}