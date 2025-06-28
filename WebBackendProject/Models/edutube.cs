using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBackendProject.Models
{
    public class edutube
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string category { get; set; }
        public int school_id { get; set; }
       public DateTime upload_date { get; set; } = DateTime.UtcNow;
       public int? added_by_master { get; set; }
       public int? added_by_school { get; set; }

    }
}
