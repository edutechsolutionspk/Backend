namespace WebBackendProject.business.assessment.dtos
{
    public record CreateAssessmentFileDto(
        int classId,
        int subjectId,
        int schoolId,
        IFormFile file,
        string fileName,
        int? currentChuck,
        int? totalChucks,
        string? fileid,
        int masterId
    );
}
