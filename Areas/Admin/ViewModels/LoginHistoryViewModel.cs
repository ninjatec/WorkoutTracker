using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Areas.Admin.ViewModels
{
    public class LoginHistoryViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Login Time")]
        public DateTime LoginTime { get; set; }
        
        [Display(Name = "IP Address")]
        public string IpAddress { get; set; }
        
        [Display(Name = "Device")]
        public string DeviceType { get; set; }
        
        [Display(Name = "Platform")]
        public string Platform { get; set; }
        
        [Display(Name = "Status")]
        public bool IsSuccessful { get; set; }
        
        public string FormattedLoginTime => LoginTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        
        public string StatusBadge => IsSuccessful 
            ? "<span class=\"badge bg-success\">Success</span>" 
            : "<span class=\"badge bg-danger\">Failed</span>";
    }
}