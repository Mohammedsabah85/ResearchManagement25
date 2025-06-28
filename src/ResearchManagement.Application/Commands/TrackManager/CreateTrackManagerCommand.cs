using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Commands.TrackManager
{
    public class CreateTrackManagerCommand : IRequest<int>
    {
        public CreateTrackManagerDto TrackManager { get; set; } = new();
    }

    public class CreateTrackManagerCommandHandler : IRequestHandler<CreateTrackManagerCommand, int>
    {
        private readonly ITrackManagerRepository _trackManagerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateTrackManagerCommandHandler(
            ITrackManagerRepository trackManagerRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _trackManagerRepository = trackManagerRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<int> Handle(CreateTrackManagerCommand request, CancellationToken cancellationToken)
        {
            var trackManager = _mapper.Map<Domain.Entities.TrackManager>(request.TrackManager);
            
            await _trackManagerRepository.AddAsync(trackManager);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return trackManager.Id;
        }
    }
}