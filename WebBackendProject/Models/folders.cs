using System.ComponentModel.DataAnnotations.Schema;

namespace WebBackendProject.Models
{
    public class folders
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; } 
        public string folder_name { get; set; }  
        public string folder_path { get; set; }
        public int subject_id {  get; set; }
        public int class_id { get; set;}
        public string module { get; set;}
        public DateTime created_at { get; set; } = DateTime.UtcNow;
        public DateTime updated_at { get; set; } = DateTime.UtcNow;
        public int created_by { get; set; }
        public int school_id { get; set; }
    }
}
