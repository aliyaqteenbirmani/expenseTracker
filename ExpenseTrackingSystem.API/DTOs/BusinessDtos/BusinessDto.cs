using Microsoft.AspNetCore.Http;

namespace SpendwiseSystem.Domain.DTOs.BusinessDtos
{
    public class BusinessDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null;
    }
}
