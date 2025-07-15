using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Domain.Enums;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Web.Models.ViewModels.User;
using AutoMapper;
using ResearchManagement.Infrastructure.Repositories;

namespace ResearchManagement.Web.Controllers
{
    [Authorize(Roles = "SystemAdmin,ConferenceManager")]
    public class UserController : BaseController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            UserManager<User> userManager,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<UserController> logger,
            RoleManager<IdentityRole> roleManager) : base(userManager)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        // GET: User
        public async Task<IActionResult> Index(string? search, UserRole? role, bool? isActive, int page = 1, int pageSize = 10)
        {
            try
            {
                var users = await _userManager.Users
                    .Where(u => !u.IsDeleted)
                    .ToListAsync();

                // تطبيق الفلاتر
                if (!string.IsNullOrEmpty(search))
                {
                    users = users.Where(u =>
                        u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Institution != null && u.Institution.Contains(search, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                if (role.HasValue)
                {
                    users = users.Where(u => u.Role == role.Value).ToList();
                }

                if (isActive.HasValue)
                {
                    users = users.Where(u => u.IsActive == isActive.Value).ToList();
                }

                // ترتيب النتائج
                users = users.OrderByDescending(u => u.CreatedAt).ToList();

                // حساب التصفح
                var totalUsers = users.Count;
                var pagedUsers = users
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var viewModel = new UserListViewModel
                {
                    Users = pagedUsers,
                    SearchTerm = search,
                    SelectedRole = role,
                    SelectedIsActive = isActive,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalUsers = totalUsers,
                    TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                    RoleOptions = GetRoleOptions(),
                    Statistics = await GetUserStatisticsAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                AddErrorMessage("حدث خطأ في تحميل قائمة المستخدمين");
                return View(new UserListViewModel());
            }
        }

        //GET: User/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null || user.IsDeleted)
                {
                    return NotFound();
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var userDto = _mapper.Map<UserDto>(user);

                var viewModel = new UserDetailsViewModel
                {
                    User = userDto,
                    Roles = userRoles.ToList(),
                    LastLoginDate = user.LastLoginAt,
                    RegistrationDate = user.CreatedAt,
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = user.IsActive
                };

                // إضافة إحصائيات المستخدم
                await LoadUserStatisticsAsync(viewModel, id);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                AddErrorMessage("حدث خطأ في تحميل تفاصيل المستخدم");
                return RedirectToAction(nameof(Index));
            }
        }
        // في UserController.cs - تحديث Details Action

        // GET: User/Details/5

        // GET: User/Create
        public IActionResult Create()
        {
            var viewModel = new CreateUserViewModel
            {
                RoleOptions = GetRoleOptions(),
                IsActive = true
            };

            return View(viewModel);
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RoleOptions = GetRoleOptions();
                return View(model);
            }

            try
            {
                // التحقق من عدم وجود مستخدم بنفس البريد الإلكتروني
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم مسبقاً");
                    model.RoleOptions = GetRoleOptions();
                    return View(model);
                }

                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    FirstNameEn = model.FirstNameEn,
                    LastNameEn = model.LastNameEn,
                    Institution = model.Institution,
                    AcademicDegree = model.AcademicDegree,
                    OrcidId = model.OrcidId,
                    Role = model.Role,
                    IsActive = model.IsActive,
                    EmailConfirmed = model.EmailConfirmed,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId()
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // إضافة المستخدم إلى الدور المناسب
                    var roleName = GetRoleName(model.Role);
                    await _userManager.AddToRoleAsync(user, roleName);

                    _logger.LogInformation("User created successfully: {Email} by {CreatedBy}", user.Email, GetCurrentUserId());
                    AddSuccessMessage("تم إنشاء المستخدم بنجاح");
                    return RedirectToAction(nameof(Details), new { id = user.Id });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", model.Email);
                AddErrorMessage("حدث خطأ في إنشاء المستخدم");
            }

            model.RoleOptions = GetRoleOptions();
            return View(model);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null || user.IsDeleted)
                {
                    return NotFound();
                }

                var viewModel = new EditUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FirstNameEn = user.FirstNameEn,
                    LastNameEn = user.LastNameEn,
                    Email = user.Email,
                    Institution = user.Institution,
                    AcademicDegree = user.AcademicDegree,
                    OrcidId = user.OrcidId,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    RoleOptions = GetRoleOptions()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for edit: {UserId}", id);
                AddErrorMessage("حدث خطأ في تحميل بيانات المستخدم");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RoleOptions = GetRoleOptions();
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null || user.IsDeleted)
                {
                    return NotFound();
                }

                // التحقق من عدم تغيير البريد الإلكتروني إلى بريد موجود
                if (user.Email != model.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم مسبقاً");
                        model.RoleOptions = GetRoleOptions();
                        return View(model);
                    }
                }

                // حفظ الدور القديم
                var oldRole = user.Role;

                // تحديث بيانات المستخدم
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.FirstNameEn = model.FirstNameEn;
                user.LastNameEn = model.LastNameEn;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.Institution = model.Institution;
                user.AcademicDegree = model.AcademicDegree;
                user.OrcidId = model.OrcidId;
                user.Role = model.Role;
                user.IsActive = model.IsActive;
                user.EmailConfirmed = model.EmailConfirmed;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = GetCurrentUserId();

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // تحديث الأدوار إذا تغير الدور
                    if (oldRole != model.Role)
                    {
                        var oldRoleName = GetRoleName(oldRole);
                        var newRoleName = GetRoleName(model.Role);

                        await _userManager.RemoveFromRoleAsync(user, oldRoleName);
                        await _userManager.AddToRoleAsync(user, newRoleName);
                    }

                    _logger.LogInformation("User updated successfully: {UserId} by {UpdatedBy}", user.Id, GetCurrentUserId());
                    AddSuccessMessage("تم تحديث بيانات المستخدم بنجاح");
                    return RedirectToAction(nameof(Details), new { id = user.Id });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", model.Id);
                AddErrorMessage("حدث خطأ في تحديث بيانات المستخدم");
            }

            model.RoleOptions = GetRoleOptions();
            return View(model);
        }

        // POST: User/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "معرف المستخدم غير صحيح" });
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null || user.IsDeleted)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                // منع حذف المستخدم الحالي
                if (user.Id == GetCurrentUserId())
                {
                    return Json(new { success = false, message = "لا يمكن حذف حسابك الخاص" });
                }

                // حذف منطقي
                user.IsDeleted = true;
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = GetCurrentUserId();

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User deleted successfully: {UserId} by {DeletedBy}", user.Id, GetCurrentUserId());
                    return Json(new { success = true, message = "تم حذف المستخدم بنجاح" });
                }

                return Json(new { success = false, message = "حدث خطأ في حذف المستخدم" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return Json(new { success = false, message = "حدث خطأ في حذف المستخدم" });
            }
        }

        // POST: User/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "معرف المستخدم غير صحيح" });
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null || user.IsDeleted)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                // منع إلغاء تفعيل المستخدم الحالي
                if (user.Id == GetCurrentUserId() && user.IsActive)
                {
                    return Json(new { success = false, message = "لا يمكن إلغاء تفعيل حسابك الخاص" });
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = GetCurrentUserId();

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var status = user.IsActive ? "تفعيل" : "إلغاء تفعيل";
                    _logger.LogInformation("User status toggled: {UserId} to {Status} by {UpdatedBy}", user.Id, status, GetCurrentUserId());

                    return Json(new
                    {
                        success = true,
                        message = $"تم {status} المستخدم بنجاح",
                        isActive = user.IsActive
                    });
                }

                return Json(new { success = false, message = "حدث خطأ في تغيير حالة المستخدم" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status: {UserId}", id);
                return Json(new { success = false, message = "حدث خطأ في تغيير حالة المستخدم" });
            }
        }

        // GET: User/ResetPassword/5
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            var viewModel = new ResetPasswordViewModel
            {
                UserId = user.Id,
                UserName = $"{user.FirstName} {user.LastName}",
                Email = user.Email
            };

            return View(viewModel);
        }

        // POST: User/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null || user.IsDeleted)
                {
                    return NotFound();
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successfully for user: {UserId} by {ResetBy}", user.Id, GetCurrentUserId());
                    AddSuccessMessage("تم إعادة تعيين كلمة المرور بنجاح");
                    return RedirectToAction(nameof(Details), new { id = user.Id });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", model.UserId);
                AddErrorMessage("حدث خطأ في إعادة تعيين كلمة المرور");
            }

            return View(model);
        }

        // Helper Methods
        private List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetRoleOptions()
        {
            return Enum.GetValues<UserRole>()
                .Select(role => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = ((int)role).ToString(),
                    Text = GetRoleDisplayName(role)
                })
                .ToList();
        }

        private string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.Researcher => "باحث",
                UserRole.Reviewer => "مقيم",
                UserRole.TrackManager => "مدير تراك",
                UserRole.ConferenceManager => "مدير المؤتمر",
                UserRole.SystemAdmin => "مدير النظام",
                _ => role.ToString()
            };
        }

        private string GetRoleName(UserRole role)
        {
            return role switch
            {
                UserRole.Researcher => "Researcher",
                UserRole.Reviewer => "Reviewer",
                UserRole.TrackManager => "TrackManager",
                UserRole.ConferenceManager => "ConferenceManager",
                UserRole.SystemAdmin => "SystemAdmin",
                _ => "Researcher"
            };
        }

        private async Task<UserStatisticsViewModel> GetUserStatisticsAsync()
        {
            var allUsers = await _userManager.Users.Where(u => !u.IsDeleted).ToListAsync();

            return new UserStatisticsViewModel
            {
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.IsActive),
                InactiveUsers = allUsers.Count(u => !u.IsActive),
                EmailConfirmedUsers = allUsers.Count(u => u.EmailConfirmed),
                ResearchersCount = allUsers.Count(u => u.Role == UserRole.Researcher),
                ReviewersCount = allUsers.Count(u => u.Role == UserRole.Reviewer),
                TrackManagersCount = allUsers.Count(u => u.Role == UserRole.TrackManager),
                ConferenceManagersCount = allUsers.Count(u => u.Role == UserRole.ConferenceManager),
                SystemAdminsCount = allUsers.Count(u => u.Role == UserRole.SystemAdmin),
                RecentRegistrations = allUsers.Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30)).Count()
            };
        }

        private async Task LoadUserStatisticsAsync(UserDetailsViewModel viewModel, string userId)
        {
            // يمكن إضافة إحصائيات خاصة بالمستخدم هنا
            // مثل عدد البحوث المقدمة، المراجعات المكتملة، إلخ
            await Task.CompletedTask;
        }
    }
}