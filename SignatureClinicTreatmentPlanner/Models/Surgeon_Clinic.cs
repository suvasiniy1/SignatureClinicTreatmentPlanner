using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SignatureClinicTreatmentPlanner.Models
{
    public class Surgeon_Clinic
    {

        [Key]
        [Column("SurgeonID")] // Mapping to the exact column name in DB
        public int SurgeonID { get; set; }

        [Key]
        [Column("ClinicID")] // Mapping to the exact column name in DB
        public int ClinicID { get; set; }

        [ForeignKey("SurgeonID")]
        public Surgeon Surgeon { get; set; }

        [ForeignKey("ClinicID")]
        public Clinic Clinic { get; set; }
    }
}
