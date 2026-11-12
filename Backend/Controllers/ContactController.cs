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
    public class ContactController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ContactController> _logger;
        private readonly DbConnectionFactory _dbFactory;
        private readonly IEmailService _emailService;

        public ContactController(
            IConfiguration config, 
            ILogger<ContactController> logger, 
            DbConnectionFactory dbFactory,
            IEmailService emailService)
        {
            _config = config;
            _logger = logger;
            _dbFactory = dbFactory;
            _emailService = emailService;
        }

        [HttpPost("submit")]
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] ContactInquiryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Tpno) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Name, Phone Number, Subject and Message are required." });
            }

            // 1. Dispatch Email Notification immediately to Reception (info@djtrust.lk) and Developer BCC (dinupad@djtrust.lk)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendInquiryNotificationAsync(
                        request.Name.Trim(),
                        request.Tpno.Trim(),
                        request.Email?.Trim() ?? string.Empty,
                        request.Subject.Trim(),
                        request.Message.Trim());
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to dispatch inquiry notification email.");
                }
            });

            // 2. Save inquiry into Database
            try
            {
                using var db = await _dbFactory.CreateOpenConnectionAsync();
                string insertSql = @"
                    INSERT INTO [dbo].[Inquiry] 
                        ([Name], [Email], [Tpno], [Type], [Header], [Comment], [Status], [Date])
                    VALUES 
                        (@Name, @Email, @Tpno, 'General', @Subject, @Message, 'New', GETDATE());";

                await db.ExecuteAsync(insertSql, new
                {
                    request.Name,
                    Email = request.Email ?? string.Empty,
                    request.Tpno,
                    request.Subject,
                    request.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to insert inquiry into database. (Email notification was dispatched successfully)");
            }

            return Ok(new { success = true, message = "Your message has been sent. Thank you!" });
        }

        [HttpGet("test-email")]
        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromQuery] string? to)
        {
            var result = await _emailService.SendTestEmailAsync(to);
            if (result.success)
            {
                return Ok(new { success = true, message = result.message });
            }
            return StatusCode(500, new { success = false, message = result.message });
        }
    }
}

