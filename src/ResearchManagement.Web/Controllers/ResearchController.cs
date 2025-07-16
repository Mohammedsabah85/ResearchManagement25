using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using AutoMapper;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Commands.Research;
using ResearchManagement.Application.Queries.Research;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Web.Models.ViewModels.Research;
using ResearchManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using ResearchManagement.Application.Mappings;


using System.Linq;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Infrastructure.Data;
using static ResearchManagement.Web.Models.ViewModels.Research.ResearchTrackAssignmentDto;
using ResearchManagement.Web.Filters;



namespace ResearchManagement.Web.Controllers
{
    [Authorize]
    public class ResearchController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly ILogger<ResearchController> _logger;
        private readonly ApplicationDbContext _context; // إضافة هذا

        public ResearchController(
            UserManager<User> userManager,
            IMediator mediator,
            IMapper mapper,
            IFileService fileService,
            ILogger<ResearchController> logger,
            ApplicationDbContext context) : base(userManager) // إضافة context
        {
            _mediator = mediator;
            _mapper = mapper;
            _fileService = fileService;
            _logger = logger;
            _context = context; // إضافة هذا
        }

        // GET: Research
        public async Task<IActionResult> Index(
            string? searchTerm,
            ResearchStatus? status,
            ResearchTrack? track,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 10,
            string sortBy = "SubmissionDate",
            bool sortDescending = true)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var query = new GetResearchListQuery(user.Id, user.Role)
                {
                    SearchTerm = searchTerm,
                    Status = status,
                    Track = track,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _mediator.Send(query);

                var viewModel = new ResearchListViewModel
                {
                    Researches = result,
                    Filter = new ResearchFilterViewModel
                    {
                        SearchTerm = searchTerm,
                        Status = status,
                        Track = track,
                        FromDate = fromDate,
                        ToDate = toDate,
                        Page = page,
                        PageSize = pageSize,
                        SortBy = sortBy,
                        SortDescending = sortDescending
                    },
                    StatusOptions = GetStatusOptions(),
                    TrackOptions = GetTrackOptions(),
                    CurrentUserId = user.Id,
                    CurrentUserRole = user.Role,
                    CanCreateResearch = CanCreateResearch(user.Role),
                    CanManageResearches = CanManageResearches(user.Role)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading research list");
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل قائمة البحوث";
                return View(new ResearchListViewModel());
            }
        }

        // GET: Research/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var query = new GetResearchByIdQuery(id, user.Id);
                var research = await _mediator.Send(query);

