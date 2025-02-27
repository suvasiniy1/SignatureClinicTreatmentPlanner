using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignatureClinicTreatmentPlanner.Data;
using SignatureClinicTreatmentPlanner.Models;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;

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
            COALESCE(u.PhoneNumber, '') AS PhoneNumber,
            COALESCE(r.Name, 'No Role') AS RoleName, 
            u.RoleId,  -- ✅ Ensure RoleId is returned
            u.IsActive
        FROM AspNetUsers u
        LEFT JOIN Roles r ON u.RoleId = r.Id";

                using (var connection = _context.Database.GetDbConnection())
                {
                    var users = await connection.QueryAsync<UserDto>(sql);
                    var formattedUsers = users.Select(u => new {
                        id = u.Id, // ✅ Ensure ID is included
                        userName = u.UserName,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        email = u.Email,
                        phoneNumber = u.PhoneNumber ?? "N/A",
                        roleId = u.RoleId,
                        roleName = u.RoleName,
                        isActive = u.IsActive
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
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            return Json(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("UserName,FirstName,LastName,Email,RoleId,,PhoneNumber,IsActive,PasswordHash")] User model)
        {
            if (model.RoleId == 0)
            {
                return Json(new { success = false, message = "Role is required." });
            }

            ModelState.Remove("Role");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine("ModelState Errors: " + string.Join(", ", errors));
                return Json(new { success = false, message = "Invalid user data.", errors = errors });
            }

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow,  // ✅ Ensure valid default value
                ModifiedDate = null
            };

            // Encrypt Password Before Saving
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, model.PasswordHash);

            Console.WriteLine($"Inserting user with CreatedDate: {user.CreatedDate}");

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
            user.PhoneNumber = model.PhoneNumber;
            user.ModifiedDate = DateTime.UtcNow;
            // ✅ Do NOT update PasswordHash to avoid overriding the existing password
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User updated successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            user.IsActive = false; // ✅ Set user as inactive instead of deleting
            user.ModifiedDate = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User deactivated successfully!" });
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
