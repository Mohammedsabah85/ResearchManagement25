using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;

namespace ResearchManagement.Application.Queries.TrackManager
{
    public class GetAllTrackManagersQuery : IRequest<IEnumerable<TrackManagerDto>>
    {
        public bool ActiveOnly { get; set; } = true;
    }

    public class GetAllTrackManagersQueryHandler : IRequestHandler<GetAllTrackManagersQuery, IEnumerable<TrackManagerDto>>
    {
        private readonly ITrackManagerRepository _trackManagerRepository;
        private readonly IMapper _mapper;

        public GetAllTrackManagersQueryHandler(
            ITrackManagerRepository trackManagerRepository,
            IMapper mapper)
        {
            _trackManagerRepository = trackManagerRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TrackManagerDto>> Handle(GetAllTrackManagersQuery request, CancellationToken cancellationToken)
        {
            var trackManagers = request.ActiveOnly 
                ? await _trackManagerRepository.GetAllActiveAsync()
                : await _trackManagerRepository.GetAllAsync();
                
            return trackManagers.Select(tm => _mapper.Map<TrackManagerDto>(tm));
        }
    }
}