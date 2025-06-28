namespace WebBackendProject.Models
{
    public class schools
    {
        public int id { get; set; }
        public string school_name { get; set; }
        public string principal_name { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string address { get; set; }
        public string contact_no { get; set; }
        public string max_class { get; set; }
        public string status { get; set; }
        public string principal_image_path { get; set; }
        public DateTime? created_at { get; set; }
        public int? created_by { get; set; }
         public int gender { get; set; }
    }


}
