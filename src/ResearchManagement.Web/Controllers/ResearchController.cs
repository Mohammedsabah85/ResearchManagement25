﻿using Microsoft.AspNetCore.Mvc;
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

using System.Linq;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Infrastructure.Data;



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
                    IsEditMode = false
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
        public async Task<IActionResult> Create(CreateResearchViewModel model, List<IFormFile> files)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                // Create research DTO
                var createResearchDto = _mapper.Map<CreateResearchDto>(model);


                // Handle file uploads
                if (files?.Any() == true)
                {
                    var uploadedFiles = new List<ResearchFileDto>();
                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            using var memoryStream = new MemoryStream();
                            await file.CopyToAsync(memoryStream);
                            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                            var filePath = await _fileService.UploadFileAsync(memoryStream.ToArray(), fileName, file.ContentType);

                            uploadedFiles.Add(new ResearchFileDto
                            {
                                FileName = fileName,
                                OriginalFileName = file.FileName,
                                FilePath = filePath,
                                ContentType = file.ContentType,
                                FileSize = file.Length,
                                FileType = GetFileType(file.ContentType),
                                Description = "ملف البحث الرئيسي"
                            });
                        }
                    }
                    createResearchDto.Files = uploadedFiles;
                }

                var command = new CreateResearchCommand
                {
                    Research = createResearchDto,
                    UserId = user.Id
                };

                var researchId = await _mediator.Send(command);

                TempData["SuccessMessage"] = "تم تقديم البحث بنجاح";
                return RedirectToAction(nameof(Details), new { id = researchId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating research");
                TempData["ErrorMessage"] = "حدث خطأ أثناء تقديم البحث";
                return View(model);
            }
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
                            ContentType = file.ContentType,
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

        private List<SelectListItem> GetTrackOptions()
        {
            return Enum.GetValues<ResearchTrack>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = GetTrackDisplayName(x)
                }).ToList();
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

        // GET: Research/DownloadFile/5
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");

                // Get research file info from database first
                var research = await _mediator.Send(new GetResearchByIdQuery(fileId, user.Id.ToString()));
				var fileDto = await _mediator
	.Send(new GetResearchFileByIdQuery(fileId, user.Id));

				//if (research == null || research.Files?.Any() != true)
				if (fileDto == null)

				{
					TempData["ErrorMessage"] = "الملف غير موجود أو ليس لديك صلاحية للوصول إليه";
                    return RedirectToAction(nameof(Index));
                }

                var file = research.Files.FirstOrDefault();
                if (file == null)
                {
                    TempData["ErrorMessage"] = "الملف غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                try
                {

					var bytes = await _fileService.DownloadFileAsync(fileDto.FilePath);
					return File(bytes, fileDto.ContentType, fileDto.OriginalFileName);


                    //var fileBytes = await _fileService.DownloadFileAsync(file.FilePath);
                    //return File(fileBytes, file.ContentType, file.OriginalFileName);
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل الملف";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading file with ID: {FileId}", fileId);
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
            ResearchTrack.InformationTechnology => "تقنية المعلومات",
            ResearchTrack.InformationSecurity => "أمن المعلومات",
            ResearchTrack.SoftwareEngineering => "هندسة البرمجيات",
            ResearchTrack.ArtificialIntelligence => "الذكاء الاصطناعي",
            ResearchTrack.DataScience => "علوم البيانات",
            ResearchTrack.NetworkingAndCommunications => "الشبكات والاتصالات",
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

    // Additional Commands that might be needed
//    public class UpdateResearchCommand : IRequest<bool>
//    {
//        public int ResearchId { get; set; }
//        public CreateResearchDto Research { get; set; } = new();
//        public string UserId { get; set; } = string.Empty;
//    }

//    public class DeleteResearchCommand : IRequest<bool>
//    {
//        public int ResearchId { get; set; }
//        public string UserId { get; set; } = string.Empty;
//    }
}


