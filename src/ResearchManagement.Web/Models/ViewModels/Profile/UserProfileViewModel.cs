// UserProfileViewModel.cs
using ResearchManagement.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Web.Models.ViewModels.Profile
{
    public class UserProfileViewModel
    {
        public UserDto User { get; set; } = new();
        public bool CanEdit { get; set; }
        public List<string> Roles { get; set; } = new();
        public UserActivityStatistics ActivityStats { get; set; } = new();
        public List<ProfileActivityViewModel> RecentActivities { get; set; } = new();
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

    public class ProfileActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ActionUrl { get; set; }
        public string StatusClass { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم الأول يجب أن يكون أقل من 100 حرف")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(100, ErrorMessage = "اسم العائلة يجب أن يكون أقل من 100 حرف")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "الاسم الأول بالإنجليزية يجب أن يكون أقل من 100 حرف")]
        public string? FirstNameEn { get; set; }

        [StringLength(100, ErrorMessage = "اسم العائلة بالإنجليزية يجب أن يكون أقل من 100 حرف")]
        public string? LastNameEn { get; set; }

        [StringLength(200, ErrorMessage = "اسم المؤسسة يجب أن يكون أقل من 200 حرف")]
        public string? Institution { get; set; }

        [StringLength(100, ErrorMessage = "الدرجة العلمية يجب أن تكون أقل من 100 حرف")]
        public string? AcademicDegree { get; set; }

        [StringLength(50, ErrorMessage = "معرف ORCID يجب أن يكون أقل من 50 حرف")]
        [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{4}$", ErrorMessage = "تنسيق معرف ORCID غير صحيح (0000-0000-0000-0000)")]
        public string? OrcidId { get; set; }

        public string CurrentEmail { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون بين {2} و {1} حرف", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "كلمة المرور الجديدة وتأكيد كلمة المرور غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserActivityListViewModel
    {
        public List<ProfileActivityViewModel> Activities { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
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