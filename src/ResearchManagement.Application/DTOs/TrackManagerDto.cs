using System;
using System.Collections.Generic;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Application.DTOs
{
    public class TrackManagerDto
    {
        public int Id { get; set; }
        public ResearchTrack Track { get; set; }
        public string TrackDescription { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Additional properties for UI
        public string TrackDisplayName => Track switch
        {
            ResearchTrack.ComputerScience => "علوم الحاسب",
            ResearchTrack.InformationSystems => "نظم المعلومات",
            ResearchTrack.SoftwareEngineering => "هندسة البرمجيات",
            ResearchTrack.ArtificialIntelligence => "الذكاء الاصطناعي",
            ResearchTrack.Cybersecurity => "الأمن السيبراني",
            ResearchTrack.DataScience => "علوم البيانات",
            ResearchTrack.NetworksAndCommunications => "الشبكات والاتصالات",
            ResearchTrack.HumanComputerInteraction => "تفاعل الإنسان والحاسوب",
            ResearchTrack.Other => "أخرى",
            _ => "غير محدد"
        };
        
        public string StatusDisplayName => IsActive ? "نشط" : "غير نشط";
    }

    public class CreateTrackManagerDto
    {
        public ResearchTrack Track { get; set; }
        public string? TrackDescription { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTrackManagerDto
    {
        public int Id { get; set; }
        public ResearchTrack Track { get; set; }
        public string? TrackDescription { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class TrackManagerSummaryDto
    {
        public int Id { get; set; }
        public ResearchTrack Track { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;
        public int ManagedResearchesCount { get; set; }
        public int ReviewersCount { get; set; }
        public bool IsActive { get; set; }
        
        public string TrackDisplayName => Track switch
        {
            ResearchTrack.ComputerScience => "علوم الحاسب",
            ResearchTrack.InformationSystems => "نظم المعلومات",
            ResearchTrack.SoftwareEngineering => "هندسة البرمجيات",
            ResearchTrack.ArtificialIntelligence => "الذكاء الاصطناعي",
            ResearchTrack.Cybersecurity => "الأمن السيبراني",
            ResearchTrack.DataScience => "علوم البيانات",
            ResearchTrack.NetworksAndCommunications => "الشبكات والاتصالات",
            ResearchTrack.HumanComputerInteraction => "تفاعل الإنسان والحاسوب",
            ResearchTrack.Other => "أخرى",
            _ => "غير محدد"
        };
    }
}