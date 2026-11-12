using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using DjTrust.Api.Models;

namespace DjTrust.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendInquiryNotificationAsync(
            string name,
            string phone,
            string email,
            string subject,
            string message,
            CancellationToken cancellationToken = default)
        {
            var recipients = GetInquiryRecipients();
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No notification recipients configured for Web Inquiry.");
                return false;
            }

            var emailSubject = $"[D.J. Trust Web Inquiry] {subject}";

            var customerEmailDisplay = string.IsNullOrWhiteSpace(email) ? "Not provided" : email;
            var receivedTime = DateTime.UtcNow.AddHours(5.5).ToString("yyyy-MM-dd hh:mm tt"); // Sri Lanka local time

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; }}
        .email-container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #003d8f 0%, #00224f 100%); color: #ffffff; padding: 25px 30px; text-align: left; }}
        .header h2 {{ margin: 0 0 5px 0; font-size: 22px; font-weight: 700; letter-spacing: -0.5px; }}
        .header p {{ margin: 0; font-size: 13px; opacity: 0.85; }}
        .content {{ padding: 30px; }}
        .badge {{ display: inline-block; background-color: #e0f2fe; color: #0369a1; padding: 4px 10px; font-size: 11px; font-weight: 700; border-radius: 20px; text-transform: uppercase; margin-bottom: 20px; }}
        .data-table {{ width: 100%; border-collapse: collapse; margin-bottom: 25px; }}
        .data-table td {{ padding: 12px 10px; border-bottom: 1px solid #edf2f7; font-size: 14px; vertical-align: top; }}
        .data-table td.label {{ width: 35%; font-weight: 600; color: #475569; }}
        .data-table td.value {{ color: #0f172a; font-weight: 500; }}
        .data-table td.value a {{ color: #003d8f; text-decoration: none; font-weight: 600; }}
        .message-box {{ background-color: #f8fafc; border-left: 4px solid #003d8f; padding: 15px 20px; border-radius: 0 8px 8px 0; font-size: 14px; line-height: 1.6; color: #1e293b; margin-top: 5px; }}
        .footer {{ background-color: #f8fafc; padding: 18px 30px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; }}
        .footer a {{ color: #003d8f; text-decoration: none; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h2>D.J. Trust Web Inquiry</h2>
            <p>A new customer inquiry has been received from the website contact form.</p>
        </div>
        <div class=""content"">
            <span class=""badge"">New Web Inquiry</span>
            <table class=""data-table"">
                <tr>
                    <td class=""label"">Customer Name:</td>
                    <td class=""value""><strong>{name}</strong></td>
                </tr>
                <tr>
                    <td class=""label"">Contact Number:</td>
                    <td class=""value""><a href=""tel:{phone}"">{phone}</a></td>
                </tr>
                <tr>
                    <td class=""label"">Customer Email:</td>
                    <td class=""value"">{(string.IsNullOrWhiteSpace(email) ? "Not provided" : $"<a href=\"mailto:{email}\">{email}</a>")}</td>
                </tr>
                <tr>
                    <td class=""label"">Subject:</td>
                    <td class=""value"">{subject}</td>
                </tr>
                <tr>
                    <td class=""label"">Received Time:</td>
                    <td class=""value"">{receivedTime} (Sri Lanka Time)</td>
                </tr>
            </table>

            <div style=""font-weight: 600; font-size: 13px; color: #475569; margin-bottom: 6px;"">Customer Message:</div>
            <div class=""message-box"">
                {message.Replace("\n", "<br/>")}
            </div>
        </div>
        <div class=""footer"">
            This is an automated notification from <a href=""https://www.djtrust.lk"">D.J. Trust Website</a>.<br/>
            Please contact the customer promptly.
        </div>
    </div>
</body>
</html>";

            var plainText = $@"
D.J. TRUST WEB INQUIRY
===========================================
Customer Name  : {name}
Phone Number   : {phone}
Customer Email : {customerEmailDisplay}
Subject        : {subject}
Date & Time    : {receivedTime}

Message:
{message}
===========================================
Automated notification from D.J. Trust Website (www.djtrust.lk)
";

            return await SendEmailAsync(recipients, emailSubject, htmlBody, plainText, email, cancellationToken);
        }

        public async Task<bool> SendJobApplicationNotificationAsync(
            string jobTitle,
            string applicantName,
            string applicantEmail,
            string applicantPhone,
            string? applicantNotes,
            string? cvFileName,
            string? coverLetterFileName,
            CancellationToken cancellationToken = default)
        {
            var recipients = GetCareersRecipients();
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No notification recipients configured for Job Application.");
                return false;
            }

            var safeJobTitle = string.IsNullOrWhiteSpace(jobTitle) ? "General Application" : jobTitle.Trim();
            var emailSubject = $"[D.J. Trust Careers] - New Job Application: {safeJobTitle} - {applicantName} ({applicantPhone})";
            var receivedTime = DateTime.UtcNow.AddHours(5.5).ToString("yyyy-MM-dd hh:mm tt");

            var digitsOnly = new string((applicantPhone ?? string.Empty).Where(char.IsDigit).ToArray());
            var waDigits = digitsOnly.StartsWith("0") ? "94" + digitsOnly.Substring(1) : (digitsOnly.StartsWith("94") ? digitsOnly : "94" + digitsOnly);
            var cleanPhone = digitsOnly.StartsWith("0") ? digitsOnly : "0" + digitsOnly;

            var cvDisplay = !string.IsNullOrWhiteSpace(cvFileName) 
                ? $"<span style=\"color: #0f172a; font-weight: 700;\">Uploaded:</span> <code>{cvFileName}</code> <span style=\"font-size: 11px; color: #64748b;\">(Saved on server under /Uploads/CV/)</span>"
                : "<span style=\"color: #64748b;\">Not Provided</span>";

            var clDisplay = !string.IsNullOrWhiteSpace(coverLetterFileName)
                ? $"<span style=\"color: #0f172a; font-weight: 700;\">Uploaded:</span> <code>{coverLetterFileName}</code>"
                : "<span style=\"color: #64748b;\">Not Provided</span>";

            var notesHtml = !string.IsNullOrWhiteSpace(applicantNotes)
                ? applicantNotes.Replace("\n", "<br/>")
                : "<span style=\"color: #94a3b8; font-style: italic;\">No additional notes provided by applicant.</span>";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>New Job Application</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; margin: 0; padding: 24px 12px;"">
    <div style=""max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.08); border: 1px solid #e2e8f0;"">
        
        <!-- Header -->
        <div style=""background: linear-gradient(135deg, #003d8f 0%, #001f4d 100%); padding: 30px; color: #ffffff;"">
            <div style=""display: inline-block; background: rgba(255,255,255,0.18); border: 1px solid rgba(255,255,255,0.25); color: #ffffff; padding: 4px 12px; border-radius: 9999px; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.6px; margin-bottom: 12px;"">
                HR / Careers Portal
            </div>
            <h1 style=""margin: 0 0 6px 0; font-size: 22px; font-weight: 800; letter-spacing: -0.4px;"">
                New Job Application Received
            </h1>
            <p style=""margin: 0; font-size: 13px; color: #cbd5e1; line-height: 1.5;"">
                A candidate has submitted an application for <strong>{safeJobTitle}</strong> through the D.J. Trust Careers portal.
            </p>
        </div>

        <!-- Content Body -->
        <div style=""padding: 28px 24px;"">

            <!-- Section 1: Position Applied -->
            <div style=""background-color: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 10px; padding: 14px 18px; margin-bottom: 22px;"">
                <div style=""font-size: 11px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.6px; color: #166534; margin-bottom: 4px;"">Target Position</div>
                <div style=""font-size: 16px; font-weight: 800; color: #15803d;"">{safeJobTitle}</div>
            </div>

            <!-- Section 2: Candidate Details -->
            <div style=""font-size: 13px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.8px; color: #003d8f; margin-bottom: 12px; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;"">
                👤 Candidate Details
            </div>
            <table style=""width: 100%; border-collapse: separate; border-spacing: 0; margin-bottom: 24px;"">
                <tr style=""background-color: #f8fafc;"">
                    <td style=""width: 38%; padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Candidate Name:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; color: #0f172a; border-bottom: 1px solid #edf2f7;"">{applicantName}</td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Contact Number:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; border-bottom: 1px solid #edf2f7;"">
                        <a href=""tel:{cleanPhone}"" style=""color: #003d8f; text-decoration: none;"">{applicantPhone}</a>
                    </td>
                </tr>
                <tr style=""background-color: #f8fafc;"">
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Email Address:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 700; border-bottom: 1px solid #edf2f7;"">
                        <a href=""mailto:{applicantEmail}"" style=""color: #003d8f; text-decoration: none;"">{applicantEmail}</a>
                    </td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Curriculum Vitae (CV):</td>
                    <td style=""padding: 11px 12px; font-size: 13px; border-bottom: 1px solid #edf2f7;"">{cvDisplay}</td>
                </tr>
                <tr style=""background-color: #f8fafc;"">
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Cover Letter:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; border-bottom: 1px solid #edf2f7;"">{clDisplay}</td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Submission Date & Time:</td>
                    <td style=""padding: 11px 12px; font-size: 12px; color: #64748b; font-weight: 600; border-bottom: 1px solid #edf2f7;"">{receivedTime} (Sri Lanka Time)</td>
                </tr>
            </table>

            <!-- Section 3: Candidate Notes -->
            <div style=""font-size: 13px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.8px; color: #003d8f; margin-bottom: 8px; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;"">
                📝 Candidate Statement / Notes
            </div>
            <div style=""background-color: #f8fafc; border-left: 4px solid #003d8f; padding: 14px 18px; border-radius: 0 8px 8px 0; font-size: 13px; line-height: 1.6; color: #1e293b; margin-bottom: 24px;"">
                {notesHtml}
            </div>

            <!-- Quick Action Buttons for HR -->
            <div style=""background: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 12px; padding: 18px 20px; text-align: center; margin-top: 10px;"">
                <div style=""font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.6px; color: #475569; margin-bottom: 12px;"">
                    Quick HR Actions
                </div>
                <div>
                    <a href=""tel:{cleanPhone}"" style=""display: inline-block; background-color: #003d8f; color: #ffffff !important; padding: 9px 18px; border-radius: 8px; font-size: 13px; font-weight: 700; text-decoration: none; margin: 4px;"">
                        📞 Call Candidate
                    </a>
                    <a href=""https://wa.me/{waDigits}"" target=""_blank"" style=""display: inline-block; background-color: #22c55e; color: #ffffff !important; padding: 9px 18px; border-radius: 8px; font-size: 13px; font-weight: 700; text-decoration: none; margin: 4px;"">
                        💬 WhatsApp Candidate
                    </a>
                    <a href=""mailto:{applicantEmail}"" style=""display: inline-block; background-color: #475569; color: #ffffff !important; padding: 9px 18px; border-radius: 8px; font-size: 13px; font-weight: 700; text-decoration: none; margin: 4px;"">
                        ✉️ Email Candidate
                    </a>
                </div>
            </div>

        </div>

        <!-- Footer -->
        <div style=""background-color: #f8fafc; padding: 20px 30px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; line-height: 1.6;"">
            This is an automated recruitment notification from <a href=""https://www.djtrust.lk"" style=""color: #003d8f; text-decoration: none; font-weight: 700;"">D.J. Trust Careers Portal</a>.<br/>
            Please review the application and contact the candidate promptly.
        </div>
    </div>
</body>
</html>";

            var plainText = $@"
D.J. TRUST CAREERS - NEW APPLICATION
=====================================================
Position Applied    : {safeJobTitle}
Candidate Name      : {applicantName}
Contact Number      : {applicantPhone}
Email Address       : {applicantEmail}
Curriculum Vitae    : {(!string.IsNullOrWhiteSpace(cvFileName) ? cvFileName : "Not Provided")}
Cover Letter        : {(!string.IsNullOrWhiteSpace(coverLetterFileName) ? coverLetterFileName : "Not Provided")}

CANDIDATE NOTES:
{applicantNotes}

SUBMISSION INFO:
Submitted At        : {receivedTime} (Sri Lanka Time)
=====================================================
Automated notification from D.J. Trust Careers Portal (www.djtrust.lk)
";

            return await SendEmailAsync(recipients, emailSubject, htmlBody, plainText, applicantEmail, cancellationToken);
        }

        public async Task<bool> SendLoanApplicationNotificationAsync(
            LoanApplicationRequest request, 
            CancellationToken cancellationToken = default)
        {
            var brand = !string.IsNullOrWhiteSpace(request.Brandname) && request.Brandname != "-" && request.Brandname != "N/A" 
                ? request.Brandname 
                : (request.Brand ?? "-");

            var model = !string.IsNullOrWhiteSpace(request.Modelname) && request.Modelname != "-" && request.Modelname != "N/A" 
                ? request.Modelname 
                : (request.Model ?? "-");

            decimal? loanAmount = null;
            if (request.Loanamount != null && decimal.TryParse(request.Loanamount.ToString(), out var amt) && amt > 0)
            {
                loanAmount = amt;
            }

            int? noof = null;
            if (request.Noof != null && int.TryParse(request.Noof.ToString(), out var n) && n > 0)
            {
                noof = n;
            }

            return await SendLoanRequestNotificationAsync(
                request.Name ?? "-",
                request.Nic ?? "-",
                request.Mobile ?? "-",
                request.City ?? "-",
                brand,
                model,
                request.Vehicleno ?? "-",
                request.Condition ?? "-",
                request.RegYear ?? "-",
                loanAmount,
                noof,
                request.IDDocumentPath,
                request.BillingProofPath,
                cancellationToken);
        }

        public async Task<bool> SendLoanRequestNotificationAsync(
            string name,
            string nic,
            string mobile,
            string city,
            string brand,
            string model,
            string vehicleNo,
            string condition,
            string regYear,
            decimal? loanAmount = null,
            int? noOfInstallments = null,
            string? idDocumentPath = null,
            string? billingProofPath = null,
            CancellationToken cancellationToken = default)
        {
            var recipients = GetLoanRecipients();
            if (recipients.Count == 0)
            {
                _logger.LogWarning("No notification recipients configured for Loan Request.");
                return false;
            }

            var receivedTime = DateTime.UtcNow.AddHours(5.5).ToString("yyyy-MM-dd hh:mm tt");

            // Format phone numbers for call and WhatsApp
            var digitsOnly = new string((mobile ?? string.Empty).Where(char.IsDigit).ToArray());
            var waDigits = digitsOnly.StartsWith("0") ? "94" + digitsOnly.Substring(1) : (digitsOnly.StartsWith("94") ? digitsOnly : "94" + digitsOnly);
            var cleanPhone = digitsOnly.StartsWith("0") ? digitsOnly : "0" + digitsOnly;

            // Detect if this is Form 1 (Vehicle Loan) or Form 2 (Finance/Cash Loan) or hybrid
            var hasVehicleInfo = (!string.IsNullOrWhiteSpace(brand) && brand != "-" && brand != "N/A") || 
                                 (!string.IsNullOrWhiteSpace(condition) && condition != "-");

            var hasFinanceInfo = (loanAmount.HasValue && loanAmount.Value > 0) || 
                                 (noOfInstallments.HasValue && noOfInstallments.Value > 0) || 
                                 (!string.IsNullOrWhiteSpace(vehicleNo) && vehicleNo.Contains("Loan", StringComparison.OrdinalIgnoreCase));

            // Dynamic Subject Line
            string emailSubject;
            string formCategoryTitle;
            if (hasVehicleInfo && !hasFinanceInfo)
            {
                emailSubject = $"[D.J. Trust Vehicle Loan] - {brand} {model} - {name} ({mobile})";
                formCategoryTitle = "Vehicle Loan Request";
            }
            else if (hasFinanceInfo && !hasVehicleInfo)
            {
                var facilityName = (!string.IsNullOrWhiteSpace(vehicleNo) && vehicleNo != "-" && !vehicleNo.Equals("Loan Application", StringComparison.OrdinalIgnoreCase)) 
                    ? vehicleNo 
                    : "Finance Facility";
                emailSubject = $"[D.J. Trust Loan Application] - {facilityName} - {name} ({mobile})";
                formCategoryTitle = "Finance Facility Request";
            }
            else
            {
                emailSubject = $"[D.J. Trust Loan Application] - {name} ({mobile})";
                formCategoryTitle = "Online Loan Application";
            }

            var isBrandNew = (condition ?? string.Empty).Trim().Equals("Brand New", StringComparison.OrdinalIgnoreCase);
            var isSecondHand = (condition ?? string.Empty).Trim().Equals("Second Hand", StringComparison.OrdinalIgnoreCase);

            var conditionBadge = isBrandNew 
                ? "<span style=\"display: inline-block; padding: 4px 12px; border-radius: 9999px; font-size: 12px; font-weight: 700; background-color: #dcfce7; color: #15803d; border: 1px solid #bbf7d0;\">Brand New</span>"
                : (isSecondHand 
                    ? "<span style=\"display: inline-block; padding: 4px 12px; border-radius: 9999px; font-size: 12px; font-weight: 700; background-color: #fef3c7; color: #b45309; border: 1px solid #fde68a;\">Second Hand</span>"
                    : $"<span style=\"display: inline-block; padding: 4px 12px; border-radius: 9999px; font-size: 12px; font-weight: 600; background-color: #f1f5f9; color: #475569;\">{condition}</span>");

            var regYearDisplay = string.IsNullOrWhiteSpace(regYear) || regYear == "-" 
                ? (isBrandNew ? "Brand New (Unregistered)" : "Not Specified") 
                : regYear;

            var vehicleRegDisplay = string.IsNullOrWhiteSpace(vehicleNo) || vehicleNo == "-" || vehicleNo.Equals("Loan Application", StringComparison.OrdinalIgnoreCase)
                ? (isBrandNew ? "Unregistered (Brand New)" : "Not provided / Unregistered")
                : vehicleNo;

            var loanAmountDisplay = loanAmount.HasValue && loanAmount.Value > 0 
                ? $"Rs. {loanAmount.Value:N2}" 
                : "Not specified";

            var installmentDisplay = noOfInstallments.HasValue && noOfInstallments.Value > 0 
                ? $"{noOfInstallments.Value} Months" 
                : "Flexible";

            var idDocDisplay = !string.IsNullOrWhiteSpace(idDocumentPath) ? $"Uploaded ({idDocumentPath})" : "Not Uploaded";
            var billingProofDisplay = !string.IsNullOrWhiteSpace(billingProofPath) ? $"Uploaded ({billingProofPath})" : "Not Uploaded";

            // Vehicle HTML Section (Form 1)
            var vehicleHtmlSection = "";
            if (hasVehicleInfo || (!hasFinanceInfo && !hasVehicleInfo))
            {
                vehicleHtmlSection = $@"
            <!-- Section 2: Vehicle Requirements (Form 1) -->
            <div style=""font-size: 13px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.8px; color: #003d8f; margin-bottom: 12px; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;"">
                🏍️ Vehicle Requirements
            </div>
            <table style=""width: 100%; border-collapse: separate; border-spacing: 0; margin-bottom: 24px;"">
                <tr style=""background-color: #f8fafc;"">
                    <td style=""width: 38%; padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Preferred Make:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; color: #0f172a; border-bottom: 1px solid #edf2f7;"">{brand}</td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Preferred Model:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; color: #0f172a; border-bottom: 1px solid #edf2f7;"">{model}</td>
                </tr>
                <tr style=""background-color: #f8fafc;"">
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Vehicle Condition:</td>
                    <td style=""padding: 11px 12px; border-bottom: 1px solid #edf2f7;"">{conditionBadge}</td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Registration Year:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 700; border-bottom: 1px solid #edf2f7;"">
                        <span style=""display: inline-block; background: #e0f2fe; color: #0369a1; border: 1px solid #bae6fd; padding: 3px 10px; border-radius: 6px; font-family: monospace;"">{regYearDisplay}</span>
                    </td>
                </tr>
                <tr style=""background-color: #f8fafc;"">
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Registration Number:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 700; color: #1e293b; border-bottom: 1px solid #edf2f7;"">{vehicleRegDisplay}</td>
                </tr>
            </table>";
            }

            // Finance HTML Section (Form 2)
            var financeHtmlSection = "";
            if (hasFinanceInfo)
            {
                financeHtmlSection = $@"
            <!-- Section 3: Finance Requirements (Form 2) -->
            <div style=""font-size: 13px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.8px; color: #003d8f; margin-bottom: 12px; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;"">
                💼 Loan & Finance Details
            </div>
            <table style=""width: 100%; border-collapse: separate; border-spacing: 0; margin-bottom: 24px;"">
                <tr style=""background-color: #f8fafc;"">
                    <td style=""width: 38%; padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Loan Facility:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; color: #0f172a; border-bottom: 1px solid #edf2f7;"">{vehicleNo}</td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Requested Loan Amount:</td>
                    <td style=""padding: 11px 12px; font-size: 15px; font-weight: 800; color: #003d8f; border-bottom: 1px solid #edf2f7;"">{loanAmountDisplay}</td>
                </tr>
                <tr style=""background-color: #f8fafc;"">
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Repayment Period:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; color: #1e293b; border-bottom: 1px solid #edf2f7;"">{installmentDisplay}</td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">National ID Document:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">{idDocDisplay}</td>
                </tr>
                <tr style=""background-color: #f8fafc;"">
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Billing Proof Document:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">{billingProofDisplay}</td>
                </tr>
            </table>";
            }

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>New Loan Application</title>
</head>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; margin: 0; padding: 24px 12px;"">
    <div style=""max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.08); border: 1px solid #e2e8f0;"">
        
        <!-- Header -->
        <div style=""background: linear-gradient(135deg, #003d8f 0%, #001f4d 100%); padding: 30px; color: #ffffff;"">
            <div style=""display: inline-block; background: rgba(255,255,255,0.18); border: 1px solid rgba(255,255,255,0.25); color: #ffffff; padding: 4px 12px; border-radius: 9999px; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.6px; margin-bottom: 12px;"">
                {formCategoryTitle}
            </div>
            <h1 style=""margin: 0 0 6px 0; font-size: 22px; font-weight: 800; letter-spacing: -0.4px;"">
                New Loan Application
            </h1>
            <p style=""margin: 0; font-size: 13px; color: #cbd5e1; line-height: 1.5;"">
                A customer has submitted a new loan request through the D.J. Trust website.
            </p>
        </div>

        <!-- Content Body -->
        <div style=""padding: 28px 24px;"">

            <!-- Section 1: Applicant Details -->
            <div style=""font-size: 13px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.8px; color: #003d8f; margin-bottom: 12px; border-bottom: 2px solid #e2e8f0; padding-bottom: 6px;"">
                👤 Applicant Details
            </div>
            <table style=""width: 100%; border-collapse: separate; border-spacing: 0; margin-bottom: 24px;"">
                <tr style=""background-color: #f8fafc;"">
                    <td style=""width: 38%; padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Name With Initials:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; color: #0f172a; border-bottom: 1px solid #edf2f7;"">{name}</td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">NIC Number:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; font-family: monospace; font-weight: 700; color: #1e293b; border-bottom: 1px solid #edf2f7;"">
                        <span style=""background: #f1f5f9; padding: 3px 8px; border-radius: 4px; border: 1px solid #cbd5e1;"">{nic}</span>
                    </td>
                </tr>
                <tr style=""background-color: #f8fafc;"">
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Contact Number:</td>
                    <td style=""padding: 11px 12px; font-size: 14px; font-weight: 700; border-bottom: 1px solid #edf2f7;"">
                        <a href=""tel:{cleanPhone}"" style=""color: #003d8f; text-decoration: none;"">{mobile}</a>
                    </td>
                </tr>
                <tr>
                    <td style=""padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Personal Address / City:</td>
                    <td style=""padding: 11px 12px; font-size: 13px; color: #1e293b; font-weight: 600; border-bottom: 1px solid #edf2f7;"">{city}</td>
                </tr>
            </table>

            {vehicleHtmlSection}

            {financeHtmlSection}

            <!-- Submission Timestamp -->
            <table style=""width: 100%; border-collapse: separate; border-spacing: 0; margin-bottom: 24px;"">
                <tr style=""background-color: #f8fafc;"">
                    <td style=""width: 38%; padding: 11px 12px; font-size: 13px; font-weight: 600; color: #475569; border-bottom: 1px solid #edf2f7;"">Submission Date & Time:</td>
                    <td style=""padding: 11px 12px; font-size: 12px; color: #64748b; font-weight: 600; border-bottom: 1px solid #edf2f7;"">{receivedTime} (Sri Lanka Time)</td>
                </tr>
            </table>

            <!-- Quick Action Buttons for Loan Officer -->
            <div style=""background: #f8fafc; border: 1px dashed #cbd5e1; border-radius: 12px; padding: 18px 20px; text-align: center; margin-top: 20px;"">
                <div style=""font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.6px; color: #475569; margin-bottom: 12px;"">
                    Quick Contact Actions
                </div>
                <div>
                    <a href=""tel:{cleanPhone}"" style=""display: inline-block; background-color: #003d8f; color: #ffffff !important; padding: 9px 18px; border-radius: 8px; font-size: 13px; font-weight: 700; text-decoration: none; margin: 4px;"">
                        📞 Call Applicant
                    </a>
                    <a href=""https://wa.me/{waDigits}"" target=""_blank"" style=""display: inline-block; background-color: #22c55e; color: #ffffff !important; padding: 9px 18px; border-radius: 8px; font-size: 13px; font-weight: 700; text-decoration: none; margin: 4px;"">
                        💬 WhatsApp Applicant
                    </a>
                </div>
            </div>

        </div>

        <!-- Footer -->
        <div style=""background-color: #f8fafc; padding: 20px 30px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; line-height: 1.6;"">
            This is an automated notification from <a href=""https://www.djtrust.lk"" style=""color: #003d8f; text-decoration: none; font-weight: 700;"">D.J. Trust Website</a>.<br/>
            Please review the application and contact the customer promptly.
        </div>
    </div>
</body>
</html>";

            var plainText = $@"
D.J. TRUST - NEW LOAN APPLICATION ({formCategoryTitle})
=====================================================
APPLICANT DETAILS:
Name With Initials        : {name}
NIC Number                : {nic}
Contact Number            : {mobile}
Personal Address / City   : {city}
" + (hasVehicleInfo ? $@"
VEHICLE REQUIREMENTS:
Preferred Make            : {brand}
Preferred Model           : {model}
Vehicle Condition         : {condition}
Registration Year         : {regYearDisplay}
Registration Number       : {vehicleRegDisplay}
" : "") + (hasFinanceInfo ? $@"
FINANCE REQUIREMENTS:
Loan Facility             : {vehicleNo}
Requested Loan Amount     : {loanAmountDisplay}
Repayment Period          : {installmentDisplay}
NIC Document              : {idDocDisplay}
Billing Proof             : {billingProofDisplay}
" : "") + $@"
SUBMISSION INFO:
Submitted At              : {receivedTime} (Sri Lanka Time)
=====================================================
Automated notification from D.J. Trust Website (www.djtrust.lk)
";

            return await SendEmailAsync(recipients, emailSubject, htmlBody, plainText, null, cancellationToken);
        }

        public async Task<(bool success, string message)> SendTestLoanEmailAsync(string? recipient = null, string formType = "vehicle", CancellationToken cancellationToken = default)
        {
            var recipients = string.IsNullOrWhiteSpace(recipient) 
                ? GetLoanRecipients() 
                : new List<string> { recipient.Trim() };

            if (formType.Equals("finance", StringComparison.OrdinalIgnoreCase))
            {
                // Test Form 2: Finance Modal (Cash / Loan Amount & Installments)
                return await SendLoanRequestNotificationToRecipientsAsync(
                    recipients,
                    "K.M. Perera",
                    "981234567V",
                    "071 987 6543",
                    "Galle",
                    "-",
                    "-",
                    "Motorcycle Loan Application",
                    "-",
                    "-",
                    350000,
                    24,
                    "sample-nic.jpg",
                    "sample-electricity-bill.pdf",
                    cancellationToken);
            }
            else
            {
                // Test Form 1: Vehicle Loan Modal (Make, Model, Condition, Reg Year, Reg No)
                return await SendLoanRequestNotificationToRecipientsAsync(
                    recipients,
                    "J.A. Ranasinghe",
                    "950000000V",
                    "077 123 4567",
                    "Ambalangoda",
                    "HONDA",
                    "DIO 110",
                    "CP BFD-1234",
                    "Second Hand",
                    "2020",
                    null,
                    null,
                    null,
                    null,
                    cancellationToken);
            }
        }

        private async Task<(bool success, string message)> SendLoanRequestNotificationToRecipientsAsync(
            List<string> recipients,
            string name,
            string nic,
            string mobile,
            string city,
            string brand,
            string model,
            string vehicleNo,
            string condition,
            string regYear,
            decimal? loanAmount = null,
            int? noOfInstallments = null,
            string? idDocumentPath = null,
            string? billingProofPath = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var ok = await SendLoanRequestNotificationAsync(
                    name, 
                    nic, 
                    mobile, 
                    city, 
                    brand, 
                    model, 
                    vehicleNo, 
                    condition, 
                    regYear, 
                    loanAmount, 
                    noOfInstallments, 
                    idDocumentPath, 
                    billingProofPath, 
                    cancellationToken);

                return ok 
                    ? (true, $"Loan request email successfully sent to: {string.Join(", ", recipients)}") 
                    : (false, "Failed to send loan request email. Check logs for details.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching loan request email.");
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> SendTestEmailAsync(string? recipient = null, CancellationToken cancellationToken = default)
        {
            var recipients = string.IsNullOrWhiteSpace(recipient) 
                ? GetInquiryRecipients() 
                : new List<string> { recipient.Trim() };

            if (recipients.Count == 0)
            {
                return (false, "No recipients specified or found in configuration.");
            }

            try
            {
                var ok = await SendInquiryNotificationAsync(
                    "Kamal Gunasekara (Test Inquiry)",
                    "077 555 1234",
                    "kamal@example.com",
                    "Customer Inquiry Test from Contact Us Page",
                    "This is a test web inquiry to verify that the Contact Us form dispatches properly to Reception and Developer emails.",
                    cancellationToken);

                return ok 
                    ? (true, $"Inquiry test email successfully sent to: {string.Join(", ", recipients)}") 
                    : (false, "Failed to send test email. Check server logs for details.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test email dispatch threw an exception.");
                return (false, $"Error: {ex.Message}");
            }
        }

        private async Task<bool> SendEmailAsync(
            List<string> recipients,
            string subject,
            string htmlBody,
            string plainText,
            string? replyToEmail = null,
            CancellationToken cancellationToken = default)
        {
            var senderEmail = _config["SmtpSettings:SenderEmail"] 
                ?? _config["AppSettings:SupportEmail"] 
                ?? "webmaster@djtrust.lk";
            var senderName = _config["SmtpSettings:SenderName"] 
                ?? _config["AppSettings:SenderName"] 
                ?? "D.J. Trust Website";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));

            foreach (var r in recipients)
            {
                if (MailboxAddress.TryParse(r.Trim(), out var mailbox))
                {
                    message.To.Add(mailbox);
                }
            }

            if (message.To.Count == 0)
            {
                _logger.LogWarning("No valid recipient email addresses parsed.");
                return false;
            }

            // Add BCC recipients (e.g. Developer / Auditor copy - sent privately)
            var bccList = GetBccRecipients();
            foreach (var b in bccList)
            {
                if (MailboxAddress.TryParse(b.Trim(), out var bccBox))
                {
                    if (!message.To.Mailboxes.Any(m => m.Address.Equals(bccBox.Address, StringComparison.OrdinalIgnoreCase)) &&
                        !message.Bcc.Mailboxes.Any(m => m.Address.Equals(bccBox.Address, StringComparison.OrdinalIgnoreCase)))
                    {
                        message.Bcc.Add(bccBox);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(replyToEmail) && MailboxAddress.TryParse(replyToEmail.Trim(), out var replyMailbox))
            {
                message.ReplyTo.Add(replyMailbox);
            }

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = plainText
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Smtp server resolution:
            // 1. Direct Office 365 SMTP AUTH (if username/password configured)
            // 2. Or Microsoft 365 Direct Send endpoint (Port 25, no credentials needed for internal tenant domains)
            var smtpHost = _config["SmtpSettings:Server"] 
                ?? Environment.GetEnvironmentVariable("SMTP_SERVER") 
                ?? "djtrust-lk.mail.protection.outlook.com";

            var smtpPortStr = _config["SmtpSettings:Port"] 
                ?? Environment.GetEnvironmentVariable("SMTP_PORT");
            
            var smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 25;

            var username = _config["SmtpSettings:Username"] 
                ?? Environment.GetEnvironmentVariable("SMTP_USERNAME");

            var password = _config["SmtpSettings:Password"] 
                ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD");

            // If username & password are provided, prefer smtp.office365.com:587
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                if (smtpHost.Contains("mail.protection.outlook.com", StringComparison.OrdinalIgnoreCase))
                {
                    smtpHost = "smtp.office365.com";
                    smtpPort = 587;
                }
            }

            // Attempt 1: Configured SMTP host
            var sent = await TrySendWithHostAsync(message, smtpHost, smtpPort, username, password, cancellationToken);
            if (sent) return true;

            // Attempt 2 Fallback: If primary was smtp.office365.com and failed, fallback to Microsoft 365 Direct Send
            if (!smtpHost.Equals("djtrust-lk.mail.protection.outlook.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Attempting fallback to Microsoft 365 Direct Send (djtrust-lk.mail.protection.outlook.com:25)...");
                sent = await TrySendWithHostAsync(message, "djtrust-lk.mail.protection.outlook.com", 25, null, null, cancellationToken);
                if (sent) return true;
            }

            // Attempt 3 Fallback: smtp.office365.com:587 only if password is provided
            if (!string.IsNullOrWhiteSpace(password) && !smtpHost.Equals("smtp.office365.com", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Attempting fallback to smtp.office365.com:587...");
                sent = await TrySendWithHostAsync(message, "smtp.office365.com", 587, username, password, cancellationToken);
                if (sent) return true;
            }

            _logger.LogError("Failed to deliver notification email to all candidates.");
            return false;
        }

        private async Task<bool> TrySendWithHostAsync(
            MimeMessage message, 
            string host, 
            int port, 
            string? username, 
            string? password, 
            CancellationToken cancellationToken)
        {
            using var client = new SmtpClient();
            try
            {
                // Timeout after 12 seconds per attempt
                client.Timeout = 12000;

                // Accept all SSL certificates for enterprise mail gateways / self-signed relays
                client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                var socketOption = port == 465 
                    ? SecureSocketOptions.SslOnConnect 
                    : (port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.StartTlsWhenAvailable);

                _logger.LogInformation("Connecting to SMTP host {Host}:{Port} (Option: {SocketOption})...", host, port, socketOption);
                await client.ConnectAsync(host, port, socketOption, cancellationToken);

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogInformation("Authenticating with SMTP server as {User}...", username);
                    await client.AuthenticateAsync(username, password, cancellationToken);
                }

                var bccSummary = message.Bcc.Count > 0 
                    ? $" (BCC: {string.Join(", ", message.Bcc.Select(b => b.ToString()))})" 
                    : string.Empty;
                _logger.LogInformation("Sending email notification to {Recipients}{Bcc}...", string.Join(", ", message.To.Select(t => t.ToString())), bccSummary);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Email notification sent successfully via {Host}:{Port}!", host, port);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed sending email through SMTP {Host}:{Port}: {Message}", host, port, ex.Message);
                try { if (client.IsConnected) await client.DisconnectAsync(true, cancellationToken); } catch { }
                return false;
            }
        }

        public List<string> GetCareersRecipients()
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var fromConfig = _config.GetSection("AppSettings:NotificationRecipients:Careers").Get<string[]>();
            if (fromConfig != null && fromConfig.Length > 0)
            {
                foreach (var email in fromConfig)
                {
                    if (!string.IsNullOrWhiteSpace(email)) list.Add(email.Trim());
                }
            }

            var hrEmail = _config["AppSettings:CareersEmail"] ?? "careers@djtrust.lk";
            if (!string.IsNullOrWhiteSpace(hrEmail)) list.Add(hrEmail.Trim());

            return list.ToList();
        }

        public List<string> GetLoanRecipients()
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var fromConfig = _config.GetSection("AppSettings:NotificationRecipients:Loans").Get<string[]>();
            if (fromConfig != null && fromConfig.Length > 0)
            {
                foreach (var email in fromConfig)
                {
                    if (!string.IsNullOrWhiteSpace(email)) list.Add(email.Trim());
                }
            }

            var salesEmail = _config["AppSettings:SalesEmail"] ?? "marketing@djtrust.lk";
            if (!string.IsNullOrWhiteSpace(salesEmail)) list.Add(salesEmail.Trim());

            return list.ToList();
        }

        public List<string> GetInquiryRecipients()
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var fromConfig = _config.GetSection("AppSettings:NotificationRecipients:Inquiries").Get<string[]>();
            if (fromConfig != null && fromConfig.Length > 0)
            {
                foreach (var email in fromConfig)
                {
                    if (!string.IsNullOrWhiteSpace(email)) list.Add(email.Trim());
                }
            }

            var supportEmail = _config["AppSettings:SupportEmail"] ?? "info@djtrust.lk";
            if (!string.IsNullOrWhiteSpace(supportEmail)) list.Add(supportEmail.Trim());

            return list.ToList();
        }

        public List<string> GetBccRecipients()
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var fromConfig = _config.GetSection("AppSettings:NotificationRecipients:Bcc").Get<string[]>();
            if (fromConfig != null && fromConfig.Length > 0)
            {
                foreach (var email in fromConfig)
                {
                    if (!string.IsNullOrWhiteSpace(email)) list.Add(email.Trim());
                }
            }

            var devEmail = _config["AppSettings:DeveloperEmail"] ?? "dinupad@djtrust.lk";
            if (!string.IsNullOrWhiteSpace(devEmail)) list.Add(devEmail.Trim());

            return list.ToList();
        }

        public List<string> GetNotificationRecipients()
        {
            return GetBccRecipients();
        }
    }
}

