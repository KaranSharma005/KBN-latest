namespace KBN.Models
{
    public class EmailSettingsModal
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FromEmail { get; set; }
    }
}
