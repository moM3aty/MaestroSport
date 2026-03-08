using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MaestroSport.Models
{
    // هذا الكلاس يمثل الأقسام (بدلات، شورتات، إلخ)
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [Display(Name = "اسم القسم")]
        public string Name { get; set; }

        [Display(Name = "أيقونة القسم (FontAwesome)")]
        public string IconClass { get; set; } // مثال: fa-tshirt

        [Display(Name = "لون الأيقونة")]
        public string ColorClass { get; set; } // مثال: blue أو green

        public int DisplayOrder { get; set; }

        // العلاقة مع المنتجات (كل قسم يحتوي على عدة منتجات)
        public ICollection<Product> Products { get; set; }
    }
}