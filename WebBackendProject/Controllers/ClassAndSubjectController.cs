using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;
using WebBackendProject.Other;
using WebBackendProject.Services;

namespace WebBackendProject.Controllers
{
    [Route("api/ClassAndSubjectController")]
    public class ClassAndSubjectController : Controller
    {
        ApplicationDbContext _context;
        FolderAndFilesHandler _handler;
        ClassesSubjectsServices _classesSubjectsServices;
        public ClassAndSubjectController(ApplicationDbContext context)
        {
            _context = context;
            _handler = new FolderAndFilesHandler(context);
            _classesSubjectsServices = new ClassesSubjectsServices(context);
        }

        [HttpGet("AllSchools")]
        public async Task<IActionResult> AllSchools()
        {

            var schools = await _context.Schools
            .OrderBy(s => s.id) // Always order before skipping
            .Select(s => new {
                s.id,
                s.school_name,

            }).ToListAsync();

            return Ok(schools);
        }


        [HttpGet("GetMaxClass")]
        public async Task<IActionResult> GetClassesByType(int type)
        {

            int maxClass = await _classesSubjectsServices.GetMaxClass(type);

            if (maxClass == 0)
            {
                return BadRequest(new { success = false, message = "Invalid class type" });
            }

            var classes = await _context.Classes
                .Where(c => c.ClassID <= maxClass)
                .ToListAsync();

            return Ok(new { success = true, data = classes });
        }
        [HttpGet("getSchoolSubjects/{classid}")]
        public async Task<IActionResult> GetButtons(int? classid)
        {
            // Step 1: Check if classid is provided, if not, fetch ClassID for the user
            
            // Step 2: Get Subjects for the user's ClassID
            var buttons = await _context.ClassSubjects
                .Where(cs => cs.ClassID == classid)  // classid is nullable, this will work fine
                .Join(_context.subjects.AsNoTracking(),
                    cs => cs.SubjectID,
                    s => s.SubjectID,
                    (cs, s) => new
                    {
                        id = s.SubjectID,
                        text = s.SubjectName
                    })
                .ToListAsync();

            return Ok(buttons);
        }
        [HttpGet("getUserSubjects")]
        public async Task<IActionResult> GetUserSubject(int schoolid)
        {
            // Step 1: Check if classid is provided, if not, fetch ClassID for the user
           
            var  classid = await _classesSubjectsServices.GetStudentClassId(schoolid);
            

            // Step 2: Get Subjects for the user's ClassID
            var buttons = await _context.ClassSubjects
                .Where(cs => cs.ClassID == classid)  // classid is nullable, this will work fine
                .Join(_context.subjects.AsNoTracking(),
                    cs => cs.SubjectID,
                    s => s.SubjectID,
                    (cs, s) => new
                    {
                        id = s.SubjectID,
                        text = s.SubjectName
                    })
                .ToListAsync();

            return Ok(buttons);
        }
        //for teachers  
        [HttpGet("teacher-classes/{teacherId}")]
        public IActionResult GetClassesByTeacher(int teacherId)
        {
            var classes = _context.Teacher_Subject_Classes
                .AsNoTracking()
                .Where(tcs => tcs.teacher_id == teacherId)
                .Join(_context.Classes.AsNoTracking(),
                      tcs => tcs.class_id,
                      cls => cls.ClassID,
                      (tcs, cls) => new
                      {
                          ClassID = cls.ClassID,
                          ClassName = cls.ClassName
                      })
                .Distinct()
                .ToList();

            return Ok(classes);
        }
        [HttpGet("teacher-subjects/{teacherId}/class/{classId}")]
        public IActionResult GetSubjectsByTeacherAndClass(int teacherId, int classId)
        {
            var subjects = (from tcs in _context.Teacher_Subject_Classes
                            join sub in _context.subjects on tcs.subject_id equals sub.SubjectID
                            where tcs.teacher_id == teacherId && tcs.class_id == classId
                            select new
                            {
                                SubjectID = sub.SubjectID,
                                SubjectName = sub.SubjectName
                            }).Distinct().ToList();

            return Ok(subjects);
        }


        // for teacher , school.master
        [HttpGet("get-category/{schoolid}")]
        public IActionResult GetCategory(int schoolid)
        {
            var maxClass = _context.Schools
                .Where(sc => sc.id == schoolid)
                .Select(sc => sc.max_class)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(maxClass))
                return NotFound("School not found or max_class is empty");

            // Define full hierarchy
            var categoryLevels = new List<string> { "Pre-Elementary", "Elementary", "Secondary" };

            // Get index of maxClass in hierarchy
            int maxIndex = categoryLevels.IndexOf(maxClass);

            if (maxIndex == -1)
                return BadRequest("Invalid max_class value");

            // Take only levels up to the defined max class
            var availableCategories = categoryLevels
                .Take(maxIndex + 1)
                .Select(cat => new
                {
                    value = cat,
                    text = cat
                }).ToList();

            return Ok(availableCategories);
        }


    }
}
