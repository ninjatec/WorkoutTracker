using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace WorkoutTrackerWeb.Models
{
    public class User
    {
        public int UserId { get; set; }
        public required string Name { get; set; }    
        public ICollection<Session> Session { get; set; }
    }
}