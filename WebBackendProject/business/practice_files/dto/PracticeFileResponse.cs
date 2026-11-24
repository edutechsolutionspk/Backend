using System.Collections.Generic;

namespace WebBackendProject.business.practice_files.dto;

public record PracticeFileResponse(int statusCode, string msg, List<PracticeFilesDto> practiceFiles);
