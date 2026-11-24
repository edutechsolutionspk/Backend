using Microsoft.EntityFrameworkCore;
using WebBackendProject.business.assessment.dtos;
using WebBackendProject.Data;

namespace WebBackendProject.business.assessment.services
{
    public class ListAssessmentService
    {
        private ApplicationDbContext _dbContext;

        public ListAssessmentService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AssessmentFileResponse> AssessmentFilesList(AssessmentFileRequest request)
        {
            return new AssessmentFileResponse(200, "List", (await (from s in _dbContext.assessmentFiles
                                                                 where s.class_id == request.classId && s.school_id == request.schoolId && s.subject_id == request.SubjectId
                                                                 select new AssessmentFileDto
                                                                 {
                                                                     id = s.id,
                                                                     name = s.file_name
                                                                 }).ToListAsync()) ?? new List<AssessmentFileDto>());
        }
    }
}