//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Authorization;
//using MediatR;
//using ResearchManagement.Domain.Entities;
//using ResearchManagement.Domain.Enums;
//using ResearchManagement.Application.DTOs;
//using ResearchManagement.Application.Commands.Research;
//using ResearchManagement.Application.Interfaces;
//using Microsoft.EntityFrameworkCore;
//using ResearchManagement.Infrastructure.Data;
//using ResearchManagement.Web.Models.ViewModels.Research;

//namespace ResearchManagement.Web.Controllers
//{
//    [Authorize]
//    public class ResearchController : BaseController
//    {
//        private readonly IMediator _mediator;
//        private readonly IResearchRepository _researchRepository;
//        private readonly IFileService _fileService;
//        private readonly ILogger<ResearchController> _logger;
//        private readonly ApplicationDbContext _context;
//        public ResearchController(
//            UserManager<User> userManager,
//            IMediator mediator,
//            IResearchRepository researchRepository,
//            IFileService fileService,
//            ApplicationDbContext context,
//            ILogger<ResearchController> logger) : base(userManager)
//        {
//            _mediator = mediator;
//            _researchRepository = researchRepository;
//            _fileService = fileService;
//            _context = context;
//            _logger = logger;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var user = await GetCurrentUserAsync();
//            if (user == null)
//                return RedirectToAction("Login", "Account");

//            IEnumerable<Research> researches;

//            switch (user.Role)
//            {
//                case UserRole.Researcher:
//                    researches = await _researchRepository.GetByUserIdAsync(user.Id);
//                    break;
//                case UserRole.ConferenceManager:
//                    researches = await _researchRepository.GetAllAsync();
//                    break;
//                default:
//                    researches = new List<Research>();
//                    break;
//            }

//            return View(researches);
//        }

//        [HttpGet]
//        public async Task<IActionResult> Details(int id)
//        {
//            var research = await _researchRepository.GetByIdWithDetailsAsync(id);
//            if (research == null)
//                return NotFound();

//            var user = await GetCurrentUserAsync();
//            if (user == null)
//                return RedirectToAction("Login", "Account");

//            // التحقق من الصلاحيات
//            if (user.Role == UserRole.Researcher && research.SubmittedById != user.Id)
//                return Forbid();

//            // تحويل إلى ViewModel
//            var researchDto = new ResearchDto
//            {
//                Id = research.Id,
//                Title = research.Title,
//                TitleEn = research.TitleEn,
//                AbstractAr = research.AbstractAr,
//                AbstractEn = research.AbstractEn,
//                Keywords = research.Keywords,
//                KeywordsEn = research.KeywordsEn,
//                ResearchType = research.ResearchType,
//                Language = research.Language,
//                Track = research.Track,
//                Methodology = research.Methodology,
//                Status = research.Status,
//                SubmissionDate = research.SubmissionDate,
//                DecisionDate = research.DecisionDate,
//                RejectionReason = research.RejectionReason,
//                ReviewDeadline = research.ReviewDeadline,
//                SubmittedByName = research.SubmittedBy != null ? $"{research.SubmittedBy.FirstName} {research.SubmittedBy.LastName}" : "غير محدد",
//                AssignedTrackManagerName = research.AssignedTrackManager?.User != null ? $"{research.AssignedTrackManager.User.FirstName} {research.AssignedTrackManager.User.LastName}" : null,
//                Authors = research.Authors?.Select(a => new ResearchAuthorDto
//                {
//                    FirstName = a.FirstName,
//                    LastName = a.LastName,
//                    FirstNameEn = a.FirstNameEn,
//                    LastNameEn = a.LastNameEn,
//                    Email = a.Email,
//                    Institution = a.Institution,
//                    AcademicDegree = a.AcademicDegree,
//                    OrcidId = a.OrcidId,
//                    Order = a.Order,
//                    IsCorresponding = a.IsCorresponding
//                }).ToList() ?? new List<ResearchAuthorDto>()
//            };

