using WebBackendProject.Data;

namespace WebBackendProject.Other
{
    public class FolderAndFilesHandler
    {
        private readonly string basePath;
        private readonly ApplicationDbContext _db;

        public FolderAndFilesHandler(ApplicationDbContext db)
        {
            _db = db;
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploadedfiles");

            EnsureBaseDirectoryExists(); // Ensure base directory exists
        }

        private void EnsureBaseDirectoryExists()
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
        }

        public string ConvertToRelativePath(string absolutePath)
        {
            // Get the relative path from "wwwroot"
            return absolutePath.Replace(basePath, "").TrimStart(Path.DirectorySeparatorChar)
                               .Replace("\\", "/"); // Convert Windows-style paths to forward slashes
        }

        public string CreateSchoolFolder(int schoolid)
        {
            var schoolData = _db.Schools.FirstOrDefault(c => c.id == schoolid);
            if (schoolData == null) return null;

            string schoolPath = Path.Combine(basePath, schoolData.school_name);
            if (!Directory.Exists(schoolPath)) Directory.CreateDirectory(schoolPath);

            return ConvertToRelativePath(schoolPath);
        }
        public string CreateSectionFolder(int? teacherid,int schoolid)
        {
            string schoolPath = CreateSchoolFolder(schoolid);
            string sectionFolderName;

            if (teacherid.HasValue)
            {
                sectionFolderName = "teachercontent";
            }
            else
            {
                sectionFolderName = "eduteachcontent";
            }

            string sectionPath = Path.Combine(basePath, schoolPath, sectionFolderName);

            if (!Directory.Exists(sectionPath))
            {
                Directory.CreateDirectory(sectionPath);
            }

            return ConvertToRelativePath(sectionPath);
        }

        public string CreateClassFolder(int classid, int? teacherid, int schoolid)
        {
            string sectionPath = CreateSectionFolder(teacherid, schoolid);
            var classData = _db.Classes.FirstOrDefault(c => c.ClassID == classid);
            if (classData == null) return null;

            string classPath = Path.Combine(basePath, sectionPath, classData.ClassName);
            if (!Directory.Exists(classPath)) Directory.CreateDirectory(classPath);

            return ConvertToRelativePath(classPath);
        }

        public string CreateSubjectFolder(int classid, int subjectid, int? teacherid, int schoolid)
        {
            string classPath = CreateClassFolder(classid,teacherid,schoolid);
            if (classPath == null) return null;

            var subjectData = _db.subjects.FirstOrDefault(s => s.SubjectID == subjectid);
            if (subjectData == null) return null;

            string subjectPath = Path.Combine(basePath, classPath, subjectData.SubjectName);
            if (!Directory.Exists(subjectPath)) Directory.CreateDirectory(subjectPath);

            return ConvertToRelativePath(subjectPath);
        }

        public string CreateModuleFolder(int classid, int subjectid, string module, int? teacherid, int schoolid)
        {
            string subjectPath = CreateSubjectFolder(classid, subjectid,teacherid,schoolid);
            if (subjectPath == null) return null;

            string moduleFolder = module.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "pdf"
                                : module.Equals("flash", StringComparison.OrdinalIgnoreCase) ? "flash"
                                : module.Equals("video", StringComparison.OrdinalIgnoreCase) ? "video" : null; 
                                ;
                             
            if (moduleFolder == null) return null; // Return null if module is invalid

            string modulePath = Path.Combine(basePath, subjectPath, moduleFolder);
            if (!Directory.Exists(modulePath)) Directory.CreateDirectory(modulePath);

            return ConvertToRelativePath(modulePath);
        }

        public string CreateFolder(int classid, int subjectid, string module, string foldername, int? teacherid, int schoolid)
        {
            string modulePath = CreateModuleFolder(classid, subjectid, module,teacherid,schoolid);
            if (modulePath == null) return null;

            string folderPath = Path.Combine(basePath, modulePath, foldername);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            return ConvertToRelativePath(folderPath);
        }

        public string UploadFiles(int classid, int subjectid, string module, string foldername,string filename, int? teacherid, int schoolid)
        {
           string modulepath =   CreateFolder(classid, subjectid, module,foldername,teacherid,schoolid);
            string filePath = "";
            if (module.Equals("pdf"))
            {
               filePath =  Path.Combine(basePath,modulepath, filename);
            }
            else if (module.Equals("flash"))
            {
                filePath = Path.Combine(basePath, modulepath, filename);
            }
            return ConvertToRelativePath(filePath);


        }


        public string CreateCategoryFolder(string category)
        {
            if (category != "Pre-Elementary" && category != "Elementary" && category != "Secondary")
                return null;

            string categoryPath = Path.Combine(basePath, category);
            if (!Directory.Exists(categoryPath)) Directory.CreateDirectory(categoryPath);

            return ConvertToRelativePath(categoryPath);
        }

        public string CreateModuleFolderByCategory(string category, string module)
        {
            string categoryPath = CreateCategoryFolder(category);
            if (categoryPath == null) return null;

            string? moduleFolder =module.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "pdf"
                          : module.Equals("flash", StringComparison.OrdinalIgnoreCase) ? "flash"
                          : null;

            if (moduleFolder == null) return null;

            string modulePath = Path.Combine(basePath, categoryPath, moduleFolder);
            if (!Directory.Exists(modulePath)) Directory.CreateDirectory(modulePath);

            return ConvertToRelativePath(modulePath);
        }

        public string CreateThumbnailFolder(string module)
        {
            
            string? thumbnailCategory = module.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "PdfThumbnails"
                          : module.Equals("flash", StringComparison.OrdinalIgnoreCase) ? "FlashThumbnails"
                          : module.Equals("html", StringComparison.OrdinalIgnoreCase) ? "EcourseThumbnails":
                          null;
            string thumbnailPath = Path.Combine(basePath, thumbnailCategory);

            if (!Directory.Exists(thumbnailPath)) Directory.CreateDirectory(thumbnailPath);

            return ConvertToRelativePath(thumbnailPath);
        }


    }
}
