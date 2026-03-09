using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaestroSport.Models
{
    public class Fabric
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القماش مطلوب")]
        [Display(Name = "نوع القماش")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "السعر الإضافي للقطعة (ريال)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; }
    }
}