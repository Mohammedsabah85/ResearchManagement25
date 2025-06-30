using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Commands.Review;
using ResearchManagement.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Web.Models.ViewModels.Review;
using ResearchManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using ResearchManagement.Application.Queries.Research;

namespace ResearchManagement.Web.Controllers
{
    [Authorize(Roles = "Reviewer,TrackManager")]
    public class ReviewController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly IReviewRepository _reviewRepository;
        private readonly IResearchRepository _researchRepository;
        private readonly ILogger<ResearchController> _logger;
        private readonly ApplicationDbContext _context; // إضافة هذا

        public ReviewController(
            UserManager<User> userManager,
            IMediator mediator,
            IReviewRepository reviewRepository,
            IResearchRepository researchRepository,
            ILogger<ResearchController> logger,
            ApplicationDbContext context) : base(userManager) 
        {
            _mediator = mediator;
            _reviewRepository = reviewRepository;
            _researchRepository = researchRepository;
            _logger = logger;
             _context = context; 
        }

        // In your ReviewController.cs, replace the Index action with this:

        // In your ReviewController.cs, replace the Index action with this:

        public async Task<IActionResult> Index(ReviewFilterViewModel filter = null)
        {
            try
            {
                filter ??= new ReviewFilterViewModel();
                var currentUserId = GetCurrentUserId();
                var currentUser = await GetCurrentUserAsync();

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                // Get reviews based on user role
                IQueryable<Review> reviewsQuery = _context.Reviews
                    .Include(r => r.Research)
                        .ThenInclude(res => res.Authors)
                    .Include(r => r.Reviewer)
                    .Where(r => !r.IsDeleted);

                // Filter by user role
                if (currentUser.Role == UserRole.Reviewer)
                {
                    reviewsQuery = reviewsQuery.Where(r => r.ReviewerId == currentUserId);
                }
                else if (currentUser.Role == UserRole.TrackManager)
                {
                    var trackManager = await _context.TrackManagers
                        .FirstOrDefaultAsync(tm => tm.UserId == currentUserId);
                    if (trackManager != null)
                    {
                        reviewsQuery = reviewsQuery.Where(r => r.Research.Track == trackManager.Track);
                    }
                }
                // ConferenceManager and SystemAdmin can see all reviews

                // Apply filters
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    reviewsQuery = reviewsQuery.Where(r =>
                        r.Research.Title.Contains(filter.SearchTerm) ||
                        r.Research.TitleEn.Contains(filter.SearchTerm));
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    bool isCompleted = filter.Status == "completed";
                    reviewsQuery = reviewsQuery.Where(r => r.IsCompleted == isCompleted);
                }

                if (!string.IsNullOrEmpty(filter.Track) && Enum.TryParse<ResearchTrack>(filter.Track, out var track))
                {
                    reviewsQuery = reviewsQuery.Where(r => r.Research.Track == track);
                }

                if (filter.FromDate.HasValue)
                {
                    reviewsQuery = reviewsQuery.Where(r => r.AssignedDate.Date >= filter.FromDate.Value.Date);
                }

                if (filter.ToDate.HasValue)
                {
                    reviewsQuery = reviewsQuery.Where(r => r.AssignedDate.Date <= filter.ToDate.Value.Date);
                }

                // Apply sorting
                reviewsQuery = filter.SortBy switch
                {
                    "Title" => filter.SortDescending
                        ? reviewsQuery.OrderByDescending(r => r.Research.Title)
                        : reviewsQuery.OrderBy(r => r.Research.Title),
                    "DueDate" => filter.SortDescending
                        ? reviewsQuery.OrderByDescending(r => r.Deadline)
                        : reviewsQuery.OrderBy(r => r.Deadline),
                    "CompletedDate" => filter.SortDescending
                        ? reviewsQuery.OrderByDescending(r => r.CompletedDate)
                        : reviewsQuery.OrderBy(r => r.CompletedDate),
                    _ => filter.SortDescending
                        ? reviewsQuery.OrderByDescending(r => r.AssignedDate)
                        : reviewsQuery.OrderBy(r => r.AssignedDate)
                };

                // Get total count for pagination
                var totalCount = await reviewsQuery.CountAsync();

                // Apply pagination
                var reviews = await reviewsQuery
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // Map to ViewModels
                var reviewItems = reviews.Select(r => new ReviewItemViewModel
                {
                    Id = r.Id,
                    ResearchId = r.ResearchId,
                    ResearchTitle = r.Research.Title,
                    ResearchTitleEn = r.Research.TitleEn,
                    ResearchAuthor = r.Research.Authors?.FirstOrDefault()?.FirstName + " " + r.Research.Authors?.FirstOrDefault()?.LastName ?? "",
                    Track = r.Research.Track,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = $"{r.Reviewer?.FirstName} {r.Reviewer?.LastName}",
                    AssignedDate = r.AssignedDate,
                    DueDate = r.Deadline,
                    CompletedDate = r.CompletedDate,
                    IsCompleted = r.IsCompleted,
                    Score = r.IsCompleted ? (int?)((r.OriginalityScore + r.MethodologyScore + r.ClarityScore + r.SignificanceScore) / 4) : null,
                    Decision = r.Decision,
                    CommentsToAuthor = r.CommentsToAuthor,
                    CommentsToTrackManager = r.CommentsToTrackManager
                }).ToList();

                // Calculate statistics
                var allUserReviews = await _context.Reviews
                    .Where(r => !r.IsDeleted && (currentUser.Role != UserRole.Reviewer || r.ReviewerId == currentUserId))
                    .ToListAsync();

                var statistics = new ReviewStatisticsViewModel
                {
                    TotalReviews = allUserReviews.Count,
                    PendingReviews = allUserReviews.Count(r => !r.IsCompleted),
                    CompletedReviews = allUserReviews.Count(r => r.IsCompleted),
                    OverdueReviews = allUserReviews.Count(r => !r.IsCompleted && r.Deadline.HasValue && DateTime.UtcNow > r.Deadline.Value),
                    AverageScore = allUserReviews.Where(r => r.IsCompleted).Any()
                        ? allUserReviews.Where(r => r.IsCompleted).Average(r => (r.OriginalityScore + r.MethodologyScore + r.ClarityScore + r.SignificanceScore) / 4.0)
                        : 0,
                    AcceptedCount = allUserReviews.Count(r => r.Decision == ReviewDecision.AcceptAsIs),
                    RejectedCount = allUserReviews.Count(r => r.Decision == ReviewDecision.Reject),
                    MinorRevisionsCount = allUserReviews.Count(r => r.Decision == ReviewDecision.AcceptWithMinorRevisions),
                    MajorRevisionsCount = allUserReviews.Count(r => r.Decision == ReviewDecision.MajorRevisionsRequired)
                };

                // Create paged result
                var pagedResult = new ReviewPagedResult<ReviewItemViewModel>
                {
                    Items = reviewItems,
                    TotalCount = totalCount,
                    PageNumber = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                };

                // Prepare track options for filter
                var trackOptions = Enum.GetValues<ResearchTrack>()
                    .Select(t => new SelectListItem
                    {
                        Value = t.ToString(),
                        Text = GetTrackDisplayName(t)
                    }).ToList();

                var model = new ReviewListViewModel
                {
                    Reviews = pagedResult,
                    Filter = filter,
                    Statistics = statistics,
                    TrackOptions = trackOptions,
                    CurrentUserId = currentUserId,
                    CurrentUserRole = currentUser.Role,
                    CanCreateReview = currentUser.Role == UserRole.Reviewer,
                    CanManageReviews = currentUser.Role == UserRole.TrackManager ||
                                      currentUser.Role == UserRole.ConferenceManager ||
                                      currentUser.Role == UserRole.SystemAdmin
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading reviews for user {UserId}", GetCurrentUserId());
                AddErrorMessage("حدث خطأ في تحميل قائمة المراجعات");
                return View(new ReviewListViewModel());
            }
        }

