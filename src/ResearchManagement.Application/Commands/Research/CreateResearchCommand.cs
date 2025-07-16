using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Application.Commands.Research
{
    public class CreateResearchCommand : IRequest<int>
    {
        public CreateResearchDto Research { get; set; } = new();
        public string UserId { get; set; } = string.Empty;
    }


    public class CreateResearchCommandHandler : IRequestHandler<CreateResearchCommand, int>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateResearchCommandHandler> _logger;
        private readonly IEmailService _emailService;

        public CreateResearchCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateResearchCommandHandler> logger,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
        }
        //    public async Task<int> Handle(CreateResearchCommand request, CancellationToken cancellationToken)
        //    {
        //        _logger.LogInformation("بدء إنشاء بحث جديد للمستخدم {UserId}", request.UserId);

        //        await _unitOfWork.BeginTransactionAsync();

        //        try
        //        {
        //            // 1. تحويل DTO إلى Entity

        //            var research = _mapper.Map<Domain.Entities.Research>(request.Research);
        //            research.SubmittedById = request.UserId;
        //            research.Status = ResearchStatus.Submitted;
        //            research.SubmissionDate = DateTime.UtcNow;
        //            research.CreatedAt = DateTime.UtcNow;
        //            research.CreatedBy = request.UserId;
        //            //research.Track = request.Research.Track != 0 ? request.Research.Track : ResearchTrack.NotAssigned;


        //            _logger.LogInformation("تم تحويل البيانات بنجاح");

        //            // 2. إضافة البحث إلى قاعدة البيانات

        //            //var addedResearch = await _unitOfWork.Research.AddAsync(research);
        //            var addedResearch = await _unitOfWork.Research.AddAsync(research);
        //            var saveResult1 = await _unitOfWork.SaveChangesAsync(cancellationToken);

        //            _logger.LogInformation("تم حفظ البحث - المعرف: {ResearchId}, النتيجة: {SaveResult}",
        //                research.Id, saveResult1);

        //            if (research.Id == 0)
        //            {
        //                throw new InvalidOperationException("فشل في الحصول على معرف البحث");
        //            }

        //            // 3. إضافة المؤلفين
        //            if (request.Research.Authors?.Any() == true)
        //            {
        //                foreach (var authorDto in request.Research.Authors)
        //                {
        //                    var author = _mapper.Map<ResearchAuthor>(authorDto);
        //                    author.ResearchId = research.Id;
        //                    author.CreatedAt = DateTime.UtcNow;
        //                    author.CreatedBy = request.UserId;

        //                    research.Authors.Add(author);

        //                    _logger.LogInformation("تم إضافة مؤلف: {AuthorName}", author.FirstName);
        //                }

        //                var saveResult2 = await _unitOfWork.SaveChangesAsync(cancellationToken);
        //                _logger.LogInformation("تم حفظ المؤلفين - النتيجة: {SaveResult}", saveResult2);
        //            }





        //// 4. persist uploaded files
        //if (request.Research.Files?.Any() == true)
        //{
        //	foreach (var fileDto in request.Research.Files)
        //	{
        //		var fileEntity = new ResearchFile
        //		{
        //			ResearchId = research.Id,
        //			FileName = fileDto.FileName,
        //			OriginalFileName = fileDto.OriginalFileName,
        //			FilePath = fileDto.FilePath,
        //			ContentType = fileDto.ContentType,
        //			FileSize = fileDto.FileSize,
        //			FileType = fileDto.FileType,
        //			Description = fileDto.Description,
        //			Version = 1,
        //			IsActive = true,
        //			CreatedAt = DateTime.UtcNow,
        //			CreatedBy = request.UserId
        //		};
        //		research.Files.Add(fileEntity);
        //	}
        //	var saveResult3 = await _unitOfWork.SaveChangesAsync(cancellationToken);
        //	_logger.LogInformation("تم حفظ الملفات – النتيجة: {SaveResult}", saveResult3);
        //}



        //// 4. إرسال إيميل التأكيد
        //try
        //            {
        //                await _emailService.SendResearchSubmissionConfirmationAsync(research.Id);
        //                _logger.LogInformation("تم إرسال إيميل التأكيد");
        //            }
        //            catch (Exception emailEx)
        //            {
        //                _logger.LogWarning(emailEx, "فشل في إرسال إيميل التأكيد");
        //                // لا نوقف العملية بسبب خطأ الإيميل
        //            }

        //            await _unitOfWork.CommitTransactionAsync();

        //            _logger.LogInformation("تم إنشاء البحث بنجاح - المعرف: {ResearchId}", research.Id);
        //            return research.Id;
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "فشل في إنشاء البحث للمستخدم {UserId}", request.UserId);

        //            await _unitOfWork.RollbackTransactionAsync();
        //            throw new InvalidOperationException($"فشل في حفظ البحث: {ex.Message}", ex);
        //        }
        //    }

        // تحسين CreateResearchCommandHandler
        public async Task<int> Handle(CreateResearchCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== بدء إنشاء بحث جديد للمستخدم {UserId} ===", request.UserId);

            // طباعة تفاصيل الطلب للتشخيص
            _logger.LogInformation("Research Title: {Title}", request.Research.Title);
            _logger.LogInformation("Research Type: {ResearchType}", request.Research.ResearchType);
            _logger.LogInformation("Language: {Language}", request.Research.Language);
            _logger.LogInformation("Track: {Track}", request.Research.Track);
            _logger.LogInformation("Authors Count: {AuthorsCount}", request.Research.Authors?.Count ?? 0);
            _logger.LogInformation("Files Count: {FilesCount}", request.Research.Files?.Count ?? 0);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. التحقق من صحة البيانات
                if (string.IsNullOrWhiteSpace(request.Research.Title))
                {
                    throw new ArgumentException("عنوان البحث مطلوب");
                }

                if (string.IsNullOrWhiteSpace(request.Research.AbstractAr))
                {
                    throw new ArgumentException("الملخص باللغة العربية مطلوب");
                }

                if (request.Research.Authors?.Any() != true)
                {
                    throw new ArgumentException("يجب إضافة مؤلف واحد على الأقل");
                }

                // 2. تحويل DTO إلى Entity
                var research = _mapper.Map<Domain.Entities.Research>(request.Research);

                // تأكد من تعيين القيم المطلوبة
                research.SubmittedById = request.UserId;
                research.Status = ResearchStatus.Submitted;
                research.SubmissionDate = DateTime.UtcNow;
                research.CreatedAt = DateTime.UtcNow;
                research.CreatedBy = request.UserId;
                research.IsDeleted = false;

                // تأكد من وجود Track
                if (!research.Track.HasValue)
                {
                    research.Track = ResearchTrack.NotAssigned;
                }

                _logger.LogInformation("تم تحويل DTO إلى Entity بنجاح");

                // 3. إضافة البحث إلى قاعدة البيانات
                var addedResearch = await _unitOfWork.Research.AddAsync(research);
                var saveResult1 = await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("تم حفظ البحث - المعرف: {ResearchId}, النتيجة: {SaveResult}",
                    research.Id, saveResult1);

                if (research.Id == 0)
                {
                    throw new InvalidOperationException("فشل في الحصول على معرف البحث");
                }

                // 4. إضافة المؤلفين
                if (request.Research.Authors?.Any() == true)
                {
                    _logger.LogInformation("بدء إضافة المؤلفين - العدد: {Count}", request.Research.Authors.Count);

                    foreach (var authorDto in request.Research.Authors)
                    {
                        // التحقق من صحة بيانات المؤلف
                        if (string.IsNullOrWhiteSpace(authorDto.FirstName) || string.IsNullOrWhiteSpace(authorDto.LastName))
                        {
                            _logger.LogWarning("تم تجاهل مؤلف بسبب بيانات ناقصة");
                            continue;
                        }

                        var author = _mapper.Map<ResearchAuthor>(authorDto);
                        author.ResearchId = research.Id;
                        author.CreatedAt = DateTime.UtcNow;
                        author.CreatedBy = request.UserId;
                        author.IsDeleted = false;

                        // تأكد من Order
                        if (author.Order <= 0)
                        {
                            author.Order = research.Authors.Count + 1;
                        }

                        await _unitOfWork.ResearchAuthors.AddAsync(author);
                        _logger.LogInformation("تم إضافة مؤلف: {AuthorName}", $"{author.FirstName} {author.LastName}");
                    }

                    var saveResult2 = await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("تم حفظ المؤلفين - النتيجة: {SaveResult}", saveResult2);
                }

                // 5. إضافة الملفات المرفوعة
                if (request.Research.Files?.Any() == true)
                {
                    _logger.LogInformation("بدء إضافة الملفات - العدد: {Count}", request.Research.Files.Count);

                    foreach (var fileDto in request.Research.Files)
                    {
                        var fileEntity = new ResearchFile
                        {
                            ResearchId = research.Id,
                            FileName = fileDto.FileName,
                            OriginalFileName = fileDto.OriginalFileName,
                            FilePath = fileDto.FilePath,
                            ContentType = fileDto.ContentType,
                            FileSize = fileDto.FileSize,
                            FileType = fileDto.FileType,
                            Description = fileDto.Description,
                            Version = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = request.UserId
                        };

                        await _unitOfWork.ResearchFiles.AddAsync(fileEntity);
                        _logger.LogInformation("تم إضافة ملف: {FileName}", fileEntity.OriginalFileName);
                    }

                    var saveResult3 = await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("تم حفظ الملفات – النتيجة: {SaveResult}", saveResult3);
                }

                // 6. إرسال إيميل التأكيد
                try
                {
                    await _emailService.SendResearchSubmissionConfirmationAsync(research.Id);
                    _logger.LogInformation("تم إرسال إيميل التأكيد");
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "فشل في إرسال إيميل التأكيد - سيتم تجاهل هذا الخطأ");
                    // لا نوقف العملية بسبب خطأ الإيميل
                }

                // 7. إتمام Transaction
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("=== تم إنشاء البحث بنجاح - المعرف: {ResearchId} ===", research.Id);
                return research.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== فشل في إنشاء البحث للمستخدم {UserId} ===", request.UserId);
                _logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception Type: {InnerExceptionType}", ex.InnerException.GetType().Name);
                    _logger.LogError("Inner Exception Message: {InnerMessage}", ex.InnerException.Message);
                    _logger.LogError("Inner Stack Trace: {InnerStackTrace}", ex.InnerException.StackTrace);
                }

                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogInformation("تم التراجع عن Transaction بنجاح");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "فشل في التراجع عن Transaction");
                }

                throw new InvalidOperationException($"فشل في حفظ البحث: {ex.Message}", ex);
            }
        }

    }
}

