using System.ComponentModel.DataAnnotations;

namespace CRUDDEMO1.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Jins tanlanishi majburiy")]
    public string Gender { get; set; } = "";
    public string Company { get; set; } = "";
    public string Department { get; set; } = "";

    //public ICollection<Children> Children { get; set; } = new List<Children>();
    public List<Children> Children { get; set; } = new List<Children>();
}
