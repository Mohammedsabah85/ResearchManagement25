using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;

namespace ResearchManagement.Application.Queries.TrackManager
{
    public class GetTrackManagerByIdQuery : IRequest<TrackManagerDto?>
    {
        public int Id { get; set; }
    }

    public class GetTrackManagerByIdQueryHandler : IRequestHandler<GetTrackManagerByIdQuery, TrackManagerDto?>
    {
        private readonly ITrackManagerRepository _trackManagerRepository;
        private readonly IMapper _mapper;

        public GetTrackManagerByIdQueryHandler(
            ITrackManagerRepository trackManagerRepository,
            IMapper mapper)
        {
            _trackManagerRepository = trackManagerRepository;
            _mapper = mapper;
        }

        public async Task<TrackManagerDto?> Handle(GetTrackManagerByIdQuery request, CancellationToken cancellationToken)
        {
            var trackManager = await _trackManagerRepository.GetByIdWithDetailsAsync(request.Id);
            return trackManager != null ? _mapper.Map<TrackManagerDto>(trackManager) : null;
        }
    }
}