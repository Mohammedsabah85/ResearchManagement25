using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ResearchManagement.Application.Interfaces;

namespace ResearchManagement.Application.Commands.TrackManager
{
    public class DeleteTrackManagerCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }

    public class DeleteTrackManagerCommandHandler : IRequestHandler<DeleteTrackManagerCommand, bool>
    {
        private readonly ITrackManagerRepository _trackManagerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteTrackManagerCommandHandler(
            ITrackManagerRepository trackManagerRepository,
            IUnitOfWork unitOfWork)
        {
            _trackManagerRepository = trackManagerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteTrackManagerCommand request, CancellationToken cancellationToken)
        {
            var trackManager = await _trackManagerRepository.GetByIdAsync(request.Id);
            if (trackManager == null)
                return false;

            await _trackManagerRepository.DeleteAsync(request.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return true;
        }
    }
}