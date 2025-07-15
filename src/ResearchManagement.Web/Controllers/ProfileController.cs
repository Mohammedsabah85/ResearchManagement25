using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Web.Models.ViewModels.User;
using ResearchManagement.Web.Models.ViewModels.Profile;
using AutoMapper;
using ResearchManagement.Domain.Enums;
using LoginAttemptViewModel = ResearchManagement.Web.Models.ViewModels.Profile.LoginAttemptViewModel;
using UserActivityStatistics = ResearchManagement.Web.Models.ViewModels.Profile.UserActivityStatistics;
using SecuritySettingsViewModel = ResearchManagement.Web.Models.ViewModels.Profile.SecuritySettingsViewModel;
using UserActivityListViewModel = ResearchManagement.Web.Models.ViewModels.Profile.UserActivityListViewModel;

namespace ResearchManagement.Web.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly IResearchRepository _researchRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<User> userManager,
            IUserRepository userRepository,
            IResearchRepository researchRepository,
            IReviewRepository reviewRepository,
            IMapper mapper,
            ILogger<ProfileController> logger) : base(userManager)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _researchRepository = researchRepository ?? throw new ArgumentNullException(nameof(researchRepository));
            _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var userDto = _mapper.Map<UserDto>(currentUser);
                var userRoles = await _userManager.GetRolesAsync(currentUser);

                var viewModel = new UserProfileViewModel
                {
                    User = userDto,
                    CanEdit = true,
                    Roles = userRoles?.ToList() ?? new List<string>(),
                    ActivityStats = await GetUserActivityStatisticsAsync(currentUser.Id),
                    RecentActivities = await GetRecentActivitiesAsync(currentUser.Id)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الملف الشخصي";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: Profile/Edit
        public async Task<IActionResult> Edit()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = new EditProfileViewModel
                {
                    FirstName = currentUser.FirstName,
                    LastName = currentUser.LastName,
                    FirstNameEn = currentUser.FirstNameEn,
                    LastNameEn = currentUser.LastNameEn,
                    Institution = currentUser.Institution,
                    AcademicDegree = currentUser.AcademicDegree,
                    OrcidId = currentUser.OrcidId,
                    CurrentEmail = currentUser.Email ?? string.Empty
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile edit form");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل نموذج التعديل";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // تحديث البيانات الشخصية
                currentUser.FirstName = model.FirstName;
                currentUser.LastName = model.LastName;
                currentUser.FirstNameEn = model.FirstNameEn;
                currentUser.LastNameEn = model.LastNameEn;
                currentUser.Institution = model.Institution;
                currentUser.AcademicDegree = model.AcademicDegree;
                currentUser.OrcidId = model.OrcidId;
                currentUser.UpdatedAt = DateTime.UtcNow;
                currentUser.UpdatedBy = currentUser.Id;

                var result = await _userManager.UpdateAsync(currentUser);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User profile updated successfully: {UserId}", currentUser.Id);
                    TempData["SuccessMessage"] = "تم تحديث الملف الشخصي بنجاح";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                TempData["ErrorMessage"] = "حدث خطأ في تحديث الملف الشخصي";
            }

            return View(model);
        }

        // GET: Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var result = await _userManager.ChangePasswordAsync(currentUser, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Password changed successfully for user: {UserId}", currentUser.Id);
                    TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                TempData["ErrorMessage"] = "حدث خطأ في تغيير كلمة المرور";
            }

            return View(model);
        }

        // GET: Profile/Activity
        public async Task<IActionResult> Activity(int page = 1, int pageSize = 20)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var activities = await GetUserActivitiesAsync(currentUser.Id, page, pageSize);

                var viewModel = new UserActivityListViewModel
                {
                    Activities = activities,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalActivities = await GetUserActivitiesCountAsync(currentUser.Id)
                };

                viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalActivities / pageSize);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user activities");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الأنشطة";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Profile/Security
        public async Task<IActionResult> Security()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var viewModel = new SecuritySettingsViewModel
                {
                    EmailConfirmed = currentUser.EmailConfirmed,
                    TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(currentUser),
                    LastPasswordChange = currentUser.UpdatedAt,
                    LoginAttempts = await GetRecentLoginAttemptsAsync(currentUser.Id)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security settings");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل إعدادات الأمان";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Profile/EnableTwoFactor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                await _userManager.SetTwoFactorEnabledAsync(currentUser, true);

                _logger.LogInformation("Two-factor authentication enabled for user: {UserId}", currentUser.Id);
                return Json(new { success = true, message = "تم تفعيل المصادقة الثنائية بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling two-factor authentication");
                return Json(new { success = false, message = "حدث خطأ في تفعيل المصادقة الثنائية" });
            }
        }

        // POST: Profile/DisableTwoFactor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                await _userManager.SetTwoFactorEnabledAsync(currentUser, false);

                _logger.LogInformation("Two-factor authentication disabled for user: {UserId}", currentUser.Id);
                return Json(new { success = true, message = "تم إلغاء تفعيل المصادقة الثنائية بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling two-factor authentication");
                return Json(new { success = false, message = "حدث خطأ في إلغاء تفعيل المصادقة الثنائية" });
            }
        }

        // GET: Profile/GetActivityStats - API endpoint for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetActivityStats()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                var stats = await GetUserActivityStatisticsAsync(currentUser.Id);

                return Json(new
                {
                    success = true,
                    stats = new
                    {
                        totalResearches = stats.TotalResearches,
                        acceptedResearches = stats.AcceptedResearches,
                        rejectedResearches = stats.RejectedResearches,
                        pendingResearches = stats.PendingResearches,
                        completedReviews = stats.CompletedReviews,
                        pendingReviews = stats.PendingReviews,
                        overdueReviews = stats.OverdueReviews,
                        averageReviewScore = Math.Round(stats.AverageReviewScore, 1)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity stats for user");
                return Json(new { success = false, message = "حدث خطأ في جلب الإحصائيات" });
            }
        }

        // Helper Methods
        private async Task<UserActivityStatistics> GetUserActivityStatisticsAsync(string userId)
        {
            try
            {
                var researches = await _researchRepository.GetByUserIdAsync(userId);
                var reviews = await _reviewRepository.GetByReviewerIdAsync(userId);
                var pendingReviews = await _reviewRepository.GetPendingReviewsAsync(userId);

                var completedReviews = reviews.Where(r => r.IsCompleted).ToList();
                var averageScore = completedReviews.Any()
                    ? Convert.ToDouble(completedReviews.Average(r => r.OverallScore))
                    : 0.0;

                return new UserActivityStatistics
                {
                    TotalResearches = researches.Count(),
                    AcceptedResearches = researches.Count(r => r.Status == ResearchStatus.Accepted),
                    RejectedResearches = researches.Count(r => r.Status == ResearchStatus.Rejected),
                    PendingResearches = researches.Count(r =>
                        r.Status == ResearchStatus.Submitted ||
                        r.Status == ResearchStatus.UnderReview ||
                        r.Status == ResearchStatus.UnderEvaluation),
                    TotalReviews = reviews.Count(),
                    CompletedReviews = completedReviews.Count,
                    PendingReviews = pendingReviews.Count(),
                    OverdueReviews = pendingReviews.Count(r => r.Deadline.HasValue && r.Deadline < DateTime.UtcNow),
                    AverageReviewScore = averageScore,
                    LastActivity = GetLastActivityDate(researches, reviews)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating user activity statistics for user: {UserId}", userId);
                return new UserActivityStatistics
                {
                    TotalResearches = 0,
                    AcceptedResearches = 0,
                    RejectedResearches = 0,
                    PendingResearches = 0,
                    TotalReviews = 0,
                    CompletedReviews = 0,
                    PendingReviews = 0,
                    OverdueReviews = 0,
                    AverageReviewScore = 0.0,
                    LastActivity = null
                };
            }
        }

        private DateTime? GetLastActivityDate(IEnumerable<Research> researches, IEnumerable<Review> reviews)
        {
            try
            {
                var lastResearchDate = researches.Any() ? researches.Max(r => r.SubmissionDate) : (DateTime?)null;
                var completedReviews = reviews.Where(r => r.CompletedDate.HasValue);
                var lastReviewDate = completedReviews.Any()
                    ? completedReviews.Max(r => r.CompletedDate!.Value)
                    : (DateTime?)null;

                if (lastResearchDate.HasValue && lastReviewDate.HasValue)
                    return lastResearchDate > lastReviewDate ? lastResearchDate : lastReviewDate;

                return lastResearchDate ?? lastReviewDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating last activity date");
                return null;
            }
        }

        private async Task<List<ProfileActivityViewModel>> GetRecentActivitiesAsync(string userId)
        {
            try
            {
                var activities = new List<ProfileActivityViewModel>();

                // أنشطة البحوث
                var researches = await _researchRepository.GetByUserIdAsync(userId);
                foreach (var research in researches.OrderByDescending(r => r.SubmissionDate).Take(5))
                {
                    activities.Add(new ProfileActivityViewModel
                    {
                        ActivityType = "research_submitted",
                        Title = "تقديم بحث",
                        Description = $"تم تقديم البحث: {research.Title ?? "غير محدد"}",
                        CreatedAt = research.SubmissionDate,
                        ActionUrl = Url.Action("Details", "Research", new { id = research.Id }),
                        StatusClass = "primary",
                        Icon = "fas fa-file-upload"
                    });
                }

                // أنشطة المراجعات
                var reviews = await _reviewRepository.GetByReviewerIdAsync(userId);
                foreach (var review in reviews.Where(r => r.IsCompleted).OrderByDescending(r => r.CompletedDate).Take(5))
                {
                    activities.Add(new ProfileActivityViewModel
                    {
                        ActivityType = "review_completed",
                        Title = "إكمال مراجعة",
                        Description = $"تم إكمال مراجعة البحث: {review.Research?.Title ?? "غير محدد"}",
                        CreatedAt = review.CompletedDate ?? review.AssignedDate,
                        ActionUrl = Url.Action("Details", "Review", new { id = review.Id }),
                        StatusClass = "success",
                        Icon = "fas fa-check-circle"
                    });
                }

                return activities.OrderByDescending(a => a.CreatedAt).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent activities for user: {UserId}", userId);
                return new List<ProfileActivityViewModel>();
            }
        }

        private async Task<List<ProfileActivityViewModel>> GetUserActivitiesAsync(string userId, int page, int pageSize)
        {
            try
            {
                var activities = await GetRecentActivitiesAsync(userId);

                return activities
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activities for page {Page}, size {PageSize}", page, pageSize);
                return new List<ProfileActivityViewModel>();
            }
        }

        private async Task<int> GetUserActivitiesCountAsync(string userId)
        {
            try
            {
                var researches = await _researchRepository.GetByUserIdAsync(userId);
                var reviews = await _reviewRepository.GetByReviewerIdAsync(userId);

                return researches.Count() + reviews.Count(r => r.IsCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting user activities for user: {UserId}", userId);
                return 0;
            }
        }

        private async Task<List<LoginAttemptViewModel>> GetRecentLoginAttemptsAsync(string userId)
        {
            // هذه دالة وهمية - يمكن تطويرها لاحقاً لتتبع محاولات الدخول
            await Task.CompletedTask;

            return new List<LoginAttemptViewModel>
            {
                new LoginAttemptViewModel
                {
                    IpAddress = "192.168.1.100",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    LoginTime = DateTime.UtcNow.AddHours(-2),
                    IsSuccessful = true,
                    Location = "Baghdad, Iraq"
                },
                new LoginAttemptViewModel
                {
                    IpAddress = "192.168.1.101",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    LoginTime = DateTime.UtcNow.AddDays(-1),
                    IsSuccessful = true,
                    Location = "Baghdad, Iraq"
                }
            };
        }

        // Helper methods for TempData messages
        private void AddSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        private void AddErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }
    }
}