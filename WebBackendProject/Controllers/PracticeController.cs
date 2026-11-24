using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.business.practice_files.dto;
using WebBackendProject.business.practice_files.services;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Other;

namespace WebBackendProject.business.practice_files;

[Route("api/[controller]")]
[ApiController]
public class PracticeController : ControllerBase
{
	public AddPracticeFilesService _addPracticeFilesService;

	private readonly FileEncryptionHelper fileEncryptionHelper = new FileEncryptionHelper();

	private readonly ApplicationDbContext _context;

	public readonly ListPracticeFiles _listPracticeFiles;

	public DeletePracticeFileService _delete;

	public PracticeController(AddPracticeFilesService addPracticeFilesService, ListPracticeFiles listPracticeFiles, DeletePracticeFileService delete, ApplicationDbContext context)
	{
		_addPracticeFilesService = addPracticeFilesService;
		_context = context;
		_listPracticeFiles = listPracticeFiles;
		_delete = delete;
	}

	[HttpPost("file")]
	public async Task<IActionResult> UploadFile([FromForm] CreatePracticeFileDto request)
	{
		CreatePracticeFilesResponse result = await _addPracticeFilesService.AddFile(request);
		int statusCode = result.statusCode;
		if (statusCode == 200)
		{
			return Ok(result);
		}
		return BadRequest(result);
	}

	[HttpGet("file/{fileId}")]
	public async Task<IActionResult> ViewPracticeFile(int fileId)
	{
		var fileRecord = await (from f in _context.practiceFiles
			where f.id == fileId
			select new { f.file_path, f.encryption_key, f.iv }).FirstOrDefaultAsync();
		if (fileRecord == null)
		{
			return NotFound("File not found.");
		}
		string encryptedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileRecord.file_path);
		if (!System.IO.File.Exists(encryptedFilePath))
		{
			return NotFound("File does not exist.");
		}
		byte[] key = Convert.FromBase64String(fileRecord.encryption_key);
		byte[] iv = Convert.FromBase64String(fileRecord.iv);
		Stream decryptedStream = fileEncryptionHelper.DecryptFileStream(encryptedFilePath, key, iv);
		Response.Headers["Content-Disposition"] = "inline";
		Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
		Response.Headers["Pragma"] = "no-cache";
		Response.Headers["Expires"] = "0";
		return new FileStreamResult(decryptedStream, "text/html");
	}

	[HttpPost("files")]
	public async Task<IActionResult> AllFiles([FromBody] PracticeFileRequest request)
	{
		PracticeFileResponse result = await _listPracticeFiles.PracticeFilesList(request);
		int statusCode = result.statusCode;
		if (statusCode == 200)
		{
			return Ok(result);
		}
		return BadRequest(result);
	}

	[HttpDelete("file")]
	public async Task<IActionResult> DeleteFile([FromQuery] int id)
	{
		DeletePracticeResponse result = await _delete.DelelteFile(id);
		int statusCode = result.statusCode;
		if (statusCode == 200)
		{
			return Ok(result);
		}
		return BadRequest(result);
	}
}
