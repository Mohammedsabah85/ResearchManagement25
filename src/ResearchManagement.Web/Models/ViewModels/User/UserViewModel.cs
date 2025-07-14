using ResearchManagement.Application.DTOs;
using ResearchManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Web.Models.ViewModels.User
{
    // ViewModel لقائمة المستخدمين مع الفلترة والبحث
    public class UserListViewModel
    {
        public List<ResearchManagement.Domain.Entities.User> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public UserRole? SelectedRole { get; set; }
        public bool? SelectedIsActive { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public UserStatisticsViewModel Statistics { get; set; } = new();

        // خصائص محسوبة للتنقل
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartIndex => (CurrentPage - 1) * PageSize + 1;
        public int EndIndex => Math.Min(CurrentPage * PageSize, TotalUsers);

        // معلومات التصفح
        public List<int> PageNumbers
        {
            get
            {
                var pages = new List<int>();
                var start = Math.Max(1, CurrentPage - 2);
                var end = Math.Min(TotalPages, CurrentPage + 2);

                for (int i = start; i <= end; i++)
                {
                    pages.Add(i);
                }
                return pages;
            }
        }
    }

    // ViewModel لتفاصيل المستخدم
    public class UserDetailsViewModel
    {
        public UserDto User { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public DateTime? LastLoginDate { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }

        // إحصائيات المستخدم
        public int TotalResearches { get; set; }
        public int AcceptedResearches { get; set; }
        public int RejectedResearches { get; set; }
        public int PendingResearches { get; set; }
        public int CompletedReviews { get; set; }
        public int PendingReviews { get; set; }
        public int OverdueReviews { get; set; }
        public double AverageReviewScore { get; set; }

        // معلومات إضافية محسوبة
        public TimeSpan AccountAge => DateTime.UtcNow - RegistrationDate;
        public string AccountAgeText
        {
            get
            {
                var age = AccountAge;
                if (age.TotalDays < 1)
                    return "أقل من يوم";
                if (age.TotalDays < 30)
                    return $"{(int)age.TotalDays} يوم";
                if (age.TotalDays < 365)
                    return $"{(int)(age.TotalDays / 30)} شهر";
                return $"{(int)(age.TotalDays / 365)} سنة";
            }
        }

        public string LastLoginText
        {
            get
            {
                if (!LastLoginDate.HasValue)
                    return "لم يسجل دخول مطلقاً";

                var timeSince = DateTime.UtcNow - LastLoginDate.Value;
                if (timeSince.TotalMinutes < 1)
                    return "الآن";
                if (timeSince.TotalMinutes < 60)
                    return $"منذ {(int)timeSince.TotalMinutes} دقيقة";
                if (timeSince.TotalHours < 24)
                    return $"منذ {(int)timeSince.TotalHours} ساعة";
                if (timeSince.TotalDays < 7)
                    return $"منذ {(int)timeSince.TotalDays} يوم";
                return LastLoginDate.Value.ToString("yyyy/MM/dd");
            }
        }

        // نسب الأداء
        public double ResearchAcceptanceRate => TotalResearches > 0 ?
                                               (double)AcceptedResearches / TotalResearches * 100 : 0;
        public double ReviewCompletionRate => (CompletedReviews + PendingReviews) > 0 ?
                                             (double)CompletedReviews / (CompletedReviews + PendingReviews) * 100 : 0;
    }

    // ViewModel لإنشاء مستخدم جديد
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم الأول يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(100, ErrorMessage = "اسم العائلة يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "الاسم الأول الإنجليزي يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "الاسم الأول (إنجليزي)")]
        public string? FirstNameEn { get; set; }

        [StringLength(100, ErrorMessage = "اسم العائلة الإنجليزي يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "اسم العائلة (إنجليزي)")]
        public string? LastNameEn { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [StringLength(256, ErrorMessage = "البريد الإلكتروني يجب أن يكون أقل من 256 حرف")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون بين {2} و {1} حرف", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{6,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي على حرف كبير وحرف صغير ورقم واحد على الأقل")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "المؤسسة يجب أن تكون أقل من 200 حرف")]
        [Display(Name = "المؤسسة")]
        public string? Institution { get; set; }

        [StringLength(100, ErrorMessage = "الدرجة العلمية يجب أن تكون أقل من 100 حرف")]
        [Display(Name = "الدرجة العلمية")]
        public string? AcademicDegree { get; set; }

        [StringLength(50, ErrorMessage = "معرف ORCID يجب أن يكون أقل من 50 حرف")]
        [Display(Name = "معرف ORCID")]
        [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$",
            ErrorMessage = "معرف ORCID غير صحيح. الصيغة المطلوبة: 0000-0000-0000-0000")]
        public string? OrcidId { get; set; }

        [Required(ErrorMessage = "الدور مطلوب")]
        [Display(Name = "الدور")]
        public UserRole Role { get; set; } = UserRole.Researcher;

        [Display(Name = "الحساب نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "البريد الإلكتروني مؤكد")]
        public bool EmailConfirmed { get; set; } = false;

        [Display(Name = "إرسال بريد ترحيب")]
        public bool SendWelcomeEmail { get; set; } = true;

        [Display(Name = "إجبار تغيير كلمة المرور في أول دخول")]
        public bool RequirePasswordChange { get; set; } = false;

        // قوائم الخيارات
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public List<SelectListItem> AcademicDegreeOptions { get; set; } = new()
        {
            new() { Value = "بكالوريوس", Text = "بكالوريوس" },
            new() { Value = "ماجستير", Text = "ماجستير" },
            new() { Value = "دكتوراه", Text = "دكتوراه" },
            new() { Value = "أستاذ مساعد", Text = "أستاذ مساعد" },
            new() { Value = "أستاذ مشارك", Text = "أستاذ مشارك" },
            new() { Value = "أستاذ", Text = "أستاذ" },
            new() { Value = "أخرى", Text = "أخرى" }
        };

        // خصائص محسوبة
        public string FullName => $"{FirstName} {LastName}";
        public string? FullNameEn => !string.IsNullOrEmpty(FirstNameEn) && !string.IsNullOrEmpty(LastNameEn)
            ? $"{FirstNameEn} {LastNameEn}"
            : null;
    }

    // ViewModel لتعديل المستخدم
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم الأول يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(100, ErrorMessage = "اسم العائلة يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "الاسم الأول الإنجليزي يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "الاسم الأول (إنجليزي)")]
        public string? FirstNameEn { get; set; }

        [StringLength(100, ErrorMessage = "اسم العائلة الإنجليزي يجب أن يكون أقل من 100 حرف")]
        [Display(Name = "اسم العائلة (إنجليزي)")]
        public string? LastNameEn { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [StringLength(256, ErrorMessage = "البريد الإلكتروني يجب أن يكون أقل من 256 حرف")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "المؤسسة يجب أن تكون أقل من 200 حرف")]
        [Display(Name = "المؤسسة")]
        public string? Institution { get; set; }

        [StringLength(100, ErrorMessage = "الدرجة العلمية يجب أن تكون أقل من 100 حرف")]
        [Display(Name = "الدرجة العلمية")]
        public string? AcademicDegree { get; set; }

        [StringLength(50, ErrorMessage = "معرف ORCID يجب أن يكون أقل من 50 حرف")]
        [Display(Name = "معرف ORCID")]
        [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$",
            ErrorMessage = "معرف ORCID غير صحيح. الصيغة المطلوبة: 0000-0000-0000-0000")]
        public string? OrcidId { get; set; }

        [Required(ErrorMessage = "الدور مطلوب")]
        [Display(Name = "الدور")]
        public UserRole Role { get; set; }

        [Display(Name = "الحساب نشط")]
        public bool IsActive { get; set; }

        [Display(Name = "البريد الإلكتروني مؤكد")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "ملاحظات التعديل")]
        [StringLength(500, ErrorMessage = "الملاحظات يجب أن تكون أقل من 500 حرف")]
        public string? EditNotes { get; set; }

        [Display(Name = "إرسال إشعار للمستخدم بالتعديل")]
        public bool NotifyUser { get; set; } = true;

        // قوائم الخيارات
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public List<SelectListItem> AcademicDegreeOptions { get; set; } = new()
        {
            new() { Value = "بكالوريوس", Text = "بكالوريوس" },
            new() { Value = "ماجستير", Text = "ماجستير" },
            new() { Value = "دكتوراه", Text = "دكتوراه" },
            new() { Value = "أستاذ مساعد", Text = "أستاذ مساعد" },
            new() { Value = "أستاذ مشارك", Text = "أستاذ مشارك" },
            new() { Value = "أستاذ", Text = "أستاذ" },
            new() { Value = "أخرى", Text = "أخرى" }
        };

        // معلومات أصلية للمقارنة
        public string OriginalEmail { get; set; } = string.Empty;
        public UserRole OriginalRole { get; set; }
        public bool OriginalIsActive { get; set; }

        // خصائص محسوبة
        public string FullName => $"{FirstName} {LastName}";
        public string? FullNameEn => !string.IsNullOrEmpty(FirstNameEn) && !string.IsNullOrEmpty(LastNameEn)
            ? $"{FirstNameEn} {LastNameEn}"
            : null;
        public bool HasEmailChanged => Email != OriginalEmail;
        public bool HasRoleChanged => Role != OriginalRole;
        public bool HasStatusChanged => IsActive != OriginalIsActive;
    }

    // ViewModel لإعادة تعيين كلمة المرور
    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "اسم المستخدم")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون بين {2} و {1} حرف", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{6,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي على حرف كبير وحرف صغير ورقم واحد على الأقل")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        [Compare("NewPassword", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "إجبار تغيير كلمة المرور في أول دخول")]
        public bool RequirePasswordChange { get; set; } = true;

        [Display(Name = "إرسال كلمة المرور الجديدة بالبريد الإلكتروني")]
        public bool SendEmailNotification { get; set; } = true;

        [Display(Name = "سبب إعادة التعيين")]
        [StringLength(200, ErrorMessage = "السبب يجب أن يكون أقل من 200 حرف")]
        public string? ResetReason { get; set; }
    }

    // ViewModel لإحصائيات المستخدمين
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
        public int RecentRegistrations { get; set; } // آخر 30 يوم
        public int TodayLogins { get; set; }
        public int WeeklyLogins { get; set; }
        public int MonthlyLogins { get; set; }

        // خصائص محسوبة للنسب المئوية
        public double ActiveUsersPercentage => TotalUsers > 0 ? (double)ActiveUsers / TotalUsers * 100 : 0;
        public double EmailConfirmedPercentage => TotalUsers > 0 ? (double)EmailConfirmedUsers / TotalUsers * 100 : 0;
        public double ResearchersPercentage => TotalUsers > 0 ? (double)ResearchersCount / TotalUsers * 100 : 0;
        public double ReviewersPercentage => TotalUsers > 0 ? (double)ReviewersCount / TotalUsers * 100 : 0;

        // إحصائيات النمو
        public List<MonthlyUserStats> MonthlyGrowth { get; set; } = new();
        public List<RoleDistributionItem> RoleDistribution { get; set; } = new();
    }

    // ViewModel لملف المستخدم الشخصي
    public class UserProfileViewModel
    {
        public UserDto User { get; set; } = new();
        public bool CanEdit { get; set; }
        public List<string> Roles { get; set; } = new();

        // إحصائيات المستخدم
        public UserActivityStatistics ActivityStats { get; set; } = new();

        // الأنشطة الأخيرة
        public List<UserActivityViewModel> RecentActivities { get; set; } = new();

        // الإنجازات والشارات
        public List<UserAchievementViewModel> Achievements { get; set; } = new();

        // إعدادات الإشعارات
        public UserNotificationSettings NotificationSettings { get; set; } = new();
    }

    // ViewModel لإحصائيات نشاط المستخدم
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

        // إحصائيات الوقت
        public double AverageReviewTime { get; set; } // بالأيام
        public int FastestReview { get; set; } // بالأيام
        public int TotalReviewDays { get; set; }

        // إحصائيات هذا الشهر
        public int ThisMonthResearches { get; set; }
        public int ThisMonthReviews { get; set; }

        // معدلات الأداء
        public double ResearchSuccessRate => TotalResearches > 0 ?
                                           (double)AcceptedResearches / TotalResearches * 100 : 0;
        public double ReviewCompletionRate => TotalReviews > 0 ?
                                             (double)CompletedReviews / TotalReviews * 100 : 0;
        public double OnTimeReviewRate => CompletedReviews > 0 ?
                                         (double)(CompletedReviews - OverdueReviews) / CompletedReviews * 100 : 0;
    }

    // ViewModel لنشاط المستخدم
    public class UserActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ActionUrl { get; set; }
        public string StatusClass { get; set; } = "primary";
        public string Icon { get; set; } = "fas fa-info-circle";
        public Dictionary<string, object> Metadata { get; set; } = new();

        // خصائص محسوبة
        public string RelativeTime
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;

                if (timeSpan.TotalMinutes < 1)
                    return "الآن";
                if (timeSpan.TotalMinutes < 60)
                    return $"منذ {(int)timeSpan.TotalMinutes} دقيقة";
                if (timeSpan.TotalHours < 24)
                    return $"منذ {(int)timeSpan.TotalHours} ساعة";
                if (timeSpan.TotalDays < 7)
                    return $"منذ {(int)timeSpan.TotalDays} يوم";
                if (timeSpan.TotalDays < 30)
                    return $"منذ {(int)(timeSpan.TotalDays / 7)} أسبوع";

                return CreatedAt.ToString("yyyy/MM/dd");
            }
        }
    }

    // ViewModel للإنجازات والشارات
    public class UserAchievementViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "primary";
        public DateTime EarnedDate { get; set; }
        public bool IsUnlocked { get; set; }
        public int Progress { get; set; } // 0-100
        public int Target { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    // ViewModel لإعدادات الإشعارات
    public class UserNotificationSettings
    {
        public bool EmailNotifications { get; set; } = true;
        public bool ResearchStatusUpdates { get; set; } = true;
        public bool ReviewAssignments { get; set; } = true;
        public bool DeadlineReminders { get; set; } = true;
        public bool SystemAnnouncements { get; set; } = true;
        public bool WeeklyDigest { get; set; } = false;
        public bool MonthlyReport { get; set; } = false;
    }

    // إحصائيات شهرية للمستخدمين
    public class MonthlyUserStats
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewUsers { get; set; }
        public int ActiveUsers { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    // توزيع الأدوار
    public class RoleDistributionItem
    {
        public UserRole Role { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    // ViewModel للمستخدمين المحذوفين
    public class DeletedUsersViewModel
    {
        public List<DeletedUserItem> DeletedUsers { get; set; } = new();
        public int TotalDeleted { get; set; }
        public DateTime? LastDeletionDate { get; set; }
    }

    public class DeletedUserItem
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public string? DeletionReason { get; set; }
        public bool CanRestore { get; set; }
    }

    // ViewModel للعمليات المجمعة
    public class BulkUserActionViewModel
    {
        public List<string> UserIds { get; set; } = new();
        public string Action { get; set; } = string.Empty; // activate, deactivate, delete, change_role
        public UserRole? NewRole { get; set; }
        public string? Reason { get; set; }
        public bool SendNotification { get; set; } = true;
    }

    // ViewModel لاستيراد المستخدمين
    public class ImportUsersViewModel
    {
        public IFormFile? CsvFile { get; set; }
        public bool HasHeader { get; set; } = true;
        public bool SendWelcomeEmails { get; set; } = false;
        public UserRole DefaultRole { get; set; } = UserRole.Researcher;
        public bool AutoConfirmEmails { get; set; } = false;
        public List<ImportUserItem> PreviewData { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class ImportUserItem
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Institution { get; set; }
        public string? AcademicDegree { get; set; }
        public UserRole Role { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}