using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignatureClinicTreatmentPlanner.Data;
using SignatureClinicTreatmentPlanner.Models;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class ManageUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager; // Identity UserManager

        public ManageUserController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Fetch all users with roles
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                string sql = @"
            SELECT 
                u.Id, 
                u.UserName,
                u.FirstName,
                u.LastName,
                u.Email, 
                COALESCE(r.Name, 'No Role') AS RoleName, 
                u.IsActive
            FROM AspNetUsers u
            LEFT JOIN Roles r ON u.RoleId = r.Id";

                using (var connection = _context.Database.GetDbConnection()) // ✅ Get DB Connection
                {
                    var users = await connection.QueryAsync<UserDto>(sql);
                    var formattedUsers = users.Select(u => new {
                        userName = u.UserName,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        email = u.Email,
                        roleName = u.RoleName,
                        isActive = u.IsActive,
                        id = u.Id

                    }).ToList();
                    return Json(new { data = formattedUsers });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        // Fetch user details by ID
        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await (from u in _context.Users
                                  join r in _context.Roles on u.RoleId equals r.Id into roleGroup
                                  from role in roleGroup.DefaultIfEmpty()  // Left join to handle null roles
                                  where u.Id == id
                                  select new
                                  {
                                      u.Id,
                                      u.UserName,
                                      u.FirstName,
                                      u.LastName,
                                      u.Email,
                                      u.IsActive,
                                      RoleId = u.RoleId,
                                      RoleName = role != null ? role.Name : null  // Avoid null reference error
                                  })
                                 .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                return Json(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public async Task<IActionResult> Create(User model)
        {
            if (string.IsNullOrEmpty(model.FirstName) || string.IsNullOrEmpty(model.LastName))
            {
                return Json(new { success = false, message = "First Name and Last Name are required." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid user data." });
            }

            // Create a new user object
            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                EmailConfirmed = true // Auto-confirm email
            };

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, model.PasswordHash);

            var result = await _userManager.CreateAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User added successfully!" });
            }

            return Json(new { success = false, message = "Error creating user.", errors = result.Errors });
        }

        // Update existing user
        [HttpPost]
        public async Task<IActionResult> Update(User model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid user data." });
            }

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.RoleId = model.RoleId;
            user.IsActive = model.IsActive;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User updated successfully!" });
        }

        // Delete user
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User deleted successfully!" });
        }
        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                string sql = "SELECT Id, Name AS RoleName FROM Roles"; // ✅ Ensure alias is correct
                using (var connection = _context.Database.GetDbConnection())
                {
                    var roles = await connection.QueryAsync<RoleDto>(sql);

                    return Json(new { success = true, data = roles });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
