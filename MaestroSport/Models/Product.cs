using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace MaestroSport.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الموديل مطلوب")]
        [Display(Name = "اسم الموديل")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "السعر الأساسي")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Display(Name = "صورة الموديل")]
        public string ImageUrl { get; set; }

        // خاصية لرفع الصورة من الجهاز
        [NotMapped]
        [Display(Name = "اختر صورة")]
        public IFormFile ImageFile { get; set; }

        [Display(Name = "هل هو تصميم خاص؟")]
        public bool IsCustomDesign { get; set; }

        [Display(Name = "القسم")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}