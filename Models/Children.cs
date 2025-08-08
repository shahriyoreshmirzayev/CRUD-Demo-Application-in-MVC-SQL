using System.ComponentModel.DataAnnotations;

namespace CRUDDEMO1.Models;

public class Children
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Jins tanlanishi majburiy")]
    public string Gender { get; set; } = "";
    public int? Age { get; set; }
    public string School { get; set; } = "";
    public string Grade { get; set; } = "";

    [Required]
    public int EmployeeId { get; set; } 
    public Employee? Employee { get; set; } 
}
