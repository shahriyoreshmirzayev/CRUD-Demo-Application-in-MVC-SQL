using CRUDDEMO1.Models;
using Microsoft.AspNetCore.Mvc;

namespace CRUDDEMO1.Controllers;

[Route("[controller]/[action]")]
public class EmployeeController : Controller
{
    Employee_dal employeeDAL = new Employee_dal();

    public IActionResult Index()
    {
        List<Employee> employees = employeeDAL.GetAllEmployee().ToList();
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
            try
            {
                int employeeId = employeeDAL.AddEmployee(employee);

                if (employeeId > 0)
                {
                    if (employee.Children != null && employee.Children.Count > 0)
                    {
                        foreach (var child in employee.Children)
                        {
                            child.EmployeeId = employeeId;
                            employeeDAL.CreateChildren(child);
                        }
                    }

                    TempData["SuccessMessage"] = $"Employee va {employee.Children?.Count ?? 0} ta bola muvaffaqiyatli saqlandi!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Employee saqlashda xato yuz berdi.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Xato: " + ex.Message);
            }
        }
        return View(employee);
    }

    public IActionResult Details(int? id)
    {
        if (id == null)
            return NotFound();

        Employee emp = employeeDAL.GetEmployeeWithChildrenById(id);
        if (emp == null)
            return NotFound();

        return View(emp);
    }

    [HttpGet]
    public IActionResult Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = employeeDAL.GetEmployeeWithChildrenById(id);
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
            try
            {
                bool result = employeeDAL.UpdateEmployee(employee);

                if (result)
                {
                    TempData["SuccessMessage"] = "Employee va bolalar ma'lumotlari muvaffaqiyatli yangilandi!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Ma'lumotlarni yangilashda xato yuz berdi.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Xato: " + ex.Message);
            }
        }

        employee = employeeDAL.GetEmployeeWithChildrenById(id);
        return View(employee);
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
        try
        {
            bool result = employeeDAL.DeleteEmployee(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Employee muvaffaqiyatli o'chirildi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Employee o'chirishda xato yuz berdi.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Xato: " + ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteChild(int childId, int employeeId)
    {
        try
        {
            bool result = employeeDAL.DeleteChild(childId);
            if (result)
            {
                TempData["SuccessMessage"] = "Bola ma'lumotlari muvaffaqiyatli o'chirildi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Bola ma'lumotlarini o'chirishda xato yuz berdi.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Xato: " + ex.Message;
        }

        return RedirectToAction("Edit", new { id = employeeId });
    }

    [HttpPost]
    public JsonResult UpdateChild([FromBody] Children child)
    {
        try
        {
            if (ModelState.IsValid)
            {
                bool result = employeeDAL.UpdateChildren(child);
                return Json(new
                {
                    success = result,
                    message = result ? "Muvaffaqiyatli yangilandi" : "Yangilashda xato"
                });
            }
            else
            {
                return Json(new { success = false, message = "Ma'lumotlar to'ldirilmagan" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Xato: " + ex.Message });
        }
    }
}