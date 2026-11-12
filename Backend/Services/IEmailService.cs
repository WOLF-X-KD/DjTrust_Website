using DjTrust.Api.Models;

namespace DjTrust.Api.Services
{
    public interface IEmailService
    {
        Task<bool> SendInquiryNotificationAsync(
            string name,
            string phone,
            string email,
            string subject,
            string message,
            CancellationToken cancellationToken = default);

        Task<bool> SendJobApplicationNotificationAsync(
            string jobTitle,
            string applicantName,
            string applicantEmail,
            string applicantPhone,
            string? applicantNotes,
            string? cvFileName,
            string? coverLetterFileName,
            CancellationToken cancellationToken = default);

        Task<bool> SendLoanApplicationNotificationAsync(
            LoanApplicationRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> SendLoanRequestNotificationAsync(
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
            CancellationToken cancellationToken = default);

        Task<(bool success, string message)> SendTestEmailAsync(string? recipient = null, CancellationToken cancellationToken = default);

        Task<(bool success, string message)> SendTestLoanEmailAsync(string? recipient = null, string formType = "vehicle", CancellationToken cancellationToken = default);
    }
}


