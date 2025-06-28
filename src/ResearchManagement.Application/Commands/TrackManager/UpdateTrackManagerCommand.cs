using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Interfaces;

namespace ResearchManagement.Application.Commands.TrackManager
{
    public class UpdateTrackManagerCommand : IRequest<bool>
    {
        public UpdateTrackManagerDto TrackManager { get; set; } = new();
    }

    public class UpdateTrackManagerCommandHandler : IRequestHandler<UpdateTrackManagerCommand, bool>
    {
        private readonly ITrackManagerRepository _trackManagerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateTrackManagerCommandHandler(
            ITrackManagerRepository trackManagerRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _trackManagerRepository = trackManagerRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> Handle(UpdateTrackManagerCommand request, CancellationToken cancellationToken)
        {
            var trackManager = await _trackManagerRepository.GetByIdAsync(request.TrackManager.Id);
            if (trackManager == null)
                return false;

            trackManager.Track = request.TrackManager.Track;
            trackManager.TrackDescription = request.TrackManager.TrackDescription;
            trackManager.UserId = request.TrackManager.UserId;
            trackManager.IsActive = request.TrackManager.IsActive;
            trackManager.UpdatedAt = DateTime.UtcNow;

            await _trackManagerRepository.UpdateAsync(trackManager);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return true;
        }
    }
}