using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignatureClinicTreatmentPlanner.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int Treatment { get; set; }

        //[Required]
        //public List<int> SurgeonIDs { get; set; } = new List<int>();

        [Required]
        public int Clinic { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
        [Required]
        public int SurgeonID { get; set; }


        // public List<PatientSurgeons> PatientSurgeons { get; set; } = new List<PatientSurgeons>();
    }
}
