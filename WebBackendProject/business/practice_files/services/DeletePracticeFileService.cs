using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.business.practice_files.dto;
using WebBackendProject.Data;
using WebBackendProject.Models;

namespace WebBackendProject.business.practice_files.services;

public class DeletePracticeFileService
{
	private ApplicationDbContext _dbContext;

	public DeletePracticeFileService(ApplicationDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<DeletePracticeResponse> DelelteFile(int id)
	{
		PracticeFiles file = await _dbContext.practiceFiles.Where((PracticeFiles s) => s.id == id).FirstOrDefaultAsync();
		if (file == null)
		{
			return new DeletePracticeResponse(404, "File not found");
		}
		string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.file_path);
		if (File.Exists(fullPath))
		{
			File.Delete(fullPath);
		}
		_dbContext.practiceFiles.Remove(file);
		await _dbContext.SaveChangesAsync();
		return new DeletePracticeResponse(200, "File Deleted");
	}
}
