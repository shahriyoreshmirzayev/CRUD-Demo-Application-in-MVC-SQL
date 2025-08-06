using Npgsql;
using System.Data;

namespace CRUDDEMO1.Models;

public class Children_dal
{
    string _connectionString = "Host=localhost;Database=EMPLOYEEDB1;Username=postgres;Password=postgres";
    public bool AddChildren(Children child)
    {
        using NpgsqlConnection con = new(_connectionString);
        string query = "SELECT sp_insertchildren(@p_name, @p_gender, @p_age, @p_school, @p_grade, @p_employee_id)";
        using NpgsqlCommand cmd = new(query, con);
        cmd.CommandType = CommandType.Text;
        cmd.Parameters.AddWithValue("p_name", child.Name);
        cmd.Parameters.AddWithValue("p_gender", child.Gender);
        cmd.Parameters.AddWithValue("p_age", child.Age);
        cmd.Parameters.AddWithValue("p_school", child.School);
        cmd.Parameters.AddWithValue("p_grade", child.Grade);
        cmd.Parameters.AddWithValue("p_employee_id", child.EmployeeId);
        con.Open();
        return (bool)cmd.ExecuteScalar();
    }

    public bool DeleteChildren(int? id)
    {
        using NpgsqlConnection con = new(_connectionString);
        string query = "SELECT sp_deletechildren(@p_id)";
        using var cmd = new NpgsqlCommand(query, con);
        cmd.Parameters.AddWithValue("p_id", id);
        con.Open();
        return (bool)cmd.ExecuteScalar();
    }

    public Children GetChildrenById(int? id)
    {
        Children child = new Children();
        using (NpgsqlConnection con = new(_connectionString))
        {
            string query = "SELECT * FROM sp_getchildrenbyid(@p_id)";
            NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("p_id", id);
            con.Open();
            using NpgsqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                child.Id = Convert.ToInt32(reader["Id"]);
                child.Name = reader["Name"].ToString();
                child.Gender = reader["Gender"].ToString();
                child.Age = Convert.ToInt32(reader["Age"]);
                child.School = reader["School"].ToString();
                child.Grade = reader["Grade"].ToString();
                child.EmployeeId = Convert.ToInt32(reader["employee_id"]);
            }
        }
        return child;
    }
}