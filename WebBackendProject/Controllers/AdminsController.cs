using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Models.Dto;

namespace WebBackendProject.Controllers
{
    [Route("api/AdminsController")]
    public class AdminsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
       public AdminsController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost("CreateMaster")]
        public async Task<IActionResult> CreateMaster([FromBody]admin admins){
            if (admins == null)
            {
                return BadRequest("Admin data is invalid.");
            }
            // Assign the hashed password to the admin object
            admins.password = PasswordHasher.HashPassword(admins.password);
            admins.created_at = DateTime.UtcNow;
            // Save the admin object to the database (your EF context would go here)
             _context.Admin.Add(admins);
             await _context.SaveChangesAsync();

            return Ok("Admin created successfully.");

        }

        [HttpPost("CheckAdminId")]
        public async Task<IActionResult> CheckCredentials([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required." });
            }

            var teacher = await _context.Admin
                .FirstOrDefaultAsync(s => s.username == loginRequest.Username);

            if (teacher == null)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }

            bool isPasswordCorrect = PasswordHasher.VerifyPassword(loginRequest.Password, teacher.password);

            if (!isPasswordCorrect)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }
           
           
            return Ok(new
            {
                success = true,
                message = "Login successful.",
                id = teacher.id,
                name = teacher.username,
            });
        }

    }


}
