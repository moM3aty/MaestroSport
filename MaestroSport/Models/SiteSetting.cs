using System.ComponentModel.DataAnnotations;

namespace MaestroSport.Models
{
    // كلاس جديد لحفظ إعدادات الموقع مثل شريط الإعلانات
    public class SiteSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }
    }
}