                if (research == null)
                {
                    TempData["ErrorMessage"] = "البحث غير موجود أو ليس لديك صلاحية للوصول إليه";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new ResearchDetailsViewModel
                {
                    Research = research,
                    Files = research.Files?.ToList() ?? new List<ResearchFileDto>(),
                    Reviews = research.Reviews?.ToList() ?? new List<ReviewDto>(),
                    CurrentUserId = user.Id,
                    CurrentUserRole = user.Role,
                    CanEdit = CanEditResearch(research, user),
                    CanDelete = CanDeleteResearch(research, user),
                    CanReview = CanReviewResearch(research, user),
                    CanManageStatus = CanManageStatus(user.Role),
                    CanDownloadFiles = CanDownloadFiles(research, user),
                    CanUploadFiles = CanUploadFiles(research, user),
                    IsAuthor = IsAuthor(research, user.Id),
                    IsReviewer = IsReviewer(research, user.Id),
                    IsTrackManager = IsTrackManager(research, user.Id)
                };

                // Calculate statistics
                if (viewModel.Reviews.Any())
                {
                    viewModel.TotalReviews = viewModel.Reviews.Count;
                    viewModel.CompletedReviews = viewModel.Reviews.Count(r => r.IsCompleted);
                    viewModel.PendingReviews = viewModel.TotalReviews - viewModel.CompletedReviews;

                    var completedReviews = viewModel.Reviews.Where(r => r.IsCompleted && r.Score > 0);
                    if (completedReviews.Any())
                    {
                        viewModel.AverageScore = (double)completedReviews.Average(r => r.Score);
                    }
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading research details for ID: {ResearchId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل تفاصيل البحث";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Research/Create
        //[Authorize(Roles = "Researcher,SystemAdmin")]
        //public async Task<IActionResult> Create()
        //{
        //    try
        //    {
        //        var user = await GetCurrentUserAsync();
        //        if (user == null)
        //            return RedirectToAction("Login", "Account");

        //        var viewModel = new CreateResearchViewModel
        //        {
        //            CurrentUserId = user.Id,
        //            IsEditMode = false
        //        };

        //        return View(viewModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while loading create research page");
        //        TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل صفحة إنشاء البحث";
        //        return RedirectToAction(nameof(Index));
        //    }
        //}

        // POST: Research/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Researcher,SystemAdmin")]
        //public async Task<IActionResult> Create()
        //{
        //    try
        //    {
        //        var user = await GetCurrentUserAsync();
        //        if (user == null)
        //            return RedirectToAction("Login", "Account");

        //        var viewModel = new CreateResearchViewModel
        //        {
        //            CurrentUserId = user.Id,
        //            IsEditMode = false,
        //            ResearchTypeOptions = GetResearchTypeOptions(),
        //            LanguageOptions = GetLanguageOptions(),
        //            TrackOptions = GetTrackOptions()
        //        };

        //        return View(viewModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while loading create research page");
        //        TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل صفحة إنشاء البحث";
        //        return RedirectToAction(nameof(Index));
        //    }
        //}

        // POST: Research/Create
        // ResearchController.cs - Create Actions محدث

        // GET: Research/Create
        [Authorize(Roles = "Researcher,SystemAdmin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var viewModel = new CreateResearchViewModel
                {
                    CurrentUserId = user.Id,
                    IsEditMode = false,
                    ResearchTypeOptions = GetResearchTypeOptions(),
                    LanguageOptions = GetLanguageOptions(),
                    TrackOptions = GetTrackOptions()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create research page");
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل صفحة إنشاء البحث";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Research/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Researcher,SystemAdmin")]
        [ResearchModelBindingFilter] // إضافة الـ Filter
        public async Task<IActionResult> Create(CreateResearchViewModel model, IFormFile researchFile)
        {
            _logger.LogInformation("=== بدء عملية إنشاء بحث جديد ===");
            _logger.LogInformation("Title: {Title}", model.Title);
            _logger.LogInformation("ResearchType: {ResearchType}", model.ResearchType);
            _logger.LogInformation("Language: {Language}", model.Language);
            _logger.LogInformation("Track: {Track}", model.Track);
            _logger.LogInformation("Authors Count: {AuthorsCount}", model.Authors?.Count ?? 0);
            _logger.LogInformation("Has File: {HasFile}", researchFile != null);

            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    _logger.LogWarning("المستخدم غير مصرح له");
                    return RedirectToAction("Login", "Account");
                }

                // إعادة تهيئة القوائم المنسدلة دائماً
                model.ResearchTypeOptions = GetResearchTypeOptions();
                model.LanguageOptions = GetLanguageOptions();
                model.TrackOptions = GetTrackOptions();

                // التحقق من Model State مع تجاهل بعض الحقول
                var keysToIgnore = new[] { "ResearchTypeOptions", "LanguageOptions", "TrackOptions" };
                var filteredModelState = ModelState.Where(x => !keysToIgnore.Any(k => x.Key.Contains(k)));

                // طباعة تفاصيل الأخطاء للتشخيص
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("النموذج غير صالح");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Any())
                        {
                            _logger.LogWarning("خطأ في {Key}: {Errors}",
                                error.Key,
                                string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }
                }

                // التحقق الخاص من الحقول المطلوبة
                var customValidationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(model.Title))
                    customValidationErrors.Add("عنوان البحث مطلوب");

                if (string.IsNullOrWhiteSpace(model.AbstractAr))
                    customValidationErrors.Add("الملخص باللغة العربية مطلوب");

                if (model.ResearchType == 0)
                    customValidationErrors.Add("نوع البحث مطلوب");

                if (model.Language == 0)
                    customValidationErrors.Add("لغة البحث مطلوبة");

                if (model.Authors?.Any() != true)
                    customValidationErrors.Add("يجب إضافة مؤلف واحد على الأقل");
                else
                {
                    var firstAuthor = model.Authors.FirstOrDefault();
                    if (firstAuthor == null ||
                        string.IsNullOrWhiteSpace(firstAuthor.FirstName) ||
                        string.IsNullOrWhiteSpace(firstAuthor.LastName) ||
                        string.IsNullOrWhiteSpace(firstAuthor.Email))
                    {
                        customValidationErrors.Add("يجب ملء بيانات الباحث الرئيسي");
                    }
                }

                if (researchFile == null || researchFile.Length == 0)
                    customValidationErrors.Add("ملف البحث مطلوب");

                if (customValidationErrors.Any())
                {
                    foreach (var error in customValidationErrors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View(model);
                }

                _logger.LogInformation("User authenticated: {UserId}", user.Id);

                // تحويل ViewModel إلى DTO
                var createResearchDto = _mapper.Map<CreateResearchDto>(model);

                // تأكد من القيم الافتراضية
                if (!createResearchDto.Track.HasValue)
                {
                    createResearchDto.Track = ResearchTrack.NotAssigned;
                }

                _logger.LogInformation("DTO created successfully");

                // معالجة المؤلفين
                if (model.Authors?.Any() == true)
                {
                    _logger.LogInformation("بدء معالجة المؤلفين - العدد: {Count}", model.Authors.Count);

                    createResearchDto.Authors = new List<CreateResearchAuthorDto>();

                    foreach (var author in model.Authors.Where(a => !string.IsNullOrEmpty(a.FirstName) && !string.IsNullOrEmpty(a.LastName)))
                    {
                        var authorDto = _mapper.Map<CreateResearchAuthorDto>(author);

                        // تأكد من Order
                        if (authorDto.Order <= 0)
                        {
                            authorDto.Order = createResearchDto.Authors.Count + 1;
                        }

                        createResearchDto.Authors.Add(authorDto);
                        _logger.LogInformation("تم إضافة مؤلف: {FirstName} {LastName}", authorDto.FirstName, authorDto.LastName);
                    }
                }
                else
                {
                    _logger.LogWarning("لا توجد مؤلفين في النموذج");
                    ModelState.AddModelError("", "يجب إضافة مؤلف واحد على الأقل");
                    return View(model);
                }

                // معالجة الملف المرفوع
                if (researchFile != null && researchFile.Length > 0)
                {
                    _logger.LogInformation("بدء معالجة الملف: {FileName}, Size: {Size}",
                        researchFile.FileName, researchFile.Length);

                    try
                    {
                        // التحقق من نوع الملف
                        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                        var fileExtension = Path.GetExtension(researchFile.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "نوع الملف غير مدعوم. الأنواع المدعومة: PDF, DOC, DOCX");
                            return View(model);
                        }

                        // التحقق من حجم الملف (50 ميجابايت)
                        if (researchFile.Length > 50 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "حجم الملف كبير جداً. الحد الأقصى 50 ميجابايت");
                            return View(model);
                        }

                        using var memoryStream = new MemoryStream();
                        await researchFile.CopyToAsync(memoryStream);
                        var fileName = $"{Guid.NewGuid()}_{researchFile.FileName}";
                        var filePath = await _fileService.UploadFileAsync(
                            memoryStream.ToArray(),
                            fileName,
                            researchFile.ContentType);

                        var fileDto = new ResearchFileDto
                        {
                            FileName = fileName,
                            OriginalFileName = researchFile.FileName,
                            FilePath = filePath,
                            ContentType = ContentTypeHelper.GetShortContentType(researchFile.ContentType),
                            FileSize = researchFile.Length,
                            FileType = GetFileType(researchFile.ContentType),
                            Description = "ملف البحث الرئيسي"
                        };

                        createResearchDto.Files = new List<ResearchFileDto> { fileDto };
                        _logger.LogInformation("تم رفع الملف بنجاح: {FileName}", fileName);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError(fileEx, "خطأ في معالجة الملف");
                        ModelState.AddModelError("", "حدث خطأ في رفع الملف");
                        return View(model);
                    }
                }

                // إنشاء الأمر
                var command = new CreateResearchCommand
                {
                    Research = createResearchDto,
                    UserId = user.Id
                };

                _logger.LogInformation("بدء تنفيذ الأمر...");

                // تنفيذ الأمر
                var researchId = await _mediator.Send(command);

                if (researchId > 0)
                {
                    _logger.LogInformation("تم إنشاء البحث بنجاح - ID: {ResearchId}", researchId);
                    TempData["SuccessMessage"] = "تم تقديم البحث بنجاح";
                    return RedirectToAction(nameof(Details), new { id = researchId });
                }
                else
                {
                    _logger.LogError("فشل في إنشاء البحث - تم إرجاع ID = {ResearchId}", researchId);
                    TempData["ErrorMessage"] = "فشل في تقديم البحث";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء البحث");
                _logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {InnerMessage}", ex.InnerException.Message);
                    _logger.LogError("Inner Stack Trace: {InnerStackTrace}", ex.InnerException.StackTrace);
                }

                TempData["ErrorMessage"] = $"حدث خطأ أثناء تقديم البحث: {ex.Message}";

                // إعادة تهيئة القوائم المنسدلة
                model.ResearchTypeOptions = GetResearchTypeOptions();
                model.LanguageOptions = GetLanguageOptions();
                model.TrackOptions = GetTrackOptions();

                return View(model);
            }

            //public async Task<IActionResult> Create(CreateResearchViewModel model, List<IFormFile> files)
            //{
            //    try
            //    {
            //        if (!ModelState.IsValid)
            //        {
            //            return View(model);
            //        }

            //        var user = await GetCurrentUserAsync();
            //        if (user == null)
            //            return RedirectToAction("Login", "Account");

            //        // Create research DTO
            //        var createResearchDto = _mapper.Map<CreateResearchDto>(model);


            //        // Handle file uploads
            //        if (files?.Any() == true)
            //        {
            //            var uploadedFiles = new List<ResearchFileDto>();
            //            foreach (var file in files)
            //            {
            //                if (file.Length > 0)
            //                {
            //                    using var memoryStream = new MemoryStream();
            //                    await file.CopyToAsync(memoryStream);
            //                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            //                    var filePath = await _fileService.UploadFileAsync(memoryStream.ToArray(), fileName, file.ContentType);