        // Helper method for track display names
        private string GetTrackDisplayName(ResearchTrack track) => track switch
        {
            ResearchTrack.EnergyAndRenewableEnergy => "الطاقة والطاقة المتجددة",
            ResearchTrack.ElectricalAndElectronicsEngineering => "الهندسة الكهربائية والإلكترونية",
            ResearchTrack.MaterialScienceAndMechanicalEngineering => "علوم المواد والهندسة الميكانيكية",
            ResearchTrack.NavigationGuidanceSystemsComputerAndCommunicationEngineering => "أنظمة الملاحة والتوجيه وهندسة الحاسوب والاتصالات",
            ResearchTrack.ElectromechanicalSystemAndMechanicsEngineering => "النظم الكهروميكانيكية وهندسة الميكانيك",
            ResearchTrack.AvionicsSystemsAircraftAndUnmannedAircraftEngineering => "أنظمة الطيران وهندسة الطائرات والطائرات بدون طيار",
            ResearchTrack.EarthNaturalResourcesGasAndPetroleumSystemsEquipment => "الموارد الطبيعية للأرض وأنظمة ومعدات الغاز والبترول",
            _ => track.ToString()
        };
        //public async Task<IActionResult> Index()
        //{
        //    var userId = GetCurrentUserId();
        //    var reviews = await _reviewRepository.GetByReviewerIdAsync(userId);
        //    return View(reviews);
        //}

