using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Domain.Enums;
using ResearchManagement.Infrastructure.Services;

namespace ResearchManagement.Infrastructure.Services
{
    public interface IRoleService
    {
        Task<bool> InitializeRolesAsync();
        Task<bool> AssignUserToRoleAsync(string userId, UserRole role);
        Task<bool> RemoveUserFromRoleAsync(string userId, UserRole role);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> IsUserInRoleAsync(string userId, UserRole role);
        Task<List<User>> GetUsersInRoleAsync(UserRole role);
    }

    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            RoleManager<IdentityRole> roleManager,
            UserManager<User> userManager,
            ILogger<RoleService> logger)
        {
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> InitializeRolesAsync()
        {
            try
            {
                var roles = new[]
                {
                    "Researcher",
                    "Reviewer",
                    "TrackManager",
                    "ConferenceManager",
                    "SystemAdmin"
                };

                foreach (var roleName in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        var role = new IdentityRole(roleName);
                        var result = await _roleManager.CreateAsync(role);

                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Role created successfully: {RoleName}", roleName);
                        }
                        else
                        {
                            _logger.LogError("Failed to create role: {RoleName}. Errors: {Errors}",
                                roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing roles");
                return false;
            }
        }

        public async Task<bool> AssignUserToRoleAsync(string userId, UserRole role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                var roleName = GetRoleName(role);

                // Remove user from all roles first
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Add user to new role
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    // Update user role in entity
                    user.Role = role;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("User {UserId} assigned to role {RoleName}", userId, roleName);
                    return true;
                }

                _logger.LogError("Failed to assign user {UserId} to role {RoleName}. Errors: {Errors}",
                    userId, roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning user {UserId} to role {Role}", userId, role);
                return false;
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(string userId, UserRole role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                var roleName = GetRoleName(role);
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} removed from role {RoleName}", userId, roleName);
                    return true;
                }

                _logger.LogError("Failed to remove user {UserId} from role {RoleName}. Errors: {Errors}",
                    userId, roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from role {Role}", userId, role);
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new List<string>();
                }

                var roles = await _userManager.GetRolesAsync(user);
                return roles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<bool> IsUserInRoleAsync(string userId, UserRole role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                var roleName = GetRoleName(role);
                return await _userManager.IsInRoleAsync(user, roleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} is in role {Role}", userId, role);
                return false;
            }
        }

        public async Task<List<User>> GetUsersInRoleAsync(UserRole role)
        {
            try
            {
                var roleName = GetRoleName(role);
                var users = await _userManager.GetUsersInRoleAsync(roleName);
                return users.Where(u => !u.IsDeleted && u.IsActive).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users in role {Role}", role);
                return new List<User>();
            }
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
    }

    // Extension class for dependency injection
    public static class RoleServiceExtensions
    {
        public static IServiceCollection AddRoleServices(this IServiceCollection services)
        {
            services.AddScoped<IRoleService, RoleService>();
            return services;
        }
    }
}

// Data Seeder for initial admin user
namespace ResearchManagement.Infrastructure.Data
{
    public class DataSeeder
    {
        private readonly UserManager<User> _userManager;
        private readonly IRoleService _roleService;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            UserManager<User> userManager,
            IRoleService roleService,
            ILogger<DataSeeder> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SeedAsync()
        {
            try
            {
                // Initialize roles first
                await _roleService.InitializeRolesAsync();

                // Create default admin user if not exists
                await CreateDefaultAdminAsync();

                _logger.LogInformation("Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data seeding");
                throw;
            }
        }

        private async Task CreateDefaultAdminAsync()
        {
            const string adminEmail = "admin@researchmanagement.com";

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Default admin user already exists");
                return;
            }

            var adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "مدير",
                LastName = "النظام",
                FirstNameEn = "System",
                LastNameEn = "Administrator",
                Institution = "إدارة النظام",
                AcademicDegree = "مدير نظام",
                Role = UserRole.SystemAdmin,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await _roleService.AssignUserToRoleAsync(adminUser.Id, UserRole.SystemAdmin);
                _logger.LogInformation("Default admin user created successfully: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to create default admin user. Errors: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    // Extension method for seeding data
    //public static class WebApplicationExtensions
    //{
    //    public static async Task SeedDataAsync(this WebApplication app)
    //    {
    //        using var scope = app.Services.CreateScope();
    //        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    //        await seeder.SeedAsync();
    //    }
    //}
}