//            var model = new ResearchDetailsViewModel
//            {
//                Research = researchDto,
//                CurrentUserId = user.Id,
//                CurrentUserRole = user.Role,
//                IsAuthor = research.SubmittedById == user.Id,
//                CanEdit = user.Role == UserRole.Researcher && research.SubmittedById == user.Id,
//                CanDelete = user.Role == UserRole.Researcher && research.SubmittedById == user.Id,
//                CanDownloadFiles = true
//            };

//            return View(model);
//        }

//        [HttpGet]
//        [Authorize(Roles = "Researcher")]
//        public IActionResult Create()
//        {
//            //var model = new CreateResearchDto();
//            var model = new CreateResearchViewModel();
//            return View(model);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "Researcher")]
//        //public async Task<IActionResult> Create(CreateResearchDto model, IFormFile? researchFile)
//        public async Task<IActionResult> Create(CreateResearchViewModel viewModel, IFormFile? researchFile)

//        {
//            if (!ModelState.IsValid)
//            {
//                // إضافة معلومات debug
//                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
//                {
//                    _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
//                }
//                //return View(model);
//                return View(viewModel);
//            }

//            try
//            {
//                //_logger.LogInformation("Creating research for user {UserId}: {Title}", GetCurrentUserId(), model.Title);
//                _logger.LogInformation("Creating research for user {UserId}: {Title}", GetCurrentUserId(), viewModel.Title);

//                // تحويل ViewModel إلى DTO
//                var researchDto = new CreateResearchDto
//                {
//                    Title = viewModel.Title,
//                    TitleEn = viewModel.TitleEn,
//                    AbstractAr = viewModel.AbstractAr,
//                    AbstractEn = viewModel.AbstractEn,
//                    Keywords = viewModel.Keywords,
//                    KeywordsEn = viewModel.KeywordsEn,
//                    ResearchType = viewModel.ResearchType,
//                    Language = viewModel.Language,
//                    Track = viewModel.Track,
//                    Methodology = viewModel.Methodology
//                };
//                var command = new CreateResearchCommand
//                {
//                    Research = researchDto,
//                    UserId = GetCurrentUserId()
//                };

//                var researchId = await _mediator.Send(command);

//                _logger.LogInformation("Research created successfully with ID {ResearchId}", researchId);

//                // رفع الملف إذا تم تحديده
//                if (researchFile != null && researchFile.Length > 0)
//                {
//                    try
//                    {
//                        await UploadResearchFile(researchId, researchFile, FileType.OriginalResearch);
//                        _logger.LogInformation("File uploaded successfully for research {ResearchId}", researchId);
//                    }
//                    catch (Exception fileEx)
//                    {
//                        _logger.LogError(fileEx, "Failed to upload file for research {ResearchId}", researchId);
//                        AddWarningMessage("تم حفظ البحث بنجاح لكن فشل في رفع الملف");
//                    }
//                }

//                AddSuccessMessage("تم تقديم البحث بنجاح");
//                return RedirectToAction("Details", new { id = researchId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating research for user {UserId}", GetCurrentUserId());
//                AddErrorMessage($"حدث خطأ في تقديم البحث: {ex.Message}");
//                return View(viewModel);
//            }
//        }

//        [HttpGet]
//        [Authorize(Roles = "Researcher")]
//        public async Task<IActionResult> Edit(int id)
//        {
//            var research = await _researchRepository.GetByIdWithDetailsAsync(id);
//            if (research == null)
//                return NotFound();

//            // التحقق من الملكية
//            if (research.SubmittedById != GetCurrentUserId())
//                return Forbid();

//            // التحقق من إمكانية التعديل
//            if (research.Status != ResearchStatus.Submitted &&
//                research.Status != ResearchStatus.RequiresMinorRevisions &&
//                research.Status != ResearchStatus.RequiresMajorRevisions)
//            {
//                AddWarningMessage("لا يمكن تعديل البحث في الحالة الحالية");
//                return RedirectToAction("Details", new { id });
//            }

