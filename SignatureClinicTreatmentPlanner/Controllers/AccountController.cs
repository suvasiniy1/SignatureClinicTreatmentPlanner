using Microsoft.AspNetCore.Mvc;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            // You can add logic to handle password reset here
            ViewBag.Message = "Password reset link has been sent to your email (dummy message).";
            return View();
        }

    }
}
