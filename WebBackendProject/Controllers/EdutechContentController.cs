using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using WebBackendProject.Data;



namespace WebBackendProject.Controllers
{
    [Route("api/Edutechcontent")] // ✅ Set base route
    [ApiController] // ✅ Required for Web API behavior
    public class EdutechContentController : ControllerBase // ✅ Use ControllerBase for APIs
    {
        private readonly ApplicationDbContext _context;

        public EdutechContentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("buttons/{userId}")]
        public async Task<IActionResult> GetButtons(int userId)
        {
            // Step 1: Find ClassID of the user
            var user = await _context.Students
                .Where(s => s.id == userId)
                .Select(s => new { s.class_id })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Step 2: Get Subjects for the user's ClassID
            var buttons = await _context.ClassSubjects
                .Where(cs => cs.ClassID == user.class_id)
                .Join(_context.subjects.AsNoTracking(),
                    cs => cs.SubjectID,
                    s => s.SubjectID,
                    (cs, s) => new
                    {

                        id = s.SubjectID,
                        ClassID = user.class_id,
                        text = s.SubjectName,
                        image = $"/images/subjects/{s.SubjectID}.png"
                    })
                .ToListAsync();

            return Ok(buttons);
        }
    }
}
