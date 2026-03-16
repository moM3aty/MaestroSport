using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaestroSport.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "كود الخصم مطلوب")]
        [Display(Name = "كود الخصم")]
        public string Code { get; set; }

        [Display(Name = "نسبة الخصم (%)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercentage { get; set; }

        [Display(Name = "هل هو كود قطعة مجانية؟")]
        public bool IsFreePiece { get; set; } = false;

        [Display(Name = "الحد الأدنى للقطع (اختياري)")]
        public int MinQuantity { get; set; } = 0; 

        [Display(Name = "مخصص لقسم محدد (اختياري)")]
        public int? TargetCategoryId { get; set; }

        [Display(Name = "مخصص لقماش محدد (اختياري)")]
        public string? TargetFabricName { get; set; }

        [Display(Name = "تاريخ الانتهاء (اختياري)")]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}