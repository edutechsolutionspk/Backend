namespace WebBackendProject.Models.Dto
{
    public class FolderRequestDto
    {
        public int? ClassId { get; set; }
        public int? UserId { get; set; }
        public int SubjectId { get; set; }
        public string Module { get; set; }
        public int SchoolId { get; set; }
    }
}
