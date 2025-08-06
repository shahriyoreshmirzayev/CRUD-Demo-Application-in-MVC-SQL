using Npgsql;
using System.Data;

namespace CRUDDEMO1.Models;

public class Employee_dal
{
    string _connectionString = "Host=localhost;Database=EMPLOYEEDB1;Username=postgres;Password=postgres";

    // Get all employees
    public IEnumerable<Employee> GetAllEmployee()
    {
        List<Employee> employees = new();
        using (NpgsqlConnection con = new(_connectionString))
        {
            string query = "SELECT * FROM sp_getallemployee()";
            NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            cmd.CommandType = CommandType.Text;
            con.Open();
            using NpgsqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Employee employee = new Employee
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString(),
                    Gender = reader["Gender"].ToString(),
                    Company = reader["Company"].ToString(),
                    Department = reader["Department"].ToString()
                };
                employees.Add(employee);
            }
        }
        return employees;
    }

    // Add employee
    public int AddEmployee(Employee employee)
    {
        using (NpgsqlConnection con = new NpgsqlConnection(_connectionString))
        {
            string query = "SELECT sp_insertemployee(@p_name, @p_gender, @p_company, @p_department)";
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("p_name", employee.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_gender", employee.Gender ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_company", employee.Company ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("p_department", employee.Department ?? (object)DBNull.Value);
                con.Open();
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }
    }

    // Get employee by ID (simple)
    public Employee GetEmployeeById(int? id)
    {
        Employee employee = new Employee();
        using (NpgsqlConnection con = new(_connectionString))
        {
            string query = "SELECT * FROM sp_getemployeebyid(@p_id)";
            NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("p_id", id ?? (object)DBNull.Value);
            con.Open();
            using NpgsqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                employee.Id = Convert.ToInt32(reader["Id"]);
                employee.Name = reader["Name"].ToString();
                employee.Gender = reader["Gender"].ToString();
                employee.Company = reader["Company"].ToString();
                employee.Department = reader["Department"].ToString();
            }
        }
        return employee;
    }

    // Get employee with children
    public Employee GetEmployeeWithChildrenById(int? id)
    {
        Employee employee = null;

        using (NpgsqlConnection con = new(_connectionString))
        {
            string query = "SELECT * FROM get_employee_with_children(@p_id)";
            NpgsqlCommand cmd = new(query, con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@p_id", id ?? (object)DBNull.Value);
            con.Open();

            using NpgsqlDataReader reader = cmd.ExecuteReader();
            bool employeeCreated = false;

            while (reader.Read())
            {
                if (!employeeCreated)
                {
                    employee = new Employee
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = reader["name"].ToString(),
                        Gender = reader["gender"].ToString(),
                        Company = reader["company"].ToString(),
                        Department = reader["department"].ToString(),
                        Children = new List<Children>()
                    };
                    employeeCreated = true;
                }

                if (reader["children_id"] != DBNull.Value)
                {
                    var child = new Children
                    {
                        Id = Convert.ToInt32(reader["children_id"]),
                        Name = reader["children_name"].ToString(),
                        Gender = reader["children_gender"].ToString(),
                        Age = reader["children_age"] != DBNull.Value ? Convert.ToInt32(reader["children_age"]) : 0,
                        School = reader["children_school"]?.ToString() ?? "",
                        Grade = reader["children_grade"]?.ToString() ?? "",
                        EmployeeId = Convert.ToInt32(reader["employee_id"])
                    };
                    employee.Children.Add(child);
                }
            }
        }
        return employee;
    }

    // Update employee with children (Transaction)
    public bool UpdateEmployee(Employee employee)
    {
        using (NpgsqlConnection con = new(_connectionString))
        {
            con.Open();
            using (var transaction = con.BeginTransaction())
            {
                try
                {
                    // Employee update
                    string employeeQuery = "SELECT sp_updateemployee(@p_id, @p_name, @p_gender, @p_company, @p_department)";
                    using (var cmd = new NpgsqlCommand(employeeQuery, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("p_id", employee.Id);
                        cmd.Parameters.AddWithValue("p_name", employee.Name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_gender", employee.Gender ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_company", employee.Company ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_department", employee.Department ?? (object)DBNull.Value);

                        bool employeeUpdated = (bool)cmd.ExecuteScalar();
                        if (!employeeUpdated)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    // Children handle - update existing or create new
                    if (employee.Children != null && employee.Children.Any())
                    {
                        foreach (var child in employee.Children)
                        {
                            child.EmployeeId = employee.Id;

                            if (child.Id > 0)
                            {
                                // Update existing child
                                string childQuery = "SELECT sp_updatechildren(@p_id, @p_name, @p_gender, @p_age, @p_school, @p_grade, @p_employee_id)";
                                using var cmd = new NpgsqlCommand(childQuery, con, transaction);
                                cmd.Parameters.AddWithValue("p_id", child.Id);
                                cmd.Parameters.AddWithValue("p_name", child.Name ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("p_gender", child.Gender ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("p_age", child.Age ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("p_school", child.School ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("p_grade", child.Grade ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("p_employee_id", child.EmployeeId);

                                bool childUpdated = (bool)cmd.ExecuteScalar();
                                if (!childUpdated)
                                {
                                    transaction.Rollback();
                                    return false;
                                }
                            }
                            else
                            {
                                // Create new child
                                bool childCreated = CreateChildInTransaction(child, con, transaction);
                                if (!childCreated)
                                {
                                    transaction.Rollback();
                                    return false;
                                }
                            }
                        }
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }
    }

    // Create child (standalone)
    public bool CreateChildren(Children child)
    {
        using NpgsqlConnection con = new(_connectionString);
        try
        {
            con.Open();
            return CreateChildInTransaction(child, con, null);
        }
        catch
        {
            return false;
        }
    }

    // Create child in transaction (internal method)
    private bool CreateChildInTransaction(Children child, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        string query = @"INSERT INTO children (name, gender, age, school, grade, employee_id) 
                        VALUES (@name, @gender, @age, @school, @grade, @employee_id)";

        using var cmd = new NpgsqlCommand(query, connection, transaction);
        cmd.Parameters.AddWithValue("@name", child.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@gender", child.Gender ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@age", child.Age ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@school", child.School ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@grade", child.Grade ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@employee_id", child.EmployeeId);

        int result = cmd.ExecuteNonQuery();
        return result > 0;
    }

    // Update single child
    public bool UpdateChildren(Children child)
    {
        using NpgsqlConnection con = new(_connectionString);
        string query = "SELECT sp_updatechildren(@p_id, @p_name, @p_gender, @p_age, @p_school, @p_grade, @p_employee_id)";
        using var cmd = new NpgsqlCommand(query, con);
        cmd.Parameters.AddWithValue("p_id", child.Id);
        cmd.Parameters.AddWithValue("p_name", child.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_gender", child.Gender ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_age", child.Age ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_school", child.School ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_grade", child.Grade ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_employee_id", child.EmployeeId);

        try
        {
            con.Open();
            return (bool)cmd.ExecuteScalar();
        }
        catch
        {
            return false;
        }
    }

    // Delete employee
    public bool DeleteEmployee(int? id)
    {
        using NpgsqlConnection con = new(_connectionString);
        string query = "SELECT sp_deleteemployee(@p_id)";
        using var cmd = new NpgsqlCommand(query, con);
        cmd.Parameters.AddWithValue("p_id", id ?? (object)DBNull.Value);

        try
        {
            con.Open();
            return (bool)cmd.ExecuteScalar();
        }
        catch
        {
            return false;
        }
    }

    // Delete child
    public bool DeleteChild(int childId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        string query = "DELETE FROM children WHERE id = @id";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@id", childId);

        try
        {
            connection.Open();
            int result = command.ExecuteNonQuery();
            return result > 0;
        }
        catch
        {
            return false;
        }
    }
}