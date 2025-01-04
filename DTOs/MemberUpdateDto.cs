using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace personsapi.DTOs
{
    public class MemberUpdateDto
    {
        public required string Introduction { get; set; }
        public required string LookingFor { get; set; }
        public required string Interests { get; set; }
        public required string City { get; set; }
        public required string Country { get; set; }
    }
}