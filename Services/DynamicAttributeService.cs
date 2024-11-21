using EAV_Draft.Dto_s;
using EAV_Draft.Models;
using Microsoft.EntityFrameworkCore;

namespace EAV_Draft.Services
{
    public class DynamicAttributeService
    {
        private readonly AppDbContext _context;

        public DynamicAttributeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddOrValidateDynamicAttributesAsync(int tenantId, int productId, List<DynamicAttributeDto> dynamicAttributes)
        {
            foreach (var dynamicAttribute in dynamicAttributes)
            {
                var dbAttribute = await _context.Attributes
                    .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Name == dynamicAttribute.Name);

                if (dbAttribute == null)
                {
                    dbAttribute = new Models.Attribute
                    {
                        TenantId = tenantId,
                        Name = dynamicAttribute.Name,
                        DataType = dynamicAttribute.Type 
                    };

                    _context.Attributes.Add(dbAttribute);
                    await _context.SaveChangesAsync(); 
                }

                string valueToStore;
                switch (dynamicAttribute.Type.ToLower())
                {
                    case "string":
                        valueToStore = dynamicAttribute.Value;
                        break;
                    case "decimal":
                        if (!decimal.TryParse(dynamicAttribute.Value, out _))
                            throw new ArgumentException($"Invalid value for attribute '{dynamicAttribute.Name}' of type '{dynamicAttribute.Type}'.");
                        valueToStore = dynamicAttribute.Value;
                        break;
                    case "int":
                        if (!int.TryParse(dynamicAttribute.Value, out _))
                            throw new ArgumentException($"Invalid value for attribute '{dynamicAttribute.Name}' of type '{dynamicAttribute.Type}'.");
                        valueToStore = dynamicAttribute.Value;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported attribute type: {dynamicAttribute.Type}");
                }

                var attributeValue = new AttributeValue
                {
                    ProductId = productId,
                    AttributeId = dbAttribute.AttributeId,
                    Value = valueToStore
                };

                _context.AttributeValues.Add(attributeValue);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateDynamicAttributesAsync(int productId, List<DynamicAttributeDto> dynamicAttributes)
        {
            foreach (var dynamicAttribute in dynamicAttributes)
            {              
                var dbAttribute = await _context.Attributes
                    .FirstOrDefaultAsync(a => a.Name == dynamicAttribute.Name);

                if (dbAttribute == null)
                {
                    dbAttribute = new Models.Attribute
                    {
                        Name = dynamicAttribute.Name,
                        DataType = dynamicAttribute.Type 
                    };

                    _context.Attributes.Add(dbAttribute);
                    await _context.SaveChangesAsync();
                }

                var attributeValue = await _context.AttributeValues
                    .FirstOrDefaultAsync(av => av.ProductId == productId && av.AttributeId == dbAttribute.AttributeId);

                if (attributeValue == null)
                {
                    attributeValue = new AttributeValue
                    {
                        ProductId = productId,
                        AttributeId = dbAttribute.AttributeId,
                        Value = dynamicAttribute.Value
                    };

                    _context.AttributeValues.Add(attributeValue);
                }
                else
                {
                    attributeValue.Value = dynamicAttribute.Value;
                    _context.AttributeValues.Update(attributeValue);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
