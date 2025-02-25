using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignatureClinicTreatmentPlanner.Models
{
    public class Surgeon_Treatment
    {
        [Key]
        [Column("SurgeonID")]
        public int SurgeonID { get; set; }

        [Key]
        [Column("TreatmentID")]
        public int TreatmentID { get; set; }

        // Navigation Properties
        [ForeignKey("SurgeonID")]
        public Surgeon Surgeon { get; set; }

        [ForeignKey("TreatmentID")]
        public Treatment Treatment { get; set; }
    }
}