        //[HttpGet]
        //public async Task<IActionResult> Details(int id)
        //{
        //    var review = await _reviewRepository.GetByIdWithDetailsAsync(id);
        //    if (review == null)
        //        return NotFound();

        //    var user = await GetCurrentUserAsync();
        //    if (user == null)
        //        return RedirectToAction("Login", "Account");

        //    // التحقق من الصلاحيات
        //    if (user.Role == UserRole.Reviewer && review.ReviewerId != user.Id)
        //        return Forbid();

        //    return View(review);
        //}

        [HttpGet]
        [Authorize(Roles = "Reviewer")]
        public async Task<IActionResult> Create(int researchId)
        {
            var research = await _researchRepository.GetByIdWithDetailsAsync(researchId);
            if (research == null)
                return NotFound();

            // التحقق من وجود مراجعة سابقة
            var existingReview = (await _reviewRepository.GetByResearchIdAsync(researchId))
                .FirstOrDefault(r => r.ReviewerId == GetCurrentUserId());

            if (existingReview != null)
            {
                return RedirectToAction("Edit", new { id = existingReview.Id });
            }

            var model = new CreateReviewDto
            {
                ResearchId = researchId
            };

            ViewData["Research"] = research;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Reviewer")]
        public async Task<IActionResult> Create(CreateReviewDto model)
        {
            if (!ModelState.IsValid)
            {
                var research = await _researchRepository.GetByIdWithDetailsAsync(model.ResearchId);
                ViewData["Research"] = research;
                return View(model);
            }

            try
            {
                // التحقق من وجود مراجعة سابقة
                var existingReviews = await _reviewRepository.GetByResearchIdAsync(model.ResearchId);
                var userReview = existingReviews.FirstOrDefault(r => r.ReviewerId == GetCurrentUserId());

                if (userReview != null && userReview.IsCompleted)
                {
                    AddErrorMessage("لقد تم إكمال مراجعة هذا البحث مسبقاً");
                    return RedirectToAction("Index");
                }

                var command = new CreateReviewCommand
                {
                    Review = model,
                    ReviewerId = GetCurrentUserId()
                };

                var reviewId = await _mediator.Send(command);

                AddSuccessMessage("تم إرسال المراجعة بنجاح");
                return RedirectToAction("Details", new { id = reviewId });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating review for research {ResearchId}", model.ResearchId);
                AddErrorMessage($"حدث خطأ في إرسال المراجعة: {ex.Message}");

                var research = await _researchRepository.GetByIdWithDetailsAsync(model.ResearchId);
                ViewData["Research"] = research;
                return View(model);
            }
        }

        //[HttpGet]
        //[Authorize(Roles = "Reviewer")]
        //public async Task<IActionResult> Edit(int id)
        //{
        //    var review = await _reviewRepository.GetByIdWithDetailsAsync(id);
        //    if (review == null)
        //        return NotFound();

        //    // التحقق من الملكية
        //    if (review.ReviewerId != GetCurrentUserId())
        //        return Forbid();

        //    // التحقق من إمكانية التعديل
        //    if (review.IsCompleted && !review.RequiresReReview)
        //    {
        //        AddWarningMessage("لا يمكن تعديل المراجعة بعد الانتهاء منها");
        //        return RedirectToAction("Details", new { id });
        //    }

        //    var model = new UpdateReviewDto
        //    {
        //        Id = review.Id,
        //        Decision = review.Decision,
        //        OriginalityScore = review.OriginalityScore,
        //        MethodologyScore = review.MethodologyScore,
        //        ClarityScore = review.ClarityScore,
        //        SignificanceScore = review.SignificanceScore,
        //        ReferencesScore = review.ReferencesScore,
        //        CommentsToAuthor = review.CommentsToAuthor,
        //        CommentsToTrackManager = review.CommentsToTrackManager,
        //        RequiresReReview = review.RequiresReReview,
        //        ResearchId = review.ResearchId
        //    };

        //    ViewData["Research"] = review.Research;
        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Reviewer")]
        //public async Task<IActionResult> Edit(UpdateReviewDto model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var research = await _researchRepository.GetByIdWithDetailsAsync(model.ResearchId);
        //        ViewData["Research"] = research;
        //        return View(model);
        //    }

        //    try
        //    {
        //        var review = await _reviewRepository.GetByIdAsync(model.Id);
        //        if (review == null)
        //            return NotFound();

        //        // التحقق من الملكية
        //        if (review.ReviewerId != GetCurrentUserId())
        //            return Forbid();

        //        // تحديث البيانات
        //        review.Decision = model.Decision;
        //        review.OriginalityScore = model.OriginalityScore;
        //        review.MethodologyScore = model.MethodologyScore;
        //        review.ClarityScore = model.ClarityScore;
        //        review.SignificanceScore = model.SignificanceScore;
        //        review.ReferencesScore = model.ReferencesScore;
        //        review.CommentsToAuthor = model.CommentsToAuthor;
        //        review.CommentsToTrackManager = model.CommentsToTrackManager;
        //        review.RequiresReReview = model.RequiresReReview;
        //        review.CompletedDate = DateTime.UtcNow;
        //        review.IsCompleted = true;
        //        review.UpdatedAt = DateTime.UtcNow;
        //        review.UpdatedBy = GetCurrentUserId();

        //        await _reviewRepository.UpdateAsync(review);

        //        AddSuccessMessage("تم تحديث المراجعة بنجاح");
        //        return RedirectToAction("Details", new { id = model.Id });
        //    }
        //    catch (Exception ex)
        //    {
        //        AddErrorMessage($"حدث خطأ في تحديث المراجعة: {ex.Message}");
        //        var research = await _researchRepository.GetByIdWithDetailsAsync(model.ResearchId);
        //        ViewData["Research"] = research;
        //        return View(model);
        //    }
        //}

        public List<SelectListItem> DecisionOptions { get; set; } = new()
{
    new SelectListItem { Value = "", Text = "اختر القرار" },
    new SelectListItem { Value = "1", Text = "قبول البحث" },
    new SelectListItem { Value = "2", Text = "رفض البحث" },
    new SelectListItem { Value = "3", Text = "قبول مع تعديلات طفيفة" },
    new SelectListItem { Value = "4", Text = "قبول مع تعديلات جوهرية" }
};

        // إضافة هذه الأكشنز في ReviewController.cs

        //[HttpGet]
        //[Authorize(Roles = "Reviewer")]
        //public async Task<IActionResult> Edit(int id)
        //{
        //    try
        //    {
        //        var review = await _reviewRepository.GetByIdWithDetailsAsync(id);
        //        if (review == null)
        //            return NotFound();

        //        // التحقق من الملكية
        //        var currentUserId = GetCurrentUserId();
        //        if (review.ReviewerId != currentUserId)
        //            return Forbid();

        //        // التحقق من إمكانية التعديل
        //        if (review.IsCompleted && !review.RequiresReReview)
        //        {
        //            AddWarningMessage("لا يمكن تعديل المراجعة بعد الانتهاء منها");
        //            return RedirectToAction("Details", new { id });
        //        }

        //        // تحويل Review إلى ViewModel
        //        var model = new CreateReviewViewModel
        //        {
        //            ResearchId = review.ResearchId,
        //            ReviewerId = review.ReviewerId,
        //            Research = await MapToResearchDto(review.Research),
        //            OriginalityScore = review.OriginalityScore,
        //            MethodologyScore = review.MethodologyScore,
        //            ResultsScore = review.ClarityScore, // تأكد من الخرائط الصحيحة
        //            WritingScore = review.SignificanceScore, // تأكد من الخرائط الصحيحة
        //            CommentsToAuthor = review.CommentsToAuthor,
        //            CommentsToTrackManager = review.CommentsToTrackManager,
        //            Decision = review.Decision,
        //            DueDate = review.Deadline,
        //            IsDraft = !review.IsCompleted
        //        };

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "Error loading review edit form for review {ReviewId}", id);
        //        AddErrorMessage("حدث خطأ في تحميل نموذج تعديل المراجعة");
        //        return RedirectToAction("Index");
        //    }
        //}
        [HttpGet]
        [Authorize(Roles = "Reviewer")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var review = await _reviewRepository.GetByIdWithDetailsAsync(id);
                if (review == null)
                    return NotFound();

                // التحقق من الملكية
                var currentUserId = GetCurrentUserId();
                if (review.ReviewerId != currentUserId)
                    return Forbid();

                // التحقق من إمكانية التعديل
                if (review.IsCompleted && !review.RequiresReReview)
                {
                    AddWarningMessage("لا يمكن تعديل المراجعة بعد الانتهاء منها");
                    return RedirectToAction("Details", new { id });
                }

                // تحويل Review إلى ViewModel
                var model = new CreateReviewViewModel
                {
                    ResearchId = review.ResearchId,
                    ReviewerId = review.ReviewerId,
                    Research = await MapToResearchDto(review.Research),
                    OriginalityScore = review.OriginalityScore,
                    MethodologyScore = review.MethodologyScore,
                    ResultsScore = review.ClarityScore, // تأكد من الخرائط الصحيحة
                    WritingScore = review.SignificanceScore, // تأكد من الخرائط الصحيحة
                    CommentsToAuthor = review.CommentsToAuthor,
                    CommentsToTrackManager = review.CommentsToTrackManager,
                    Decision = review.Decision,
                    DueDate = review.Deadline,
                    IsDraft = !review.IsCompleted
                };

                // Set the selected decision option
                model.SetSelectedDecision();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading review edit form for review {ReviewId}", id);
                AddErrorMessage("حدث خطأ في تحميل نموذج تعديل المراجعة");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Reviewer")]
        public async Task<IActionResult> Edit(CreateReviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // إعادة تحميل بيانات البحث
                    var research = await _researchRepository.GetByIdWithDetailsAsync(model.ResearchId);
                    if (research != null)
                    {
                        model.Research = await MapToResearchDto(research);
                    }
                    return View(model);
                }

                var currentUserId = GetCurrentUserId();

                // الحصول على المراجعة الموجودة
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ResearchId == model.ResearchId &&
                                             r.ReviewerId == currentUserId &&
                                             !r.IsDeleted);

