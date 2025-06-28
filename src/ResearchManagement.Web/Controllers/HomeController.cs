// Controllers/HomeController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ResearchManagement.Web.Models;

namespace ResearchManagement.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                // ��� ��� �������� ���� ���� ���� ���������
                if (User.Identity?.IsAuthenticated == true)
                {
                    return RedirectToAction("Index", "Dashboard");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��� �� ������ ��������");
                return RedirectToAction("Error");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            var model = new ErrorViewModel
            {
                RequestId = requestId
            };

            _logger.LogError("�� ��� ���� ����� - Request ID: {RequestId}", requestId);

            return View(model);
        }
    }
}