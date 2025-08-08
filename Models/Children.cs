using System.ComponentModel.DataAnnotations;

namespace CRUDDEMO1.Models;

public class Children
{
    public int Id { get; set; }

    [StringLength(50, ErrorMessage = "Name 50 ta belgidan oshmasligi kerak")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Jins tanlanishi majburiy")]
    public string Gender { get; set; } = "";

    [Range(0, 50, ErrorMessage = "Yosh 0 dan 50 gacha bo'lishi kerak")]
    public int? Age { get; set; }
    public string School { get; set; } = "";
    public string Grade { get; set; } = "";

    [Required]
    public int EmployeeId { get; set; } 
    public Employee? Employee { get; set; } 
}
