using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;

namespace WebBackendProject.Services
{
    public class ClassesSubjectsServices
    {
        ApplicationDbContext _context;
        public ClassesSubjectsServices(ApplicationDbContext context)
        {
            _context = context;

        }

        public async Task<int?> GetStudentClassId(int? id)
        {
            return await _context.Students
                .Where(s => s.id == id)
                .Select(s => (int?)s.class_id)
                .FirstOrDefaultAsync();
        }

        public async Task<int?> GetTeacherSchool(int? id)
        {
            return await _context.Teachers
                .Where(s => s.id == id)
                .Select(s => (int?)s.school_id)
                .FirstOrDefaultAsync();
        }
        public async Task<int> GetMaxClass(int schoolid)
        {
          var type=   await _context.Schools
                .Where(s => s.id == schoolid)
                .Select(s => s.max_class)
                .FirstOrDefaultAsync();
            int value = 0;
            switch (type)
            {
                case "Pre-Elementary":
                    value = 3;
                    break;
                case "Elementary":
                    value = 11;
                    break;
                case "Secondary":
                    value = 13;
                    break;
            }
            return value;

        }


    }

}
