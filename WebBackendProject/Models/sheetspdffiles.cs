using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBackendProject.Models
{
    public class sheetspdffiles
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

   
        public string FileName { get; set; }

        public string FilePath { get; set; }

        public string Category { get; set; } // Enum: Pre-Elementary, Elementary, Secondary

        public string EncryptionKey { get; set; }

        public string IV { get; set; }
        public DateTime? UploadDate { get; set; } = DateTime.Now;
        public string thumbnailpath { get; set; }
    }
}
