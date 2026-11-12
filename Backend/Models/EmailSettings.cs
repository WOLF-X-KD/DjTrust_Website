namespace DjTrust.Api.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "djtrust-lk.mail.protection.outlook.com";
        public int SmtpPort { get; set; } = 25;
        public string SenderName { get; set; } = "D.J. Trust Website";
        public string SenderEmail { get; set; } = "webmaster@djtrust.lk";
        public bool EnableSsl { get; set; } = true;
    }
}

