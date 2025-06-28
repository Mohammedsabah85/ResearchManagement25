using ResearchManagement.Domain.Entities;
using System.Collections.Generic;

namespace ResearchManagement.Web.Models.ViewModels
{
    public class TrackResearchDetailsViewModel
    {
        public Domain.Entities.Research Research { get; set; } = null!;
        public List<Domain.Entities.Review> Reviews { get; set; } = new();
    }
}