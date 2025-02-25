using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignatureClinicTreatmentPlanner.Models
{
    public class Surgeon
    {
        [Key]
        [Column("SurgeonID")] // Mapping to the exact column name in DB
        public int SurgeonID { get; set; }

        [Column("SurgeonName")] // Mapping to the exact column name in DB
        public string SurgeonName { get; set; }

        public string? PdfTemplate { get; set; } // Path to PDF file (optional)
        public List<PatientSurgeons> PatientSurgeons { get; set; } = new List<PatientSurgeons>();
    }
    public class PatientSurgeons
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int SurgeonId { get; set; }
        public Surgeon Surgeon { get; set; }
    }
}