                if (existingReview == null)
                {
                    AddErrorMessage("المراجعة غير موجودة");
                    return RedirectToAction("Index");
                }

                // التحقق من الصلاحيات
                if (existingReview.ReviewerId != currentUserId)
                {
                    AddErrorMessage("ليس لديك صلاحية لتعديل هذه المراجعة");
                    return RedirectToAction("Index");
                }

                // تحديث بيانات المراجعة
                existingReview.OriginalityScore = model.OriginalityScore;
                existingReview.MethodologyScore = model.MethodologyScore;
                existingReview.ClarityScore = model.ResultsScore; // تأكد من الخرائط الصحيحة
                existingReview.SignificanceScore = model.WritingScore; // تأكد من الخرائط الصحيحة
                existingReview.ReferencesScore = (model.OriginalityScore + model.MethodologyScore + model.ResultsScore + model.WritingScore) / 4; // أو قيمة افتراضية
                existingReview.CommentsToAuthor = model.CommentsToAuthor;
                existingReview.CommentsToTrackManager = model.CommentsToTrackManager;
                existingReview.Decision = model.Decision;
                existingReview.RequiresReReview = model.Decision == ReviewDecision.MajorRevisionsRequired;

                // تحديث حالة الإكمال
                if (!model.IsDraft)
                {
                    existingReview.IsCompleted = true;
                    existingReview.CompletedDate = DateTime.UtcNow;
                }