//            var model = new UpdateResearchDto
//            {
//                Id = research.Id,
//                Title = research.Title,
//                TitleEn = research.TitleEn,
//                AbstractAr = research.AbstractAr,
//                AbstractEn = research.AbstractEn,
//                Keywords = research.Keywords,
//                KeywordsEn = research.KeywordsEn,
//                ResearchType = research.ResearchType,
//                Language = research.Language,
//                Track = research.Track,
//                Methodology = research.Methodology,
//                Authors = research.Authors.Select(a => new UpdateResearchAuthorDto
//                {
//                    Id = a.Id,
//                    FirstName = a.FirstName,
//                    LastName = a.LastName,
//                    FirstNameEn = a.FirstNameEn,
//                    LastNameEn = a.LastNameEn,
//                    Email = a.Email,
//                    Institution = a.Institution,
//                    AcademicDegree = a.AcademicDegree,
//                    OrcidId = a.OrcidId,
//                    Order = a.Order,
//                    IsCorresponding = a.IsCorresponding
//                }).ToList()
//            };

//            return View(model);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "Researcher")]
//        public async Task<IActionResult> Edit(UpdateResearchDto model, IFormFile? newResearchFile)
//        {
//            if (!ModelState.IsValid)
//                return View(model);

//            try
//            {
//                var research = await _researchRepository.GetByIdAsync(model.Id);
//                if (research == null)
//                    return NotFound();

//                // التحقق من الملكية
//                if (research.SubmittedById != GetCurrentUserId())
//                    return Forbid();

//                // تحديث البيانات
//                research.Title = model.Title;
//                research.TitleEn = model.TitleEn;
//                research.AbstractAr = model.AbstractAr;
//                research.AbstractEn = model.AbstractEn;
//                research.Keywords = model.Keywords;
//                research.KeywordsEn = model.KeywordsEn;
//                research.ResearchType = model.ResearchType;
//                research.Language = model.Language;
//                research.Track = model.Track;
//                research.Methodology = model.Methodology;
//                research.UpdatedAt = DateTime.UtcNow;
//                research.UpdatedBy = GetCurrentUserId();

//                // تحديث الحالة إذا كان البحث يتطلب تعديلات
//                if (research.Status == ResearchStatus.RequiresMinorRevisions ||
//                    research.Status == ResearchStatus.RequiresMajorRevisions)
//                {
//                    research.Status = ResearchStatus.RevisionsSubmitted;
//                }

//                await _researchRepository.UpdateAsync(research);

//                // رفع ملف جديد إذا تم تحديده
//                if (newResearchFile != null && newResearchFile.Length > 0)
//                {
//                    await UploadResearchFile(model.Id, newResearchFile, FileType.RevisedVersion);
//                }

//                AddSuccessMessage("تم تحديث البحث بنجاح");
//                return RedirectToAction("Details", new { id = model.Id });
//            }
//            catch (Exception ex)
//            {
//                AddErrorMessage($"حدث خطأ في تحديث البحث: {ex.Message}");
//                return View(model);
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> DownloadFile(int fileId)
//        {
//            try
//            {
//                // البحث عن الملف في قاعدة البيانات
//                var researchFile = await _context.ResearchFiles
//                    .Include(f => f.Research)
//                    .FirstOrDefaultAsync(f => f.Id == fileId && f.IsActive);

//                if (researchFile == null)
//                {
//                    AddErrorMessage("الملف غير موجود");
//                    return NotFound();
//                }

//                // التحقق من صلاحيات الوصول
//                var currentUser = await GetCurrentUserAsync();
//                if (currentUser == null)
//                {
//                    return RedirectToAction("Login", "Account");
//                }

//                // التحقق من الصلاحيات حسب دور المستخدم
//                bool hasAccess = false;

