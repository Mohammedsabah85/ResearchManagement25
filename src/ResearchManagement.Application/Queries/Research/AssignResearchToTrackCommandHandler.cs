using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Microsoft.Extensions.Logging;
using ResearchManagement.Application.Commands.Research;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Domain.Enums;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Handlers.Research
{
    public class AssignResearchToTrackCommandHandler : IRequestHandler<AssignResearchToTrackCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AssignResearchToTrackCommandHandler> _logger;

        public AssignResearchToTrackCommandHandler(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            ILogger<AssignResearchToTrackCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<bool> Handle(AssignResearchToTrackCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // جلب البحث مع معلومات المقدم
                var research = await _unitOfWork.Research.GetByIdWithDetailsAsync(request.ResearchId);

                if (research == null)
                {
                    _logger.LogWarning("لم يتم العثور على البحث {ResearchId}", request.ResearchId);
                    return false;
                }

                var oldTrack = research.Track;
                var oldTrackDisplayName = GetTrackDisplayName(oldTrack);
                var newTrackDisplayName = GetTrackDisplayName(request.NewTrack);

                await _unitOfWork.BeginTransactionAsync();

                // تحديث المسار
                research.Track = request.NewTrack;
                research.UpdatedAt = DateTime.UtcNow;
                research.UpdatedBy = request.UserId;

                // البحث عن مدير المسار المناسب
                var trackManager = await _unitOfWork.TrackManagers
                    .FirstOrDefaultAsync(tm => tm.Track == request.NewTrack && tm.IsActive);

                if (trackManager != null)
                {
                    research.AssignedTrackManagerId = trackManager.Id;
                    research.Status = ResearchStatus.AssignedForReview;

                    // إرسال إشعار لمدير المسار الجديد
                    await _notificationService.NotifyTrackManagerAsync(
                        trackManager.UserId,
                        research.Id,
                        research.Title);
                }
                else
                {
                    _logger.LogWarning("لم يتم العثور على مدير مسار نشط للمسار {Track}", request.NewTrack);
                }

                // تحديث البحث
                await _unitOfWork.Research.UpdateAsync(research);

                // إضافة تاريخ تغيير المسار
                var trackHistory = new ResearchTrackHistory
                {
                    ResearchId = request.ResearchId,
                    FromTrack = oldTrack,
                    ToTrack = request.NewTrack,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = request.UserId,
                    Notes = request.Notes,
                    IsActive = true
                };

                await _unitOfWork.ResearchTrackHistories.AddAsync(trackHistory);

                // إرسال إشعار للباحث حول تغيير المسار
                if (!string.IsNullOrEmpty(research.SubmittedById))
                {
                    await _notificationService.NotifyTrackChangeAsync(
                        research.SubmittedById,
                        research.Id,
                        research.Title,
                        oldTrackDisplayName,
                        newTrackDisplayName,
                        request.Notes);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("تم تحديد مسار البحث {ResearchId} من {OldTrack} إلى {NewTrack} بواسطة {UserId}",
                    request.ResearchId, oldTrack, request.NewTrack, request.UserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديد مسار البحث {ResearchId}", request.ResearchId);

                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "فشل في إلغاء المعاملة");
                }

                return false;
            }
        }

        private static string GetTrackDisplayName(ResearchTrack track) => track switch
        {
            ResearchTrack.EnergyAndRenewableEnergy => "Energy and Renewable Energy",
            ResearchTrack.ElectricalAndElectronicsEngineering => "Electromechanical System, and Mechatronics Engineering",
            ResearchTrack.MaterialScienceAndMechanicalEngineering => "Material Science & Mechanical Engineering",
            ResearchTrack.NavigationGuidanceSystemsComputerAndCommunicationEngineering => "Navigation & Guidance Systems, Computer and Communication Engineering",
            ResearchTrack.ElectromechanicalSystemAndMechanicsEngineering => "Electrical & Electronics Engineering",
            ResearchTrack.AvionicsSystemsAircraftAndUnmannedAircraftEngineering => "Avionics Systems, Aircraft and Unmanned Aircraft Engineering",
            ResearchTrack.EarthNaturalResourcesGasAndPetroleumSystemsEquipment => "Earth's Natural Resources, Gas and Petroleum Systems & Equipment",
            _ => track.ToString()
        };
    }
}