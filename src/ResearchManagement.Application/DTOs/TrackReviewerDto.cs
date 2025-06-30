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
            ResearchTrack.EnergyAndRenewableEnergy => "Energy and Renewable Energy",
            ResearchTrack.ElectricalAndElectronicsEngineering => "Electromechanical System, and Mechatronics Engineering",
            ResearchTrack.MaterialScienceAndMechanicalEngineering => "Material Science & Mechanical Engineering",
            ResearchTrack.NavigationGuidanceSystemsComputerAndCommunicationEngineering => "Navigation & Guidance Systems, Computer and Communication Engineering",
            ResearchTrack.ElectromechanicalSystemAndMechanicsEngineering => "Electrical & Electronics Engineering",
            ResearchTrack.AvionicsSystemsAircraftAndUnmannedAircraftEngineering => "Avionics Systems, Aircraft and Unmanned Aircraft Engineering",
            ResearchTrack.EarthNaturalResourcesGasAndPetroleumSystemsEquipment => "Earth's Natural Resources, Gas and Petroleum Systems & Equipment",
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