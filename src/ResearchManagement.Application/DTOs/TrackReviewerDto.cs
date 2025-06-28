using System;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Application.DTOs
{
    public class TrackReviewerDto
    {
        public int Id { get; set; }
        public ResearchTrack Track { get; set; }
        public bool IsActive { get; set; }
        public int TrackManagerId { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Additional properties for UI
        public string TrackDisplayName => Track switch
        {
            //ResearchTrack.ComputerScience => "علوم الحاسب",
            //ResearchTrack.InformationSystems => "نظم المعلومات",
            ResearchTrack.SoftwareEngineering => "هندسة البرمجيات",
            ResearchTrack.ArtificialIntelligence => "الذكاء الاصطناعي",
            //ResearchTrack.Cybersecurity => "الأمن السيبراني",
            ResearchTrack.DataScience => "علوم البيانات",
            //ResearchTrack.NetworksAndCommunications => "الشبكات والاتصالات",
            //ResearchTrack.HumanComputerInteraction => "تفاعل الإنسان والحاسوب",
            //ResearchTrack.Other => "أخرى",
            _ => "غير محدد"
        };
        
        public string StatusDisplayName => IsActive ? "نشط" : "غير نشط";
    }

    public class CreateTrackReviewerDto
    {
        public ResearchTrack Track { get; set; }
        public int TrackManagerId { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTrackReviewerDto
    {
        public int Id { get; set; }
        public ResearchTrack Track { get; set; }
        public int TrackManagerId { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}