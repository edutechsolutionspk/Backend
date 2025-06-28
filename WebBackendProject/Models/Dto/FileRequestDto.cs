namespace WebBackendProject.Models.Dto
{
    public class FileRequestDto
    {
        public int? ClassId { get; set; }
        public int? UserId { get; set; }
        public int? teacherId { get; set; }
        public int SubjectId { get; set; }
        public string Module { get; set; }
        public string FolderName { get; set; }
        public int SchoolId { get; set; }
    }
}