//                if (currentUser.Role == UserRole.SystemAdmin ||
//                    currentUser.Role == UserRole.ConferenceManager)
//                {
//                    hasAccess = true; // المدراء لهم صلاحية كاملة
//                }
//                else if (currentUser.Role == UserRole.Researcher &&
//                         researchFile.Research.SubmittedById == currentUser.Id)
//                {
//                    hasAccess = true; // الباحث يمكنه تحميل ملفات بحوثه
//                }
//                else if (currentUser.Role == UserRole.Reviewer)
//                {
//                    // المراجع يمكنه تحميل ملفات البحوث المكلف بمراجعتها
//                    var hasReviewAccess = await _context.Reviews
//                        .AnyAsync(r => r.ResearchId == researchFile.ResearchId &&
//                                      r.ReviewerId == currentUser.Id);
//                    hasAccess = hasReviewAccess;
//                }
//                else if (currentUser.Role == UserRole.TrackManager)
//                {
//                    // مدير التراك يمكنه تحميل ملفات بحوث تخصصه
//                    var trackManager = await _context.TrackManagers
//                        .FirstOrDefaultAsync(tm => tm.UserId == currentUser.Id &&
//                                                  tm.Track == researchFile.Research.Track);
//                    hasAccess = trackManager != null;
//                }

//                if (!hasAccess)
//                {
//                    AddErrorMessage("ليس لديك صلاحية لتحميل هذا الملف");
//                    return Forbid();
//                }

//                // تحميل محتوى الملف
//                var fileContent = await _fileService.DownloadFileAsync(researchFile.FilePath);

//                // إرجاع الملف للتحميل
//                return File(fileContent, researchFile.ContentType, researchFile.OriginalFileName);
//            }
//            catch (FileNotFoundException)
//            {
//                AddErrorMessage("الملف غير موجود على الخادم");
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                // تسجيل الخطأ
//                _logger?.LogError(ex, "Error downloading file {FileId}", fileId);
//                AddErrorMessage("حدث خطأ أثناء تحميل الملف");
//                return RedirectToAction("Index");
//            }
//        }

//        private async Task UploadResearchFile(int researchId, IFormFile file, FileType fileType)
//        {
//            try
//            {
//                // التحقق من صحة الملف
//                if (file == null || file.Length == 0)
//                {
//                    throw new ArgumentException("لم يتم تحديد ملف صالح");
//                }

//                // التحقق من نوع الملف
//                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
//                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

//                if (!allowedExtensions.Contains(fileExtension))
//                {
//                    throw new ArgumentException($"نوع الملف غير مدعوم. الأنواع المدعومة: {string.Join(", ", allowedExtensions)}");
//                }

//                // التحقق من حجم الملف (50 ميجابايت)
//                if (file.Length > 50 * 1024 * 1024)
//                {
//                    throw new ArgumentException("حجم الملف كبير جداً. الحد الأقصى 50 ميجابايت");
//                }

//                // تحويل الملف إلى byte array
//                using var stream = new MemoryStream();
//                await file.CopyToAsync(stream);
//                var fileContent = stream.ToArray();

//                // رفع الملف باستخدام FileService
//                var filePath = await _fileService.UploadFileAsync(fileContent, file.FileName, file.ContentType);

//                // حفظ معلومات الملف في قاعدة البيانات
//                var researchFile = new ResearchFile
//                {
//                    ResearchId = researchId,
//                    FileName = Path.GetFileName(filePath),
//                    OriginalFileName = file.FileName,
//                    FilePath = filePath,
//                    ContentType = file.ContentType,
//                    FileSize = file.Length,
//                    FileType = fileType,
//                    Version = 1,
//                    IsActive = true,
//                    CreatedAt = DateTime.UtcNow,
//                    CreatedBy = GetCurrentUserId()
//                };

//                _context.ResearchFiles.Add(researchFile);
//                await _context.SaveChangesAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger?.LogError(ex, "Error uploading file for research {ResearchId}", researchId);
//                throw; // إعادة إرسال الخطأ للتعامل معه في المستوى الأعلى
//            }
//        }
//    }
//}
