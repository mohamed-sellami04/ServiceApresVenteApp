using System.ComponentModel.DataAnnotations;

namespace ServiceApresVenteApp.Models
{
    public class SparePart
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The Name is required.")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Price is required.")]
        [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10,000.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "The Intervention is required.")]
        public int InterventionId { get; set; }

        public Intervention? Intervention { get; set; }
    }
}
