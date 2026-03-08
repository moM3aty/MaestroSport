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

        [Display(Name = "رقم الهاتف")]
        public string? PhoneNumber { get; set; }

        // --- الحقل الجديد: اسم الزبون ---
        [Display(Name = "اسم الزبون")]
        public string? CustomerName { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        // --- الحقل الجديد: صورة التصميم المرفقة من الزبون ---
        [Display(Name = "صورة التصميم المرفقة")]
        public string? CustomDesignImageUrl { get; set; }

        public string FabricType { get; set; } = "قياسي";
        public decimal FabricExtraPrice { get; set; }
        public string? CouponCode { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "قيد المراجعة";

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }


    // يمثل تفاصيل القطع داخل الطلب (مثال: طلب 3 قطع S و قطعتين 3XL من نفس الموديل)
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