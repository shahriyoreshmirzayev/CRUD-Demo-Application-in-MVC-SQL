namespace CRUDDEMO1.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Company { get; set; } = "";
    public string Department { get; set; } = "";

    //public ICollection<Children> Children { get; set; } = new List<Children>();
    public List<Children> Children { get; set; } = new List<Children>();
}
