using Microsoft.EntityFrameworkCore;

using WebBackendProject.Model;
using WebBackendProject.Models; // Import your models

namespace WebBackendProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }


        public ApplicationDbContext() : base(new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseMySql("server=localhost;port=3306;database=edutechdb;user=root;password=12345",
                         new MySqlServerVersion(new Version(8, 0, 33))) // Change MySQL version if needed
               .Options)
        { }


        // Sql tables 
        public DbSet<Class> Classes { get; set; } 
        public DbSet<ClassSubject> ClassSubjects { get; set; }
        public DbSet<Subject> subjects { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<folders> Folders {  get; set; }
        public DbSet<pdfFiles> pdffiles { get; set; }
        public DbSet<FlashFiles> flashFiles { get; set; }
        public DbSet<sheetspdffiles> sheetsPdfFiles { get; set;}
        public DbSet<sheetsflashfiles> sheetsFlashFiles { get; set;}
        public DbSet<meetings> Meetings { get; set; }
        public DbSet<edutube> Edutube { get; set; }
        public DbSet<ecoursebook> EcourseBook { get; set; }
        public DbSet<teachers> Teachers { get; set; }
        public DbSet<groupedflashfile> GroupedFlashFiles { get; set; }
        public DbSet<admin> Admin { get; set; }
        public DbSet<schools> Schools { get; set; }
        public DbSet<teacher_subject_classes> Teacher_Subject_Classes { get; set; }
        public DbSet<teacherfolders> Teacherfolders { get; set;}
        public DbSet<teacherpdf> Teacherpdf { get; set; }
        public DbSet<teachervideos> Teachervideos { get; set; }
        public DbSet<videofiles> VideoFiles { get; set;}
        public DbSet<thumbnails> Thumbnails { get; set; }
        public DbSet<PracticeFiles> practiceFiles { get; set; }

        public DbSet<AssessmentFiles> assessmentFiles { get; set; }
    }
}