using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SignatureClinicTreatmentPlanner.Models
{
    public class Treatment
    {
        [Key]
        [Column("TreatmentID")] // Map to the exact column name in the database
        public int TreatmentID { get; set; }

        [Column("TreatmentName")] // Map to the exact column name in the database
        public string TreatmentName { get; set; }
    }
}
