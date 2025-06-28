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

        public async Task<IActionResult> ResearchDetails(int id)
        {
            try
            {
                var research = await _researchRepository.GetByIdWithDetailsAsync(id);
                if (research == null)
                    return NotFound();

                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
                if (trackManager == null || research.Track != trackManager.Track)
                {
                    AddErrorMessage("ليس لديك صلاحية للوصول إلى هذا البحث");
                    return RedirectToAction("Index");
                }

                var reviews = await _reviewRepository.GetByResearchIdAsync(id);
                
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

        public async Task<IActionResult> AssignReviews()
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

                //var pendingResearches = await _researchRepository.GetByStatusAndTrackAsync(
                //    ResearchStatus.Submitted, trackManager.Track);

                var trackReviewers = await _trackManagerRepository.GetTrackReviewersAsync(trackManager.Id);
                
                var model = new AssignReviewsViewModel
                {
                    TrackId = trackManager.Id,
                    TrackName = trackManager.TrackDescription ?? trackManager.Track.ToString(),
                    //PendingResearches = pendingResearches.ToList(),
                    Reviewers = trackReviewers.Select(tr => tr.Reviewer).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement AssignReviews");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReview(AssignReviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    AddErrorMessage("بيانات غير صحيحة");
                    return RedirectToAction("AssignReviews");
                }

                var userId = GetCurrentUserId();
                var trackManager = await _trackManagerRepository.GetByUserIdAsync(userId);
                
                if (trackManager == null)
                {
                    AddErrorMessage("لم يتم العثور على بيانات مدير التراك");
                    return RedirectToAction("Index", "Dashboard");
                }

                var research = await _researchRepository.GetByIdAsync(model.ResearchId);
                if (research == null || research.Track != trackManager.Track)
                {
                    AddErrorMessage("البحث غير موجود أو ليس ضمن تخصصك");
                    return RedirectToAction("AssignReviews");
                }

                // التحقق من عدم وجود مراجعة سابقة لنفس المراجع
                var existingReviews = await _reviewRepository.GetByResearchIdAsync(model.ResearchId);
                if (existingReviews.Any(r => r.ReviewerId == model.ReviewerId))
                {
                    AddErrorMessage("تم تعيين هذا المراجع مسبقاً لهذا البحث");
                    return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
                }

                // إنشاء المراجعة
                var review = new Review
                {
                    ResearchId = model.ResearchId,
                    ReviewerId = model.ReviewerId,
                    Decision = ReviewDecision.NotReviewed,
                    AssignedDate = DateTime.UtcNow,
                    Deadline = DateTime.UtcNow.AddDays(model.DeadlineDays),
                    IsCompleted = false,
                    CreatedBy = userId
                };

                await _reviewRepository.AddAsync(review);

                // تحديث حالة البحث
                if (research.Status == ResearchStatus.Submitted)
                {
                    research.Status = ResearchStatus.UnderReview;
                    await _researchRepository.UpdateAsync(research);
                }

                AddSuccessMessage("تم تعيين المراجع بنجاح");
                return RedirectToAction("ResearchDetails", new { id = model.ResearchId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrackManagement AssignReview");
                AddErrorMessage($"حدث خطأ: {ex.Message}");
                return RedirectToAction("AssignReviews");
            }
        }

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