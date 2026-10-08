namespace ServiceApresVenteApp.Models
{
    public class Admin
    {
        public class ComplaintViewModel
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public string ClientName { get; set; }
            public string ArticleName { get; set; }
            public DateTime SubmissionDate { get; set; }
            public DateTime PurchaseDate { get; set; }
            public int WarrantyMonths { get; set; }
        }
    }
}
