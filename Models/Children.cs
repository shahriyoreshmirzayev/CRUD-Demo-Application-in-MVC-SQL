namespace CRUDDEMO1.Models;

public class Children
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Gender { get; set; } = "";
    public int? Age { get; set; }
    public string School { get; set; } = "";
    public string Grade { get; set; } = "";
    public int EmployeeId { get; set; } // Foreign Key
    public Employee? Employee { get; set; } // Navigation Property
}
