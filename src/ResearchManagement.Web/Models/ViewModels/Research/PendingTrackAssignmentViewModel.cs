// Models/ViewModels/Research/PendingTrackAssignmentViewModel.cs
using ResearchManagement.Application.DTOs;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Web.Models.ViewModels.Research
{
    public class PendingTrackAssignmentViewModel
    {
        public List<ResearchTrackAssignmentDto> PendingResearches { get; set; } = new();
        public int TotalPending => PendingResearches.Count;
        public Dictionary<ResearchTrack?, int> TrackDistribution => PendingResearches
            .GroupBy(r => r.CurrentTrack)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public class ResearchTrackAssignmentDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? TitleEn { get; set; }
        public string AbstractAr { get; set; } = string.Empty;
        public string SubmittedByName { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public ResearchTrack? CurrentTrack { get; set; }
        public ResearchTrack SuggestedTrack { get; set; }
        public List<ResearchAuthorDto> Authors { get; set; } = new();
        public bool IsUrgent => (DateTime.UtcNow - SubmissionDate).Days > 7;
        public int DaysSinceSubmission => (DateTime.UtcNow - SubmissionDate).Days;

        public string CurrentTrackDisplayName => GetTrackDisplayName(CurrentTrack);
        public string SuggestedTrackDisplayName => GetTrackDisplayName(SuggestedTrack);

        private static string GetTrackDisplayName(ResearchTrack? track)
        {
            if (!track.HasValue)
                return "غير محدد"; // أو "Not Assigned"

            return track.Value switch
            {
                ResearchTrack.EnergyAndRenewableEnergy => "Energy and Renewable Energy",
                ResearchTrack.ElectricalAndElectronicsEngineering => "Electromechanical System, and Mechatronics Engineering",
                ResearchTrack.MaterialScienceAndMechanicalEngineering => "Material Science & Mechanical Engineering",
                ResearchTrack.NavigationGuidanceSystemsComputerAndCommunicationEngineering => "Navigation & Guidance Systems, Computer and Communication Engineering",
                ResearchTrack.ElectromechanicalSystemAndMechanicsEngineering => "Electrical & Electronics Engineering",
                ResearchTrack.AvionicsSystemsAircraftAndUnmannedAircraftEngineering => "Avionics Systems, Aircraft and Unmanned Aircraft Engineering",
                ResearchTrack.EarthNaturalResourcesGasAndPetroleumSystemsEquipment => "Earth's Natural Resources, Gas and Petroleum Systems & Equipment",
                _ => track.Value.ToString()
            };
        }

        public class BulkTrackAssignmentDto
        {
            public int ResearchId { get; set; }
            public ResearchTrack Track { get; set; }
            public string? Notes { get; set; }
        }
    }
}