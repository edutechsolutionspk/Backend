using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebBackendProject.Model
{
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public required string full_name { get; set; }
        public required string father_name { get; set; }
        public required int age { get; set; }
        public required string username { get; set; }
        public required string email { get; set; }
        public required string password { get; set; }

        public required int class_id { get; set; }

        public required string picture_path { get; set; }

        public required int school_id { get; set; }
        public string status { get; set; } ="active";
        public int? created_by_principal { get; set; }
        public int? created_by_master { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;
        public string g_contact { get; set; }
    }

    
}