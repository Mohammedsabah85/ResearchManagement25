using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ResearchManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Domain.Entities
{
    public class ResearchTrackHistory
    {
        [Key]
        public int Id { get; set; }
        public int ResearchId { get; set; }
        public ResearchTrack? FromTrack { get; set; }
        public ResearchTrack? ToTrack { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual Research Research { get; set; } = null!;
        public virtual User ChangedByUser { get; set; } = null!;
    }
}