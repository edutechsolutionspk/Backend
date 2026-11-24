using System;
using System.IO;
using System.Threading.Tasks;
using WebBackendProject.business.practice_files.dto;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Other;

namespace WebBackendProject.business.practice_files.services;

public class AddPracticeFilesService
{
	private ApplicationDbContext _context;

	public AddPracticeFilesService(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<CreatePracticeFilesResponse> AddFile(CreatePracticeFileDto request)
	{
		_ = 1;
		try
		{
			string uploadFolderPath = Path.Combine("wwwroot", "uploadedfiles", "practicefiles");
			string fileid = request.fileid;
			if (request.file == null || request.file.Length == 0L)
			{
				return new CreatePracticeFilesResponse(400, "File not exist", null);
			}
			if (!Directory.Exists(uploadFolderPath))
			{
				Directory.CreateDirectory(uploadFolderPath);
			}
			if (request.currentChuck.HasValue)
			{
				if (fileid == null)
				{
					fileid = GenerateUUID();
				}
				await CreateChuckFiles(request, uploadFolderPath, fileid);
				return new CreatePracticeFilesResponse(200, "File Uploaded", fileid);
			}
			return await DirectSaveFile(uploadFolderPath, request);
		}
		catch (Exception)
		{
			return new CreatePracticeFilesResponse(500, "server Error", null);
		}
	}

	private void GetMappedModel(CreatePracticeFileDto request, string finalPath, string iv, string key)
	{
		string relativeFilePath = finalPath.Substring(8);
		PracticeFiles parcticeFile = new PracticeFiles
		{
			file_name = request.fileName,
			class_id = request.classId,
			school_id = request.schoolId,
			file_path = relativeFilePath,
			subject_id = request.subjectId,
			encryption_key = key,
			iv = iv,
			uploaded_by_master = request.masterId,
			upload_date = DateTime.Now
		};
		_context.practiceFiles.Add(parcticeFile);
		_context.SaveChanges();
	}

	internal async Task<string> CreateChuckFiles(CreatePracticeFileDto request, string path, string fileID)
	{
		string tempPath = Path.Combine(path, fileID);
		if (!Directory.Exists(tempPath))
		{
			Directory.CreateDirectory(tempPath);
		}
		if (request.currentChuck >= request.totalChucks)
		{
			await EncrptFile(await MergeFile(tempPath, request.totalChucks.Value, path), request);
			return fileID;
		}
		string chunkPath = Path.Combine(tempPath, request.currentChuck.ToString());
		using (FileStream fileStream = new FileStream(chunkPath, FileMode.Create))
		{
			await request.file.CopyToAsync(fileStream);
		}
		return fileID;
	}

	internal async Task<string> MergeFile(string tempPath, int totalChunks, string uploadFolderPath)
	{
		string filename = GenerateUUID();
		string finalFilePath = Path.Combine(uploadFolderPath, filename);
		using (FileStream finalStream = new FileStream(finalFilePath, FileMode.Create))
		{
			for (int i = 0; i < totalChunks; i++)
			{
				string partPath = Path.Combine(tempPath, i.ToString());
				using FileStream partStream = new FileStream(partPath, FileMode.Open);
				await partStream.CopyToAsync(finalStream);
			}
		}
		if (Directory.Exists(tempPath))
		{
			Directory.Delete(tempPath, recursive: true);
		}
		return finalFilePath;
	}

	private async Task<CreatePracticeFilesResponse> DirectSaveFile(string path, CreatePracticeFileDto  request)
	{
		string finalFilePath = Path.Combine(path, GenerateUUID());
		using (FileStream stream = new FileStream(finalFilePath, FileMode.Create))
		{
			await request.file.CopyToAsync(stream);
		}
		await EncrptFile(finalFilePath, request);
		return new CreatePracticeFilesResponse(200, "File uploaded directly", null);
	}

	private async Task<string> EncrptFile(string finalFilePath, CreatePracticeFileDto request)
	{
		string encryptionKey = FileEncryptionHelper.GenerateRandomKey();
		string iv = FileEncryptionHelper.GenerateRandomIV();
		using (new FileStream(finalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
		}
		FileEncryptionHelper.EncryptFile(finalFilePath, finalFilePath, encryptionKey, iv);
		GetMappedModel(request, finalFilePath, iv, encryptionKey);
		return finalFilePath;
	}

	private string GenerateUUID()
	{
		return Guid.NewGuid().ToString();
	}
}
