
using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Web.Models.ViewModels.Profile
    {
        // ViewModel لتعديل الملف الشخصي
        public class EditProfileViewModel
        {
            [Required(ErrorMessage = "الاسم الأول مطلوب")]
            [StringLength(100, ErrorMessage = "الاسم الأول يجب أن يكون أقل من 100 حرف")]
            [Display(Name = "الاسم الأول")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "اسم العائلة مطلوب")]
            [StringLength(100, ErrorMessage = "اسم العائلة يجب أن يكون أقل من 100 حرف")]
            [Display(Name = "اسم العائلة")]
            public string LastName { get; set; } = string.Empty;

            [StringLength(100, ErrorMessage = "الاسم الأول الإنجليزي يجب أن يكون أقل من 100 حرف")]
            [Display(Name = "الاسم الأول (إنجليزي)")]
            public string? FirstNameEn { get; set; }

            [StringLength(100, ErrorMessage = "اسم العائلة الإنجليزي يجب أن يكون أقل من 100 حرف")]
            [Display(Name = "اسم العائلة (إنجليزي)")]
            public string? LastNameEn { get; set; }

            [StringLength(200, ErrorMessage = "المؤسسة يجب أن تكون أقل من 200 حرف")]
            [Display(Name = "المؤسسة")]
            public string? Institution { get; set; }

            [StringLength(100, ErrorMessage = "الدرجة العلمية يجب أن تكون أقل من 100 حرف")]
            [Display(Name = "الدرجة العلمية")]
            public string? AcademicDegree { get; set; }

            [StringLength(50, ErrorMessage = "معرف ORCID يجب أن يكون أقل من 50 حرف")]
            [Display(Name = "معرف ORCID")]
            [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$", ErrorMessage = "معرف ORCID غير صحيح. الصيغة المطلوبة: 0000-0000-0000-0000")]
            public string? OrcidId { get; set; }

            [Display(Name = "نبذة شخصية")]
            [StringLength(1000, ErrorMessage = "النبذة الشخصية يجب أن تكون أقل من 1000 حرف")]
            public string? Bio { get; set; }

            [Display(Name = "التخصص")]
            [StringLength(200, ErrorMessage = "التخصص يجب أن يكون أقل من 200 حرف")]
            public string? Specialization { get; set; }

            [Display(Name = "الموقع الشخصي")]
            [Url(ErrorMessage = "رابط الموقع الشخصي غير صحيح")]
            public string? Website { get; set; }

            [Display(Name = "LinkedIn")]
            [Url(ErrorMessage = "رابط LinkedIn غير صحيح")]
            public string? LinkedInProfile { get; set; }

            [Display(Name = "Google Scholar")]
            [Url(ErrorMessage = "رابط Google Scholar غير صحيح")]
            public string? GoogleScholarProfile { get; set; }

            [Display(Name = "ResearchGate")]
            [Url(ErrorMessage = "رابط ResearchGate غير صحيح")]
            public string? ResearchGateProfile { get; set; }

            [Display(Name = "البريد الإلكتروني الحالي")]
            public string CurrentEmail { get; set; } = string.Empty;

            // قائمة الدرجات العلمية
            public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AcademicDegreeOptions => new()
        {
            new() { Value = "", Text = "اختر الدرجة العلمية" },
            new() { Value = "بكالوريوس", Text = "بكالوريوس" },
            new() { Value = "ماجستير", Text = "ماجستير" },
            new() { Value = "دكتوراه", Text = "دكتوراه" },
            new() { Value = "أستاذ مساعد", Text = "أستاذ مساعد" },
            new() { Value = "أستاذ مشارك", Text = "أستاذ مشارك" },
            new() { Value = "أستاذ", Text = "أستاذ" },
            new() { Value = "باحث", Text = "باحث" },
            new() { Value = "أخرى", Text = "أخرى" }
        };

            // خصائص محسوبة
            public string FullName => $"{FirstName} {LastName}";
            public string? FullNameEn => !string.IsNullOrEmpty(FirstNameEn) && !string.IsNullOrEmpty(LastNameEn)
                ? $"{FirstNameEn} {LastNameEn}"
                : null;
        }

        // ViewModel لتغيير كلمة المرور
        public class ChangePasswordViewModel
        {
            [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
            [DataType(DataType.Password)]
            [Display(Name = "كلمة المرور الحالية")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
            [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون بين {2} و {1} حرف", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "كلمة المرور الجديدة")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{6,}$",
                ErrorMessage = "كلمة المرور يجب أن تحتوي على حرف كبير وحرف صغير ورقم واحد على الأقل")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
            [DataType(DataType.Password)]
            [Display(Name = "تأكيد كلمة المرور الجديدة")]
            [Compare("NewPassword", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "إنهاء جميع الجلسات الأخرى")]
            public bool LogoutOtherSessions { get; set; } = false;
        }

        // ViewModel لقائمة أنشطة المستخدم
        public class UserActivityListViewModel
        {
            public List<ResearchManagement.Web.Models.ViewModels.User.UserActivityViewModel> Activities { get; set; } = new();
            public int CurrentPage { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public int TotalActivities { get; set; }
            public int TotalPages { get; set; }
            public string? FilterType { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }

            // خصائص محسوبة للتنقل
            public bool HasPreviousPage => CurrentPage > 1;
            public bool HasNextPage => CurrentPage < TotalPages;
            public int StartIndex => (CurrentPage - 1) * PageSize + 1;
            public int EndIndex => Math.Min(CurrentPage * PageSize, TotalActivities);

            // فلاتر الأنشطة
            public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ActivityTypeOptions => new()
        {
            new() { Value = "", Text = "جميع الأنشطة" },
            new() { Value = "research_submitted", Text = "تقديم بحث" },
            new() { Value = "research_accepted", Text = "قبول بحث" },
            new() { Value = "research_rejected", Text = "رفض بحث" },
            new() { Value = "review_assigned", Text = "تعيين مراجعة" },
            new() { Value = "review_completed", Text = "إكمال مراجعة" },
            new() { Value = "profile_updated", Text = "تحديث الملف الشخصي" },
            new() { Value = "password_changed", Text = "تغيير كلمة المرور" },
            new() { Value = "login", Text = "تسجيل دخول" }
        };
        }

        // ViewModel لإعدادات الأمان
        public class SecuritySettingsViewModel
        {
            public bool EmailConfirmed { get; set; }
            public bool TwoFactorEnabled { get; set; }
            public DateTime? LastPasswordChange { get; set; }
            public List<LoginAttemptViewModel> LoginAttempts { get; set; } = new();
            public List<ActiveSessionViewModel> ActiveSessions { get; set; } = new();
            public List<SecurityEventViewModel> SecurityEvents { get; set; } = new();

            public string LastPasswordChangeText
            {
                get
                {
                    if (!LastPasswordChange.HasValue)
                        return "لم يتم تغيير كلمة المرور مطلقاً";

                    var timeSpan = DateTime.UtcNow - LastPasswordChange.Value;

                    if (timeSpan.TotalDays < 1)
                        return "أقل من يوم";
                    if (timeSpan.TotalDays < 30)
                        return $"{(int)timeSpan.TotalDays} يوم";
                    if (timeSpan.TotalDays < 365)
                        return $"{(int)(timeSpan.TotalDays / 30)} شهر";
                    return $"{(int)(timeSpan.TotalDays / 365)} سنة";
                }
            }

            public bool IsPasswordChangeNeeded => LastPasswordChange.HasValue &&
                                               (DateTime.UtcNow - LastPasswordChange.Value).TotalDays > 90;

            public string SecurityScore
            {
                get
                {
                    int score = 0;
                    if (EmailConfirmed) score += 25;
                    if (TwoFactorEnabled) score += 25;
                    if (LastPasswordChange.HasValue && (DateTime.UtcNow - LastPasswordChange.Value).TotalDays < 90) score += 25;
                    if (ActiveSessions.Count <= 2) score += 25;

                    return score switch
                    {
                        >= 90 => "ممتاز",
                        >= 70 => "جيد",
                        >= 50 => "متوسط",
                        _ => "ضعيف"
                    };
                }
            }

            public string SecurityScoreColor
            {
                get
                {
                    int score = 0;
                    if (EmailConfirmed) score += 25;
                    if (TwoFactorEnabled) score += 25;
                    if (LastPasswordChange.HasValue && (DateTime.UtcNow - LastPasswordChange.Value).TotalDays < 90) score += 25;
                    if (ActiveSessions.Count <= 2) score += 25;

                    return score switch
                    {
                        >= 90 => "success",
                        >= 70 => "info",
                        >= 50 => "warning",
                        _ => "danger"
                    };
                }
            }
        }

        // ViewModel لمحاولات تسجيل الدخول
        public class LoginAttemptViewModel
        {
            public string IpAddress { get; set; } = string.Empty;
            public string UserAgent { get; set; } = string.Empty;
            public DateTime LoginTime { get; set; }
            public bool IsSuccessful { get; set; }
            public string Location { get; set; } = string.Empty;
            public string? FailureReason { get; set; }

            public string LoginTimeText => LoginTime.ToString("yyyy/MM/dd HH:mm");
            public string StatusText => IsSuccessful ? "نجح" : "فشل";
            public string StatusClass => IsSuccessful ? "success" : "danger";
            public string DeviceInfo
            {
                get
                {
                    if (UserAgent.Contains("Windows"))
                        return "Windows";
                    if (UserAgent.Contains("Mac"))
                        return "Mac";
                    if (UserAgent.Contains("Linux"))
                        return "Linux";
                    if (UserAgent.Contains("Android"))
                        return "Android";
                    if (UserAgent.Contains("iPhone") || UserAgent.Contains("iPad"))
                        return "iOS";
                    return "Unknown";
                }
            }

            public string BrowserInfo
            {
                get
                {
                    if (UserAgent.Contains("Chrome"))
                        return "Chrome";
                    if (UserAgent.Contains("Firefox"))
                        return "Firefox";
                    if (UserAgent.Contains("Safari") && !UserAgent.Contains("Chrome"))
                        return "Safari";
                    if (UserAgent.Contains("Edge"))
                        return "Edge";
                    return "Other";
                }
            }
        }

        // ViewModel للجلسات النشطة
        public class ActiveSessionViewModel
        {
            public string SessionId { get; set; } = string.Empty;
            public string IpAddress { get; set; } = string.Empty;
            public string UserAgent { get; set; } = string.Empty;
            public DateTime LastActivity { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsCurrent { get; set; }
            public string Location { get; set; } = string.Empty;

            public string DeviceInfo => GetDeviceInfo(UserAgent);
            public string BrowserInfo => GetBrowserInfo(UserAgent);
            public string LastActivityText
            {
                get
                {
                    var timeSpan = DateTime.UtcNow - LastActivity;
                    if (timeSpan.TotalMinutes < 1)
                        return "الآن";
                    if (timeSpan.TotalMinutes < 60)
                        return $"منذ {(int)timeSpan.TotalMinutes} دقيقة";
                    if (timeSpan.TotalHours < 24)
                        return $"منذ {(int)timeSpan.TotalHours} ساعة";
                    return LastActivity.ToString("yyyy/MM/dd");
                }
            }

            private static string GetDeviceInfo(string userAgent)
            {
                if (userAgent.Contains("Windows")) return "Windows";
                if (userAgent.Contains("Mac")) return "Mac";
                if (userAgent.Contains("Linux")) return "Linux";
                if (userAgent.Contains("Android")) return "Android";
                if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";
                return "Unknown";
            }

            private static string GetBrowserInfo(string userAgent)
            {
                if (userAgent.Contains("Chrome")) return "Chrome";
                if (userAgent.Contains("Firefox")) return "Firefox";
                if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
                if (userAgent.Contains("Edge")) return "Edge";
                return "Other";
            }
        }

        // ViewModel لأحداث الأمان
        public class SecurityEventViewModel
        {
            public string EventType { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string IpAddress { get; set; } = string.Empty;
            public string? UserAgent { get; set; }
            public string Severity { get; set; } = "info"; // info, warning, danger
            public Dictionary<string, object> Metadata { get; set; } = new();

            public string EventTypeDisplay => EventType switch
            {
                "password_changed" => "تغيير كلمة المرور",
                "login_failed" => "فشل تسجيل الدخول",
                "login_success" => "نجح تسجيل الدخول",
                "profile_updated" => "تحديث الملف الشخصي",
                "email_changed" => "تغيير البريد الإلكتروني",
                "two_factor_enabled" => "تفعيل المصادقة الثنائية",
                "two_factor_disabled" => "إلغاء المصادقة الثنائية",
                "suspicious_activity" => "نشاط مشبوه",
                _ => EventType
            };

            public string SeverityIcon => Severity switch
            {
                "info" => "fas fa-info-circle",
                "warning" => "fas fa-exclamation-triangle",
                "danger" => "fas fa-exclamation-circle",
                _ => "fas fa-info-circle"
            };

            public string TimeAgo
            {
                get
                {
                    var timeSpan = DateTime.UtcNow - Timestamp;
                    if (timeSpan.TotalMinutes < 1)
                        return "الآن";
                    if (timeSpan.TotalMinutes < 60)
                        return $"منذ {(int)timeSpan.TotalMinutes} دقيقة";
                    if (timeSpan.TotalHours < 24)
                        return $"منذ {(int)timeSpan.TotalHours} ساعة";
                    if (timeSpan.TotalDays < 7)
                        return $"منذ {(int)timeSpan.TotalDays} يوم";
                    return Timestamp.ToString("yyyy/MM/dd");
                }
            }
        }

        // ViewModel لإعدادات الإشعارات
        public class NotificationSettingsViewModel
        {
            [Display(Name = "إشعارات البريد الإلكتروني")]
            public bool EmailNotifications { get; set; } = true;

            [Display(Name = "إشعار تقديم بحث جديد")]
            public bool ResearchSubmissionNotifications { get; set; } = true;

            [Display(Name = "إشعار تحديث حالة البحث")]
            public bool ResearchStatusNotifications { get; set; } = true;

            [Display(Name = "إشعار تعيين مراجعة جديدة")]
            public bool ReviewAssignmentNotifications { get; set; } = true;

            [Display(Name = "إشعار اقتراب موعد المراجعة")]
            public bool ReviewDeadlineNotifications { get; set; } = true;

            [Display(Name = "إشعار إكمال المراجعة")]
            public bool ReviewCompletionNotifications { get; set; } = true;

            [Display(Name = "إشعارات النشاطات العامة")]
            public bool GeneralActivityNotifications { get; set; } = false;

            [Display(Name = "إشعارات الرسائل النصية")]
            public bool SmsNotifications { get; set; } = false;

            [Display(Name = "إشعارات فورية في المتصفح")]
            public bool BrowserNotifications { get; set; } = true;

            [Display(Name = "التقرير الأسبوعي")]
            public bool WeeklyDigest { get; set; } = true;

            [Display(Name = "التقرير الشهري")]
            public bool MonthlyReport { get; set; } = false;

            [Display(Name = "إشعارات الأمان")]
            public bool SecurityNotifications { get; set; } = true;

            [Display(Name = "إشعارات التسويق")]
            public bool MarketingNotifications { get; set; } = false;
        }

        // ViewModel لملخص الملف الشخصي
        public class ProfileSummaryViewModel
        {
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? FullNameEn { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? Institution { get; set; }
            public string? AcademicDegree { get; set; }
            public string Role { get; set; } = string.Empty;
            public DateTime JoinDate { get; set; }
            public DateTime? LastActivity { get; set; }
            public bool IsActive { get; set; }
            public bool EmailConfirmed { get; set; }
            public string? ProfileImageUrl { get; set; }

            // إحصائيات سريعة
            public int TotalResearches { get; set; }
            public int CompletedReviews { get; set; }
            public double AverageScore { get; set; }
            public int TotalContributions => TotalResearches + CompletedReviews;

            // نصوص محسوبة
            public string JoinDateText => JoinDate.ToString("MMMM yyyy");
            public string LastActivityText
            {
                get
                {
                    if (!LastActivity.HasValue)
                        return "لا يوجد نشاط";

                    var timeSpan = DateTime.UtcNow - LastActivity.Value;

                    if (timeSpan.TotalMinutes < 1)
                        return "الآن";
                    if (timeSpan.TotalMinutes < 60)
                        return $"منذ {(int)timeSpan.TotalMinutes} دقيقة";
                    if (timeSpan.TotalHours < 24)
                        return $"منذ {(int)timeSpan.TotalHours} ساعة";
                    if (timeSpan.TotalDays < 7)
                        return $"منذ {(int)timeSpan.TotalDays} يوم";

                    return LastActivity.Value.ToString("yyyy/MM/dd");
                }
            }

            // مستوى النشاط
            public string ActivityLevel
            {
                get
                {
                    if (TotalContributions >= 50) return "نشط جداً";
                    if (TotalContributions >= 20) return "نشط";
                    if (TotalContributions >= 5) return "متوسط النشاط";
                    return "قليل النشاط";
                }
            }

            public string ActivityLevelColor
            {
                get
                {
                    if (TotalContributions >= 50) return "success";
                    if (TotalContributions >= 20) return "info";
                    if (TotalContributions >= 5) return "warning";
                    return "secondary";
                }
            }
        }

        // ViewModel لإحصائيات الملف الشخصي المتقدمة
        public class ProfileStatisticsViewModel
        {
            // إحصائيات البحوث
            public int TotalResearches { get; set; }
            public int AcceptedResearches { get; set; }
            public int RejectedResearches { get; set; }
            public int PendingResearches { get; set; }
            public double ResearchAcceptanceRate => TotalResearches > 0 ?
                                                   (double)AcceptedResearches / TotalResearches * 100 : 0;

            // إحصائيات المراجعات
            public int TotalReviews { get; set; }
            public int CompletedReviews { get; set; }
            public int PendingReviews { get; set; }
            public int OverdueReviews { get; set; }
            public double AverageReviewScore { get; set; }
            public double ReviewCompletionRate => TotalReviews > 0 ?
                                                 (double)CompletedReviews / TotalReviews * 100 : 0;

            // إحصائيات زمنية
            public int ThisMonthResearches { get; set; }
            public int ThisMonthReviews { get; set; }
            public int ThisYearResearches { get; set; }
            public int ThisYearReviews { get; set; }

            // مقاييس الأداء
            public double AverageReviewTime { get; set; } // بالأيام
            public int FastestReview { get; set; } // بالأيام
            public int SlowestReview { get; set; } // بالأيام
            public double OnTimeReviewRate { get; set; } // نسبة المراجعات في الوقت المحدد

            // اتجاهات حديثة (آخر 6 أشهر)
            public List<MonthlyActivityViewModel> MonthlyActivity { get; set; } = new();
            public List<ResearchTrackPerformance> TrackPerformance { get; set; } = new();
        }

        public class MonthlyActivityViewModel
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public int ResearchCount { get; set; }
            public int ReviewCount { get; set; }
            public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
        }

        public class ResearchTrackPerformance
        {
            public string TrackName { get; set; } = string.Empty;
            public int ResearchesSubmitted { get; set; }
            public int ResearchesAccepted { get; set; }
            public int ReviewsCompleted { get; set; }
            public double AverageScore { get; set; }
            public double AcceptanceRate => ResearchesSubmitted > 0 ?
                                           (double)ResearchesAccepted / ResearchesSubmitted * 100 : 0;
        }

        // ViewModel لتفضيلات المستخدم
        public class UserPreferencesViewModel
        {
            [Display(Name = "اللغة المفضلة")]
            public string PreferredLanguage { get; set; } = "ar";

            [Display(Name = "المنطقة الزمنية")]
            public string TimeZone { get; set; } = "Asia/Baghdad";

            [Display(Name = "تنسيق التاريخ")]
            public string DateFormat { get; set; } = "dd/MM/yyyy";

            [Display(Name = "عدد العناصر في الصفحة")]
            [Range(5, 50, ErrorMessage = "يجب أن يكون العدد بين 5 و 50")]
            public int ItemsPerPage { get; set; } = 10;

            [Display(Name = "إظهار البيانات باللغة الإنجليزية")]
            public bool ShowEnglishData { get; set; } = false;

            [Display(Name = "استخدام الوضع المظلم")]
            public bool DarkMode { get; set; } = false;

            [Display(Name = "إخفاء البحوث المرفوضة من القائمة الرئيسية")]
            public bool HideRejectedResearches { get; set; } = false;

            [Display(Name = "عرض الإحصائيات المتقدمة")]
            public bool ShowAdvancedStatistics { get; set; } = true;

            [Display(Name = "تحديث تلقائي للصفحات")]
            public bool AutoRefresh { get; set; } = false;

            [Display(Name = "فترة التحديث التلقائي (بالدقائق)")]
            [Range(1, 60, ErrorMessage = "يجب أن تكون الفترة بين 1 و 60 دقيقة")]
            public int AutoRefreshInterval { get; set; } = 5;

            // قوائم الخيارات
            public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> LanguageOptions => new()
        {
            new() { Value = "ar", Text = "العربية" },
            new() { Value = "en", Text = "English" }
        };

            public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> TimeZoneOptions => new()
        {
            new() { Value = "Asia/Baghdad", Text = "بغداد (UTC+3)" },
            new() { Value = "Asia/Riyadh", Text = "الرياض (UTC+3)" },
            new() { Value = "Asia/Kuwait", Text = "الكويت (UTC+3)" },
            new() { Value = "Asia/Dubai", Text = "دبي (UTC+4)" },
            new() { Value = "Asia/Cairo", Text = "القاهرة (UTC+2)" },
            new() { Value = "UTC", Text = "التوقيت العالمي (UTC)" }
        };

            public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> DateFormatOptions => new()
        {
            new() { Value = "dd/MM/yyyy", Text = "31/12/2023" },
            new() { Value = "MM/dd/yyyy", Text = "12/31/2023" },
            new() { Value = "yyyy-MM-dd", Text = "2023-12-31" },
            new() { Value = "dd-MM-yyyy", Text = "31-12-2023" },
            new() { Value = "yyyy/MM/dd", Text = "2023/12/31" }
        };
        }
    }
