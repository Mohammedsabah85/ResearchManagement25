using AutoMapper;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;
using System;
using System.Linq;

namespace ResearchManagement.Application.Mappings
{
    public class ApplicationMappingProfile : Profile
    {
        public ApplicationMappingProfile()
        {
            // User Mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.FirstNameEn) && !string.IsNullOrEmpty(src.LastNameEn)
                        ? $"{src.FirstNameEn} {src.LastNameEn}" : null));

            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            // Research Mappings
            CreateMap<Research, ResearchDto>()
                .ForMember(dest => dest.SubmittedByName, opt => opt.MapFrom(src =>
                    src.SubmittedBy != null ? $"{src.SubmittedBy.FirstName} {src.SubmittedBy.LastName}" : string.Empty))
                .ForMember(dest => dest.AssignedTrackManagerName, opt => opt.MapFrom(src =>
                    src.AssignedTrackManager != null && src.AssignedTrackManager.User != null
                        ? $"{src.AssignedTrackManager.User.FirstName} {src.AssignedTrackManager.User.LastName}"
                        : string.Empty));

            CreateMap<CreateResearchDto, Research>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SubmissionDate, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.Status, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.SubmittedById, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.SubmittedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Files, opt => opt.Ignore()) // Handled separately
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.StatusHistory, opt => opt.Ignore())
                //.ForMember(dest => dest.TrackHistories, opt => opt.Ignore())
                .ForMember(dest => dest.Authors, opt => opt.Ignore()) // Handled separately
                .ForMember(dest => dest.AssignedTrackManager, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTrackManagerId, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.ReviewDeadline, opt => opt.Ignore())
                .ForMember(dest => dest.DecisionDate, opt => opt.Ignore())
                .ForMember(dest => dest.RejectionReason, opt => opt.Ignore());

            CreateMap<UpdateResearchDto, Research>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SubmissionDate, opt => opt.Ignore())
                .ForMember(dest => dest.SubmittedById, opt => opt.Ignore())
                .ForMember(dest => dest.SubmittedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Files, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.StatusHistory, opt => opt.Ignore())
                //.ForMember(dest => dest.TrackHistories, opt => opt.Ignore())
                .ForMember(dest => dest.Authors, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTrackManager, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTrackManagerId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewDeadline, opt => opt.Ignore())
                .ForMember(dest => dest.DecisionDate, opt => opt.Ignore())
                .ForMember(dest => dest.RejectionReason, opt => opt.Ignore());

            // Reverse Mappings for editing
            CreateMap<Research, CreateResearchDto>()
                .ForMember(dest => dest.Authors, opt => opt.MapFrom(src => src.Authors
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.Order)))
                .ForMember(dest => dest.Files, opt => opt.Ignore());

            // Research Author Mappings
            CreateMap<ResearchAuthor, ResearchAuthorDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.FirstNameEn) && !string.IsNullOrEmpty(src.LastNameEn)
                        ? $"{src.FirstNameEn} {src.LastNameEn}" : null));

            CreateMap<CreateResearchAuthorDto, ResearchAuthor>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ResearchId, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.Research, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            // Reverse Mapping for authors
            CreateMap<ResearchAuthor, CreateResearchAuthorDto>();

            CreateMap<UpdateResearchAuthorDto, ResearchAuthor>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ResearchId, opt => opt.Ignore())
                .ForMember(dest => dest.Research, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            // Review Mappings
            CreateMap<Review, ReviewDto>()
                .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? $"{src.Reviewer.FirstName} {src.Reviewer.LastName}" : string.Empty))
                .ForMember(dest => dest.ResearchTitle, opt => opt.MapFrom(src =>
                    src.Research != null ? src.Research.Title : string.Empty))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.OverallScore));

            CreateMap<CreateReviewDto, Review>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewerId, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.AssignedDate, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.Deadline, opt => opt.MapFrom(src => src.Deadline ?? DateTime.UtcNow.AddDays(21)))
                .ForMember(dest => dest.IsCompleted, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())
                .ForMember(dest => dest.RequiresReReview, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set manually in handler
                .ForMember(dest => dest.Research, opt => opt.Ignore())
                .ForMember(dest => dest.Reviewer, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewFiles, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            CreateMap<UpdateReviewDto, Review>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewerId, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Deadline, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Research, opt => opt.Ignore())
                .ForMember(dest => dest.Reviewer, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewFiles, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            // Research File Mappings
            CreateMap<ResearchFile, ResearchFileDto>();

            CreateMap<UploadFileDto, ResearchFile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ResearchId, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.FileName, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.OriginalFileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.FilePath, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileContent.Length))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Research, opt => opt.Ignore())
                .ForMember(dest => dest.Review, opt => opt.Ignore());

            // TrackManager Mappings
            CreateMap<TrackManager, TrackManagerDto>()
                .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src =>
                    src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : string.Empty))
                .ForMember(dest => dest.ManagerEmail, opt => opt.MapFrom(src =>
                    src.User != null ? src.User.Email : string.Empty));

            CreateMap<CreateTrackManagerDto, TrackManager>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ManagedResearches, opt => opt.Ignore())
                .ForMember(dest => dest.TrackReviewers, opt => opt.Ignore());

            CreateMap<UpdateTrackManagerDto, TrackManager>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ManagedResearches, opt => opt.Ignore())
                .ForMember(dest => dest.TrackReviewers, opt => opt.Ignore());

            // TrackReviewer Mappings
            CreateMap<TrackReviewer, TrackReviewerDto>()
                .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? $"{src.Reviewer.FirstName} {src.Reviewer.LastName}" : string.Empty))
                .ForMember(dest => dest.ReviewerEmail, opt => opt.MapFrom(src =>
                    src.Reviewer != null ? src.Reviewer.Email : string.Empty));

            CreateMap<CreateTrackReviewerDto, TrackReviewer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TrackManager, opt => opt.Ignore())
                .ForMember(dest => dest.Reviewer, opt => opt.Ignore());

            CreateMap<UpdateTrackReviewerDto, TrackReviewer>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TrackManager, opt => opt.Ignore())
                .ForMember(dest => dest.Reviewer, opt => opt.Ignore());
        }
    }
}

