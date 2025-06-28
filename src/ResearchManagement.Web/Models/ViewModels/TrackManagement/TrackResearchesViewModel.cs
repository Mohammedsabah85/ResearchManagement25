using ResearchManagement.Domain.Entities;
using System.Collections.Generic;

namespace ResearchManagement.Web.Models.ViewModels
{
    public class TrackResearchesViewModel
    {
        public int TrackId { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public List<Research> Researches { get; set; } = new();
    }
}