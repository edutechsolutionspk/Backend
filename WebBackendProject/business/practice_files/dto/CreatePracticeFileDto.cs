using Microsoft.AspNetCore.Http;

namespace WebBackendProject.business.practice_files.dto;

public record CreatePracticeFileDto(int classId, int subjectId, int schoolId, IFormFile file, string fileName, int? currentChuck, int? totalChucks, string? fileid, int masterId);
