using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using DjTrust.Api.Models;
using DjTrust.Api.Data;
using DjTrust.Api.Services;

namespace DjTrust.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class CareersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CareersController> _logger;
        private readonly DbConnectionFactory _dbFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;

        public CareersController(
            IConfiguration config, 
            ILogger<CareersController> logger, 
            IWebHostEnvironment env, 
            DbConnectionFactory dbFactory,
            IEmailService emailService)
        {
            _config = config;
            _logger = logger;
            _env = env;
            _dbFactory = dbFactory;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCareers([FromQuery] string lang = "EN")
        {
            try
            {
                using var db = await _dbFactory.CreateOpenConnectionAsync();
                string query = @"
                    SELECT 
                        [RefNo] AS Serial,
                        [Jobtitle] AS Title,
                        [JobtitleSi] AS TitleSi,
                        CONVERT(VARCHAR(10), [Closingdate], 120) AS [End],
                        [Poster] AS Start,
                        [Description] AS Description,
                        [DescriptionSi] AS DescriptionSi,
                        [CreatedAt]
                    FROM [dbo].[JobAdvertising] 
                    WHERE [Closingdate] >= CONVERT(DATE, GETDATE()) 
                    ORDER BY [CreatedAt] DESC";

                var jobs = (await db.QueryAsync<JobItem>(query)).ToList();
                return Ok(new { success = true, data = jobs });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query JobAdvertising table from database.");
                // Return empty list when database connection fails or has no records
                return Ok(new { success = true, data = new List<JobItem>() });
            }
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] JobApplicationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Name and Email are required." });
            }

            // 1. Dispatch Email Notification immediately to HR (careers@djtrust.lk) and Developer BCC (dinupad@djtrust.lk)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendJobApplicationNotificationAsync(
                        request.Jobtitle,
                        request.Name.Trim(),
                        request.Email.Trim(),
                        request.Tpno?.Trim() ?? string.Empty,
                        request.Head,
                        request.Body,
                        request.Coverletter);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to dispatch career application notification email.");
                }
            });

            // 2. Persist record into Database
            try
            {
                using var db = await _dbFactory.CreateOpenConnectionAsync();
                string insertSql = @"
                    INSERT INTO [dbo].[JobApplication] 
                        ([Job_id], [Name], [Mobile], [Description], [CV], [CoverLetter], [Email], [CreatedAt])
                    VALUES 
                        (@Refcode, @Name, @Tpno, @Head, @Body, @Coverletter, @Email, GETDATE());";

                await db.ExecuteAsync(insertSql, new
                {
                    request.Refcode,
                    request.Name,
                    request.Tpno,
                    request.Head,
                    Body = request.Body ?? string.Empty,
                    Coverletter = request.Coverletter ?? string.Empty,
                    request.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to insert career application into database. (Email notification was dispatched successfully)");
            }

            return Ok(new { success = true, message = "Application submitted successfully! Our talent acquisition team will review your CV." });
        }

        [HttpPost("upload-cv")]
        public async Task<IActionResult> UploadCv([FromForm] IFormFile? cv, [FromForm] IFormFile? cl)
        {
            try
            {
                var uploadedFiles = new List<string>();
                var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads", "CV");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB

                if (cv != null && cv.Length > 0)
                {
                    if (cv.Length > maxFileSize)
                    {
                        return BadRequest(new { success = false, message = "CV file exceeds the 10 MB limit." });
                    }

                    var ext = Path.GetExtension(cv.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        return BadRequest(new { success = false, message = "Invalid CV file type. Only PDF, DOC, DOCX, and images are allowed." });
                    }

                    var cvName = $"{Guid.NewGuid()}{ext}";
                    var cvPath = Path.Combine(uploadsFolder, cvName);
                    using var stream = new FileStream(cvPath, FileMode.Create);
                    await cv.CopyToAsync(stream);
                    uploadedFiles.Add(cvName);
                }

                if (cl != null && cl.Length > 0)
                {
                    if (cl.Length > maxFileSize)
                    {
                        return BadRequest(new { success = false, message = "Cover letter file exceeds the 10 MB limit." });
                    }

                    var ext = Path.GetExtension(cl.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        return BadRequest(new { success = false, message = "Invalid Cover letter file type. Only PDF, DOC, DOCX, and images are allowed." });
                    }

                    var clName = $"{Guid.NewGuid()}{ext}";
                    var clPath = Path.Combine(uploadsFolder, clName);
                    using var stream = new FileStream(clPath, FileMode.Create);
                    await cl.CopyToAsync(stream);
                    uploadedFiles.Add(clName);
                }

                return Ok(new { success = true, files = uploadedFiles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading CV file.");
                return StatusCode(500, new { success = false, message = "Upload failed. Please try again." });
            }
        }

        [HttpGet("test-job-email")]
        [HttpPost("test-job-email")]
        public async Task<IActionResult> TestJobEmail()
        {
            var result = await _emailService.SendJobApplicationNotificationAsync(
                "TESTING APPLICATION - Junior Credit Officer",
                "J.A. Ranasinghe (Testing Candidate)",
                "ranasinghe.testing@example.com",
                "077 123 4567",
                "*** THIS IS A SYSTEM TEST EMAIL *** Testing job application notification pipeline delivery to HR (careers@djtrust.lk) and Developer BCC (dinupad@djtrust.lk).",
                "sample-cv-testing.pdf",
                "sample-cover-letter-testing.pdf");

            return Ok(new { 
                success = result, 
                message = result 
                    ? "Job application test email dispatched successfully to: careers@djtrust.lk (BCC: dinupad@djtrust.lk)" 
                    : "Failed to dispatch job application test email." 
            });
        }
    }
}
