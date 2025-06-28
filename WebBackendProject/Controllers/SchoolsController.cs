using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Models.Dto;
using WebBackendProject.Services;

namespace WebBackendProject.Controllers
{
    [Route("api/SchoolsController")]
    public class SchoolsController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private  ClassesSubjectsServices _classesSubjectsServices;
        private DeleteServices _deleteServices;
        public SchoolsController(ApplicationDbContext context)
        {
            _context = context;
            _classesSubjectsServices = new ClassesSubjectsServices(context);
            _deleteServices = new DeleteServices(context);
        }

        [HttpPost("AddSchoolData")]
            public async Task<IActionResult> AddSchoolData([FromForm] SchoolCreateDto schoolData)
            {
                try
                {
                    if (schoolData == null)
                        return BadRequest(new { error = "Invalid school data." });

                    string? imagePath = null;
                const long maxFileSize = 2 * 1024 * 1024; // 2MB in bytes

                if (schoolData.profilePicture.Length > maxFileSize)
                {
                    // File is too large
                    return BadRequest("The uploaded profile picture must not exceed 2MB in size.");
                }
                if (schoolData.profilePicture != null && schoolData.profilePicture.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/principalPics");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = $"{Guid.NewGuid()}_{schoolData.profilePicture.FileName}";
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await schoolData.profilePicture.CopyToAsync(stream);
                        }
                        imagePath = $"/images/principalPics/{uniqueFileName}";
                    }

                    var newSchool = new schools
                    {
                        school_name = schoolData.schoolName,
                        principal_name = schoolData.principalname,
                        username = schoolData.username,
                        password = PasswordHasher.HashPassword(schoolData.password),
                        address = schoolData.address,
                        contact_no = schoolData.ContactNo,
                        max_class = schoolData.MaxClass,
                        status = "active",
                        created_at = DateTime.Now,
                        gender = schoolData.gender,
                        principal_image_path = imagePath,
                        created_by = schoolData.createdby
                    };

                    _context.Schools.Add(newSchool);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "School added successfully!", imageUrl = imagePath });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Something went wrong.", details = ex.Message });
                }
    
        }

         public class SchoolCreateDto
            {
            public string schoolName { get; set; }
            public string principalname { get; set; }
            public string contactNo { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string address { get; set; }
            public string ContactNo { get; set; }
            public string? MaxClass { get; set; }
            public int gender { get; set; }
            public int createdby { get; set; }
            public IFormFile? profilePicture { get; set; }
         }

        [HttpPost("CheckSchoolLogin")]
        public async Task<IActionResult> CheckSchoolCredentials([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required." });
            }

            var school = await _context.Schools
                .FirstOrDefaultAsync(s => s.username == loginRequest.Username);

            if (school == null)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }

            bool isPasswordCorrect = PasswordHasher.VerifyPassword(loginRequest.Password, school.password);

            if (!isPasswordCorrect)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }
            if (!string.Equals(school.status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { success = false, message = "Account is inactive. Please contact administration." });
            }

            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string? fullImagePath = !string.IsNullOrEmpty(school.principal_image_path)
                                    ? $"{baseUrl}{school.principal_image_path}"
                                    : null;

            return Ok(new
            {
                success = true,
                message = "Login successful.",
                schoolName = school.school_name,
                schoolid = school.id,
                max_class = school.max_class,
                imgPath = fullImagePath
            });
        }

        [HttpGet("GetAllSchools")]
        public async Task<IActionResult> GetAllSchools([FromQuery] int page = 1)
        {
           int totalRecords = _context.Schools.Count();//3
           int pageSize = 15; 
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            int currentPage = page;

            if (currentPage < 1)
                currentPage = totalPages;
            else if (currentPage > totalPages)
                currentPage = 1;

            var schools = await _context.Schools
            .OrderBy(s => s.id) // Always order before skipping
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            
            .Select(s => new {
             s.id,
             s.school_name,
             s.principal_name,
             s.address,
             s.contact_no,
             s.status
     })
     .ToListAsync();

            return Ok(new
            {
                totalPages,
                currentPage,
                data = schools
            });
        }

        
      
        

        [HttpDelete("DeleteSchool")]
        public async Task<IActionResult> DeleteSchool([FromQuery] int SchoolId)
        {

            await _deleteServices.DeleteStudents(SchoolId, null);
            await _deleteServices.DeleteEdutube(SchoolId, null);
            await _deleteServices.DeleteTeacherContantFiles(SchoolId, "pdf");
            await _deleteServices.DeleteTeacherContantFiles(SchoolId, "video");
            await _deleteServices.DeleteTeacherContantFolder(SchoolId);
            await _deleteServices.DeleteTeacher(SchoolId, null);
            await _deleteServices.DeleteEdutechContantFiles(SchoolId, "pdf");
            await _deleteServices.DeleteEdutechContantFiles(SchoolId, "flash");
            await _deleteServices.DeleteEdutechContantFiles(SchoolId, "video");

            await _deleteServices.DeleteEdutechContantFolder(SchoolId);


            var school = await _context.Schools.FindAsync(SchoolId);

            if (school == null)
            {
                return NotFound(new { success = false, message = "Student not found" });
            }

            // 🔹 Retrieve image path from database (assuming student has an ImagePath column)
            string imagePath = school.principal_image_path;

            // 🔹 Delete image file if it exists
            if (!string.IsNullOrEmpty(imagePath))
            {
                string fullImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath);
                if (System.IO.File.Exists(fullImagePath))
                {
                    System.IO.File.Delete(fullImagePath);
                }
            }
            // 🔹 Remove student from database
            _context.Schools.Remove(school);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Student deleted successfully" });
        }


        [HttpPut("UpdateSchoolStatus")]
        public async Task<IActionResult> UpdateSchoolStatus([FromBody] StatusUpdateModel statusUpdateModel)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Update the school's status
                var school = await _context.Schools.FirstOrDefaultAsync(s => s.id == statusUpdateModel.id);
                if (school == null)
                    return NotFound(new { success = false, message = "School not found" });

                school.status = statusUpdateModel.status;
                await _context.SaveChangesAsync();

                // 2. Bulk update all students of this school
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE students SET status = {0} WHERE school_id = {1}",
                    statusUpdateModel.status, statusUpdateModel.id
                );

                // 3. Bulk update all teachers of this school
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE teachers SET status = {0} WHERE school_id = {1}",
                    statusUpdateModel.status, statusUpdateModel.id
                );

                // 4. Commit transaction
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "School, students, and teachers status updated successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Error occurred while updating status", error = ex.Message });
            }
        }

    
    }

}
