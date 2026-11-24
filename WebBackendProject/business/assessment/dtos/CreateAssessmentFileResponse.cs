namespace WebBackendProject.business.assessment.dtos
{
    public record CreateAssessmentFileResponse (
    int statusCode,
    string msg,
    string? fileId
    );
}
