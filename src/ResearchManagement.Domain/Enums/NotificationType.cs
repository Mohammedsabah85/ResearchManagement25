using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Domain.Enums
{
    public enum NotificationType
    {
        //NewResearchAssigned = 1,
        //ReviewDeadlineApproaching = 2,
        //ReviewCompleted = 3,
        //ResearchStatusChanged = 4,
        //NewReviewerAssigned = 5,
        //TrackChanged = 6,
        //SystemAlert = 7
        [Display(Name = "تأكيد التسجيل")]
        RegistrationConfirmation = 1,

        [Display(Name = "تأكيد استلام البحث")]
        ResearchSubmissionConfirmation = 2,

        [Display(Name = "تحديث حالة البحث")]
        ResearchStatusUpdate = 3,

        [Display(Name = "تكليف مراجعة")]
        ReviewAssignment = 4,

        [Display(Name = "وصول تعليقات المقيمين")]
        ReviewCommentsReceived = 5,

        [Display(Name = "طلب تعديلات")]
        RevisionRequest = 6,

        [Display(Name = "القرار النهائي")]
        FinalDecision = 7,

        [Display(Name = "تذكير موعد")]
        DeadlineReminder = 8,

        [Display(Name = "تقرير دوري")]
        PeriodicReport = 9,

        [Display(Name = "إشعار عام")]
        GeneralNotification = 10
    }
}
