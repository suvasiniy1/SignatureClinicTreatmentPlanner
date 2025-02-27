﻿namespace SignatureClinicTreatmentPlanner.Models
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
        public string PhoneNumber { get; set; }
    }
}
