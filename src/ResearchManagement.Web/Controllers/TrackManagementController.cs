using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Web.Models.ViewModels;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using ResearchManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ResearchManagement.Web.Controllers
{

    [Authorize(Roles = "TrackManager,Admin")]
    public class TrackManagementController : BaseController
    {

        private readonly IMediator _mediator;
        private readonly ITrackManagerRepository _trackManagerRepository;
        private readonly IResearchRepository _researchRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<TrackManagementController> _logger;
        private readonly ApplicationDbContext _context; // إضافة هذا
        public TrackManagementController(
            UserManager<User> userManager,
            IMediator mediator,
            ITrackManagerRepository trackManagerRepository,
            IResearchRepository researchRepository,
            IReviewRepository reviewRepository,
            IUserRepository userRepository,
            
            ILogger<TrackManagementController> logger,
            ApplicationDbContext context) : base(userManager) 
        {
            _mediator = mediator;
            _trackManagerRepository = trackManagerRepository;
            _researchRepository = researchRepository;
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _logger = logger;
            _context = context; // إضافة هذا
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
                if (trackManager == null)
                {
                    AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
                    return RedirectToAction("Index", "Dashboard");
                }

                var researches = await _trackManagerRepository.GetManagedResearchesAsync(trackManager.Id);
                
                var model = new TrackResearchesViewModel
                {
                    TrackId = trackManager.Id,
                    TrackName = trackManager.TrackDescription ?? trackManager.Track.ToString(),
                    Researches = researches.ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement Index");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("Index", "Dashboard");
            }
        }



        // إصلاح شامل لـ TrackManagementController.cs

        public async Task<IActionResult> ResearchDetails(int id)
        {
            try
            {
                // الحصول على البحث مع جميع التفاصيل
                var research = await _context.Researches
                    .Include(r => r.Authors)
                    .Include(r => r.Files)
                    .Include(r => r.SubmittedBy)
                    .Where(r => !r.IsDeleted)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (research == null)
                    return NotFound();

                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

                if (trackManager == null || research.Track != trackManager.Track)
                {
                    AddErrorMessage("ليس لديك صلاحية للوصول إلى هذا البحث");
                    return RedirectToAction("Index");
                }

                // الحصول على المراجعات مع بيانات المراجعين - هنا المشكلة الأساسية
                var reviews = await _context.Reviews
                    .Include(r => r.Reviewer) // مهم جداً!
                    .Where(r => r.ResearchId == id && !r.IsDeleted)
                    .OrderBy(r => r.AssignedDate)
                    .ToListAsync();

                _logger.LogInformation($"Found {reviews.Count} reviews for research {id}");

                // طباعة تفاصيل المراجعات للتشخيص
                foreach (var review in reviews)
                {
                    _logger.LogInformation($"Review ID: {review.Id}, Reviewer: {review.Reviewer?.FirstName} {review.Reviewer?.LastName}, Completed: {review.IsCompleted}");
                }

                // الحصول على المراجعين المتاحين
                List<User> availableReviewers = new List<User>();

                try
                {
                    // الحصول على المراجعين المخصصين للتراك
                    var trackReviewers = await _context.TrackReviewers
                        .Include(tr => tr.Reviewer)
                        .Where(tr => tr.TrackManagerId == trackManager.Id &&
                                    tr.IsActive &&
                                    !tr.IsDeleted)
                        .Select(tr => tr.Reviewer)
                        .ToListAsync();

                    _logger.LogInformation($"Found {trackReviewers.Count} track reviewers");

                    // إذا لم يوجد مراجعين في التراك، استخدم جميع المراجعين
                    if (trackReviewers.Count == 0)
                    {
                        _logger.LogWarning("No track reviewers found, getting all reviewers");

                        var allReviewers = await _context.Users
                            .Where(u => u.Role == UserRole.Reviewer &&
                                       !u.IsDeleted &&
                                       u.EmailConfirmed)
                            .ToListAsync();

                        trackReviewers = allReviewers;
                        _logger.LogInformation($"Found {allReviewers.Count} total reviewers in system");
                    }

                    // استبعاد المراجعين المعينين مسبقاً
                    var existingReviewerIds = reviews.Select(r => r.ReviewerId).ToHashSet();
                    availableReviewers = trackReviewers
                        .Where(reviewer => !existingReviewerIds.Contains(reviewer.Id))
                        .ToList();

                    _logger.LogInformation($"Available reviewers after filtering: {availableReviewers.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting available reviewers");
                    availableReviewers = new List<User>();
                }

                // إضافة البيانات إلى ViewBag
                ViewBag.AvailableReviewers = availableReviewers;
                ViewBag.DebugInfo = new
                {
                    TrackManagerId = trackManager.Id,
                    TrackManagerTrack = trackManager.Track.ToString(),
                    ResearchId = id,
                    ReviewsCount = reviews.Count,
                    AvailableReviewersCount = availableReviewers.Count,
                    AllReviewersInSystem = await _context.Users.CountAsync(u => u.Role == UserRole.Reviewer && !u.IsDeleted),
                    TrackReviewersCount = await _context.TrackReviewers.CountAsync(tr => tr.TrackManagerId == trackManager.Id && tr.IsActive),
                    ReviewDetails = reviews.Select(r => new {
                        Id = r.Id,
                        ReviewerName = $"{r.Reviewer?.FirstName} {r.Reviewer?.LastName}",
                        IsCompleted = r.IsCompleted,
                        AssignedDate = r.AssignedDate
                    }).ToList()
                };

                var model = new TrackResearchDetailsViewModel
                {
                    Research = research,
                    Reviews = reviews.ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement ResearchDetails");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        // إصلاح AssignReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReview(AssignReviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    AddErrorMessage("بيانات غير صحيحة");
                    return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
                }

                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

                if (trackManager == null)
                {
                    AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
                    return RedirectToAction("Index", "Dashboard");
                }

                // التحقق من البحث
                var research = await _context.Researches
                    .FirstOrDefaultAsync(r => r.Id == model.ResearchId && !r.IsDeleted);

                if (research == null || research.Track != trackManager.Track)
                {
                    AddErrorMessage("البحث غير موجود أو ليس ضمن تخصصك");
                    return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
                }

                // التحقق من المراجع
                var reviewer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == model.ReviewerId && u.Role == UserRole.Reviewer);

                if (reviewer == null)
                {
                    AddErrorMessage("المراجع المحدد غير موجود");
                    return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
                }

                // التحقق من عدم وجود مراجعة سابقة لنفس المراجع
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ResearchId == model.ResearchId &&
                                             r.ReviewerId == model.ReviewerId &&
                                             !r.IsDeleted);

                if (existingReview != null)
                {
                    AddErrorMessage("تم تعيين هذا المراجع مسبقاً لهذا البحث");
                    return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
                }

                // إنشاء المراجعة الجديدة
                var review = new Review
                {
                    ResearchId = model.ResearchId,
                    ReviewerId = model.ReviewerId,
                    Decision = ReviewDecision.NotReviewed,
                    AssignedDate = DateTime.UtcNow,
                    Deadline = DateTime.UtcNow.AddDays(model.DeadlineDays),
                    IsCompleted = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // تحديث حالة البحث إذا كانت أول مراجعة
                if (research.Status == ResearchStatus.Submitted)
                {
                    research.Status = ResearchStatus.UnderReview;
                    research.UpdatedAt = DateTime.UtcNow;
                    research.UpdatedBy = userId;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Successfully assigned reviewer {model.ReviewerId} to research {model.ResearchId}");

                AddSuccessMessage($"تم تعيين المراجع {reviewer.FirstName} {reviewer.LastName} بنجاح");
                return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignReview");
                AddErrorMessage($"حدث خطأ في تعيين المراجع: {ex.Message}");
                return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
            }
        }












        //// 1. تحديث TrackManagementController.cs - تعديل ResearchDetails

        //public async Task<IActionResult> ResearchDetails(int id)
        //{
        //    try
        //    {
        //        var research = await _researchRepository.GetByIdWithDetailsAsync(id);
        //        if (research == null)
        //            return NotFound();

        //        var userId = GetCurrentUserId();
        //        var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

        //        if (trackManager == null || research.Track != trackManager.Track)
        //        {
        //            AddErrorMessage("ليس لديك صلاحية للوصول إلى هذا البحث");
        //            return RedirectToAction("Index");
        //        }

        //        var reviews = await _reviewRepository.GetByResearchIdAsync(id);
        //        List<User> availableReviewers = new List<User>();

        //        try
        //        {
        //            // أولاً: محاولة الحصول على المراجعين المخصصين للتراك
        //            var trackReviewers = await _context.TrackReviewers
        //                .Include(tr => tr.Reviewer)
        //                .Where(tr => tr.TrackManagerId == trackManager.Id &&
        //                            tr.IsActive &&
        //                            !tr.IsDeleted)
        //                .Select(tr => tr.Reviewer)
        //                .ToListAsync();

        //            _logger.LogInformation($"المراجعين في التراك: {trackReviewers.Count}");

        //            // إذا لم يوجد مراجعين في التراك، استخدم جميع المراجعين
        //            if (trackReviewers.Count == 0)
        //            {
        //                _logger.LogWarning("لا يوجد مراجعين مخصصين للتراك، سيتم عرض جميع المراجعين");

        //                var allReviewers = await _context.Users
        //                    .Where(u => u.Role == UserRole.Reviewer &&
        //                               !u.IsDeleted &&
        //                               u.EmailConfirmed)
        //                    .ToListAsync();

        //                trackReviewers = allReviewers;
        //                _logger.LogInformation($"إجمالي المراجعين في النظام: {allReviewers.Count}");
        //            }

        //            // استبعاد المراجعين المعينين مسبقاً
        //            var existingReviewerIds = reviews.Select(r => r.ReviewerId).ToHashSet();
        //            availableReviewers = trackReviewers
        //                .Where(reviewer => !existingReviewerIds.Contains(reviewer.Id))
        //                .ToList();

        //            _logger.LogInformation($"المراجعين المتاحين للتعيين: {availableReviewers.Count}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "خطأ في الحصول على المراجعين");
        //            availableReviewers = new List<User>();
        //        }

        //        // إضافة البيانات إلى ViewBag
        //        ViewBag.AvailableReviewers = availableReviewers;
        //        ViewBag.DebugInfo = new
        //        {
        //            TrackManagerId = trackManager.Id,
        //            TrackManagerTrack = trackManager.Track.ToString(),
        //            ResearchId = id,
        //            ReviewsCount = reviews.Count(),
        //            AvailableReviewersCount = availableReviewers.Count,
        //            AllReviewersInSystem = await _context.Users.CountAsync(u => u.Role == UserRole.Reviewer && !u.IsDeleted),
        //            TrackReviewersCount = await _context.TrackReviewers.CountAsync(tr => tr.TrackManagerId == trackManager.Id && tr.IsActive)
        //        };

        //        var model = new TrackResearchDetailsViewModel
        //        {
        //            Research = research,
        //            Reviews = reviews.ToList()
        //        };

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in TrackManagement ResearchDetails");
        //        AddErrorMessage($"حدث خطأ: {ex.Message}");
        //        return RedirectToAction("Index");
        //    }
        //}

        //// 2. إضافة أكشن لربط المراجعين بالتراك تلقائياً
        [HttpPost]
        public async Task<IActionResult> AutoAssignReviewersToTrack()
        {
            try
            {
                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

                if (trackManager == null)
                {
                    return Json(new { success = false, message = "لم يتم العثور على بيانات مدير التراك" });
                }

                // الحصول على جميع المراجعين في النظام
                var allReviewers = await _context.Users
                    .Where(u => u.Role == UserRole.Reviewer && !u.IsDeleted)
                    .ToListAsync();

                // الحصول على المراجعين المرتبطين بالتراك حالياً
                var existingTrackReviewers = await _context.TrackReviewers
                    .Where(tr => tr.TrackManagerId == trackManager.Id && tr.IsActive)
                    .Select(tr => tr.ReviewerId)
                    .ToListAsync();

                // إضافة المراجعين الغير مرتبطين
                var reviewersToAdd = allReviewers
                    .Where(r => !existingTrackReviewers.Contains(r.Id))
                    .ToList();

                foreach (var reviewer in reviewersToAdd)
                {
                    var trackReviewer = new TrackReviewer
                    {
                        TrackManagerId = trackManager.Id,
                        ReviewerId = reviewer.Id,
                        Track = trackManager.Track,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };

                    await _context.TrackReviewers.AddAsync(trackReviewer);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم ربط {reviewersToAdd.Count} مراجع بالتراك {trackManager.Track}");

                return Json(new
                {
                    success = true,
                    message = $"تم ربط {reviewersToAdd.Count} مراجع بالتراك بنجاح",
                    addedCount = reviewersToAdd.Count,
                    totalTrackReviewers = existingTrackReviewers.Count + reviewersToAdd.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في ربط المراجعين بالتراك");
                return Json(new { success = false, message = "حدث خطأ في ربط المراجعين" });
            }
        }

        //// 3. تحديث GetAvailableReviewersForResearch
        //[HttpGet]
        //public async Task<IActionResult> GetAvailableReviewersForResearch(int researchId)
        //{
        //    try
        //    {
        //        var userId = GetCurrentUserId();
        //        var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

        //        if (trackManager == null)
        //        {
        //            return Json(new { success = false, message = "لم يتم العثور على بيانات مدير التراك" });
        //        }

        //        var research = await _researchRepository.GetByIdAsync(researchId);
        //        if (research == null || research.Track != trackManager.Track)
        //        {
        //            return Json(new { success = false, message = "البحث غير موجود أو ليس في تراكك" });
        //        }

        //        // الحصول على المراجعات الموجودة
        //        var existingReviews = await _reviewRepository.GetByResearchIdAsync(researchId);
        //        var existingReviewerIds = existingReviews.Select(r => r.ReviewerId).ToHashSet();

        //        // محاولة الحصول على مراجعين التراك أولاً
        //        var trackReviewers = await _context.TrackReviewers
        //            .Include(tr => tr.Reviewer)
        //            .Where(tr => tr.TrackManagerId == trackManager.Id && tr.IsActive)
        //            .Select(tr => tr.Reviewer)
        //            .Where(r => !existingReviewerIds.Contains(r.Id))
        //            .ToListAsync();

        //        List<object> availableReviewers;

        //        if (trackReviewers.Any())
        //        {
        //            // استخدام مراجعين التراك
        //            availableReviewers = trackReviewers.Select(r => new
        //            {
        //                Id = r.Id,
        //                Name = $"{r.FirstName} {r.LastName}",
        //                Email = r.Email,
        //                Institution = r.Institution ?? "",
        //                FullDisplay = $"{r.FirstName} {r.LastName} - {r.Email}" +
        //                            (string.IsNullOrEmpty(r.Institution) ? "" : $" ({r.Institution})")
        //            }).ToList<object>();

        //            return Json(new
        //            {
        //                success = true,
        //                reviewers = availableReviewers,
        //                totalFound = availableReviewers.Count,
        //                method = "track_reviewers",
        //                message = "تم العثور على مراجعين مخصصين للتراك"
        //            });
        //        }
        //        else
        //        {
        //            // استخدام جميع المراجعين كحل احتياطي
        //            var allReviewers = await _context.Users
        //                .Where(u => u.Role == UserRole.Reviewer && !u.IsDeleted)
        //                .Where(r => !existingReviewerIds.Contains(r.Id))
        //                .ToListAsync();

        //            availableReviewers = allReviewers.Select(r => new
        //            {
        //                Id = r.Id,
        //                Name = $"{r.FirstName} {r.LastName}",
        //                Email = r.Email,
        //                Institution = r.Institution ?? "",
        //                FullDisplay = $"{r.FirstName} {r.LastName} - {r.Email}" +
        //                            (string.IsNullOrEmpty(r.Institution) ? "" : $" ({r.Institution})") + " (غير مخصص للتراك)"
        //            }).ToList<object>();

        //            return Json(new
        //            {
        //                success = true,
        //                reviewers = availableReviewers,
        //                totalFound = availableReviewers.Count,
        //                method = "all_reviewers",
        //                message = "لا يوجد مراجعين مخصصين للتراك - عرض جميع المراجعين",
        //                needsTrackAssignment = true
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error getting available reviewers for research {researchId}");
        //        return Json(new { success = false, message = "حدث خطأ في استرجاع البيانات" });
        //    }
        //}






        //  في TrackManagementController.cs - تحديث أكشن ResearchDetails

        //  الحل النهائي في TrackManagementController.cs

        //////////public async Task<IActionResult> ResearchDetails(int id)
        //////////{
        //////////    try
        //////////    {
        //////////        var research = await _researchRepository.GetByIdWithDetailsAsync(id);
        //////////        if (research == null)
        //////////            return NotFound();

        //////////        var userId = GetCurrentUserId();
        //////////        var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

        //////////        if (trackManager == null || research.Track != trackManager.Track)
        //////////        {
        //////////            AddErrorMessage("ليس لديك صلاحية للوصول إلى هذا البحث");
        //////////            return RedirectToAction("Index");
        //////////        }

        //////////        var reviews = await _reviewRepository.GetByResearchIdAsync(id);

        //////////        // الحل الشامل: استخدام عدة طرق للحصول على المراجعين
        //////////        List<User> availableReviewers = new List<User>();

        //////////        try
        //////////        {
        //////////            // الطريقة الأولى: المراجعين المخصصين للتراك
        //////////            var trackReviewers = await _context.TrackReviewers
        //////////                .Include(tr => tr.Reviewer)
        //////////                .Where(tr => tr.TrackManagerId == trackManager.Id &&
        //////////                            tr.IsActive &&
        //////////                            !tr.IsDeleted)
        //////////                .Select(tr => tr.Reviewer)
        //////////                .ToListAsync();

        //////////            // استبعاد المراجعين المعينين مسبقاً
        //////////            var existingReviewerIds = reviews.Select(r => r.ReviewerId).ToHashSet();
        //////////            availableReviewers = trackReviewers
        //////////                .Where(reviewer => !existingReviewerIds.Contains(reviewer.Id))
        //////////                .ToList();

        //////////            _logger.LogInformation($"Method 1: Found {trackReviewers.Count} track reviewers, {availableReviewers.Count} available");

        //////////            // إذا لم نجد مراجعين في التراك، استخدم جميع المراجعين في النظام
        //////////            if (availableReviewers.Count == 0)
        //////////            {
        //////////                var allReviewers = await _context.Users
        //////////                    .Where(u => u.Role == UserRole.Reviewer &&
        //////////                               !u.IsDeleted &&
        //////////                               u.EmailConfirmed)
        //////////                    .ToListAsync();

        //////////                availableReviewers = allReviewers
        //////////                    .Where(reviewer => !existingReviewerIds.Contains(reviewer.Id))
        //////////                    .ToList();

        //////////                _logger.LogInformation($"Method 2: Found {allReviewers.Count} system reviewers, {availableReviewers.Count} available");
        //////////            }

        //////////            // إذا لم نجد أي مراجعين، أنشئ قائمة تجريبية
        //////////            if (availableReviewers.Count == 0)
        //////////            {
        //////////                _logger.LogWarning("No reviewers found, creating test data");

        //////////                // إنشاء مراجعين تجريبيين إذا لم يوجدوا
        //////////                var testReviewers = await CreateTestReviewersIfNeeded();
        //////////                availableReviewers.AddRange(testReviewers);
        //////////            }
        //////////        }
        //////////        catch (Exception ex)
        //////////        {
        //////////            _logger.LogError(ex, "Error getting available reviewers, using fallback");

        //////////            // حل احتياطي: قائمة ثابتة
        //////////            availableReviewers = GetFallbackReviewers();
        //////////        }

        //////////        // إضافة البيانات إلى ViewBag و ViewData
        //////////        ViewBag.AvailableReviewers = availableReviewers;
        //////////        ViewData["AvailableReviewers"] = availableReviewers;
        //////////        ViewBag.DebugInfo = new
        //////////        {
        //////////            TrackManagerId = trackManager.Id,
        //////////            ResearchId = id,
        //////////            ReviewsCount = reviews.Count(),
        //////////            AvailableReviewersCount = availableReviewers.Count
        //////////        };

        //////////        var model = new TrackResearchDetailsViewModel
        //////////        {
        //////////            Research = research,
        //////////            Reviews = reviews.ToList()
        //////////        };

        //////////        return View(model);
        //////////    }
        //////////    catch (Exception ex)
        //////////    {
        //////////        _logger.LogError(ex, "Error in TrackManagement ResearchDetails");
        //////////        AddErrorMessage($"حدث خطأ: {ex.Message}");
        //////////        return RedirectToAction("Index");
        //////////    }
        //////////}

        // إنشاء مراجعين تجريبيين إذا لم يوجدوا
        private async Task<List<User>> CreateTestReviewersIfNeeded()
        {
            var existingTestReviewers = await _context.Users
                .Where(u => u.Email.Contains("test.reviewer"))
                .ToListAsync();

            if (existingTestReviewers.Count >= 2)
            {
                return existingTestReviewers;
            }

            var testReviewers = new List<User>();

            if (!existingTestReviewers.Any(u => u.Email == "ahmed.test.reviewer@example.com"))
            {
                var reviewer1 = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "د. أحمد",
                    LastName = "محمد",
                    Email = "ahmed.test.reviewer@example.com",
                    UserName = "ahmed.test.reviewer@example.com",
                    Role = UserRole.Reviewer,
                    EmailConfirmed = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    Institution = "الجامعة التجريبية"
                };

                await _context.Users.AddAsync(reviewer1);
                testReviewers.Add(reviewer1);
            }

            if (!existingTestReviewers.Any(u => u.Email == "fatima.test.reviewer@example.com"))
            {
                var reviewer2 = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "د. فاطمة",
                    LastName = "علي",
                    Email = "fatima.test.reviewer@example.com",
                    UserName = "fatima.test.reviewer@example.com",
                    Role = UserRole.Reviewer,
                    EmailConfirmed = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    Institution = "الجامعة التجريبية"
                };

                await _context.Users.AddAsync(reviewer2);
                testReviewers.Add(reviewer2);
            }

            if (testReviewers.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created {testReviewers.Count} test reviewers");
            }

            return existingTestReviewers.Concat(testReviewers).ToList();
        }

        // قائمة احتياطية ثابتة
        private List<User> GetFallbackReviewers()
        {
            return new List<User>
        {
            new User
            {
                Id = "fallback-1",
                FirstName = "د. خالد",
                LastName = "حسن",
                Email = "khalid.fallback@example.com",
                Institution = "مراجع احتياطي"
            },
            new User
            {
                Id = "fallback-2",
                FirstName = "د. مريم",
                LastName = "أحمد",
                Email = "mariam.fallback@example.com",
                Institution = "مراجع احتياطي"
            }
        };
        }

        // API محسّن للحصول على المراجعين
        [HttpGet]
        public async Task<IActionResult> GetAvailableReviewersForResearch(int researchId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

                if (trackManager == null)
                {
                    return Json(new { success = false, message = "لم يتم العثور على بيانات مدير التراك" });
                }

                // نفس المنطق المستخدم في ResearchDetails
                var reviews = await _reviewRepository.GetByResearchIdAsync(researchId);
                var existingReviewerIds = reviews.Select(r => r.ReviewerId).ToHashSet();

                // جرب عدة طرق للحصول على المراجعين
                var availableReviewers = new List<object>();

                // الطريقة الأولى: مراجعين التراك
                var trackReviewers = await _context.TrackReviewers
                    .Include(tr => tr.Reviewer)
                    .Where(tr => tr.TrackManagerId == trackManager.Id && tr.IsActive)
                    .Select(tr => tr.Reviewer)
                    .Where(r => !existingReviewerIds.Contains(r.Id))
                    .Select(r => new
                    {
                        Id = r.Id,
                        Name = $"{r.FirstName} {r.LastName}",
                        Email = r.Email,
                        Institution = r.Institution ?? "",
                        FullDisplay = $"{r.FirstName} {r.LastName} - {r.Email}" +
                                    (string.IsNullOrEmpty(r.Institution) ? "" : $" ({r.Institution})")
                    })
                    .ToListAsync();

                if (trackReviewers.Any())
                {
                    availableReviewers.AddRange(trackReviewers);
                }
                else
                {
                    // الطريقة الثانية: جميع المراجعين
                    var allReviewers = await _context.Users
                        .Where(u => u.Role == UserRole.Reviewer && !u.IsDeleted)
                        .Where(r => !existingReviewerIds.Contains(r.Id))
                        .Select(r => new
                        {
                            Id = r.Id,
                            Name = $"{r.FirstName} {r.LastName}",
                            Email = r.Email,
                            Institution = r.Institution ?? "",
                            FullDisplay = $"{r.FirstName} {r.LastName} - {r.Email}" +
                                        (string.IsNullOrEmpty(r.Institution) ? "" : $" ({r.Institution})")
                        })
                        .ToListAsync();

                    availableReviewers.AddRange(allReviewers);
                }

                return Json(new
                {
                    success = true,
                    reviewers = availableReviewers,
                    totalFound = availableReviewers.Count,
                    method = trackReviewers.Any() ? "track_reviewers" : "all_reviewers"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available reviewers for research {researchId}");

                // حل احتياطي: إرجاع قائمة ثابتة
                var fallbackReviewers = new[]
                {
                new { Id = "fallback-1", Name = "د. خالد حسن", Email = "khalid@example.com", Institution = "", FullDisplay = "د. خالد حسن - khalid@example.com" },
                new { Id = "fallback-2", Name = "د. مريم أحمد", Email = "mariam@example.com", Institution = "", FullDisplay = "د. مريم أحمد - mariam@example.com" }
            };

                return Json(new
                {
                    success = true,
                    reviewers = fallbackReviewers,
                    totalFound = fallbackReviewers.Length,
                    method = "fallback",
                    message = "تم استخدام قائمة احتياطية"
                });
            }
        }
        //إضافة أكشن منفصل للحصول على المراجعين المتاحين
       //[HttpGet]
       // public async Task<IActionResult> GetAvailableReviewersForResearch(int researchId)
       // {
       //     try
       //     {
       //         var userId = GetCurrentUserId();
       //         var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

       //         if (trackManager == null)
       //         {
       //             return Json(new { success = false, message = "لم يتم العثور على بيانات مدير التراك" });
       //         }

       //         // الحصول على البحث للتأكد من التراك
       //         var research = await _researchRepository.GetByIdAsync(researchId);
       //         if (research == null || research.Track != trackManager.Track)
       //         {
       //             return Json(new { success = false, message = "البحث غير موجود أو ليس في تراكك" });
       //         }

       //         // الحصول على المراجعين في التراك
       //         var trackReviewers = await _context.TrackReviewers
       //             .Include(tr => tr.Reviewer)
       //             .Where(tr => tr.TrackManagerId == trackManager.Id &&
       //                         tr.IsActive &&
       //                         !tr.IsDeleted)
       //             .Select(tr => tr.Reviewer)
       //             .ToListAsync();

       //         // الحصول على المراجعات الموجودة
       //         var existingReviews = await _reviewRepository.GetByResearchIdAsync(researchId);
       //         var existingReviewerIds = existingReviews.Select(r => r.ReviewerId).ToHashSet();

       //         // تصفية المراجعين المتاحين
       //         var availableReviewers = trackReviewers
       //             .Where(reviewer => !existingReviewerIds.Contains(reviewer.Id))
       //             .Select(reviewer => new
       //             {
       //                 Id = reviewer.Id,
       //                 Name = $"{reviewer.FirstName} {reviewer.LastName}",
       //                 Email = reviewer.Email,
       //                 Institution = reviewer.Institution ?? "",
       //                 FullDisplay = $"{reviewer.FirstName} {reviewer.LastName} - {reviewer.Email}" +
       //                             (string.IsNullOrEmpty(reviewer.Institution) ? "" : $" ({reviewer.Institution})")
       //             })
       //             .OrderBy(r => r.Name)
       //             .ToList();

       //         return Json(new
       //         {
       //             success = true,
       //             reviewers = availableReviewers,
       //             totalTrackReviewers = trackReviewers.Count,
       //             availableCount = availableReviewers.Count,
       //             assignedCount = existingReviewerIds.Count
       //         });
       //     }
       //     catch (Exception ex)
       //     {
       //         _logger.LogError(ex, $"Error getting available reviewers for research {researchId}");
       //         return Json(new { success = false, message = "حدث خطأ في استرجاع البيانات" });
       //     }
       // }



        //public async Task<IActionResult> ResearchDetails(int id)
        //{
        //    try
        //    {
        //        var research = await _researchRepository.GetByIdWithDetailsAsync(id);
        //        if (research == null)
        //            return NotFound();

        //        var userId = GetCurrentUserId();
        //        var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

        //        if (trackManager == null || research.Track != trackManager.Track)
        //        {
        //            AddErrorMessage("ليس لديك صلاحية للوصول إلى هذا البحث");
        //            return RedirectToAction("Index");
        //        }

        //        var reviews = await _reviewRepository.GetByResearchIdAsync(id);

        //        var model = new TrackResearchDetailsViewModel
        //        {
        //            Research = research,
        //            Reviews = reviews.ToList()
        //        };

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in TrackManagement ResearchDetails");
        //        AddErrorMessage($"حدث خطأ: {ex.Message}");
        //        return RedirectToAction("Index");
        //    }
        //}

     //////   public async Task<IActionResult> AssignReviews()
     //////   {
     //////       try
     //////       {
     //////           var userId = GetCurrentUserId();
     //////           var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
     //////           if (trackManager == null)
     //////           {
     //////               AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
     //////               return RedirectToAction("Index", "Dashboard");
     //////           }

     //////           var pendingResearches = await _context.Researches
     //////.Include(r => r.SubmittedBy)
     //////.Include(r => r.Reviews)
     //////.Include(r => r.Authors)
     //////.Where(r => r.Track == trackManager.Track &&
     //////           !r.IsDeleted &&
     //////           (r.Status == ResearchStatus.Submitted ||
     //////            r.Status == ResearchStatus.UnderReview))
     //////.ToListAsync();

     //////           var trackReviewers = await _trackManagerRepository.GetTrackReviewersAsync(trackManager.Id);
                
     //////           var model = new AssignReviewsViewModel
     //////           {
     //////               TrackId = trackManager.Id,
     //////               TrackName = trackManager.TrackDescription ?? trackManager.Track.ToString(),
     //////               PendingResearches = pendingResearches.ToList(),
     //////               Reviewers = trackReviewers.Select(tr => tr.Reviewer).ToList()
     //////           };

     //////           return View(model);
     //////       }
     //////       catch (Exception ex)
     //////       {
     //////           _logger.LogError(ex, "Error in TrackManagement AssignReviews");
     //////           AddErrorMessage($"حدث خطأ: {ex.Message}");
     //////           return RedirectToAction("Index");
     //////       }
     //////   }

     //////   [HttpPost]
     //////   [ValidateAntiForgeryToken]
     //////   public async Task<IActionResult> AssignReview(AssignReviewViewModel model)
     //////   {
     //////       try
     //////       {
     //////           if (!ModelState.IsValid)
     //////           {
     //////               AddErrorMessage("بيانات غير صحيحة");
     //////               return RedirectToAction("AssignReviews");
     //////           }

     //////           var userId = GetCurrentUserId();
     //////           var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
     //////           if (trackManager == null)
     //////           {
     //////               AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
     //////               return RedirectToAction("Index", "Dashboard");
     //////           }

     //////           var research = await _researchRepository.GetByIdAsync(model.ResearchId);
     //////           if (research == null || research.Track != trackManager.Track)
     //////           {
     //////               AddErrorMessage("البحث غير موجود أو ليس ضمن تخصصك");
     //////               return RedirectToAction("AssignReviews");
     //////           }

     //////           // التحقق من عدم وجود مراجعة سابقة لنفس المراجع
     //////           var existingReviews = await _reviewRepository.GetByResearchIdAsync(model.ResearchId);
     //////           if (existingReviews.Any(r => r.ReviewerId == model.ReviewerId))
     //////           {
     //////               AddErrorMessage("تم تعيين هذا المراجع مسبقاً لهذا البحث");
     //////               return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
     //////           }

     //////           // إنشاء المراجعة
     //////           var review = new Review
     //////           {
     //////               ResearchId = model.ResearchId,
     //////               ReviewerId = model.ReviewerId,
     //////               Decision = ReviewDecision.NotReviewed,
     //////               AssignedDate = DateTime.UtcNow,
     //////               Deadline = DateTime.UtcNow.AddDays(model.DeadlineDays),
     //////               IsCompleted = false,
     //////               CreatedBy = userId
     //////           };

     //////           await _reviewRepository.AddAsync(review);

     //////           // تحديث حالة البحث
     //////           if (research.Status == ResearchStatus.Submitted)
     //////           {
     //////               research.Status = ResearchStatus.UnderReview;
     //////               await _researchRepository.UpdateAsync(research);
     //////           }

     //////           AddSuccessMessage("تم تعيين المراجع بنجاح");
     //////           return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
     //////       }
     //////       catch (Exception ex)
     //////       {
     //////           _logger.LogError(ex, "Error in TrackManagement AssignReview");
     //////           AddErrorMessage($"حدث خطأ: {ex.Message}");
     //////           return RedirectToAction("AssignReviews");
     //////       }
     //////   }

        public async Task<IActionResult> Reviewers()
        {
            try
            {
                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);

                if (trackManager == null)
                {
                    AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
                    return RedirectToAction("Index", "Dashboard");
                }

                var trackReviewers = await _trackManagerRepository.GetTrackReviewersAsync(trackManager.Id);

                var availableReviewers = await _userRepository.GetByRoleAsync(UserRole.Reviewer);
                
                // استبعاد المراجعين الحاليين
                var currentReviewerIds = trackReviewers.Select(tr => tr.ReviewerId).ToList();
                var newReviewers = availableReviewers.Where(u => !currentReviewerIds.Contains(u.Id)).ToList();
                
                var model = new TrackReviewersViewModel
                {
                    TrackId = trackManager.Id,
                    TrackName = trackManager.TrackDescription ?? trackManager.Track.ToString(),
                    CurrentReviewers = trackReviewers.ToList(),
                    AvailableReviewers = newReviewers
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement Reviewers");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReviewer(AddTrackReviewerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    AddErrorMessage("بيانات غير صحيحة");
                    return RedirectToAction("Reviewers");
                }

                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
                if (trackManager == null || trackManager.Id != model.TrackManagerId)
                {
                    AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
                    return RedirectToAction("Index", "Dashboard");
                }

                // التحقق من عدم وجود المراجع مسبقاً
                var trackReviewers = await _trackManagerRepository.GetTrackReviewersAsync(trackManager.Id);
                if (trackReviewers.Any(tr => tr.ReviewerId == model.ReviewerId))
                {
                    AddErrorMessage("المراجع موجود مسبقاً في قائمة المراجعين");
                    return RedirectToAction("Reviewers");
                }

                // إضافة المراجع
                var trackReviewer = new TrackReviewer
                {
                    TrackManagerId = trackManager.Id,
                    ReviewerId = model.ReviewerId,
                    Track = trackManager.Track,
                    IsActive = true,
                    CreatedBy = userId
                };

                await _context.TrackReviewers.AddAsync(trackReviewer);
                await _context.SaveChangesAsync();

                AddSuccessMessage("تمت إضافة المراجع بنجاح");
                return RedirectToAction("Reviewers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement AddReviewer");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("Reviewers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveReviewer(int reviewerId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
                if (trackManager == null)
                {
                    AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
                    return RedirectToAction("Index", "Dashboard");
                }

                // البحث عن المراجع
                var trackReviewer = await _context.TrackReviewers
                    .FirstOrDefaultAsync(tr => tr.Id == reviewerId && tr.TrackManagerId == trackManager.Id);
                
                if (trackReviewer == null)
                {
                    AddErrorMessage("المراجع غير موجود");
                    return RedirectToAction("Reviewers");
                }

                // حذف المراجع (soft delete)
                trackReviewer.IsActive = false;
                trackReviewer.IsDeleted = true;
                trackReviewer.UpdatedAt = DateTime.UtcNow;
                trackReviewer.UpdatedBy = userId;

                await _context.SaveChangesAsync();

                AddSuccessMessage("تمت إزالة المراجع بنجاح");
                return RedirectToAction("Reviewers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement RemoveReviewer");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("Reviewers");
            }
        }

        public async Task<IActionResult> Reports()
        {
            try
            {
                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
                if (trackManager == null)
                {
                    AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
                    return RedirectToAction("Index", "Dashboard");
                }

                var researches = await _trackManagerRepository.GetManagedResearchesAsync(trackManager.Id);
                
                var model = new TrackReportsViewModel
                {
                    TrackId = trackManager.Id,
                    TrackName = trackManager.TrackDescription ?? trackManager.Track.ToString(),
                    TotalResearches = researches.Count(),
                    SubmittedResearches = researches.Count(r => r.Status == ResearchStatus.Submitted),
                    UnderReviewResearches = researches.Count(r => r.Status == ResearchStatus.UnderReview),
                    UnderEvaluationResearches = researches.Count(r => r.Status == ResearchStatus.UnderEvaluation),
                    AcceptedResearches = researches.Count(r => r.Status == ResearchStatus.Accepted),
                    RejectedResearches = researches.Count(r => r.Status == ResearchStatus.Rejected),
                    ResearchesByMonth = GetResearchesByMonth(researches),
                    ResearchesByType = GetResearchesByType(researches)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement Reports");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        private Dictionary<string, int> GetResearchesByMonth(IEnumerable<Research> researches)
        {
            var result = new Dictionary<string, int>();
            var groupedByMonth = researches
                .GroupBy(r => new { Month = r.SubmissionDate.Month, Year = r.SubmissionDate.Year })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month);

            foreach (var group in groupedByMonth)
            {
                var monthName = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMM yyyy");
                result.Add(monthName, group.Count());
            }

            return result;
        }

        private Dictionary<string, int> GetResearchesByType(IEnumerable<Research> researches)
        {
            var result = new Dictionary<string, int>();
            var groupedByType = researches.GroupBy(r => r.ResearchType);

            foreach (var group in groupedByType)
            {
                var typeName = group.Key.ToString();
                result.Add(typeName, group.Count());
            }

            return result;
        }
    }
}