using System.ComponentModel.DataAnnotations.Schema;

namespace WebBackendProject.Models
{
    public class teachers
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string full_name { get; set; }
        public int school_id { get; set; }
        public string qualification { get; set; }
        public int experience { get; set; }
        public string designation { get; set; }
        public long contact { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string profile_picture_path { get; set; }
        public DateTime created_at { get; set; }
        public int created_by { get; set; }
        public string status { get; set; }
    }
}
