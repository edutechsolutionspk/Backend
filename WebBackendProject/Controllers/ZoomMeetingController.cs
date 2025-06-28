using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using WebBackendProject.Data;
using WebBackendProject.Services;

namespace WebBackendProject.Controllers
{
    [Route("api/ZoomController")]
    public class ZoomMeetingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private DailyMeetingService _dailyMeetingService;
        ClassesSubjectsServices _classesSubjectsServices;

        public ZoomMeetingController(ApplicationDbContext context)
        {
            _context = context;

            _dailyMeetingService = new DailyMeetingService();
            _classesSubjectsServices = new ClassesSubjectsServices(context);
        }

        [HttpPost("CreateMeeting")]
        public async Task<IActionResult> CreateMeeting([FromBody] meetings meeting)
        {
            try
            {
                if (meeting == null)
                    return BadRequest("Meeting details are required.");

                if (meeting.start_time == null)
                    return BadRequest("Start time is required.");


                if (meeting.start_time.ToUniversalTime() <= DateTime.UtcNow)
                    return BadRequest("Start time must be in the future.");

                // Create a Daily.co meeting
                var dailyResponse = await _dailyMeetingService.CreateDailyMeeting(meeting);

                if (dailyResponse == null)
                    return StatusCode(500, "Failed to create meeting. Please check Daily.co API credentials.");

                // Save to the database
                meeting.status = "scheduled";
                meeting.created_at = DateTime.UtcNow;
                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Meeting created successfully",

                });
            }
            catch (HttpRequestException httpEx)
            {

                return StatusCode(500, "Error connecting to Daily.co API." + httpEx.Message);
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        [HttpPost("GetMeetings")]
        public async Task<IActionResult> GetMeetings([FromBody] GetMeeting meeting)
        {


           if(meeting.classId == null)
            {
                meeting.classId = (await _classesSubjectsServices.GetStudentClassId(meeting.userId)).Value;
            }
            // If classId is provided directly, use it
               // Fetch meetings directly based on classId
                var meetings = await _context.Meetings
                    .Where(m => m.class_id == meeting.classId && m.school_id == meeting.schoolid)
                    .Select(m => new
                    {
                        m.id,
                        m.topic,
                        m.start_time,
                        m.join_url
                    })
                    .ToListAsync();

                if (!meetings.Any())
                {
                    return NotFound("No meetings found for this class.");
                }

                return Ok(meetings);
            
        }
        [HttpGet("join-meeting")]
        public async Task<IActionResult> JoinMeeting(int meetingId)
        {
            try
            {
                var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.id == meetingId);
                if (meeting == null) return NotFound("Meeting not found.");

                return Ok(new
                {
                    meetingUrl = meeting.join_url
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpDelete("Deletemeeting")]
        public async Task<IActionResult> DeleteVideo([FromQuery] int id)
        {
            try
            {
                var existingVideo = await _context.Meetings.FindAsync(id);
                if (existingVideo == null)
                {
                    return BadRequest(new { message = "The requested video was not found." });
                }

                _context.Meetings.Remove(existingVideo);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Video deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the video.", error = ex.Message });
            }
        }


    }

    public class GetMeeting
    {
        public int? userId { get; set; }
        
        public int? classId { get; set; }
        public int schoolid {get; set; }
         }
}
