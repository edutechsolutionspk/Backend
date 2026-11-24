using WebBackendProject.business.assessment.dtos;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Other;

namespace WebBackendProject.business.assessment.services
{
    public class AddAssessmentService
    {
        private ApplicationDbContext _context;

        public AddAssessmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CreateAssessmentFileResponse> AddFile(CreateAssessmentFileDto request)
        {
            _ = 1;
            try
            {
                string uploadFolderPath = Path.Combine("wwwroot", "uploadedfiles", "assessmentfiles");
                string fileid = request.fileid;
                if (request.file == null || request.file.Length == 0L)
                {
                    return new CreateAssessmentFileResponse(400, "File not exist", null);
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
                    return new CreateAssessmentFileResponse(200, "File Uploaded", fileid);
                }
                return await DirectSaveFile(uploadFolderPath, request);
            }
            catch (Exception)
            {
                return new CreateAssessmentFileResponse(500, "server Error", null);
            }
        }

        private void GetMappedModel(CreateAssessmentFileDto request, string finalPath, string iv, string key)
        {
            string relativeFilePath = finalPath.Substring(8);
            AssessmentFiles assessmentFile = new AssessmentFiles
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
            _context.assessmentFiles.Add(assessmentFile);
            _context.SaveChanges();
        }

        internal async Task<string> CreateChuckFiles(CreateAssessmentFileDto request, string path, string fileID)
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

        private async Task<CreateAssessmentFileResponse> DirectSaveFile(string path, CreateAssessmentFileDto request)
        {
            string finalFilePath = Path.Combine(path, GenerateUUID());
            using (FileStream stream = new FileStream(finalFilePath, FileMode.Create))
            {
                await request.file.CopyToAsync(stream);
            }
            await EncrptFile(finalFilePath, request);
            return new CreateAssessmentFileResponse(200, "File uploaded directly", null);
        }

        private async Task<string> EncrptFile(string finalFilePath, CreateAssessmentFileDto request)
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
}
