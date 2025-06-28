using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBackendProject.Models
{
    public class ecoursebook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        [Required]
        public string WebsiteLink { get; set; }

        [Required]
        public string Category { get; set; }


        public DateTime? UploadDate { get; set; } = DateTime.Now;
        public string thumbnailpath { get; set; }
    }
}
