using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace WebBackendProject.Services
{
    public class DailyMeetingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _dailyApiKey = "b033c01b5dce7b22867415c102cf4c60836c8de1c7b1baa67b3344ec47c722a9"; // Replace with your Daily.co API Key

        public DailyMeetingService()
        {
            _httpClient = new HttpClient();
        }

        // 🔹 Function for Authentication (Reused in All API Calls)
        private void AuthenticateRequest()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _dailyApiKey);
        }

        // 🔹 Function to Create a Meeting Room
        public async Task<DailyMeetingResponse> CreateDailyMeeting(meetings meeting)
        {

            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var randomCode = Convert.ToBase64String(bytes).Replace("=", "").Replace("+", "").Replace("/", "");
            var roomName = $"school{meeting.school_id}-meet-{randomCode}";
            if (string.IsNullOrEmpty(meeting.topic) || meeting.duration <= 0)
            {
                throw new ArgumentException("Invalid meeting details. Topic and duration are required.");
            }
            // Assume meeting.start_time is in PKT
            var pkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");

            // Convert to UTC for Daily.co
            var pkTime = TimeZoneInfo.ConvertTime(meeting.start_time, pkTimeZone);
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(pkTime, pkTimeZone);

            var startTime = new DateTimeOffset(utcTime).ToUnixTimeSeconds();
            var expiration = new DateTimeOffset(utcTime.AddHours(2)).ToUnixTimeSeconds();


            var dailyMeeting = new
            {
                name = roomName,
                privacy = "private",
                properties = new
                {

                    exp = expiration,
                    nbf = startTime,
                    enable_chat = true,
                    enable_network_ui = true,
                    enable_screenshare = true,
                    eject_at_room_exp = true,

                }
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(dailyMeeting), Encoding.UTF8, "application/json");
            AuthenticateRequest(); // Use the Auth Function

            var response = await _httpClient.PostAsync("https://api.daily.co/v1/rooms", requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Daily.co API Response: {response.StatusCode} - {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to create Daily.co meeting. Response: {response.StatusCode} - {responseBody}");
            }

            var dailyResponse = JsonSerializer.Deserialize<DailyMeetingResponse>(responseBody);

            // Generate Tokens
            string hostToken = await GenerateToken(roomName, true);
            string participantToken = await GenerateToken(roomName, false);

            // Store Meeting Details
            meeting.meeting_id = dailyResponse.id;
            meeting.start_url = $"{dailyResponse.url}?t={hostToken}";
            meeting.join_url = $"{dailyResponse.url}?t={participantToken}";

            return dailyResponse;
        }

        // 🔹 Function to Generate a Token for Users (Host or Participant)
        private async Task<string> GenerateToken(string roomName, bool isOwner)
        {
            var tokenRequest = new
            {
                properties = new
                {
                    room_name = roomName,
                    is_owner = isOwner
                }
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(tokenRequest), Encoding.UTF8, "application/json");
            AuthenticateRequest(); // Use the Auth Function

            var response = await _httpClient.PostAsync("https://api.daily.co/v1/meeting-tokens", requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Token Generation Response: {response.StatusCode} - {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to generate Daily.co meeting token. Response: {response.StatusCode} - {responseBody}");
            }

            var tokenResponse = JsonSerializer.Deserialize<DailyTokenResponse>(responseBody);
            return tokenResponse.token;
        }
    }

    // 🔹 Response Model for Meeting
    public class DailyMeetingResponse
    {
        public string id { get; set; }
        public string url { get; set; }
    }

    // 🔹 Response Model for Token
    public class DailyTokenResponse
    {
        public string token { get; set; }
    }
}
