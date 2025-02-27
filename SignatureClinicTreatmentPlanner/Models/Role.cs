using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignatureClinicTreatmentPlanner.Models
{
    [Table("Roles")]  // Ensure it maps correctly to your database
    public class Role : IdentityRole<int>
    {
        [Key]
        public override int Id { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Name { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
