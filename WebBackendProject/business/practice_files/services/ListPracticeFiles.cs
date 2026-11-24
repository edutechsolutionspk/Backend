using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.business.practice_files.dto;
using WebBackendProject.Data;
using WebBackendProject.Models;

namespace WebBackendProject.business.practice_files.services;

public class ListPracticeFiles
{
	private ApplicationDbContext _dbContext;

	public ListPracticeFiles(ApplicationDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PracticeFileResponse> PracticeFilesList(PracticeFileRequest request)
	{
		return new PracticeFileResponse(200, "List", (await (from s in _dbContext.practiceFiles
			where s.class_id == request.classId && s.school_id == request.schoolId && s.subject_id == request.SubjectId
			select new PracticeFilesDto
			{
				id = s.id,
				name = s.file_name
			}).ToListAsync()) ?? new List<PracticeFilesDto>());
	}
}
