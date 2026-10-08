using System.ComponentModel.DataAnnotations;

namespace ServiceApresVenteApp.Models
{
    public class Intervention
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "The Intervention Date is required.")]
        [DataType(DataType.Date)]
        public DateTime InterventionDate { get; set; }

        [Required(ErrorMessage = "The Complaint is required.")]
        public int ComplaintId { get; set; }

        [Range(0, 10000, ErrorMessage = "Labor cost must be between 0 and 10,000.")]
        public decimal LaborCost { get; set; } = 0;

        public bool IsFree { get; set; } = false;

        [DataType(DataType.Currency)]
        public decimal TotalCost => IsFree ? 0 : LaborCost + SpareParts.Sum(sp => sp.Price);

        public Complaint Complaint { get; set; }
        public List<SparePart> SpareParts { get; set; } = new List<SparePart>();
    }
}