            //                    uploadedFiles.Add(new ResearchFileDto
            //                    {
            //                        FileName = fileName,
            //                        OriginalFileName = file.FileName,
            //                        FilePath = filePath,
            //                        //ContentType = file.ContentType,
            //                        ContentType = ContentTypeHelper.GetShortContentType(file.ContentType),
            //                        FileSize = file.Length,
            //                        FileType = GetFileType(file.ContentType),
            //                        Description = "ملف البحث الرئيسي"
            //                    });
            //                }
            //            }
            //            createResearchDto.Files = uploadedFiles;
            //        }

            //        var command = new CreateResearchCommand
            //        {
            //            Research = createResearchDto,
            //            UserId = user.Id
            //        };

            //        var researchId = await _mediator.Send(command);

            //        TempData["SuccessMessage"] = "تم تقديم البحث بنجاح";
            //        return RedirectToAction(nameof(Details), new { id = researchId });
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Error occurred while creating research");
            //        TempData["ErrorMessage"] = "حدث خطأ أثناء تقديم البحث";
            //        return View(model);
            //    }
            //}
        }
            //// GET: Research/Edit/5
            [Authorize(Roles = "Researcher,SystemAdmin")]
            public async Task<IActionResult> Edit(int id)
            {
                try
                {
                    var user = await GetCurrentUserAsync();
                    if (user == null)
                        return RedirectToAction("Login", "Account");

                    var query = new GetResearchByIdQuery(id, user.Id);
                    var research = await _mediator.Send(query);

                    if (research == null)
                    {
                        TempData["ErrorMessage"] = "البحث غير موجود أو ليس لديك صلاحية لتعديله";
                        return RedirectToAction(nameof(Index));
                    }

                    if (!CanEditResearch(research, user))
                    {
                        TempData["ErrorMessage"] = "لا يمكن تعديل هذا البحث في حالته الحالية";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    // تحويل ResearchDto إلى CreateResearchViewModel
                    var viewModel = _mapper.Map<CreateResearchViewModel>(research);
                    viewModel.IsEditMode = true;
                    viewModel.ResearchId = id;
                    viewModel.CurrentUserId = user.Id;

                    // إعادة تهيئة القوائم المنسدلة
                    viewModel.ResearchTypeOptions = GetResearchTypeOptions();
                    viewModel.LanguageOptions = GetLanguageOptions();
                    viewModel.TrackOptions = GetTrackOptions();

                    return View(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while loading edit research page for ID: {ResearchId}", id);
                    TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل صفحة تعديل البحث";
                    return RedirectToAction(nameof(Index));
                }
            }

            //// POST: Research/Edit/5
            //[HttpPost]
            //[ValidateAntiForgeryToken]
            //[Authorize(Roles = "Researcher,Admin")]
            //public async Task<IActionResult> Edit(int id, CreateResearchViewModel model, List<IFormFile> files)
            //{
            //    //try
            //    //{
            //        if (id != model.ResearchId)
            //        {
            //            return NotFound();
            //        }

            //        var user = await GetCurrentUserAsync();
            //        if (user == null)
            //            return RedirectToAction("Login", "Account");

            //        // إعادة تهيئة القوائم في حالة الخطأ
            //        model.ResearchTypeOptions = GetResearchTypeOptions();
            //        model.LanguageOptions = GetLanguageOptions();
            //        model.TrackOptions = GetTrackOptions();

            //        // التحقق من صحة النموذج
            //        if (!ModelState.IsValid)
            //        {
            //            return View(model);
            //        }

            //        // تحويل ViewModel إلى DTO
            //        var updateResearchDto = _mapper.Map<CreateResearchDto>(model);

            //        // معالجة الملفات المرفوعة
            //        if (files?.Any() == true)
            //        {
            //            var uploadedFiles = new List<ResearchFileDto>();
            //            foreach (var file in files)
            //            {
            //                if (file.Length > 0)
            //                {
            //                    try
            //                    {
            //                        using var memoryStream = new MemoryStream();
            //                        await file.CopyToAsync(memoryStream);
            //                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            //                        var filePath = await _fileService.UploadFileAsync(memoryStream.ToArray(), fileName, file.ContentType);

            //                        uploadedFiles.Add(new ResearchFileDto
            //                        {
            //                            FileName = fileName,
            //                            OriginalFileName = file.FileName,
            //                            FilePath = filePath,
            //                            ContentType = file.ContentType,
            //                            FileSize = file.Length,
            //                            FileType = GetFileType(file.ContentType),
            //                            Description = "ملف محدث"
            //                        });
            //                    }
            //                    catch (Exception fileEx)
            //                    {
            //                        _logger.LogError(fileEx, "Error uploading file: {FileName}", file.FileName);
            //                        ModelState.AddModelError("Files", $"فشل في رفع الملف: {file.FileName}");
            //                        return View(model);
            //                    }
            //                }
            //            }
            //            updateResearchDto.Files = uploadedFiles;
            //        }

            //        // إنشاء الأمر
            //        var command = new UpdateResearchCommand
            //        {
            //            ResearchId = id,
            //            Research = updateResearchDto,
            //            UserId = user.Id
            //        };

            //        // تنفيذ الأمر
            //        var result = await _mediator.Send(command);

            //        if (result)
            //        {
            //            TempData["SuccessMessage"] = "تم تحديث البحث بنجاح";
            //            return RedirectToAction(nameof(Details), new { id });
            //        }
            //        else
            //        {
            //            TempData["ErrorMessage"] = "فشل في تحديث البحث";
            //            return View(model);
            //        }
            //    }
            //    //catch (Exception ex)
            //    //{
            //    //    _logger.LogError(ex, "Error occurred while updating research with ID: {ResearchId}", id);
            //    //    TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث البحث";

            //    //    // إعادة تهيئة القوائم في حالة الخطأ
            //    //    model.ResearchTypeOptions = GetResearchTypeOptions();
            //    //    model.LanguageOptions = GetLanguageOptions();
            //    //    model.TrackOptions = GetTrackOptions();

            //    //    return View(model);
            //    //}
            ////}




            //        // POST: Research/Edit/5
            //        [HttpPost]
            //        [ValidateAntiForgeryToken]
            //        [Authorize(Roles = "Researcher,Admin")]
            //        public async Task<IActionResult> Edit(int id, CreateResearchViewModel model, List<IFormFile> files)
            //        {
            //            try
            //            {
            //                if (id != model.ResearchId)
            //                {
            //                    return NotFound();
            //                }

            //                var user = await GetCurrentUserAsync();
            //                if (user == null)
            //                    return RedirectToAction("Login", "Account");

            //                // إعادة تهيئة القوائم في حالة الخطأ
            //                model.ResearchTypeOptions = GetResearchTypeOptions();
            //                model.LanguageOptions = GetLanguageOptions();
            //                model.TrackOptions = GetTrackOptions();

            //                // التحقق من صحة النموذج
            //                if (!ModelState.IsValid)
            //                {
            //                    _logger.LogWarning("نموذج غير صالح عند تعديل البحث {ResearchId}", id);

            //                    // عرض تفاصيل أخطاء التحقق
            //                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
            //                    {
            //                        _logger.LogWarning("خطأ في التحقق: {Error}", modelError.ErrorMessage);
            //                    }

            //                    return View(model);
            //                }

            //                // التحقق من وجود البحث
            //                var existingResearch = await _mediator.Send(new GetResearchByIdQuery(id, user.Id));
            //                if (existingResearch == null)
            //                {
            //                    _logger.LogWarning("محاولة تعديل بحث غير موجود أو غير مصرح به: {ResearchId}", id);
            //                    TempData["ErrorMessage"] = "البحث غير موجود أو ليس لديك صلاحية لتعديله";
            //                    return RedirectToAction(nameof(Index));
            //                }

            //                // تحويل ViewModel إلى DTO
            //                var updateResearchDto = _mapper.Map<CreateResearchDto>(model);

            //                // معالجة الملفات المرفوعة
            //                if (files?.Any() == true)
            //                {
            //                    var uploadedFiles = new List<ResearchFileDto>();
            //                    var failedFiles = new List<string>();

            //                    foreach (var file in files)
            //                    {
            //                        if (file.Length > 0)
            //                        {
            //                            try
            //                            {
            //                                // التحقق من نوع الملف
            //                                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            //                                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            //                                if (!allowedExtensions.Contains(fileExtension))
            //                                {
            //                                    failedFiles.Add($"{file.FileName}: نوع الملف غير مدعوم");
            //                                    continue;
            //                                }

            //                                // التحقق من حجم الملف (50 ميجابايت)
            //                                if (file.Length > 50 * 1024 * 1024)
            //                                {
            //                                    failedFiles.Add($"{file.FileName}: حجم الملف كبير جداً (الحد الأقصى 50 ميجابايت)");
            //                                    continue;
            //                                }

            //                                using var memoryStream = new MemoryStream();
            //                                await file.CopyToAsync(memoryStream);
            //                                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            //                                var filePath = await _fileService.UploadFileAsync(
            //                                    memoryStream.ToArray(),
            //                                    fileName,
            //                                    file.ContentType);

            //                                uploadedFiles.Add(new ResearchFileDto
            //                                {
            //                                    FileName = fileName,
            //                                    OriginalFileName = file.FileName,
            //                                    FilePath = filePath,
            //                                    ContentType = file.ContentType,
            //                                    FileSize = file.Length,
            //                                    FileType = GetFileType(file.ContentType),
            //                                    Description = "ملف محدث"
            //                                });

            //                                _logger.LogInformation("تم رفع الملف بنجاح: {FileName}", file.FileName);
            //                            }
            //                            catch (Exception fileEx)
            //                            {
            //                                _logger.LogError(fileEx, "خطأ في رفع الملف: {FileName}", file.FileName);
            //                                failedFiles.Add($"{file.FileName}: {fileEx.Message}");
            //                            }
            //                        }
            //                    }

            //                    if (failedFiles.Any())
            //                    {
            //                        TempData["WarningMessage"] = $"فشل رفع بعض الملفات: {string.Join(", ", failedFiles)}";
            //                    }

            //                    updateResearchDto.Files = uploadedFiles;
            //                }

            //                // إنشاء الأمر
            //                var command = new UpdateResearchCommand
            //                {
            //                    ResearchId = id,
            //                    Research = updateResearchDto,
            //                    UserId = user.Id
            //                };

            //                // تنفيذ الأمر
            //                var result = await _mediator.Send(command);


            //            if (result)
            //            {
            //                TempData["SuccessMessage"] = "تم تحديث البحث بنجاح";
            //                _logger.LogInformation("تم تحديث البحث {ResearchId} بنجاح بواسطة المستخدم {UserId}", id, user.Id);
            //                return RedirectToAction(nameof(Details), new { id });
            //            }
            //            else
            //            {
            //                _logger.LogWarning("فشل تحديث البحث {ResearchId} - لم يُرجع الأمر نتيجة إيجابية", id);
            //                TempData["ErrorMessage"] = "فشل في تحديث البحث. يرجى المحاولة مرة أخرى";
            //                return View(model);
            //            }
            //        }
            //        catch (InvalidOperationException opEx)
            //        {
            //            _logger.LogError(opEx, "خطأ في العملية أثناء تحديث البحث {ResearchId}", id);
            //            TempData["ErrorMessage"] = opEx.Message;

            //            // إعادة تهيئة القوائم
            //            model.ResearchTypeOptions = GetResearchTypeOptions();
            //        model.LanguageOptions = GetLanguageOptions();
            //        model.TrackOptions = GetTrackOptions();

            //            return View(model);
            //    }

            //}
        

            [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Researcher,SystemAdmin")]
        public async Task<IActionResult> Edit(int id, CreateResearchViewModel model, List<IFormFile> files)
        {
            _logger.LogInformation("بدء تعديل البحث {ResearchId} من قبل المستخدم {UserId}", id, GetCurrentUserId());

            // طباعة تفاصيل النموذج للتشخيص
            _logger.LogInformation("بيانات النموذج: Title={Title}, ResearchType={ResearchType}, Track={Track}",
                model.Title, model.ResearchType, model.Track);


            _logger.LogInformation("=== بدء عملية تعديل البحث ===");
            _logger.LogInformation("ResearchId: {ResearchId}", id);
            _logger.LogInformation("UserId: {UserId}", GetCurrentUserId());
            _logger.LogInformation("Title: {Title}", model.Title);
            _logger.LogInformation("Authors Count: {AuthorsCount}", model.Authors?.Count ?? 0);
            _logger.LogInformation("Files Count: {FilesCount}", files?.Count ?? 0);



            _logger.LogInformation("=== بدء عملية تعديل البحث ===");
            _logger.LogInformation("ResearchId: {ResearchId}", id);
            _logger.LogInformation("UserId: {UserId}", GetCurrentUserId());
            _logger.LogInformation("Model.ResearchId: {ModelResearchId}", model.ResearchId);

            // فحص المؤلفين
            if (model.Authors != null)
            {
                _logger.LogInformation("عدد المؤلفين: {AuthorsCount}", model.Authors.Count);
                foreach (var author in model.Authors)
                {
                    _logger.LogInformation("مؤلف: {FirstName} {LastName} <{Email}> - Order: {Order}",
                        author.FirstName, author.LastName, author.Email, author.Order);
                }
            }



            // طباعة أخطاء النموذج
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("خطأ في النموذج - {Key}: {Error}", key, error.ErrorMessage);
                    }
                }
            }






            try
            {
                if (id != model.ResearchId)
                {
                    return NotFound();
                }

                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                // إعادة تهيئة القوائم دائماً
                model.ResearchTypeOptions = GetResearchTypeOptions();
                model.LanguageOptions = GetLanguageOptions();
                model.TrackOptions = GetTrackOptions();

                // التحقق من صحة النموذج
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("نموذج غير صالح عند تعديل البحث {ResearchId}", id);
                    return View(model);
                }

                // التحقق من وجود البحث والصلاحيات
                var existingResearch = await _mediator.Send(new GetResearchByIdQuery(id, user.Id));
                if (existingResearch == null)
                {
                    TempData["ErrorMessage"] = "البحث غير موجود أو ليس لديك صلاحية لتعديله";
                    return RedirectToAction(nameof(Index));
                }

                if (!CanEditResearch(existingResearch, user))
                {
                    TempData["ErrorMessage"] = "لا يمكن تعديل هذا البحث في حالته الحالية";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // تحويل ViewModel إلى DTO
                var updateResearchDto = _mapper.Map<CreateResearchDto>(model);

                // معالجة الملفات الجديدة
                if (files?.Any() == true)
                {
                    var uploadedFiles = await ProcessUploadedFiles(files, user.Id);
                    updateResearchDto.Files = uploadedFiles;
                }

                // تنفيذ الأمر
                var command = new UpdateResearchCommand
                {
                    ResearchId = id,
                    Research = updateResearchDto,
                    UserId = user.Id
                };

                var result = await _mediator.Send(command);

                if (result)
                {
                    TempData["SuccessMessage"] = "تم تحديث البحث بنجاح";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = "فشل في تحديث البحث";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تعديل البحث {ResearchId}", id);
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";

                // إعادة تهيئة القوائم
                model.ResearchTypeOptions = GetResearchTypeOptions();
                model.LanguageOptions = GetLanguageOptions();
                model.TrackOptions = GetTrackOptions();

                return View(model);
            }
        }

        [Authorize(Roles = "Researcher,SystemAdmin")]

        // دالة مساعدة لمعالجة الملفات
        private async Task<List<ResearchFileDto>> ProcessUploadedFiles(List<IFormFile> files, string userId)
        {
            var uploadedFiles = new List<ResearchFileDto>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    try
                    {
                        // التحقق من نوع الملف
                        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            continue; // تجاهل الملفات غير المدعومة
                        }

                        // التحقق من حجم الملف
                        if (file.Length > 50 * 1024 * 1024) // 50MB
                        {
                            continue; // تجاهل الملفات الكبيرة
                        }

                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = await _fileService.UploadFileAsync(
                            memoryStream.ToArray(),
                            fileName,
                            file.ContentType);

                        uploadedFiles.Add(new ResearchFileDto
                        {
                            FileName = fileName,
                            OriginalFileName = file.FileName,
                            FilePath = filePath,
                            //ContentType = file.ContentType,
                            ContentType = ContentTypeHelper.GetShortContentType(file.ContentType),
                            FileSize = file.Length,
                            FileType = GetFileType(file.ContentType),
                            Description = "ملف محدث"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "خطأ في رفع الملف: {FileName}", file.FileName);
                        // استمر في معالجة الملفات الأخرى
                    }
                }
            }

            return uploadedFiles;
        }







        // إضافة هذا Helper في ResearchController أو في فئة منفصلة
        public static class ContentTypeHelper
        {
            private static readonly Dictionary<string, string> ContentTypeMapping = new()
    {
        // PDF Files
        { "application/pdf", "application/pdf" },
        
        // Word Documents
        { "application/msword", "application/msword" },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/docx" },
        
        // Excel Files
        { "application/vnd.ms-excel", "application/excel" },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/xlsx" },
        
        // PowerPoint Files
        { "application/vnd.ms-powerpoint", "application/ppt" },
        { "application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/pptx" },
        
        // Image Files
        { "image/jpeg", "image/jpeg" },
        { "image/jpg", "image/jpg" },
        { "image/png", "image/png" },
        { "image/gif", "image/gif" },
        
        // Text Files
        { "text/plain", "text/plain" },
        { "text/csv", "text/csv" },
        
        // Archive Files
        { "application/zip", "application/zip" },
        { "application/x-rar-compressed", "application/rar" },
        
        // Default
        { "", "application/octet-stream" }
    };

            public static string GetShortContentType(string originalContentType)
            {
                if (string.IsNullOrWhiteSpace(originalContentType))
                    return "application/octet-stream";

                // البحث عن مطابقة مباشرة
                if (ContentTypeMapping.TryGetValue(originalContentType, out var mappedType))
                    return mappedType;

                // إذا لم توجد مطابقة، اختصر النوع الطويل
                if (originalContentType.Length > 50)
                {
                    // محاولة استخراج النوع الأساسي
                    if (originalContentType.Contains("wordprocessingml"))
                        return "application/docx";
                    else if (originalContentType.Contains("spreadsheetml"))
                        return "application/xlsx";
                    else if (originalContentType.Contains("presentationml"))
                        return "application/pptx";
                    else if (originalContentType.StartsWith("application/"))
                        return "application/unknown";
                    else if (originalContentType.StartsWith("text/"))
                        return "text/plain";
                    else if (originalContentType.StartsWith("image/"))
                        return "image/unknown";
                    else
                        return "application/octet-stream";
                }

                return originalContentType;
            }

            public static string GetFileExtensionFromContentType(string contentType)
            {
                return contentType switch
                {
                    "application/pdf" => ".pdf",
                    "application/msword" or "application/docx" => ".docx",
                    "application/excel" or "application/xlsx" => ".xlsx",
                    "application/ppt" or "application/pptx" => ".pptx",
                    "image/jpeg" or "image/jpg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "text/plain" => ".txt",
                    "text/csv" => ".csv",
                    "application/zip" => ".zip",
                    "application/rar" => ".rar",
                    _ => ""
                };
            }

            public static string GetDisplayName(string contentType)
            {
                return contentType switch
                {
                    "application/pdf" => "PDF Document",
                    "application/msword" or "application/docx" => "Word Document",
                    "application/excel" or "application/xlsx" => "Excel Spreadsheet",
                    "application/ppt" or "application/pptx" => "PowerPoint Presentation",
                    "image/jpeg" or "image/jpg" => "JPEG Image",
                    "image/png" => "PNG Image",
                    "image/gif" => "GIF Image",
                    "text/plain" => "Text Document",
                    "text/csv" => "CSV File",
                    "application/zip" => "ZIP Archive",
                    "application/rar" => "RAR Archive",
                    _ => "Unknown File Type"
                };
            }
        }






















        // Helper methods لإنشاء القوائم المنسدلة
        private List<SelectListItem> GetResearchTypeOptions()
        {
            return Enum.GetValues<ResearchType>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = GetResearchTypeDisplayName(x)
                }).ToList();
        }

        private List<SelectListItem> GetLanguageOptions()
        {
            return Enum.GetValues<ResearchLanguage>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = GetLanguageDisplayName(x)
                }).ToList();
        }

        //private List<SelectListItem> GetTrackOptions()
        //{
        //    return Enum.GetValues<ResearchTrack>()
        //        .Select(x => new SelectListItem
        //        {
        //            Value = ((int)x).ToString(),
        //            Text = GetTrackDisplayName(x)
        //        }).ToList();
        //}
        private List<SelectListItem> GetTrackOptions()
        {
            var options = new List<SelectListItem>
    {
        new SelectListItem { Value = "", Text = "سيتم تحديده لاحقاً", Selected = true }
    };

            options.AddRange(Enum.GetValues<ResearchTrack>()
                .Where(t => t != ResearchTrack.NotAssigned) // استبعاد NotAssigned من القائمة
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = GetTrackDisplayName(x)
                }));

            return options;
        }

        // POST: Research/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Researcher,SystemAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var query = new GetResearchByIdQuery(id, user.Id);
                var research = await _mediator.Send(query);

                if (research == null)
                {
                    TempData["ErrorMessage"] = "البحث غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                if (!CanDeleteResearch(research, user))
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف هذا البحث";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var command = new DeleteResearchCommand
                {
                    ResearchId = id,
                    UserId = user.Id
                };

                await _mediator.Send(command);

                TempData["SuccessMessage"] = "تم حذف البحث بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting research with ID: {ResearchId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف البحث";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

    //    // GET: Research/DownloadFile/5
    //    [Authorize(Roles = "Researcher,Reviewer,TrackManager,ConferenceManager,SystemAdmin")]

    //    public async Task<IActionResult> DownloadFile(int fileId)
    //    {
    //        try
    //        {
    //            var user = await GetCurrentUserAsync();
    //            if (user == null)
    //                return RedirectToAction("Login", "Account");

    //            // Get research file info from database first
    //            var research = await _mediator.Send(new GetResearchByIdQuery(fileId, user.Id.ToString()));
    //            var fileDto = await _mediator
    //.Send(new GetResearchFileByIdQuery(fileId, user.Id));

    //            //if (research == null || research.Files?.Any() != true)
    //            if (fileDto == null)

    //            {
    //                TempData["ErrorMessage"] = "الملف غير موجود أو ليس لديك صلاحية للوصول إليه";
    //                return RedirectToAction(nameof(Index));
    //            }

    //            var file = research.Files.FirstOrDefault();
    //            if (file == null)
    //            {
    //                TempData["ErrorMessage"] = "الملف غير موجود";
    //                return RedirectToAction(nameof(Index));
    //            }

    //            try
    //            {

    //                var bytes = await _fileService.DownloadFileAsync(fileDto.FilePath);
    //                return File(bytes, fileDto.ContentType, fileDto.OriginalFileName);


    //                //var fileBytes = await _fileService.DownloadFileAsync(file.FilePath);
    //                //return File(fileBytes, file.ContentType, file.OriginalFileName);
    //            }
    //            catch (Exception)
    //            {
    //                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل الملف";
    //                return RedirectToAction(nameof(Index));
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error occurred while downloading file with ID: {FileId}", fileId);
    //            TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل الملف";
    //            return RedirectToAction(nameof(Index));
    //        }
    //    }




        // GET: Research/DownloadFile/5
        [Authorize(Roles = "Researcher,Reviewer,TrackManager,ConferenceManager,SystemAdmin")]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                _logger.LogInformation("بدء تحميل الملف - File ID: {FileId}", fileId);

                var user = await GetCurrentUserAsync();
                if (user == null)
                {
                    _logger.LogWarning("المستخدم غير مصرح له");
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("المستخدم مصرح: {UserId}", user.Id);

                // جلب معلومات الملف من قاعدة البيانات
                var fileDto = await _mediator.Send(new GetResearchFileByIdQuery(fileId, user.Id));

                if (fileDto == null)
                {
                    _logger.LogWarning("الملف غير موجود أو ليس لديك صلاحية للوصول إليه - File ID: {FileId}", fileId);
                    TempData["ErrorMessage"] = "الملف غير موجود أو ليس لديك صلاحية للوصول إليه";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("تم العثور على الملف: {FileName}", fileDto.OriginalFileName);

                // التحقق من صلاحية الوصول للملف
                var research = await _mediator.Send(new GetResearchByIdQuery(fileDto.ResearchId, user.Id));
                if (research == null)
                {
                    _logger.LogWarning("البحث غير موجود أو ليس لديك صلاحية للوصول إليه - Research ID: {ResearchId}", fileDto.ResearchId);
                    TempData["ErrorMessage"] = "ليس لديك صلاحية للوصول لهذا الملف";
                    return RedirectToAction(nameof(Index));
                }

                // التحقق من إمكانية تحميل الملفات
                if (!CanDownloadFiles(research, user))
                {
                    _logger.LogWarning("المستخدم {UserId} لا يملك صلاحية تحميل ملفات البحث {ResearchId}", user.Id, research.Id);
                    TempData["ErrorMessage"] = "ليس لديك صلاحية لتحميل هذا الملف";
                    return RedirectToAction(nameof(Details), new { id = research.Id });
                }

                try
                {
                    _logger.LogInformation("بدء تحميل الملف من المسار: {FilePath}", fileDto.FilePath);

                    // تحميل الملف من الخدمة
                    var fileBytes = await _fileService.DownloadFileAsync(fileDto.FilePath);

                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        _logger.LogError("الملف فارغ أو غير موجود في النظام - File Path: {FilePath}", fileDto.FilePath);
                        TempData["ErrorMessage"] = "الملف غير متوفر حالياً";
                        return RedirectToAction(nameof(Details), new { id = research.Id });
                    }

                    _logger.LogInformation("تم تحميل الملف بنجاح - الحجم: {Size} bytes", fileBytes.Length);

                    // تحديد Content Type
                    var contentType = !string.IsNullOrEmpty(fileDto.ContentType)
                        ? fileDto.ContentType
                        : "application/octet-stream";

                    // إرجاع الملف
                    return File(fileBytes, contentType, fileDto.OriginalFileName);
                }
                catch (FileNotFoundException)
                {
                    _logger.LogError("الملف غير موجود في النظام - File Path: {FilePath}", fileDto.FilePath);
                    TempData["ErrorMessage"] = "الملف غير موجود في النظام";
                    return RedirectToAction(nameof(Details), new { id = research.Id });
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogError("غير مصرح بالوصول للملف - File Path: {FilePath}", fileDto.FilePath);
                    TempData["ErrorMessage"] = "غير مصرح بالوصول لهذا الملف";
                    return RedirectToAction(nameof(Details), new { id = research.Id });
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "خطأ في تحميل الملف من النظام - File Path: {FilePath}", fileDto.FilePath);
                    TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل الملف";
                    return RedirectToAction(nameof(Details), new { id = research.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ عام في تحميل الملف - File ID: {FileId}", fileId);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل الملف";
                return RedirectToAction(nameof(Index));
            }
        }
        // POST: Research/UpdateStatus

        // POST: Research/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "TrackManager,SystemAdmin")]
        public async Task<IActionResult> UpdateStatus(int researchId, ResearchStatus newStatus, string? notes)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Json(new { success = false, message = "المستخدم غير مصرح له" });

                var command = new UpdateResearchStatusCommand
                {
                    ResearchId = researchId,
                    NewStatus = newStatus,
                    Notes = notes,
                    UserId = user.Id
                };

                var result = await _mediator.Send(command);

                if (result)
                {
                    return Json(new { success = true, message = "تم تحديث حالة البحث بنجاح" });
                }
                else
                {
                    return Json(new { success = false, message = "فشل في تحديث حالة البحث" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating research status for ID: {ResearchId}", researchId);
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث حالة البحث" });
            }
        }



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "TrackManager,SystemAdmin")]       
        //public async Task<IActionResult> UpdateStatus(int researchId, ResearchStatus newStatus, string? notes)
        //{
        //    try
        //    {
        //        var user = await GetCurrentUserAsync();
        //        if (user == null)
        //            return RedirectToAction("Login", "Account");

        //        var command = new UpdateResearchStatusCommand
        //        {
        //            ResearchId = researchId,
        //            NewStatus = newStatus,
        //            Notes = notes,
        //            UserId = user.Id
        //        };

        //        await _mediator.Send(command);

        //        TempData["SuccessMessage"] = "تم تحديث حالة البحث بنجاح";
        //        return RedirectToAction(nameof(Details), new { id = researchId });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while updating research status for ID: {ResearchId}", researchId);
        //        TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث حالة البحث";
        //        return RedirectToAction(nameof(Details), new { id = researchId });
        //    }
        //}

        // GET: Research/MyResearches
        [Authorize(Roles = "Researcher,SystemAdmin")]
        public async Task<IActionResult> MyResearches()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var query = new GetResearchListQuery(user.Id, user.Role);
                var result = await _mediator.Send(query);

                var viewModel = new ResearchListViewModel
                {
                    Researches = result,
                    CurrentUserId = user.Id,
                    CurrentUserRole = user.Role,
                    CanCreateResearch = true,
                    CanManageResearches = false
                };

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading user's researches");
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل بحوثك";
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetResearchFiles(int researchId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                // التحقق من صلاحية الوصول للبحث
                var research = await _mediator.Send(new GetResearchByIdQuery(researchId, user.Id));
                if (research == null)
                    return NotFound("البحث غير موجود أو ليس لديك صلاحية للوصول إليه");

                var files = research.Files?.Where(f => f.IsActive).Select(f => new
                {
                    id = f.Id,
                    fileName = f.OriginalFileName,
                    fileSize = f.FileSizeFormatted,
                    contentType = f.ContentType,
                    description = f.Description,
                    version = f.Version,
                    createdAt = f.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    downloadUrl = Url.Action("DownloadFile", new { fileId = f.Id })
                }).ToList();

                return Json(new { success = true, files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب ملفات البحث {ResearchId}", researchId);
                return Json(new { success = false, message = "حدث خطأ في جلب الملفات" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteResearchFile(int fileId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                // جلب معلومات الملف
                var fileDto = await _mediator.Send(new GetResearchFileByIdQuery(fileId, user.Id));
                if (fileDto == null)
                    return NotFound("الملف غير موجود");

                // التحقق من صلاحية حذف الملف
                var research = await _mediator.Send(new GetResearchByIdQuery(fileDto.ResearchId, user.Id));
                if (research == null || !CanUploadFiles(research, user))
                    return Forbid("ليس لديك صلاحية حذف هذا الملف");

                // حذف الملف من قاعدة البيانات (Soft Delete)
                var fileEntity = await _context.ResearchFiles.FindAsync(fileId);
                if (fileEntity != null)
                {
                    fileEntity.IsActive = false;
                    fileEntity.IsDeleted = true;
                    fileEntity.UpdatedAt = DateTime.UtcNow;
                    fileEntity.UpdatedBy = user.Id;

                    await _context.SaveChangesAsync();

                    // حذف الملف الفعلي من النظام
                    try
                    {
                        await _fileService.DeleteFileAsync(fileEntity.FilePath);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning(fileEx, "فشل في حذف الملف الفعلي: {FilePath}", fileEntity.FilePath);
                    }
                }

                return Json(new { success = true, message = "تم حذف الملف بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الملف {FileId}", fileId);
                return Json(new { success = false, message = "حدث خطأ في حذف الملف" });
            }
        }
        #region Helper Methods

        private List<SelectListItem> GetStatusOptions()
        {
            return Enum.GetValues<ResearchStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = GetStatusDisplayName(s)
                }).ToList();
        }



        // إضافة هذه الـ Actions في ResearchController.cs

        // POST: Research/AssignToTrack
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ConferenceManager")]
        public async Task<IActionResult> AssignToTrack(int researchId, ResearchTrack newTrack, string? notes)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Json(new { success = false, message = "المستخدم غير مصرح له" });

                // جلب البحث
                var research = await _mediator.Send(new GetResearchByIdQuery(researchId, user.Id));
                if (research == null)
                {
                    return Json(new { success = false, message = "البحث غير موجود" });
                }

                // تحديث المسار
                var command = new AssignResearchToTrackCommand
                {
                    ResearchId = researchId,
                    NewTrack = newTrack,
                    Notes = notes,
                    UserId = user.Id
                };

                var result = await _mediator.Send(command);

                if (result)
                {
                    // تسجيل التغيير في تاريخ الحالات
                    await LogTrackAssignment(researchId, research.Track, newTrack, user.Id, notes);

                    return Json(new
                    {
                        success = true,
                        message = "تم تحديد المسار بنجاح وإرسال البحث لمدير المسار"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "فشل في تحديد المسار" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديد المسار للبحث {ResearchId}", researchId);
                return Json(new { success = false, message = "حدث خطأ أثناء تحديد المسار" });
            }
        }

        // GET: Research/PendingTrackAssignment
        [HttpGet]
        [Authorize(Roles = "SystemAdmin,ConferenceManager")]
        public async Task<IActionResult> PendingTrackAssignment()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                // جلب البحوث التي تحتاج لتحديد المسار
                var pendingResearches = await _context.Researches
                    .Include(r => r.SubmittedBy)
                    .Include(r => r.Authors)
                    .Where(r => r.Status == ResearchStatus.Submitted &&
                               !r.IsDeleted &&
                               r.AssignedTrackManagerId == null) // لم يتم تعيين مدير مسار بعد
                    .OrderBy(r => r.SubmissionDate)
                    .ToListAsync();

                var viewModel = new PendingTrackAssignmentViewModel
                {
                    PendingResearches = pendingResearches.Select(r => new ResearchTrackAssignmentDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        TitleEn = r.TitleEn,
                        AbstractAr = r.AbstractAr,
                        SubmittedByName = $"{r.SubmittedBy.FirstName} {r.SubmittedBy.LastName}",
                        SubmissionDate = r.SubmissionDate,
                        CurrentTrack = r.Track,
                         //SuggestedTrack = DetermineSuggestedTrack(r), 
                        Authors = r.Authors?.Select(a => new ResearchAuthorDto
                        {
                            FirstName = a.FirstName,
                            LastName = a.LastName,
                            Email = a.Email,
                            Institution = a.Institution
                        }).ToList() ?? new List<ResearchAuthorDto>()
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل البحوث المعلقة لتحديد المسار");
                AddErrorMessage("حدث خطأ في تحميل البحوث المعلقة");
                return RedirectToAction("Index");
            }
        }

        // Helper method لتحديد المسار المقترح
////        private ResearchTrack DetermineSuggestedTrack(Research research)
////        {
////            يمكن تطوير خوارزمية أكثر تعقيداً هنا
////            var keywords = (research.Keywords + " " + research.KeywordsEn).ToLower();
////            var abstract = (research.AbstractAr + " " + research.AbstractEn).ToLower();
////        var title = (research.Title + " " + research.TitleEn).ToLower();

////        var content = (keywords + " " + abstract + " " + title);

////     خوارزمية بسيطة للتحديد التلقائي
////    if (content.Contains("energy") || content.Contains("renewable") || content.Contains("طاقة"))
////        return ResearchTrack.EnergyAndRenewableEnergy;
    
////    if (content.Contains("electrical") || content.Contains("electronic") || content.Contains("كهربائي"))
////        return ResearchTrack.ElectricalAndElectronicsEngineering;
    
////    if (content.Contains("material") || content.Contains("mechanical") || content.Contains("مواد") || content.Contains("ميكانيكي"))
////        return ResearchTrack.MaterialScienceAndMechanicalEngineering;
    
////    if (content.Contains("computer") || content.Contains("communication") || content.Contains("حاسوب") || content.Contains("اتصالات"))
////        return ResearchTrack.NavigationGuidanceSystemsComputerAndCommunicationEngineering;
    
////    if (content.Contains("avionics") || content.Contains("aircraft") || content.Contains("طيران"))
////        return ResearchTrack.AvionicsSystemsAircraftAndUnmannedAircraftEngineering;
    
////    if (content.Contains("petroleum") || content.Contains("gas") || content.Contains("بترول") || content.Contains("غاز"))
////        return ResearchTrack.EarthNaturalResourcesGasAndPetroleumSystemsEquipment;

////     العودة للمسار الأصلي إذا لم يتم العثور على تطابق
////    return research.Track;
////}


// Helper method لتسجيل تغيير المسار
private async Task LogTrackAssignment(int researchId, ResearchTrack? oldTrack, ResearchTrack? newTrack, string userId, string? notes)
        {
            try
            {
                var trackHistory = new ResearchTrackHistory
                {
                    ResearchId = researchId,
                    FromTrack = oldTrack,
                    ToTrack = newTrack,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = userId,
                    Notes = notes,
                    IsActive = true
                };

                _context.ResearchTrackHistories.Add(trackHistory);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تسجيل تاريخ تغيير المسار");
            }
        }


        // POST: Research/BulkAssignTracks
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SystemAdmin,ConferenceManager")]
        public async Task<IActionResult> BulkAssignTracks(List<BulkTrackAssignmentDto> assignments)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return Json(new { success = false, message = "المستخدم غير مصرح له" });

                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();

                foreach (var assignment in assignments)
                {
                    try
                    {
                        var command = new AssignResearchToTrackCommand
                        {
                            ResearchId = assignment.ResearchId,
                            NewTrack = assignment.Track,
                            Notes = assignment.Notes,
                            UserId = user.Id
                        };

                        var result = await _mediator.Send(command);
                        if (result)
                        {
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                            errors.Add($"فشل في تحديد المسار للبحث رقم {assignment.ResearchId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"خطأ في البحث رقم {assignment.ResearchId}: {ex.Message}");
                        _logger.LogError(ex, "خطأ في تحديد المسار للبحث {ResearchId}", assignment.ResearchId);
                    }
                }

                return Json(new
                {
                    success = errorCount == 0,
                    successCount = successCount,
                    errorCount = errorCount,
                    errors = errors,
                    message = $"تم تحديد المسار لـ {successCount} بحث بنجاح" + (errorCount > 0 ? $" مع {errorCount} أخطاء" : "")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحديد المجمع للمسارات");
                return Json(new { success = false, message = "حدث خطأ في التحديد المجمع للمسارات" });
            }
        }
        //private List<SelectListItem> GetTrackOptions()
        //{
        //    return Enum.GetValues<ResearchTrack>()
        //        .Select(t => new SelectListItem
        //        {
        //            Value = ((int)t).ToString(),
        //            Text = GetTrackDisplayName(t)
        //        }).ToList();
        //}

        private static string GetStatusDisplayName(ResearchStatus status) => status switch
        {
            ResearchStatus.Submitted => "مُقدم",
            ResearchStatus.UnderReview => "قيد المراجعة",
            ResearchStatus.Accepted => "مقبول",
            ResearchStatus.Rejected => "مرفوض",
            ResearchStatus.RequiresMinorRevisions => "يتطلب تعديلات طفيفة",
            ResearchStatus.RequiresMajorRevisions => "يتطلب تعديلات كبيرة",
            _ => status.ToString()
        };
        private static string GetResearchTypeDisplayName(ResearchType type) => type switch
        {
            ResearchType.OriginalResearch => "بحث أصلي",
            ResearchType.SystematicReview => "مراجعة منهجية",
            ResearchType.CaseStudy => "دراسة حالة",
            ResearchType.ExperimentalStudy => "بحث تجريبي",
            ResearchType.TheoreticalStudy => "بحث نظري",
            ResearchType.AppliedResearch => "بحث تطبيقي",
            ResearchType.LiteratureReview => "مراجعة أدبية",
            ResearchType.ComparativeStudy => "بحث مقارن",
            _ => type.ToString()
        };

        private static string GetLanguageDisplayName(ResearchLanguage language) => language switch
        {
            ResearchLanguage.Arabic => "العربية",
            ResearchLanguage.English => "الإنجليزية",
            ResearchLanguage.Bilingual => "ثنائي اللغة",
            _ => language.ToString()
        };

        private static string GetTrackDisplayName(ResearchTrack track) => track switch
        {
            ResearchTrack.NotAssigned => "غير محدد",
            ResearchTrack.EnergyAndRenewableEnergy => "Energy and Renewable Energy",
            ResearchTrack.ElectricalAndElectronicsEngineering => "Electromechanical System, and Mechatronics Engineering",
            ResearchTrack.MaterialScienceAndMechanicalEngineering => "Material Science & Mechanical Engineering",
            ResearchTrack.NavigationGuidanceSystemsComputerAndCommunicationEngineering => "Navigation & Guidance Systems, Computer and Communication Engineering",
            ResearchTrack.ElectromechanicalSystemAndMechanicsEngineering => "Electrical & Electronics Engineering",
            ResearchTrack.AvionicsSystemsAircraftAndUnmannedAircraftEngineering => "Avionics Systems, Aircraft and Unmanned Aircraft Engineering",
            ResearchTrack.EarthNaturalResourcesGasAndPetroleumSystemsEquipment => "Earth's Natural Resources, Gas and Petroleum Systems & Equipment",
            _ => track.ToString()
        };


        private static FileType GetFileType(string contentType) => contentType.ToLower() switch
        {
            "application/pdf" => FileType.OriginalResearch,
            "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => FileType.SupplementaryFiles,
            "image/jpeg" or "image/png" or "image/gif" => FileType.SupplementaryFiles,
            _ => FileType.SupplementaryFiles
        };

        private static bool CanCreateResearch(UserRole role) =>
            role == UserRole.Researcher || role == UserRole.SystemAdmin;

        private static bool CanManageResearches(UserRole role) =>
            role == UserRole.TrackManager || role == UserRole.SystemAdmin;

        private static bool CanEditResearch(ResearchDto research, User user)
        {
            if (user.Role == UserRole.SystemAdmin) return true;

            if (research.SubmittedById == user.Id)
            {
                return research.Status == ResearchStatus.Submitted ||
                       research.Status == ResearchStatus.RequiresMinorRevisions;
            }

            return false;
        }

        private static bool CanDeleteResearch(ResearchDto research, User user)
        {
            if (user.Role == UserRole.SystemAdmin) return true;

            return research.SubmittedById == user.Id &&
                   research.Status == ResearchStatus.Submitted;
        }

        private static bool CanReviewResearch(ResearchDto research, User user)
        {
            if (user.Role != UserRole.Reviewer && user.Role != UserRole.SystemAdmin) return false;

            return research.Reviews?.Any(r => r.ReviewerId == user.Id.ToString() && !r.IsCompleted) == true;
        }

        private static bool CanManageStatus(UserRole role) =>
            role == UserRole.TrackManager || role == UserRole.SystemAdmin;

        private static bool CanDownloadFiles(ResearchDto research, User user)
        {
            // Authors, reviewers, track managers, and admins can download files
            return research.SubmittedById == user.Id ||
                   research.Authors?.Any(a => a.UserId == user.Id) == true ||
                   research.Reviews?.Any(r => r.ReviewerId == user.Id.ToString()) == true ||
                   (int.TryParse(user.Id, out int userId) && research.AssignedTrackManagerId == userId) ||
                   user.Role == UserRole.SystemAdmin;
        }

        private static bool CanUploadFiles(ResearchDto research, User user)
        {
            // Only authors can upload files, and only in certain statuses
            return research.SubmittedById == user.Id &&
                   (research.Status == ResearchStatus.Submitted ||
                    research.Status == ResearchStatus.RequiresMinorRevisions);
        }

        private static bool IsAuthor(ResearchDto research, string userId) =>
            research.SubmittedById == userId ||
            research.Authors?.Any(a => a.UserId == userId) == true;

        private static bool IsReviewer(ResearchDto research, string userId) =>
            research.Reviews?.Any(r => r.ReviewerId == userId) == true;

        private static bool IsTrackManager(ResearchDto research, string userId) =>
            int.TryParse(userId, out int id) && research.AssignedTrackManagerId == id;

        #endregion
    }
}



