using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using SignatureClinicTreatmentPlanner.Data;
using System.Security.Claims;
using SignatureClinicTreatmentPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public LoginController(ApplicationDbContext context, SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "Please enter both username and password.";
                return View(model);
            }

            // Find the user by username
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                ViewBag.Error = "Invalid username or password.";
                return View(model);
            }

            // Check if the password is correct
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ViewBag.Error = "Invalid username or password.";
                return View(model);
            }

            // Sign in the user
            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                // Store UserName & FullName in session
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("FullName",
                    (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
                        ? user.FirstName + " " + user.LastName
                        : user.UserName); // Use UserName as fallback if FullName is null
                                          // Generate initials: First letter of FirstName + First letter of LastName
                string initials = (user.FirstName?.FirstOrDefault().ToString() ?? "") + (user.LastName?.FirstOrDefault().ToString() ?? "");
                HttpContext.Session.SetString("UserInitials", initials.ToUpper()); // Convert to uppercase

                if (user.RoleId == 1)
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (user.RoleId == 2)
                {
                    return RedirectToAction("Create", "Patient");
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password.";
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }
    }
}
