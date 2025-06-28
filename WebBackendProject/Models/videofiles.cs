namespace WebBackendProject.Models
{
    public class videofiles
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public string FolderName { get; set; }
        public string EncryptionKey { get; set; }
        public string IV { get; set; }
        public DateTime UploadDate { get; set; }
        public int school_id { get; set; }
        public int uploaded_by_master { get; set; }
    }
}
