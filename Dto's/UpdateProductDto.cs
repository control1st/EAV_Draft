namespace EAV_Draft.Dto_s
{
    public class UpdateProductDto
    {
        public string? ProductName { get; set; } 
        public decimal? Price { get; set; } 
        public List<DynamicAttributeDto>? DynamicAttributes { get; set; }
    }
}
