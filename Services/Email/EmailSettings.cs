namespace WorkoutTrackerWeb.Services.Email
{
    public class EmailSettings
    {
        public string MailServer { get; set; }
        public int MailPort { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public string AdminEmail { get; set; }
    }
}