using System.ComponentModel.DataAnnotations.Schema;

namespace SignatureClinicTreatmentPlanner.Models
{
    public class RoleDto
    {
        public int Id { get; set; }
        [Column("RoleName")] // Ensure alias is correctly mapped
        public string RoleName { get; set; }
    }

}
