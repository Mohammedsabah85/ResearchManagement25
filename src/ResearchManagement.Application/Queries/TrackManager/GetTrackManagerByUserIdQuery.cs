using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;

namespace ResearchManagement.Application.Queries.TrackManager
{
    public class GetTrackManagerByUserIdQuery : IRequest<TrackManagerDto?>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetTrackManagerByUserIdQueryHandler : IRequestHandler<GetTrackManagerByUserIdQuery, TrackManagerDto?>
    {
        private readonly ITrackManagerRepository _trackManagerRepository;
        private readonly IMapper _mapper;

        public GetTrackManagerByUserIdQueryHandler(
            ITrackManagerRepository trackManagerRepository,
            IMapper mapper)
        {
            _trackManagerRepository = trackManagerRepository;
            _mapper = mapper;
        }

        public async Task<TrackManagerDto?> Handle(GetTrackManagerByUserIdQuery request, CancellationToken cancellationToken)
        {
            var trackManager = await _trackManagerRepository.GetByUserIdAsync(request.UserId);
            return trackManager != null ? _mapper.Map<TrackManagerDto>(trackManager) : null;
        }
    }
}