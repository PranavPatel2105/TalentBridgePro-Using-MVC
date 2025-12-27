using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;



namespace Repositories.Implementations
{
    public class UserRepository : IUserInterface
    {
        

        private NpgsqlConnection _conn;
        public UserRepository(NpgsqlConnection connection)
        {
            _conn = connection;
        }
        public async Task<List<t_user>> GetUser(string? filter = null)
        {
            List<t_user> users = new List<t_user>();
            try
            {
                string qry = @"SELECT c_userid, c_name, c_gender, c_contactno, c_email, c_address, c_password, c_profile_image, c_role, c_status, c_dob
	            FROM t_user WHERE c_status='active' ";
                if (filter != null)
                {
                    qry += filter;
                }
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new t_user
                    {
                        c_userid = reader.GetInt32(0),
                        c_name = reader.GetString(1),
                        c_gender = reader.GetString(2),
                        c_contactno = reader.GetString(3),
                        c_email = reader.GetString(4),
                        c_address = reader.GetString(5),
                        c_password = reader.GetString(6),
                        c_profile_image = reader.GetString(7),
                        c_role = reader.GetString(8),
                        c_status = reader.GetString(9),
                        c_dob = reader.GetDateTime(10)

                    });
                }


            }
            catch (System.Exception ex)
            {

                Console.WriteLine("error in fetching users:", ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }
            return users;
        }

        public async Task<int> RegisterUser(t_user user)
{
    try
    {
        string qry = @"
            INSERT INTO t_user
            (
                c_name,
                c_gender,
                c_contactno,
                c_email,
                c_address,
                c_password,
                c_profile_image,
                c_role,
                c_status,
                c_dob
            )
            VALUES
            (
                @c_name,
                @c_gender,
                @c_contactno,
                @c_email,
                @c_address,
                @c_password,
                @c_profile_image,
                @c_role,
                @c_status,
                @c_dob
            )
            RETURNING c_userid;
        ";

        using var cmd = new NpgsqlCommand(qry, _conn);

        cmd.Parameters.AddWithValue("c_name", user.c_name);
        cmd.Parameters.AddWithValue("c_gender", user.c_gender);
        cmd.Parameters.AddWithValue("c_contactno", user.c_contactno);
        cmd.Parameters.AddWithValue("c_email", user.c_email);
        cmd.Parameters.AddWithValue("c_address", user.c_address);

        var hasher = new PasswordHasher<object>();
        string hashedPassword = hasher.HashPassword(null, user.c_password);
        cmd.Parameters.AddWithValue("c_password", hashedPassword);

        cmd.Parameters.AddWithValue(
            "c_profile_image",
            (object?)user.c_profile_image ?? DBNull.Value
        );

        cmd.Parameters.AddWithValue("c_role", user.c_role);
        cmd.Parameters.AddWithValue("c_status", user.c_status);
        cmd.Parameters.AddWithValue("c_dob", user.c_dob);

        await _conn.OpenAsync();

        // THIS IS THE KEY LINE
        int newUserId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        return newUserId; // return actual DB-generated ID
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error in inserting user: " + ex.Message);
        return -1;
    }
    finally
    {
        await _conn.CloseAsync();
    }
}



        public async Task<int> ResetPassword(t_resetpassword resetpassword)
        {
            try
            {

                string qry = @"UPDATE public.t_user
               SET c_password = @password
               WHERE c_email = @email;";


                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);
                cmd.Parameters.AddWithValue("@email", resetpassword.c_email);
                var hasher = new PasswordHasher<object>();
                string hashedPassword = hasher.HashPassword(null, resetpassword.c_newpassword);
                cmd.Parameters.AddWithValue("@password", hashedPassword);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                return 1;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("error in Reseting the password(applicant):", ex.Message);
                return 0;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        //CHANGE PASSWORD
        // UserRepository
      public async Task<int> ChangePassword(int userId, vm_changePassword changePassword)
{
    try
    {
        string qry = @"
            UPDATE public.t_user
            SET c_password = @password
            WHERE c_userid = @userId;
        ";

        var hasher = new PasswordHasher<object>();
        string hashedPassword = hasher.HashPassword(null, changePassword.c_newpassword);

        using var cmd = new NpgsqlCommand(qry, _conn);
        cmd.Parameters.AddWithValue("@password", hashedPassword);
        cmd.Parameters.AddWithValue("@userId", userId);

        await _conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return 1;
    }
    catch (Exception ex)
    {
        Console.WriteLine("error in changing the password:", ex.Message);
        return 0;
    }
    finally
    {
        await _conn.CloseAsync();
    }
    }

    public async Task<string?> GetPasswordByUserId(int userId)
{
    try
    {
        string qry = @"
            SELECT c_password
            FROM public.t_user
            WHERE c_userid = @userId;
        ";

        using var cmd = new NpgsqlCommand(qry, _conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        await _conn.OpenAsync();

        var result = await cmd.ExecuteScalarAsync();

        return result?.ToString();
    }
    finally
    {
        await _conn.CloseAsync();
    }
}
    }
}