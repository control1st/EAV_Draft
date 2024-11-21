using System.ComponentModel.DataAnnotations;

namespace EAV_Draft.Models
{

        public class Attribute
        {
        [Key]
            public int AttributeId { get; set; }
            public int TenantId { get; set; } 
            public string Name { get; set; } 
            public string DataType { get; set; } 
        }    
}
