using WebBackendProject.business.practice_files.dto;

namespace WebBackendProject.business.assessment.dtos
{
    public record AssessmentFileResponse(int statusCode, string msg, List<AssessmentFileDto> assessmentFiles);
}
