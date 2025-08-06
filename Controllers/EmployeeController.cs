using CRUDDEMO1.Models;
using Microsoft.AspNetCore.Mvc;

namespace CRUDDEMO1.Controllers;

[Route("[controller]/[action]")]
public class EmployeeController : Controller
{
    Employee_dal employeeDAL = new Employee_dal();
    public IActionResult Index()
    {
        List<Employee> employees = new List<Employee>();
        employees = employeeDAL.GetAllEmployee().ToList();
        return View(employees);
    }
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
   
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Employee employee)
    {
        if (ModelState.IsValid)
        {
            Employee_dal employeeDal = new Employee_dal();
            Children_dal childrenDal = new Children_dal();

            try
            {
                bool employeeResult = employeeDal.AddEmployee(employee);

                if (employeeResult)
                {
                    var allEmployees = employeeDal.GetAllEmployee();
                    var savedEmployee = allEmployees.OrderByDescending(e => e.Id).FirstOrDefault();

                    if (savedEmployee != null && employee.Children != null && employee.Children.Count > 0)
                    {
                        foreach (var child in employee.Children)
                        {
                            child.EmployeeId = savedEmployee.Id; 
                            childrenDal.AddChildren(child);
                        }
                    }

                    TempData["SuccessMessage"] = "Employee and children saved successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to save employee.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
        }

        return View(employee);
    }

    [HttpGet]
    public IActionResult Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Employee employee = employeeDAL.GetEmployeeWithChildrenById(id);
        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [Bind("Id,Name,Gender,Company,Department,Children")] Employee employee)
    {
        if (id != employee.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            bool result = employeeDAL.UpdateEmployee(employee); 
            if (result)
            {
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Xodim ma'lumotlarini yangilashda xato yuz berdi.");
        }

        employee = employeeDAL.GetEmployeeWithChildrenById(id);
        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditChild(int childId, [Bind("Id,Name,Gender,Age,School,Grade,EmployeeId")] Children child)
    {
        if (childId != child.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            bool result = employeeDAL.UpdateChildren(child);
            if (result)
            {
                return RedirectToAction("Details", new { id = child.EmployeeId });
            }
            ModelState.AddModelError("", "Bola ma'lumotlarini yangilashda xato yuz berdi.");
        }

        var employee = employeeDAL.GetEmployeeWithChildrenById(child.EmployeeId);
        return View("Edit", employee);
    }

    [HttpGet]
    public IActionResult Details(int? id)
    {
        if (id == null)
            return NotFound();

        Employee emp = employeeDAL.GetEmployeeWithChildrenById(id);
        if (emp == null)
            return NotFound();

        return View(emp);
    }
    public IActionResult Delete(int? id)
    {
        if (id == null)
            return NotFound();
        Employee emp = employeeDAL.GetEmployeeById(id);
        if (emp == null)
            return NotFound();
        return View(emp);
        
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteEmp(int? id)
    {
        employeeDAL.DeleteEmployee(id);
        return RedirectToAction("Index");
    }
}
