using ResearchManagement.Domain.Entities;
using System.Collections.Generic;

namespace ResearchManagement.Web.Models.ViewModels
{
    public class TrackResearchDetailsViewModel
    {
        public Research Research { get; set; } = null!;
        public List<Review> Reviews { get; set; } = new();
    }
}