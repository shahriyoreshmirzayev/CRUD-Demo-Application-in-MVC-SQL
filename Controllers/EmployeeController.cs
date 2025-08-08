using CRUDDEMO1.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Newtonsoft.Json;

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

    // Export methods remain the same...
    public IActionResult ExportToExcel()
    {
        var employees = employeeDAL.GetAllEmployee().ToList();
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "Employee.xlsx");
        using (var package = new ExcelPackage(new FileInfo(templatePath)))
        {
            var worksheet = package.Workbook.Worksheets[0];

            for (int i = 0; i < employees.Count; i++)
            {
                int currentColumn = 1 + (i * 2);
                var employee = employees[i];
                if (employee == null) continue;

                var sourceRange = worksheet.Cells["A1:B2"];
                var targetRange = worksheet.Cells[1, currentColumn, 2, currentColumn + 1];
                sourceRange.Copy(targetRange);

                worksheet.Cells[1, currentColumn].Value = employee.Name;
                worksheet.Cells[2, currentColumn].Value = employee.Gender;
                worksheet.Cells[2, currentColumn + 1].Value = employee.Company;
            }

            var fileName = $"Employees_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(package.GetAsByteArray(),
                       "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                       fileName);
        }
    }

    public IActionResult ExportToPdf()
    {
        var employees = employeeDAL.GetAllEmployee();

        using (var stream = new MemoryStream())
        {
            Document document = new Document(PageSize.A4, 10, 10, 10, 10);
            PdfWriter writer = PdfWriter.GetInstance(document, stream);
            document.Open();
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var title = new Paragraph("Employee List", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);
            PdfPTable table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 3f, 2f, 3f, 3f });
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            table.AddCell(new PdfPCell(new Phrase("Name", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            table.AddCell(new PdfPCell(new Phrase("Gender", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            table.AddCell(new PdfPCell(new Phrase("Company", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            table.AddCell(new PdfPCell(new Phrase("Department", headerFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, Padding = 8 });
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            foreach (var employee in employees)
            {
                table.AddCell(new PdfPCell(new Phrase(employee.Name ?? "", cellFont)) { Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase(employee.Gender ?? "", cellFont)) { Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase(employee.Company ?? "", cellFont)) { Padding = 6 });
                table.AddCell(new PdfPCell(new Phrase(employee.Department ?? "", cellFont)) { Padding = 6 });
            }

            document.Add(table);
            document.Close();
            writer.Close();

            string fileName = $"Employees_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }
    }

    [HttpPost]
    public IActionResult ExportSelectedToExcel([FromBody] int[] selectedIds)
    {
        if (selectedIds == null || selectedIds.Length == 0)
        {
            return BadRequest("Hech qanday xodim tanlanmagan");
        }
        var allEmployees = employeeDAL.GetAllEmployee();
        var selectedEmployees = allEmployees.Where(e => selectedIds.Contains(e.Id)).ToList();
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Selected Employees");
            worksheet.Cells[1, 1].Value = "Name";
            worksheet.Cells[1, 2].Value = "Gender";
            worksheet.Cells[1, 3].Value = "Company";
            worksheet.Cells[1, 4].Value = "Department";
            using (var range = worksheet.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }
            int row = 2;
            foreach (var employee in selectedEmployees)
            {
                worksheet.Cells[row, 1].Value = employee.Name;
                worksheet.Cells[row, 2].Value = employee.Gender;
                worksheet.Cells[row, 3].Value = employee.Company;
                worksheet.Cells[row, 4].Value = employee.Department;
                using (var range = worksheet.Cells[row, 1, row, 4])
                {
                    range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }
                row++;
            }
            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            string fileName = $"Selected_Employees_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
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

    // YANGILANGAN EDIT METHOD - Barcha operatsiyalarni qo'llab-quvvatlaydi
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Employee employee, string deletedChildrenIds = "")
    {
        if (id != employee.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // O'chirilgan children'lar ID'larini parse qilish
                List<int> deletedIds = new List<int>();
                if (!string.IsNullOrEmpty(deletedChildrenIds))
                {
                    Console.WriteLine($"Deleted children IDs string: {deletedChildrenIds}"); // Debug

                    var idStrings = deletedChildrenIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var idStr in idStrings)
                    {
                        if (int.TryParse(idStr.Trim(), out int childId) && childId > 0)
                        {
                            deletedIds.Add(childId);
                            Console.WriteLine($"Adding child ID to delete: {childId}"); // Debug
                        }
                    }
                }

                Console.WriteLine($"Total children to delete: {deletedIds.Count}"); // Debug
                Console.WriteLine($"Total children to save: {employee.Children?.Count ?? 0}"); // Debug

                // Employee va children'larni yangilash
                bool result = employeeDAL.UpdateEmployeeWithChildren(employee, deletedIds);

                if (result)
                {
                    int addedCount = employee.Children?.Count(c => c.Id == 0) ?? 0;
                    int updatedCount = employee.Children?.Count(c => c.Id > 0) ?? 0;
                    int deletedCount = deletedIds.Count;

                    TempData["SuccessMessage"] = $"Muvaffaqiyatli yangilandi! " +
                        $"Qo'shildi: {addedCount}, " +
                        $"Yangilandi: {updatedCount}, " +
                        $"O'chirildi: {deletedCount}";

                    return RedirectToAction("Edit", new { id = employee.Id });
                }
                else
                {
                    ModelState.AddModelError("", "Ma'lumotlarni yangilashda xato yuz berdi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Edit method error: {ex.Message}"); // Debug
                ModelState.AddModelError("", "Xato: " + ex.Message);
            }
        }

        // Xatolik bo'lsa, qaytadan employee ma'lumotlarini yuklash
        employee = employeeDAL.GetEmployeeWithChildrenById(id);
        return View(employee);
    }

    // AJAX orqali bitta child yangilash
    [HttpPost]
    public JsonResult UpdateChild([FromBody] Children child)
    {
        try
        {
            if (child != null && child.Id > 0)
            {
                bool result = employeeDAL.UpdateChildren(child);
                return Json(new
                {
                    success = result,
                    message = result ? "Bola ma'lumotlari muvaffaqiyatli yangilandi" : "Yangilashda xato yuz berdi"
                });
            }
            else
            {
                return Json(new { success = false, message = "Noto'g'ri ma'lumotlar" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Xato: " + ex.Message });
        }
    }

    // AJAX orqali yangi child qo'shish
    [HttpPost]
    public JsonResult AddChild([FromBody] Children child)
    {
        try
        {
            if (child != null && child.EmployeeId > 0)
            {
                bool result = employeeDAL.CreateChildren(child);
                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Yangi bola muvaffaqiyatli qo'shildi"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Bola qo'shishda xato yuz berdi" });
                }
            }
            else
            {
                return Json(new { success = false, message = "Noto'g'ri ma'lumotlar" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Xato: " + ex.Message });
        }
    }

    // Child o'chirish
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

    // AJAX orqali child o'chirish
    [HttpPost]
    public JsonResult DeleteChildAjax([FromBody] DeleteChildRequest request)
    {
        try
        {
            bool result = employeeDAL.DeleteChild(request.ChildId);
            return Json(new
            {
                success = result,
                message = result ? "Bola ma'lumotlari o'chirildi" : "O'chirishda xato yuz berdi"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Xato: " + ex.Message });
        }
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
}

// Helper class for AJAX requests
public class DeleteChildRequest
{
    public int ChildId { get; set; }
}

// Helper class for batch operations
public class BatchUpdateRequest
{
    public Employee Employee { get; set; }
    public List<Children> UpdatedChildren { get; set; }
    public List<Children> NewChildren { get; set; }
    public List<int> DeletedChildrenIds { get; set; }
}