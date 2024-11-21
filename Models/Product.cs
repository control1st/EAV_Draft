namespace EAV_Draft.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int TenantId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
