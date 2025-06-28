using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;


namespace WebBackendProject.Services
{
    public class DeleteServices
    {
        ApplicationDbContext _context;
        public DeleteServices(ApplicationDbContext context)
        {
            _context = context;

        }

    public async Task<bool> DeleteStudents(int? schoolId, int? studentId)
        {
            List<int> studentIds;
            List<(int Id, string PhotoPath)> studentsToDelete;

            // 1. Determine which students to delete
            if (schoolId != null)
            {
                studentsToDelete = await _context.Students
                    .Where(s => s.school_id == schoolId)
                    .Select(s => new { s.id, s.picture_path })
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(s => (s.id, s.picture_path)).ToList());
            }
            else if (studentId != null)
            {
                studentsToDelete = await _context.Students
                    .Where(s => s.id == studentId)
                    .Select(s => new { s.id, s.picture_path })
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(s => (s.id, s.picture_path)).ToList());
            }
            else
            {
                return false; // nothing to delete
            }

            studentIds = studentsToDelete.Select(s => s.Id).ToList();
            if (studentIds.Count == 0)
                return false;

            // 2. Delete from DB using ExecuteDeleteAsync
            await _context.Students
                .Where(s => studentIds.Contains(s.id))
                .ExecuteDeleteAsync();

            // 3. Delete from disk in parallel
            await Task.WhenAll(studentsToDelete.Select(s => Task.Run(() =>
            {
                string fullImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", s.PhotoPath);

                if (File.Exists(fullImagePath))
                {

                    File.Delete(fullImagePath);
                }
            })));
            return true;
        }

    public async Task<bool> DeleteTeacher(int? schoolId, int? teacherId)
        {
            List<int> teacherIds;
            List<(int Id, string PhotoPath)> studentsToDelete;

            // 1. Determine which students to delete
            if (schoolId != null)
            {
                studentsToDelete = await _context.Teachers
                    .Where(s => s.school_id == schoolId)
                    .Select(s => new { s.id, s.profile_picture_path})
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(s => (s.id, s.profile_picture_path)).ToList());
            }
            else if (teacherId != null)
            {
                studentsToDelete = await _context.Teachers
                    .Where(s => s.id == teacherId)
                    .Select(s => new { s.id, s.profile_picture_path })
                    .ToListAsync()
                    .ContinueWith(t => t.Result.Select(s => (s.id,s.profile_picture_path)).ToList());
            }
            else
            {
                return false; // nothing to delete
            }

            teacherIds = studentsToDelete.Select(s => s.Id).ToList();

            if (teacherIds.Count == 0)
                return false;
            await _context.Teacher_Subject_Classes
      .Where(s => teacherIds.Contains(s.teacher_id))
      .ExecuteDeleteAsync();
            // 2. Delete from DB using ExecuteDeleteAsync
            await _context.Teachers
                .Where(s => teacherIds.Contains(s.id))
                .ExecuteDeleteAsync();

            // 3. Delete from disk in parallel
            await Task.WhenAll(studentsToDelete.Select(s => Task.Run(() =>
            {
                string fullImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", s.PhotoPath);

                if (File.Exists(fullImagePath))
                {
                  
                    File.Delete(fullImagePath);
                }
            })));

            return true;
        }
    
    public async Task<bool> DeleteEdutube(int? schoolId,int? edutubeId)
        {
            if (schoolId != null)
            {
                await _context.Edutube
                    .Where(s => s.school_id == schoolId)
                    .ExecuteDeleteAsync();
                return true;
            }
            else if (edutubeId != null)
            {
                await _context.Edutube
                    .Where(s => s.id == edutubeId)
                    .ExecuteDeleteAsync();
                return true;
            }

            return false;
        }
    
    
    public async Task<bool> DeleteEdutechContantFiles(int schoolid,string module){
            List<(int Id, string filePath)> allFilesToDelete =null;

            if (module == "pdf")
            {
                allFilesToDelete = await _context.pdffiles
            .Where(s => s.school_id == schoolid)
            .Select(s => new ValueTuple<int, string>(s.Id, s.FilePath))
            .ToListAsync();
                if (allFilesToDelete == null || allFilesToDelete.Count == 0)
                    return false;
                await _context.pdffiles
                    .Where(s => allFilesToDelete.Select(f => f.Id).Contains(s.Id))
                    .ExecuteDeleteAsync();
            }
            else if (module =="flash")
            {
                allFilesToDelete = await _context.flashFiles
             .Where(s => s.school_id == schoolid)
             .Select(s => new ValueTuple<int, string>(s.Id, s.FilePath))
             .ToListAsync();
                if (allFilesToDelete == null || allFilesToDelete.Count == 0)
                    return false;
                await _context.flashFiles
                    .Where(s => allFilesToDelete.Select(f => f.Id).Contains(s.Id))
                    .ExecuteDeleteAsync();

            }
            else if (module == "video")
            {
                allFilesToDelete = await _context.VideoFiles
             .Where(s => s.school_id == schoolid)
             .Select(s => new ValueTuple<int, string>(s.Id, s.FilePath))
             .ToListAsync();
                if (allFilesToDelete == null || allFilesToDelete.Count == 0)
                    return false;
                await _context.VideoFiles
                    .Where(s => allFilesToDelete.Select(f => f.Id).Contains(s.Id))
                    .ExecuteDeleteAsync();
            }
            else
            {
                return false; // nothing to delete
            }

            string root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            await Task.WhenAll(allFilesToDelete.Select(s => Task.Run(() =>
            {
                string fullFilePath = Path.Combine(root, s.filePath);
                if (File.Exists(fullFilePath))
                {
                  
                    File.Delete(fullFilePath);
                }
            })));

            return true;
        
        }

    public async Task<bool> DeleteTeacherContantFiles(int schoolid, string module)
        {
            List<(int Id, string filePath)> allFilesToDelete = null;

            if (module == "pdf")
            {
                allFilesToDelete = await _context.Teacherpdf
            .Where(s => s.school_id == schoolid)
            .Select(s => new ValueTuple<int, string>(s.Id, s.FilePath))
            .ToListAsync();
                if (allFilesToDelete == null || allFilesToDelete.Count == 0)
                    return false;
                await _context.Teacherpdf
                     .Where(s => allFilesToDelete.Select(f => f.Id).Contains(s.Id))
                     .ExecuteDeleteAsync();
            }
            else if (module == "video")
            {
                allFilesToDelete = await _context.Teachervideos
             .Where(s => s.school_id == schoolid)
             .Select(s => new ValueTuple<int, string>(s.Id, s.FilePath))
             .ToListAsync();
                if (allFilesToDelete == null || allFilesToDelete.Count == 0)
                    return false;
                await _context.Teachervideos
                    .Where(s => allFilesToDelete.Select(f => f.Id).Contains(s.Id))
                    .ExecuteDeleteAsync();
            }
            else
            {
                return false; // nothing to delete
            }

            string root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            await Task.WhenAll(allFilesToDelete.Select(s => Task.Run(() =>
            {
                string fullFilePath = Path.Combine(root, s.filePath);
                if (File.Exists(fullFilePath))
                {

                    File.Delete(fullFilePath);
                }
            })));

            return true;

        }

    public async Task<bool> DeleteTeacherContantFolder(int schoolid)
        {
            // Fetch the folders to delete
            var allFoldersToDelete = await _context.Teacherfolders
                .Where(s => s.school_id == schoolid)
                .Select(s => new ValueTuple<int, string>(s.id, s.folder_path))
                .ToListAsync();

            // Get the list of IDs to delete
            var idsToDelete = allFoldersToDelete.Select(f => f.Item1).ToList();
            if (idsToDelete == null || idsToDelete.Count == 0)
                return false;
            // Delete from the database
            await _context.Teacherfolders
                .Where(s => idsToDelete.Contains(s.id))
                .ExecuteDeleteAsync();

            // Get root path
            string root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            // Delete files from file system
            await Task.WhenAll(allFoldersToDelete.Select(s => Task.Run(() =>
            {
                string fullFilePath = Path.Combine(root, s.Item2); // Access folder_path using Item2
                if (File.Exists(fullFilePath))
                {
                    File.Delete(fullFilePath);
                }
            })));

            return true;
        }
    public async Task<bool> DeleteEdutechContantFolder(int schoolid)
        {
            // Fetch the folders to delete
            var allFoldersToDelete = await _context.Folders
                .Where(s => s.school_id == schoolid)
                .Select(s => new ValueTuple<int, string>(s.id, s.folder_path))
                .ToListAsync();

            // Get the list of IDs to delete
            var idsToDelete = allFoldersToDelete.Select(f => f.Item1).ToList();
            if (idsToDelete == null || idsToDelete.Count == 0)
                return false;
            // Delete from the database
            await _context.Folders
                .Where(s => idsToDelete.Contains(s.id))
                .ExecuteDeleteAsync();

            // Get root path
            string root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            // Delete files from file system
            await Task.WhenAll(allFoldersToDelete.Select(s => Task.Run(() =>
            {
                string fullFilePath = Path.Combine(root, s.Item2); // Access folder_path using Item2
                if (File.Exists(fullFilePath))
                {
                    File.Delete(fullFilePath);
                }
            })));

            return true;
        }
    }
}