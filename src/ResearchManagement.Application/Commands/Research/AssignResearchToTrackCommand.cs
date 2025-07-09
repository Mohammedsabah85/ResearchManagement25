using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Application/Commands/Research/AssignResearchToTrackCommand.cs
using MediatR;
using Microsoft.Extensions.Logging;
using ResearchManagement.Application.Commands.Research;
using ResearchManagement.Domain.Enums;

using MediatR;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Application.Commands.Research
{
    public class AssignResearchToTrackCommand : IRequest<bool>
    {
        public int ResearchId { get; set; }
        public ResearchTrack NewTrack { get; set; }
        public string? Notes { get; set; }
        public string UserId { get; set; } = string.Empty;

        public AssignResearchToTrackCommand() { }

        public AssignResearchToTrackCommand(int researchId, ResearchTrack newTrack, string userId, string? notes = null)
        {
            ResearchId = researchId;
            NewTrack = newTrack;
            UserId = userId;
            Notes = notes;
        }
    }
}

