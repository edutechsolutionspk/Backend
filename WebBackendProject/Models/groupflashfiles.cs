using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebBackendProject.Models;

public class groupedflashfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; }

    [Required]
    public string FilePath { get; set; }

    public int ClassId { get; set; }
    public int SubjectId { get; set; }

    [Required]
    [StringLength(255)]
    public string FolderName { get; set; }

    [Required]
    public int FlashFileId { get; set; }

    [Required]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

 
}
