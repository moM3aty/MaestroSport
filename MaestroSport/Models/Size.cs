using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaestroSport.Models
{
    // هذا الكلاس يمثل المقاسات المختلفة والفروقات في الأسعار
    public class Size
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المقاس")]
        public string Name { get; set; } // مثال: S, 3XL, 14

        [Display(Name = "مجموعة المقاسات")]
        public string GroupName { get; set; } // مثال: S-XXL أو 3XL-7XL لتنظيمها في لوحة التحكم

        [Required]
        [Display(Name = "السعر الإضافي")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; } // 0 للمقاسات العادية، ومبلغ إضافي للمقاسات الكبيرة
    }
}