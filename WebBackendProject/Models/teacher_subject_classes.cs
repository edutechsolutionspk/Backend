using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebBackendProject.Models
{
    public class teacher_subject_classes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int id { get; set; }
        public int teacher_id {  get; set; }
        public int subject_id { get; set; }
        public int class_id { get; set; }

        

    }
}
