using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ResearchManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Domain.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