////////////using AutoMapper;
////////////using ResearchManagement.Application.DTOs;
////////////using ResearchManagement.Domain.Entities;
////////////using ResearchManagement.Domain.Enums;
////////////using System;
////////////using System.Linq;

////////////namespace ResearchManagement.Application.Mappings
////////////{
////////////    public class ApplicationMappingProfile : Profile
////////////    {
////////////        public ApplicationMappingProfile()
////////////        {
////////////            // User Mappings
////////////            CreateMap<User, UserDto>()
////////////                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
////////////                .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src =>
////////////                    !string.IsNullOrEmpty(src.FirstNameEn) && !string.IsNullOrEmpty(src.LastNameEn)
////////////                        ? $"{src.FirstNameEn} {src.LastNameEn}" : null));

////////////            CreateMap<CreateUserDto, User>()
////////////                .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
////////////                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

////////////            CreateMap<UpdateUserDto, User>()
////////////                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

////////////            // Research Mappings
////////////            CreateMap<Research, ResearchDto>()
////////////                .ForMember(dest => dest.SubmittedByName, opt => opt.MapFrom(src =>
////////////                    $"{src.SubmittedBy.FirstName} {src.SubmittedBy.LastName}"));




////////////            CreateMap<CreateResearchDto, Research>()
////////////                .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////                .ForMember(dest => dest.SubmissionDate, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.ResearchStatus.Submitted))
////////////                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.SubmittedById, opt => opt.Ignore())
////////////                .ForMember(dest => dest.SubmittedBy, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Files, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
////////////                .ForMember(dest => dest.StatusHistory, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Authors, opt => opt.Ignore());



////////////            CreateMap<UpdateResearchDto, Research>()
////////////                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.SubmissionDate, opt => opt.Ignore())
////////////                .ForMember(dest => dest.SubmittedById, opt => opt.Ignore())
////////////                .ForMember(dest => dest.SubmittedBy, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Files, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
////////////                .ForMember(dest => dest.StatusHistory, opt => opt.Ignore());

////////////            // تحديث: إضافة Reverse Mapping للتعديل
////////////            CreateMap<Research, CreateResearchDto>()
////////////                .ForMember(dest => dest.Authors, opt => opt.MapFrom(src => src.Authors
////////////                    .Where(a => !a.IsDeleted)
////////////                    .OrderBy(a => a.Order)))
////////////                .ForMember(dest => dest.Files, opt => opt.Ignore());

////////////            // Research Author Mappings
////////////            CreateMap<ResearchAuthor, ResearchAuthorDto>()
////////////                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
////////////                .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src =>
////////////                    !string.IsNullOrEmpty(src.FirstNameEn) && !string.IsNullOrEmpty(src.LastNameEn)
////////////                        ? $"{src.FirstNameEn} {src.LastNameEn}" : null));

////////////            //CreateMap<CreateResearchAuthorDto, ResearchAuthor>()
////////////            //    .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////            //    .ForMember(dest => dest.ResearchId, opt => opt.Ignore())
////////////            //    .ForMember(dest => dest.Research, opt => opt.Ignore())
////////////            //    .ForMember(dest => dest.User, opt => opt.Ignore())
////////////            //    .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////            //    .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
////////////            CreateMap<CreateResearchAuthorDto, ResearchAuthor>()
////////////            .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////            .ForMember(dest => dest.ResearchId, opt => opt.Ignore())
////////////            .ForMember(dest => dest.Research, opt => opt.Ignore())
////////////            .ForMember(dest => dest.User, opt => opt.Ignore())
////////////            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // سيتم تعيينها في Handler
////////////            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // سيتم تعيينها في Handler
////////////            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
////////////            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
////////////            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

