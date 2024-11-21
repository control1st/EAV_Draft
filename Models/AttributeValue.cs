using System.ComponentModel.DataAnnotations;

namespace EAV_Draft.Models
{
    public class AttributeValue
    {
        [Key]
        public int ValueId { get; set; }
        public int ProductId { get; set; } //  base product
        public int AttributeId { get; set; } 
        public string Value { get; set; } 
    }
}
