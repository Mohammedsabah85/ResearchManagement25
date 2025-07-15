using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ResearchManagement.Domain.Enums;

namespace ResearchManagement.Web.Models.ViewModels.User
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(50, ErrorMessage = "الاسم الأول يجب ألا يزيد عن 50 حرف")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(50, ErrorMessage = "اسم العائلة يجب ألا يزيد عن 50 حرف")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "الاسم الأول (إنجليزي) يجب ألا يزيد عن 50 حرف")]
        public string? FirstNameEn { get; set; }

        [StringLength(50, ErrorMessage = "اسم العائلة (إنجليزي) يجب ألا يزيد عن 50 حرف")]
        public string? LastNameEn { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "تنسيق البريد الإلكتروني غير صحيح")]
        [StringLength(100, ErrorMessage = "البريد الإلكتروني يجب ألا يزيد عن 100 حرف")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تحتوي على 6 أحرف على الأقل")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب اختيار دور للمستخدم")]
        public UserRole Role { get; set; }

        [StringLength(200, ErrorMessage = "اسم المؤسسة يجب ألا يزيد عن 200 حرف")]
        public string? Institution { get; set; }

        [StringLength(100, ErrorMessage = "الدرجة العلمية يجب ألا تزيد عن 100 حرف")]
        public string? AcademicDegree { get; set; }

        [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{4}$", ErrorMessage = "تنسيق معرف ORCID غير صحيح (0000-0000-0000-0000)")]
        public string? OrcidId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool EmailConfirmed { get; set; } = false;

        // Navigation properties
        public List<SelectListItem> RoleOptions { get; set; } = new();
    }

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(50, ErrorMessage = "الاسم الأول يجب ألا يزيد عن 50 حرف")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(50, ErrorMessage = "اسم العائلة يجب ألا يزيد عن 50 حرف")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "الاسم الأول (إنجليزي) يجب ألا يزيد عن 50 حرف")]
        public string? FirstNameEn { get; set; }

        [StringLength(50, ErrorMessage = "اسم العائلة (إنجليزي) يجب ألا يزيد عن 50 حرف")]
        public string? LastNameEn { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "تنسيق البريد الإلكتروني غير صحيح")]
        [StringLength(100, ErrorMessage = "البريد الإلكتروني يجب ألا يزيد عن 100 حرف")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب اختيار دور للمستخدم")]
        public UserRole Role { get; set; }

        [StringLength(200, ErrorMessage = "اسم المؤسسة يجب ألا يزيد عن 200 حرف")]
        public string? Institution { get; set; }

        [StringLength(100, ErrorMessage = "الدرجة العلمية يجب ألا تزيد عن 100 حرف")]
        public string? AcademicDegree { get; set; }

        [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{4}$", ErrorMessage = "تنسيق معرف ORCID غير صحيح (0000-0000-0000-0000)")]
        public string? OrcidId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool EmailConfirmed { get; set; } = false;

        // Navigation properties
        public List<SelectListItem> RoleOptions { get; set; } = new();
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تحتوي على 6 أحرف على الأقل")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}