using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBackendProject.Models
{
    public class ecourseflash
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        [StringLength(500)]
        public string EncryptionKey { get; set; }

        [Required]
        [StringLength(500)]
        public string IV { get; set; }

        public DateTime? UploadDate { get; set; } = DateTime.Now;
        public int IsMainFile { get; set; } = 0;
    }
}
