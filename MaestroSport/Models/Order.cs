using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaestroSport.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "اسم الزبون")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "صورة التصميم المرفقة")]
        public string? CustomDesignImageUrl { get; set; }

        public string FabricType { get; set; } = "قياسي";

        [Column(TypeName = "decimal(18,2)")]
        public decimal FabricExtraPrice { get; set; }

        public string? CouponCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // --- حقول نظام الدفع الجديدة ---
        [Display(Name = "نظام الدفع")]
        public string PaymentType { get; set; } = "كامل"; // "كامل" أو "عربون"

        [Display(Name = "المدفوع الآن")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Display(Name = "المتبقي عند الاستلام")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public DateTime ExpectedDeliveryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "قيد المراجعة";

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }


public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public int SizeId { get; set; }
        [ForeignKey("SizeId")]
        public Size Size { get; set; }

        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر الوحدة وقت الطلب")]
        public decimal UnitPrice { get; set; } // السعر وقت الطلب شامل الزيادات (مقاس، تصميم، إلخ)

        [Display(Name = "صورة التصميم المرفقة")]
        public string CustomDesignImageUrl { get; set; } // في حال كان Product.IsCustomDesign = true
    }
}