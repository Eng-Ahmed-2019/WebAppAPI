using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Categories.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(50, ErrorMessage = "Number of characters must be less than or equal to 50")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Number of characters must be less than or equal to 200")]
        public string? Description { get; set; }
    }
}