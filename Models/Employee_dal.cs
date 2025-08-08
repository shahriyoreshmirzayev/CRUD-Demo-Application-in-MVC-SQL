using Npgsql;
using Org.BouncyCastle.Crypto.Generators;
using System.Data;
namespace CRUDDEMO1.Models;

public class Employee_dal
{
    string _connectionString = "Host=localhost;Database=EMPLOYEEDB1;Username=postgres;Password=postgres";
    public bool RegisterUser(string username, string password, string role)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            string query = "INSERT INTO users (username, password_hash, role) VALUES (@username, @password_hash, @role)";
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("username", username);
            command.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword(password));
            command.Parameters.AddWithValue("role", role);

            connection.Open();
            int result = command.ExecuteNonQuery();
            return result > 0;
        }
        catch
        {
            return false;
        }
    }


    // Username mavjudligini tekshirish
    public bool UserExists(string username)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        string query = "SELECT COUNT(*) FROM users WHERE username = @username";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("username", username);

        connection.Open();
        long count = (long)command.ExecuteScalar();
        return count > 0;
    }

    // Parolni qayta tiklash
    public bool ResetPassword(string username, string newPassword)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            string query = "UPDATE users SET password_hash = @password_hash WHERE username = @username";
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("username", username);
            command.Parameters.AddWithValue("password_hash", BCrypt.Net.BCrypt.HashPassword(newPassword));

            connection.Open();
            int result = command.ExecuteNonQuery();
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    // Avvalgi GetUserByUsername va ValidateUser metodlari
    public User GetUserByUsername(string username)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        string query = "SELECT * FROM users WHERE username = @username";
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("username", username);

        connection.Open();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Username = reader.GetString(reader.GetOrdinal("username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                Role = reader.GetString(reader.GetOrdinal("role"))
            };
        }
        return null;
    }

    public bool ValidateUser(string username, string password)
    {
        var user = GetUserByUsername(username);
        if (user == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }




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
    public bool UpdateEmployeeWithChildren(Employee employee, List<int> deletedChildrenIds = null)
    {
        using (NpgsqlConnection con = new(_connectionString))
        {
            con.Open();
            using (var transaction = con.BeginTransaction())
            {
                try
                {
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
                    if (deletedChildrenIds != null && deletedChildrenIds.Count > 0)
                    {
                        foreach (int childId in deletedChildrenIds)
                        {
                            if (!DeleteChildInTransaction(childId, con, transaction))
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }
                    }
                    if (employee.Children != null && employee.Children.Any())
                    {
                        foreach (var child in employee.Children)
                        {
                            child.EmployeeId = employee.Id;

                            if (child.Id > 0)
                            {
                                if (!UpdateChildInTransaction(child, con, transaction))
                                {
                                    transaction.Rollback();
                                    return false;
                                }
                            }
                            else
                            {
                                if (!CreateChildInTransaction(child, con, transaction))
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
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }
    }

    public bool UpdateEmployee(Employee employee)
    {
        return UpdateEmployeeWithChildren(employee);
    }

    private bool CreateChildInTransaction(Children child, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        try
        {

            string query = @"INSERT INTO children (name, gender, age, school, grade, employee_id) 
                        VALUES (@name, @gender, @age, @school, @grade, @employee_id)";

            using var cmd = new NpgsqlCommand(query, connection, transaction);

            cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(child.Name) ? (object)DBNull.Value : child.Name);
            cmd.Parameters.AddWithValue("@gender", string.IsNullOrWhiteSpace(child.Gender) ? (object)DBNull.Value : child.Gender);
            if (child.Age < 0 || child.Age > 50)
            {
                Console.WriteLine($"Invalid age: {child.Age}. Yosh chegarasi mavjud (0 dan 50 gacha)");
                return false;
            }
            cmd.Parameters.AddWithValue("@age", child.Age == 0 ? (object)DBNull.Value : child.Age);
            cmd.Parameters.AddWithValue("@school", string.IsNullOrWhiteSpace(child.School) ? (object)DBNull.Value : child.School);
            cmd.Parameters.AddWithValue("@grade", string.IsNullOrWhiteSpace(child.Grade) ? (object)DBNull.Value : child.Grade);
            cmd.Parameters.AddWithValue("@employee_id", child.EmployeeId);

            int result = cmd.ExecuteNonQuery();

            return result > 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    private bool UpdateChildInTransaction(Children child, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        string query = "SELECT sp_updatechildren(@p_id, @p_name, @p_gender, @p_age, @p_school, @p_grade, @p_employee_id)";
        using var cmd = new NpgsqlCommand(query, connection, transaction);
        cmd.Parameters.AddWithValue("p_id", child.Id);
        cmd.Parameters.AddWithValue("p_name", child.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_gender", child.Gender ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_age", child.Age ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_school", child.School ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_grade", child.Grade ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("p_employee_id", child.EmployeeId);

        var result = cmd.ExecuteScalar();
        return result != null && (bool)result;
    }
    private bool DeleteChildInTransaction(int childId, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        try
        {
            string query = "DELETE FROM children WHERE id = @id";
            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@id", childId);

            int result = cmd.ExecuteNonQuery();
            return result > 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public bool CreateChildren(Children child)
    {
        using NpgsqlConnection con = new(_connectionString);
        try
        {
            con.Open();
            using var transaction = con.BeginTransaction();

            bool result = CreateChildInTransaction(child, con, transaction);

            if (result)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }

            return result;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
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

    public bool AddChild(Children child)
    {
        return CreateChildren(child);
    }

    public bool UpdateChildren(List<Children> children)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var child in children.Where(c => c.Id > 0))
            {
                if (!UpdateChildInTransaction(child, connection, transaction))
                {
                    transaction.Rollback();
                    return false;
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