////////////            // تحديث: إضافة Reverse Mapping للمؤلفين
////////////            CreateMap<ResearchAuthor, CreateResearchAuthorDto>();

////////////            CreateMap<UpdateResearchAuthorDto, ResearchAuthor>()
////////////                .ForMember(dest => dest.ResearchId, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Research, opt => opt.Ignore())
////////////                .ForMember(dest => dest.User, opt => opt.Ignore())
////////////                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

////////////            // Review Mappings
////////////            CreateMap<Review, ReviewDto>()
////////////                .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src =>
////////////                    $"{src.Reviewer.FirstName} {src.Reviewer.LastName}"))
////////////                .ForMember(dest => dest.ResearchTitle, opt => opt.MapFrom(src => src.Research.Title))
////////////                .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.OverallScore));

////////////            CreateMap<CreateReviewDto, Review>()
////////////                .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////                .ForMember(dest => dest.ReviewerId, opt => opt.Ignore())
////////////                .ForMember(dest => dest.AssignedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.Deadline, opt => opt.MapFrom(src => src.Deadline ?? DateTime.UtcNow.AddDays(21)))
////////////                .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => false))
////////////                .ForMember(dest => dest.RequiresReReview, opt => opt.MapFrom(src => false))
////////////                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.Research, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Reviewer, opt => opt.Ignore())
////////////                .ForMember(dest => dest.ReviewFiles, opt => opt.Ignore());

////////////            CreateMap<UpdateReviewDto, Review>()
////////////                .ForMember(dest => dest.ReviewerId, opt => opt.Ignore())
////////////                .ForMember(dest => dest.AssignedDate, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Deadline, opt => opt.Ignore())
////////////                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.Research, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Reviewer, opt => opt.Ignore())
////////////                .ForMember(dest => dest.ReviewFiles, opt => opt.Ignore());

////////////            // Research File Mappings
////////////            CreateMap<ResearchFile, ResearchFileDto>();

////////////            CreateMap<UploadFileDto, ResearchFile>()
////////////                .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////                .ForMember(d => d.ResearchId, o => o.Ignore())
////////////                .ForMember(dest => dest.FileName, opt => opt.Ignore())
////////////                .ForMember(dest => dest.OriginalFileName, opt => opt.MapFrom(src => src.FileName))
////////////                .ForMember(dest => dest.FilePath, opt => opt.Ignore())
////////////                .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.FileContent.Length))
////////////                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1))
////////////                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
////////////                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.Research, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Review, opt => opt.Ignore());

////////////            // TrackManager Mappings
////////////            CreateMap<TrackManager, TrackManagerDto>()
////////////                .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => 
////////////                    $"{src.User.FirstName} {src.User.LastName}"))
////////////                .ForMember(dest => dest.ManagerEmail, opt => opt.MapFrom(src => src.User.Email));

////////////            CreateMap<CreateTrackManagerDto, TrackManager>()
////////////                .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.User, opt => opt.Ignore())
////////////                .ForMember(dest => dest.ManagedResearches, opt => opt.Ignore())
////////////                .ForMember(dest => dest.TrackReviewers, opt => opt.Ignore());

////////////            CreateMap<UpdateTrackManagerDto, TrackManager>()
////////////                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.User, opt => opt.Ignore())
////////////                .ForMember(dest => dest.ManagedResearches, opt => opt.Ignore())
////////////                .ForMember(dest => dest.TrackReviewers, opt => opt.Ignore());

////////////            // TrackReviewer Mappings
////////////            CreateMap<TrackReviewer, TrackReviewerDto>()
////////////                .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src => 
////////////                    $"{src.Reviewer.FirstName} {src.Reviewer.LastName}"))
////////////                .ForMember(dest => dest.ReviewerEmail, opt => opt.MapFrom(src => src.Reviewer.Email));

////////////            CreateMap<CreateTrackReviewerDto, TrackReviewer>()
////////////                .ForMember(dest => dest.Id, opt => opt.Ignore())
////////////                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.TrackManager, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Reviewer, opt => opt.Ignore());

////////////            CreateMap<UpdateTrackReviewerDto, TrackReviewer>()
////////////                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
////////////                .ForMember(dest => dest.TrackManager, opt => opt.Ignore())
////////////                .ForMember(dest => dest.Reviewer, opt => opt.Ignore());
////////////        }
////////////    }
////////////}