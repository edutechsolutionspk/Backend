using Microsoft.EntityFrameworkCore;
using WebBackendProject.business.assessment.dtos;
using WebBackendProject.Data;
using WebBackendProject.Models;

namespace WebBackendProject.business.assessment.services
{
    public class DeleteAssessmentService
    {
        private ApplicationDbContext _dbContext;

        public DeleteAssessmentService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DeleteAssessmentFileResponse> DeleteFile(int id)
        {
            AssessmentFiles file = await _dbContext.assessmentFiles.Where((AssessmentFiles s) => s.id == id).FirstOrDefaultAsync();
            if (file == null)
            {
                return new DeleteAssessmentFileResponse(404, "File not found");
            }
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.file_path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            _dbContext.assessmentFiles.Remove(file);
            await _dbContext.SaveChangesAsync();
            return new DeleteAssessmentFileResponse(200, "File Deleted");
        }
    }
}
