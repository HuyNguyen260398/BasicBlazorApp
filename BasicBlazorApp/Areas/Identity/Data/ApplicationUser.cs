using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBlazorApp.Areas.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string LastName { get; set; }

        [PersonalData]
        public string FirstName { get; set; }

        [PersonalData]
        public bool Gender { get; set; }

        [PersonalData]
        public string Address { get; set; }
    }
}