                existingReview.UpdatedAt = DateTime.UtcNow;
                existingReview.UpdatedBy = currentUserId;

                // حفظ التغييرات
                _context.Reviews.Update(existingReview);
                await _context.SaveChangesAsync();

                _logger?.LogInformation("Review {ReviewId} updated successfully by user {UserId}", existingReview.Id, currentUserId);

                AddSuccessMessage(model.IsDraft ? "تم حفظ المراجعة كمسودة بنجاح" : "تم تحديث المراجعة بنجاح");

                return RedirectToAction("Details", new { id = existingReview.Id });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating review for research {ResearchId}", model.ResearchId);
                AddErrorMessage($"حدث خطأ في تحديث المراجعة: {ex.Message}");

                // إعادة تحميل بيانات البحث
                var research = await _researchRepository.GetByIdWithDetailsAsync(model.ResearchId);
                if (research != null)
                {
                    model.Research = await MapToResearchDto(research);
                }
                return View(model);
            }
        }

        // Helper method لتحويل Research إلى DTO
        private async Task<ResearchDto> MapToResearchDto(Research research)
        {

            return new ResearchDto
            {
                Id = research.Id,
                Title = research.Title,
                TitleEn = research.TitleEn,
                AbstractAr = research.AbstractAr,
                AbstractEn = research.AbstractEn,
                Keywords = research.Keywords,
                KeywordsEn = research.KeywordsEn,
                Track = research.Track,
                ResearchType = research.ResearchType,
                Language = research.Language,
                Status = research.Status,
                SubmissionDate = research.SubmissionDate,


                Authors = research.Authors?.Select(a => new ResearchAuthorDto
                {
                    Id = a.Id,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    Institution = a.Institution,
                    Order = a.Order,
                    IsCorresponding = a.IsCorresponding
                }).ToList() ?? new List<ResearchAuthorDto>(),
                Files = research.Files?.Select(f => new ResearchFileDto
                {
                    Id = f.Id,
                    OriginalFileName = f.OriginalFileName,
                    FileType = f.FileType,
                    Description = f.Description
                }).ToList() ?? new List<ResearchFileDto>(),
                //TrackDisplayName = GetTrackDisplayName(research.Track),
                //ResearchTypeDisplayName = GetResearchTypeDisplayName(research.ResearchType)
            };
        }

        // Helper methods لأسماء العرض
        //private string GetTrackDisplayName(ResearchTrack track) => track switch
        //{
        //    ResearchTrack.EnergyAndRenewableEnergy => "Energy and Renewable Energy",
        //    ResearchTrack.ElectricalAndElectronicsEngineering => "Electromechanical System, and Mechatronics Engineering",
        //    ResearchTrack.MaterialScienceAndMechanicalEngineering => "Material Science & Mechanical Engineering",
        //    ResearchTrack.NavigationGuidanceSystemsComputerAndCommunicationEngineering => "Navigation & Guidance Systems, Computer and Communication Engineering",
        //    ResearchTrack.ElectromechanicalSystemAndMechanicsEngineering => "Electrical & Electronics Engineering",
        //    ResearchTrack.AvionicsSystemsAircraftAndUnmannedAircraftEngineering => "Avionics Systems, Aircraft and Unmanned Aircraft Engineering",
        //    ResearchTrack.EarthNaturalResourcesGasAndPetroleumSystemsEquipment => "Earth's Natural Resources, Gas and Petroleum Systems & Equipment",
        //    _ => track.ToString()
        //};

        private string GetResearchTypeDisplayName(ResearchType type) => type switch
        {
            ResearchType.OriginalResearch => "بحث كامل",
            ResearchType.CaseStudy => "بحث قصير",
            ResearchType.ExperimentalStudy => "ملصق علمي",
            ResearchType.AppliedResearch => "ورشة عمل",
            _ => type.ToString()
        };

        // إضافة Details action إذا لم يكن موجوداً
        [HttpGet]
        [Authorize(Roles = "Reviewer")]
        //public async Task<IActionResult> Details(int id)
        //{
        //    try
        //    {
        //        var review = await _context.Reviews
        //            .Include(r => r.Research)
        //                .ThenInclude(res => res.Authors)
        //            .Include(r => r.Research)
        //                .ThenInclude(res => res.Files)
        //            .Include(r => r.Reviewer)
        //            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        //        if (review == null)
        //            return NotFound();

        //        var currentUserId = GetCurrentUserId();
        //        var currentUser = await GetCurrentUserAsync();

        //        // التحقق من الصلاحيات
        //        bool canView = false;
        //        if (currentUser?.Role == UserRole.Reviewer && review.ReviewerId == currentUserId)
        //            canView = true;
        //        else if (currentUser?.Role == UserRole.TrackManager)
        //        {
        //            // التحقق من أن البحث في نفس التراك
        //            var trackManager = await _context.TrackManagers
        //                .FirstOrDefaultAsync(tm => tm.UserId == currentUserId);
        //            if (trackManager != null && review.Research.Track == trackManager.Track)
        //                canView = true;
        //        }
        //        else if (currentUser?.Role == UserRole.ConferenceManager || currentUser?.Role == UserRole.SystemAdmin)
        //            canView = true;

        //        if (!canView)
        //            return Forbid();

        //        // تحويل إلى ViewModel
        //        var model = new ReviewDetailsViewModel
        //        {
        //            Review = new ReviewDto
        //            {
        //                Id = review.Id,
        //                ResearchId = review.ResearchId,
        //                ReviewerId = review.ReviewerId,
        //                Decision = review.Decision,
        //                OriginalityScore = review.OriginalityScore > 0 ? review.OriginalityScore : 0,
        //                MethodologyScore = review.MethodologyScore > 0 ? review.MethodologyScore : 0,
        //                ClarityScore = review.ClarityScore > 0 ? review.ClarityScore : 0,
        //                SignificanceScore = review.SignificanceScore > 0 ? review.SignificanceScore : 0,
        //                ReferencesScore = review.ReferencesScore > 0 ? review.ReferencesScore : 0,

        //                //OriginalityScore = review.OriginalityScore,
        //                //MethodologyScore = review.MethodologyScore,
        //                //ClarityScore = review.ClarityScore,
        //                //SignificanceScore = review.SignificanceScore,
        //                //ReferencesScore = review.ReferencesScore,
        //                CommentsToAuthor = review.CommentsToAuthor,
        //                CommentsToTrackManager = review.CommentsToTrackManager,
        //                AssignedDate = review.AssignedDate,
        //                Deadline = review.Deadline,
        //                CompletedDate = review.CompletedDate,
        //                IsCompleted = review.IsCompleted,
        //                RequiresReReview = review.RequiresReReview
        //            },
        //            Research = await MapToResearchDto(review.Research),
        //            ReviewerName = $"{review.Reviewer.FirstName} {review.Reviewer.LastName}",
        //            CanEdit = currentUser?.Role == UserRole.Reviewer && review.ReviewerId == currentUserId && (!review.IsCompleted || review.RequiresReReview),
        //            CanDelete = false, // عادة لا يُسمح بحذف المراجعات
        //            CanView = true,
        //            CurrentUserId = currentUserId,
        //            CurrentUserRole = currentUser?.Role ?? UserRole.Researcher
        //        };

        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger?.LogError(ex, "Error loading review details for review {ReviewId}", id);
        //        AddErrorMessage("حدث خطأ في تحميل تفاصيل المراجعة");
        //        return RedirectToAction("Index");
        //    }
        //}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var review = await _context.Reviews
                    .Include(r => r.Research)
                        .ThenInclude(res => res.Authors)
                    .Include(r => r.Research)
                        .ThenInclude(res => res.Files)
                    .Include(r => r.Reviewer)
                    .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

                if (review == null)
                    return NotFound();

                var currentUserId = GetCurrentUserId();
                var currentUser = await GetCurrentUserAsync();

                // التحقق من الصلاحيات
                bool canView = false;
                if (currentUser?.Role == UserRole.Reviewer && review.ReviewerId == currentUserId)
                    canView = true;
                else if (currentUser?.Role == UserRole.TrackManager)
                {
                    var trackManager = await _context.TrackManagers
                        .FirstOrDefaultAsync(tm => tm.UserId == currentUserId);
                    if (trackManager != null && review.Research.Track == trackManager.Track)
                        canView = true;
                }
                else if (currentUser?.Role == UserRole.ConferenceManager || currentUser?.Role == UserRole.SystemAdmin)
                    canView = true;

                if (!canView)
                    return Forbid();

                // Create the model safely
                var model = new ReviewDetailsViewModel
                {
                    Review = new ReviewDto
                    {
                        Id = review.Id,
                        ResearchId = review.ResearchId,
                        ReviewerId = review.ReviewerId,
                        Decision = review.Decision,
                        OriginalityScore = review.OriginalityScore,
                        MethodologyScore = review.MethodologyScore,
                        ClarityScore = review.ClarityScore,
                        SignificanceScore = review.SignificanceScore,
                        ReferencesScore = review.ReferencesScore,
                        CommentsToAuthor = review.CommentsToAuthor,
                        CommentsToTrackManager = review.CommentsToTrackManager,
                        AssignedDate = review.AssignedDate,
                        Deadline = review.Deadline,
                        CompletedDate = review.CompletedDate,
                        IsCompleted = review.IsCompleted,
                        RequiresReReview = review.RequiresReReview
                    },
                    Research = new ResearchDto
                    {
                        Id = review.Research.Id,
                        Title = review.Research.Title ?? "",
                        TitleEn = review.Research.TitleEn,
                        AbstractAr = review.Research.AbstractAr ?? "",
                        AbstractEn = review.Research.AbstractEn,
                        Track = review.Research.Track,
                        ResearchType = review.Research.ResearchType,
                        SubmissionDate = review.Research.SubmissionDate,
                        Authors = review.Research.Authors?.Select(a => new ResearchAuthorDto
                        {
                            Id = a.Id,
                            FirstName = a.FirstName ?? "",
                            LastName = a.LastName ?? "",
                            Email = a.Email ?? "",
                            Institution = a.Institution,
                            IsCorresponding = a.IsCorresponding
                        }).ToList() ?? new List<ResearchAuthorDto>(),
                        Files = review.Research.Files?.Select(f => new ResearchFileDto
                        {
                            Id = f.Id,
                            OriginalFileName = f.OriginalFileName ?? "",
                            Description = f.Description
                        }).ToList() ?? new List<ResearchFileDto>()
                    },
                    ReviewerName = $"{review.Reviewer?.FirstName} {review.Reviewer?.LastName}",
                    CanEdit = currentUser?.Role == UserRole.Reviewer && review.ReviewerId == currentUserId && (!review.IsCompleted || review.RequiresReReview),
                    CanDelete = false,
                    CanView = true,
                    CurrentUserId = currentUserId,
                    CurrentUserRole = currentUser?.Role ?? UserRole.Researcher,
                    OverallScore = CalculateOverallScore(review),
                    OverallScoreDisplayName = GetScoreDisplayName(CalculateOverallScore(review)),
                    //IsLateSubmission = review.Deadline.HasValue && review.CompletedDate.HasValue && review.CompletedDate > review.Deadline
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading review details for review {ReviewId}", id);
                AddErrorMessage("حدث خطأ في تحميل تفاصيل المراجعة");
                return RedirectToAction("Index");
            }
        }

        // Helper methods
        private double CalculateOverallScore(Review review)
        {
            var scores = new[] { review.OriginalityScore, review.MethodologyScore, review.ClarityScore, review.SignificanceScore };
            return scores.Average();
        }

        private string GetScoreDisplayName(double score)
        {
            return score switch
            {
                >= 9 => "ممتاز",
                >= 8 => "جيد جداً",
                >= 7 => "جيد",
                >= 6 => "مقبول",
                >= 5 => "ضعيف",
                _ => "ضعيف جداً"
            };
        }
    }
}
