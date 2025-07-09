using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading.Tasks;

namespace ResearchManagement.Application.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// إرسال إشعار لمدير المسار عند تعيين بحث جديد
        /// </summary>
        Task NotifyTrackManagerAsync(string userId, int researchId, string researchTitle);

        /// <summary>
        /// إرسال إشعار للباحث عند تغيير مسار البحث
        /// </summary>
        Task NotifyTrackChangeAsync(
            string userId,
            int researchId,
            string researchTitle,
            string oldTrackName,
            string newTrackName,
            string? notes = null);

        /// <summary>
        /// إرسال إشعار لمراجع عند تعيين بحث للمراجعة
        /// </summary>
        Task NotifyReviewerAsync(string reviewerId, int researchId, string researchTitle);

        /// <summary>
        /// إرسال إشعار عام للمستخدم
        /// </summary>
        Task NotifyUserAsync(string userId, string title, string message);

        /// <summary>
        /// إرسال إشعار لعدة مستخدمين
        /// </summary>
        Task NotifyUsersAsync(string[] userIds, string title, string message);
    }
}