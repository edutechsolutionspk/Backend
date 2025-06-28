using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Models.Dto;
using WebBackendProject.Services;

namespace WebBackendProject.Controllers
{
    [Route("api/TeachersController")]
    public class TeachersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        ClassesSubjectsServices ClassesSubjectsServices;
        DeleteServices _deleteServices;
        public TeachersController(ApplicationDbContext context)
        {
            _context = context;
            ClassesSubjectsServices = new ClassesSubjectsServices(context);
        _deleteServices = new DeleteServices(context);
        }

        [HttpPost("CreateTeacher")]
        public async Task<IActionResult> CreateTeacher([FromForm] TeacherCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
           
            const long maxFileSize = 2 * 1024 * 1024; // 2MB in bytes

            if (model.profilePicture.Length > maxFileSize)
            {
                // File is too large
                return BadRequest("The uploaded profile picture must not exceed 2MB in size.");
            }
            string? imagePath = null;


            if (model.profilePicture != null && model.profilePicture.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/teacherPics");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);

                }

                string uniqueFileName = $"{Guid.NewGuid()}_{model.profilePicture.FileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.profilePicture.CopyToAsync(stream);
                }

                imagePath = $"/images/teacherPics/{uniqueFileName}";
            }
            string hashPassword = PasswordHasher.HashPassword(model.Password);
            // Save teacher info
            var teacher = new teachers
            {
                full_name = model.FullName,
                school_id = model.SchoolId,
                qualification = model.Qualification,
                experience = model.Experience,
                designation = model.Designation,
                status = "active",
                username = model.Username,
                email = model.Email,
                password = hashPassword, // TODO: Hash this
                profile_picture_path = imagePath,
                created_at = DateTime.UtcNow,
                created_by =  model.createdby,// Example admin ID
               contact = model.contactNumber
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Save subject-class associations
            foreach (var subjectPair in model.SubjectIds)
            {
                var splitPair = subjectPair.Split('-'); // "1-1" -> ["1", "1"]
                int classId = int.Parse(splitPair[0]);  // First element is classId
                int subjectId = int.Parse(splitPair[1]);  // Second element is subjectId

                var assignment = new teacher_subject_classes
                {
                    teacher_id = teacher.id, // Use the teacher's ID
                    subject_id = subjectId,
                    class_id = classId
                };
                _context.Teacher_Subject_Classes.Add(assignment);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Teacher created successfully", teacherId = teacher.id });
        }

        [HttpGet("class-subject-combinations")]
        public async Task<IActionResult> GetClassSubjectCombinations(int schoolid,int page = 1, int pageSize = 20)
        {
            int? maxClassId = await ClassesSubjectsServices.GetMaxClass(schoolid);
            var query = (
                from cs in _context.ClassSubjects
                join c in _context.Classes on cs.ClassID equals c.ClassID
                join s in _context.subjects on cs.SubjectID equals s.SubjectID
                where cs.ClassID <= maxClassId
                orderby cs.ClassID
                select new
                {
                    ClassSubjectId = cs.ClassSubjectID,
                    ClassId = cs.ClassID,
                    ClassName = c.ClassName,
                    SubjectId = cs.SubjectID,
                    SubjectName = s.SubjectName
                }
            );

            // Paginate the results
            var pagedClassSubjects = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(pagedClassSubjects);
        }
        [HttpPost("CheckTeacherId")]
        public async Task<IActionResult> CheckCredentials([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required." });
            }

            var teacher = await _context.Teachers
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
            if (!string.Equals(teacher.status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { success = false, message = "Account is inactive. Please contact administration." });
            }
            // Construct full image URL only if both username and password are correct
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string? fullImagePath = !string.IsNullOrEmpty(teacher.profile_picture_path) ? $"{baseUrl}{teacher.profile_picture_path}" : null;

            return Ok(new
            {
                success = true,
                message = "Login successful.",
                id = teacher.id,
                name = teacher.full_name,
                schoolid = teacher.school_id,
                
            });
        }
        
         
        [HttpPost("GetAllTeachers")]
        public async Task<IActionResult> GetAllTeachers([FromBody] SchoolRequest request, [FromQuery] int page =1)
        {
            try
            { 
                var baseQuery = _context.Teachers
                 .AsNoTracking()
                 .Where(t => t.school_id == request.SchoolId)
                 .Join(_context.Schools,
                  t => t.school_id,
                  s => s.id,
                  (t, s) => new {
                   id = t.id,
                   Name = t.full_name,
                   schoolname = s.school_name,
                   contact = s.contact_no,
                   status = s.status
                   });
                var totalRecords = await baseQuery.CountAsync();
                int pageSize = 15;
                int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                int currentPage = page;

                if (currentPage < 1)
                    currentPage = totalPages;
                else if (currentPage > totalPages)
                    currentPage = 1;
                
                var teachers = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                if (teachers == null )
                {
                    return NotFound("No teachers found for this school.");
                }

                return Ok(new
                {
                    totalPages,
                    currentPage,
                    data = teachers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("GetTeacherInfo")]
        public async Task<IActionResult> GetInfo([FromBody] int id)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Get basic teacher info
            var teacher = await (
                from t in _context.Teachers.AsNoTracking()
                join school in _context.Schools on t.school_id equals school.id into ss
                from school in ss.DefaultIfEmpty()
                where t.id == id
                select new
                {
                    Name = t.full_name,
                    SchoolName = school.school_name,
                    TeacherPic = string.IsNullOrEmpty(t.profile_picture_path) ? null : $"{baseUrl}{t.profile_picture_path}"
                }
            ).FirstOrDefaultAsync();

            if (teacher == null)
            {
                return NotFound(new { message = "Teacher not found." });
            }

            // Get class/subject data
            var flatData = await (from tcs in _context.Teacher_Subject_Classes
                                  join cls in _context.Classes on tcs.class_id equals cls.ClassID
                                  join sub in _context.subjects on tcs.subject_id equals sub.SubjectID
                                  where tcs.teacher_id == id
                                  select new
                                  {
                                      ClassName = cls.ClassName,
                                      SubjectName = sub.SubjectName
                                  }).ToListAsync();

            var grouped = flatData
                .GroupBy(x => x.ClassName)
                .Select(g => new ClassWithSubjectsDto
                {
                    ClassName = g.Key,
                    Subjects = g.Select(x => x.SubjectName).Distinct().ToList()
                })
                .ToList();

            // Combine into a single DTO
            var result = new TeacherDetailDto
            {
                Name = teacher.Name,
                SchoolName = teacher.SchoolName,
                TeacherPic = teacher.TeacherPic,
                Classes = grouped
            };

            return Ok(result);
        }

        [HttpDelete("DeleteTeacher")]
        public async Task<IActionResult> DeleteTeacher([FromQuery] int TeacherId)
        {
          
         await   _deleteServices.DeleteTeacher(null, TeacherId);
            return Ok(new { success = true, message = "Student deleted successfully" });
    
        }
    }

    public class TeacherCreateViewModel
    {
        public string FullName { get; set; }
        public int SchoolId { get; set; }
        public string Qualification { get; set; }
        public int Experience { get; set; }
        public string Designation { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        // MULTIPLE SUBJECTS
        public List<string> SubjectIds { get; set; }
        public long contactNumber { get; set; }
        public int createdby { get; set; }
        public IFormFile? profilePicture { get; set; }

    }
    public class SchoolRequest
    {
        public int SchoolId { get; set; }
    }
    public class ClassWithSubjectsDto
    {
        public string ClassName { get; set; }
        
        public List<string> Subjects { get; set; }
    }
    public class TeacherDetailDto
    {
        public string Name { get; set; }
        public string SchoolName { get; set; }
        public string TeacherPic { get; set; }
        public List<ClassWithSubjectsDto> Classes { get; set; }
    }
}
