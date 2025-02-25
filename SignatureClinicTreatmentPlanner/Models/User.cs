using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignatureClinicTreatmentPlanner.Models
{
    [Table("AspNetUsers")]  // Ensure it maps to your table
    public class User : IdentityUser<int>  // Use int as primary key
    {
        [Key]
        public override int Id { get; set; }

        [Required]
        public override string UserName { get; set; }
        public override string Email { get; set; }
        public override string? PhoneNumber { get; set; }

        public int RoleId { get; set; }  // Foreign Key for Role
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }  // ✅ Navigation Property

        public int HospitalID { get; set; }
        public override string? PasswordHash { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public override string? SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        public override string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        public bool IsActive { get; set; } = true;

        public string FirstName { get; set; }
        public string LastName { get; set; }
       
    }
}
