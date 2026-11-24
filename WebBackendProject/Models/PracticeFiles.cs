using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBackendProject.Models;

[Table("practicefiles")]
public class PracticeFiles
{
	[Key]
	public int id { get; set; }

	public string file_name { get; set; }

	public string file_path { get; set; }

	public int class_id { get; set; }

	public int subject_id { get; set; }

	public string encryption_key { get; set; }

	public string iv { get; set; }

	public DateTime upload_date { get; set; }

	public int school_id { get; set; }

	public int uploaded_by_master { get; set; }
}
