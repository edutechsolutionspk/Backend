using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class meetings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id { get; set; }
    public string meeting_id { get; set; }
    public int class_id { get; set; }
    public string topic { get; set; }
    public DateTime start_time { get; set; }
    public string status { get; set; } = "scheduled";
    public int duration { get; set; }
    public string passcode { get; set; }
    public string join_url { get; set; }
    public string start_url { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public int school_id { get; set; }
}
