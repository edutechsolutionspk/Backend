
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;
using WebBackendProject.Model;
using WebBackendProject.Models.Dto;
using WebBackendProject.Services;

namespace WebBackendProject.Controllers
{
    [Route("api/StudentContent")]
    [ApiController]

    public class StudentsOperation : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private DeleteServices _deleteServices;
        public StudentsOperation(ApplicationDbContext context)
        {
            _context = context;
            _deleteServices = new DeleteServices(context);
        }

        // this function upload student data to database 
        [HttpPost("AddStudentToDatabase")]
        public async Task<IActionResult> AddStudentToDatabase([FromForm] StudentCreateDto studentData,[FromQuery] bool isadmin)
        {

            try
            {

                if (studentData == null)
                    return BadRequest(new { error = "Invalid student data." });
                const long maxFileSize = 2 * 1024 * 1024; // 2MB in bytes

                if (studentData.profilePicture.Length > maxFileSize)
                {
                    // File is too large
                    return BadRequest("The uploaded profile picture must not exceed 2MB in size.");
                }

                if(studentData.Password.Length < 8)
                {
                  
                }
                string? imagePath = null;

               
                if (studentData.profilePicture != null && studentData.profilePicture.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/studentPics");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);

                    }


                    string uniqueFileName = $"{Guid.NewGuid()}_{studentData.profilePicture.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await studentData.profilePicture.CopyToAsync(stream);
                    }

                    imagePath = $"/images/studentPics/{uniqueFileName}";
                }

                string hashedPassword = PasswordHasher.HashPassword(studentData.Password);

                var newStudent = new Student
                {
                    full_name = studentData.FullName,
                    father_name = studentData.FatherName,
                    age = studentData.Age,
                    username = studentData.Username,
                    email = studentData.Email,
                    password = hashedPassword,
                    class_id = studentData.ClassId,
                    picture_path = imagePath,
                    school_id =  studentData.schoolid,
                    status = "active",
                    created_at = DateTime.UtcNow,
                    g_contact = studentData.contact,
                };

                if (isadmin)
                {
                    newStudent.created_by_master = studentData.CreatedById;
                    
                }
                else if (!isadmin)
                {
                    newStudent.created_by_principal = studentData.CreatedById;
                     }
                else
                {
                    return BadRequest("is admin not found");
                }

                _context.Students.Add(newStudent);
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Student added successfully!", imageUrl = imagePath });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                // Get the inner exception
                string inner = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";

                return StatusCode(500, new
                {
                    error = "An unexpected error occurred.",
                    message = ex.Message,
                    innerException = inner
                });
            }
        }



        public class StudentCreateDto
        {
            public required string FullName { get; set; }
            public required string FatherName { get; set; }
            public required int Age { get; set; }
            public required string Username { get; set; }
            public required string Email { get; set; }
            public required string Password { get; set; }
            public required int ClassId { get; set; }
            public required int schoolid { get; set; }
            public required int CreatedById { get; set; }
            public required string contact {  get; set; }
            public IFormFile? profilePicture { get; set; }
        }

        [HttpPost("CheckStudentId")]
        public async Task<IActionResult> CheckCredentials([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required." });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.username == loginRequest.Username);

            if (student == null)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }

            bool isPasswordCorrect = PasswordHasher.VerifyPassword(loginRequest.Password, student.password);

            if (!isPasswordCorrect)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }
            if (!string.Equals(student.status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { success = false, message = "Account is inactive. Please contact administration." });
            }
            // Construct full image URL only if both username and password are correct
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string? fullImagePath = !string.IsNullOrEmpty(student.picture_path) ? $"{baseUrl}{student.picture_path}" : null;

            return Ok(new
            {
                success = true,
                message = "Login successful.",
                id = student.id,
                schoolid = student.school_id,
                imgPath = fullImagePath
                
            });
        }



        // ✅ Get student information by ID

        [HttpGet("GetStudentInfo/{id}")]
        public async Task<IActionResult> GetStudentInfo(int id)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var student = await (
            from s in _context.Students.AsNoTracking()
            join c in _context.Classes on s.class_id equals c.ClassID into sc
            from c in sc.DefaultIfEmpty()
            join school in _context.Schools on s.school_id equals school.id into ss
            from school in ss.DefaultIfEmpty()
            where s.id == id
            select new
            {
            name = s.full_name,
            fathername = s.father_name,
            classno = c.ClassName,
            age = s.age,
            imgPath = string.IsNullOrEmpty(s.picture_path) ? null : $"{baseUrl}{s.picture_path}",
            school_name = school.school_name
            }
 ).FirstOrDefaultAsync();

            if (student == null)
            {
                return NotFound(new { message = "Student not found." });
            }


            return Ok(student);
        }

        [HttpGet("GetAllStudentInfo")]
        public async Task<IActionResult> GetAllStudentInfo([FromQuery] int? schoolId, [FromQuery] int page = 1)
        {
            var baseQuery = _context.Students
                .Where(s => !schoolId.HasValue || s.school_id == schoolId.Value)
                .Join(_context.Classes,
                      student => student.class_id,
                      cls => cls.ClassID,
                      (student, cls) => new { student, cls })
                .Join(_context.Schools,
                      combined => combined.student.school_id,
                      school => school.id,
                      (combined, school) => new
                      {
                          combined.student.id,
                          combined.student.full_name,
                          ClassName = combined.cls.ClassName,
                          combined.student.school_id,
                          SchoolName = school.school_name,
                          Status = combined.student.status.ToString()
                      });

            var totalRecords = await baseQuery.CountAsync();
            int pageSize = 15;
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            int currentPage = page;
            if (currentPage < 1)
                currentPage = totalPages;
            else if (currentPage > totalPages)
                currentPage = 1;

            var students = await baseQuery
                .OrderBy(s => s.id)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                totalPages,
                currentPage,
                data = students
            });
        }

        [HttpDelete("DeleteStudent")]
        public async Task<IActionResult> DeleteStudent([FromQuery] int studentId)
        {

          await  _deleteServices.DeleteStudents(null, studentId);
            return Ok(new { success = true, message = "Student deleted successfully" });
        }
        [HttpPut("UpdateStudentStatus")]
        public async Task<IActionResult> UpdateStudentStatus([FromBody] StatusUpdateModel statusUpdateModel)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.id == statusUpdateModel.id);

            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            student.status = statusUpdateModel.status;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Status updated successfully" });
        }

    }
    public class StatusUpdateModel
    {
        public int id { get; set; } 
        public string status { get; set; } = string.Empty;
    }

}
