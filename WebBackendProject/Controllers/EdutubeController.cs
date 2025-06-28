using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackendProject.Data;
using WebBackendProject.Models;
using WebBackendProject.Services;

namespace WebBackendProject.Controllers
{
    [Route("api/EdutubeController")]
    public class EdutubeController : ControllerBase
    {

        ApplicationDbContext _context;
        ClassesSubjectsServices _classesSubjectsServices;
        private DeleteServices _deleteServices;
        public EdutubeController(ApplicationDbContext context)
        {
            _context = context;
            _classesSubjectsServices = new ClassesSubjectsServices(context);
            _deleteServices = new DeleteServices(context);
        }
        [HttpPost("UploadEdutubeVideo")]
        public async Task<IActionResult> UploadVideo([FromBody] EdutubeDto video)
        {
            try
            {
                if (video == null)
                {
                    return BadRequest("Video object is null.");
                }



                if (!ModelState.IsValid)
                    return BadRequest("Invalid data.");

                var newvideo = new edutube
                {
                    title = video.title,
                    link = video.link,
                    category = video.category,
                    upload_date = DateTime.Now,
                    school_id = video.school_id
                };
                if(video.isAdmin)
                {
                    newvideo.added_by_master = video.added_by;
                }
                else
                {
                    newvideo.added_by_school = video.added_by;
                }
               
                _context.Edutube.Add(newvideo);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Video uploaded successfully." });
            }
            catch (HttpRequestException httpEx)
            {

                return StatusCode(500, "Error connecting to Daily.co API." + httpEx.Message);
            }
            catch (Exception ex)
            {

                string inner = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";

                return StatusCode(500, new
                {
                    error = "An unexpected error occurred.",
                    message = ex.Message,
                    innerException = inner
                });
            }
        }


        [HttpPost("SearchVideos")]
        public async Task<IActionResult> SearchVideos([FromBody] EdutubeSearch Model,   [FromQuery] string? searchQuery = null, int page=1)
        {
            try
            {

                if (string.IsNullOrEmpty(Model.category))
                {
                    int user = (await _classesSubjectsServices.GetStudentClassId(Model.userid)).Value;
                    if (user != null) // Check if user exists
                    {
                       Model.category = GetCategory(user);
                    }
                }
                IQueryable<edutube> query = _context.Edutube
                .Where(v => v.school_id == Model.schoolid);

                // Filter by category if available
                if (!string.IsNullOrEmpty(Model.category))
                {
                    query = query.Where(v => v.category == Model.category);
                }
                // If a searchQuery is provided, filter the videos by title
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(v => v.title.ToLower().Contains(searchQuery.ToLower()));
                }

                // Apply pagination if skip and take are provided
               
                // Execute query

                var totalRecords = await query.CountAsync(); ;
                int pageSize = 15;
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                if (totalPages == 0)
                    totalPages = 1;

                int currentPage = page;
                if (currentPage < 1)
                    currentPage = totalPages;
                else if (currentPage > totalPages)
                    currentPage = 1;
                var videos = await query
                    .OrderBy(s => s.id)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize).Select(v => new { v.id, v.title, v.link }).ToListAsync();
                return Ok(new { success = true, videos,totalPages, currentPage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error", error = ex.Message });
            }
        }


        private string GetCategory(int classId)
        {
            if (classId >= 1 && classId <= 3)
                return "Pre-Elementary";
            else if (classId >= 4 && classId <= 11)
                return "Elementary";
            else if (classId >= 12 && classId <= 13)
                return "Secondary";

            return "Unknown";
        }
        [HttpDelete("DeleteVideo")]
        public async Task<IActionResult> DeleteVideo([FromQuery] int id)
        {
            try
            {
                await _deleteServices.DeleteEdutube(null, id);

                return Ok(new { message = "Video deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the video.", error = ex.Message });
            }
        }
     
    }
    public class EdutubeDto
    {
        public string title { get; set; }          // YouTube video title
        public string link { get; set; }        // Original YouTube link
        public string category { get; set; }
        public int school_id { get; set; }       // School ID
        public int added_by { get; set; }        // 👈 master or principal
       public bool isAdmin {get;set;}
    }

    public class EdutubeSearch {
        public string? category { get; set; } = null;
        public int? userid { get; set; } = null;
        public int schoolid { get; set; }
    }
}


