using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaestroSport.Models
{
    // كلاس لتمثيل أكواد الخصم في قاعدة البيانات
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "كود الخصم مطلوب")]
        [Display(Name = "كود الخصم")]
        public string Code { get; set; } // مثال: MYSTRO20

        [Required(ErrorMessage = "قيمة الخصم مطلوبة")]
        [Display(Name = "قيمة الخصم (ريال)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "تاريخ الانتهاء")]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}