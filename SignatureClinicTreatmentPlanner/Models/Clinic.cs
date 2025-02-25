using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SignatureClinicTreatmentPlanner.Models
{
    public class Clinic
    {
        [Key]
        [Column("ClinicID")] // Mapping to the exact column name in DB
        public int ClinicID { get; set; }

        [Required(ErrorMessage = "Clinic name is required.")]
        [StringLength(100, ErrorMessage = "Clinic name cannot exceed 100 characters.")]
        public string ClinicName { get; set; }
    }
}
