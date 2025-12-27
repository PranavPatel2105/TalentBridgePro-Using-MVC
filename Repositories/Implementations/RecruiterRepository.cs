using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using Npgsql;

using Repositories.Interfaces;
using Repositories.Models;




namespace Repositories.Implementations
{
    public class RecruiterRepository : IRecruiterInterface
    {


        private readonly NpgsqlConnection _connection;

        public RecruiterRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }
        public async Task<List<t_job>> GetJobDetailsByRecruiterId(int recruiterid)
        {
            var jobs = new List<t_job>();

            try
            {
                await _connection.OpenAsync();

                string query = "SELECT j.*, c.c_name FROM t_job j JOIN t_companydetails c ON j.c_company_id = c.c_company_id WHERE j.c_userid = @recruiterid";
                // string query = "SELECT * FROM t_job WHERE c_userid = @recruiterid";


                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@recruiterid", recruiterid);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            jobs.Add(new t_job
                            {
                                c_job_id = Convert.ToInt32(reader["c_job_id"]),
                                c_userid = Convert.ToInt32(reader["c_userid"]),
                                c_company_id = Convert.ToInt32(reader["c_company_id"]),
                                c_job_type = reader["c_job_type"].ToString(),
                                c_skills = reader["c_skills"].ToString(),
                                c_role = reader["c_role"].ToString(),
                                c_location = reader["c_location"].ToString(),
                                c_experience = reader["c_experience"].ToString(),
                                c_description = reader["c_description"].ToString(),
                                c_salary = reader["c_salary"].ToString(),
                                c_jd_file = reader["c_jd_file"] != DBNull.Value
                                    ? reader["c_jd_file"].ToString()
                                    : null,
                                c_name = reader["c_name"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while retrieving job details: " + ex.Message);
                return null;
            }
            finally
            {
                await _connection.CloseAsync();
            }

            return jobs;
        }

        public async Task<int> AddContactUsDetails(t_contactus contactus)
        {
            try
            {
                await _connection.OpenAsync();

                string query = "INSERT INTO t_contactus (c_userid, c_subject, c_message) VALUES (@userid, @subject, @message)";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@userid", contactus.c_userid);
                    cmd.Parameters.AddWithValue("@subject", contactus.c_subject);
                    cmd.Parameters.AddWithValue("@message", contactus.c_message);

                    var res = await cmd.ExecuteNonQueryAsync();

                    if (res <= 0)
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while adding contact us details: " + ex.Message);
                return 0;
            }
            finally
            {
                await _connection.CloseAsync();
            }

            return 1;
        }


        public async Task<bool> IsJobOwnedByRecruiter(int jobId, int recruiterId)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"
            SELECT COUNT(1)
            FROM t_job
            WHERE c_job_id = @jobId
              AND c_userid = @recruiterId
        ";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@jobId", jobId);
                    cmd.Parameters.AddWithValue("@recruiterId", recruiterId);

                    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Job ownership validation failed: " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        //==========================Pranav =======================================================================================================================


        // ADD JOB 
        public async Task<int> AddJobAsync(t_job job)
        {
            const string query = @"
                INSERT INTO t_job
                (c_userid, c_company_id, c_job_type, c_skills, c_role,
                 c_location, c_experience, c_description, c_salary, c_jd_file)
                VALUES
                (@userid, @companyid, @jobtype, @skills, @role,
                 @location, @experience, @description, @salary, @jdfile)
                RETURNING c_job_id;
            ";

            try
            {
                await _connection.OpenAsync();

                using var cmd = new NpgsqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@userid", job.c_userid);
                cmd.Parameters.AddWithValue("@companyid", job.c_company_id);
                cmd.Parameters.AddWithValue("@jobtype", job.c_job_type ?? "");
                cmd.Parameters.AddWithValue("@skills", job.c_skills ?? "");
                cmd.Parameters.AddWithValue("@role", job.c_role ?? "");
                cmd.Parameters.AddWithValue("@location", job.c_location ?? "");
                cmd.Parameters.AddWithValue("@experience", job.c_experience ?? "");
                cmd.Parameters.AddWithValue("@description", job.c_description ?? "");
                cmd.Parameters.AddWithValue("@salary", job.c_salary ?? "");
                cmd.Parameters.AddWithValue("@jdfile", job.c_jd_file ?? "");

                return (int)await cmd.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                // log exception if required
                throw new Exception("Error while adding job", ex);
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        // // UPDATE JOB 
        // public async Task<bool> UpdateJobAsync(int jobId, t_job job)
        // {
        //     const string query = @"
        //         UPDATE t_job SET
        //             c_job_type = @jobtype,
        //             c_skills = @skills,
        //             c_role = @role,
        //             c_location = @location,
        //             c_experience = @experience,
        //             c_description = @description,
        //             c_salary = @salary,
        //             c_jd_file = @jdfile
        //         WHERE c_job_id = @jobid;
        //     ";

        //     try
        //     {
        //         await _connection.OpenAsync();

        //         using var cmd = new NpgsqlCommand(query, _connection);
        //         cmd.Parameters.AddWithValue("@jobid", jobId);
        //         cmd.Parameters.AddWithValue("@jobtype", job.c_job_type ?? "");
        //         cmd.Parameters.AddWithValue("@skills", job.c_skills ?? "");
        //         cmd.Parameters.AddWithValue("@role", job.c_role ?? "");
        //         cmd.Parameters.AddWithValue("@location", job.c_location ?? "");
        //         cmd.Parameters.AddWithValue("@experience", job.c_experience ?? "");
        //         cmd.Parameters.AddWithValue("@description", job.c_description ?? "");
        //         cmd.Parameters.AddWithValue("@salary", job.c_salary ?? "");
        //         cmd.Parameters.AddWithValue("@jdfile", job.c_jd_file ?? "");

        //         int rowsAffected = await cmd.ExecuteNonQueryAsync();
        //         return rowsAffected > 0;
        //     }
        //     catch (Exception ex)
        //     {
        //         throw new Exception("Error while updating job", ex);
        //     }
        //     finally
        //     {
        //         await _connection.CloseAsync();
        //     }
        // }

        // DELETE JOB 
       public async Task<int> DeleteJob(int jobId)
{
    try
    {
        await _connection.OpenAsync();
        
        string query = "DELETE FROM t_job WHERE c_job_id = @jobId";
        
        using var cmd = new NpgsqlCommand(query, _connection);
        cmd.Parameters.AddWithValue("@jobId", jobId);
        
        return await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        throw new Exception("Error deleting job", ex);
    }
    finally
    {
        if (_connection.State == ConnectionState.Open)
            await _connection.CloseAsync();
    }
}

// Also add method to check if job belongs to recruiter


public async Task<int> UpdateJob(t_job job)
{
    try
    {
        await _connection.OpenAsync();
        
        string query = @"
            UPDATE t_job 
            SET c_role = @role,
                c_job_type = @jobType,
                c_location = @location,
                c_experience = @experience,
                c_skills = @skills,
                c_salary = @salary,
                c_description = @description
            WHERE c_job_id = @jobId";
        
        using var cmd = new NpgsqlCommand(query, _connection);
        cmd.Parameters.AddWithValue("@role", job.c_role);
        cmd.Parameters.AddWithValue("@jobType", job.c_job_type);
        cmd.Parameters.AddWithValue("@location", job.c_location);
        cmd.Parameters.AddWithValue("@experience", job.c_experience);
        cmd.Parameters.AddWithValue("@skills", job.c_skills);
        cmd.Parameters.AddWithValue("@salary", job.c_salary);
        cmd.Parameters.AddWithValue("@description", job.c_description);
        cmd.Parameters.AddWithValue("@jobId", job.c_job_id);
        
        return await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        throw new Exception("Error updating job", ex);
    }
    finally
    {
        if (_connection.State == ConnectionState.Open)
            await _connection.CloseAsync();
    }
}

// (You already have GetJobByIdAndUserId from delete functionality)
public async Task<t_job> GetJobByIdAndUserId(int jobId, int userId)
{
    try
    {
        await _connection.OpenAsync();
        
        string query = "SELECT * FROM t_job WHERE c_job_id = @jobId AND c_userid = @userId";
        
        using var cmd = new NpgsqlCommand(query, _connection);
        cmd.Parameters.AddWithValue("@jobId", jobId);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        using var reader = await cmd.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return new t_job
            {
                c_job_id = reader.GetInt32(reader.GetOrdinal("c_job_id")),
                c_userid = reader.GetInt32(reader.GetOrdinal("c_userid")),
                // ... map other properties as needed
            };
        }
        
        return null;
    }
    finally
    {
        if (_connection.State == ConnectionState.Open)
            await _connection.CloseAsync();
    }
}

        //===============================================================================================================================================




        public async Task<t_user> GetOneRecruiter(int c_userid)
        {
            try
            {

                NpgsqlCommand cm = new NpgsqlCommand("select * from t_user WHERE c_userid=@c_userid", _connection);
                cm.Parameters.AddWithValue("@c_userid", c_userid);
                await _connection.CloseAsync();
                await _connection.OpenAsync();
                NpgsqlDataReader reader = cm.ExecuteReader();
                t_user recruiter = new t_user();
                if (reader.Read())
                {
                    recruiter.c_userid = Convert.ToInt32(reader["c_userid"]);
                    recruiter.c_name = Convert.ToString(reader["c_name"]);
                    recruiter.c_gender = Convert.ToString(reader["c_gender"]);
                    recruiter.c_contactno = Convert.ToString(reader["c_contactno"]);
                    recruiter.c_email = Convert.ToString(reader["c_email"]);
                    recruiter.c_address = Convert.ToString(reader["c_address"]);
                    recruiter.c_password = Convert.ToString(reader["c_password"]);
                    recruiter.c_role = Convert.ToString(reader["c_role"]);
                    recruiter.c_status = Convert.ToString(reader["c_status"]);
                    // recruiter.c_dob = Convert.ToDateTime(reader["c_dob"]);
                    var dob = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("c_dob"));
                    recruiter.c_dob = dob.ToDateTime(TimeOnly.MinValue);
                    recruiter.c_profile_image = Convert.ToString(reader["c_profile_image"]);


                }
                await _connection.CloseAsync();
                return recruiter;
            }
            catch (Exception error)
            {
                Console.WriteLine("Error in GetOneRecruiter : " + error.Message);
                return new t_user();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }


        public async Task<int> EditRecruiter(t_user recruiter)
        {
            try
            {
                var qry = @"UPDATE t_user SET  c_name=@c_name,c_gender=@c_gender, c_contactno=@c_contactno, c_address=@c_address, c_dob=@c_dob, c_profile_image=@c_profile_image WHERE c_userid=@c_userid";
                using var cmd = new NpgsqlCommand(qry, _connection);
                await _connection.OpenAsync();
                cmd.Parameters.AddWithValue("@c_userid", recruiter.c_userid);
                cmd.Parameters.AddWithValue("@c_name", recruiter.c_name);
                cmd.Parameters.AddWithValue("@c_gender", recruiter.c_gender);
                cmd.Parameters.AddWithValue("@c_contactno", recruiter.c_contactno);
                cmd.Parameters.AddWithValue("@c_address", recruiter.c_address);
                cmd.Parameters.AddWithValue("@c_dob", recruiter.c_dob);
                cmd.Parameters.AddWithValue("@c_profile_image", recruiter.c_profile_image ?? (Object)DBNull.Value);
                int affectedRows = await cmd.ExecuteNonQueryAsync();
                await _connection.CloseAsync();
                if (affectedRows > 0)
                {
                    return 1;
                }
                return 0;
            }
            catch (Exception error)
            {
                Console.WriteLine("Error in EditRecruiter : " + error.Message);
                return -1;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }


        public async Task<t_companydetails> GetOneCompany(int c_userid)
        {
            try
            {

                NpgsqlCommand cm = new NpgsqlCommand("select * from t_companydetails WHERE c_userid=@c_userid", _connection);
                cm.Parameters.AddWithValue("@c_userid", c_userid);
                await _connection.CloseAsync();
                await _connection.OpenAsync();
                NpgsqlDataReader reader = cm.ExecuteReader();
                t_companydetails Company = new t_companydetails();
                if (reader.Read())
                {
                    Company.c_company_id = Convert.ToInt32(reader["c_company_id"]);
                    Company.c_userid = Convert.ToInt32(reader["c_userid"]);
                    Company.c_company_email = Convert.ToString(reader["c_company_email"]);
                    Company.c_name = Convert.ToString(reader["c_name"]);
                    Company.c_image = Convert.ToString(reader["c_image"]);
                    Company.c_state = Convert.ToString(reader["c_state"]);
                    Company.c_city = Convert.ToString(reader["c_city"]);
                }
                await _connection.CloseAsync();
                return Company;
            }
            catch (Exception error)
            {
                Console.WriteLine("Error in GetOneCompany : " + error.Message);
                return new t_companydetails();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }


        public async Task<int> EditCompany(t_companydetails company)
        {
            try
            {
                var qry = @"UPDATE t_companydetails SET  c_name=@c_name,c_image=@c_image, c_state=@c_state, c_city=@c_city WHERE c_company_id=@c_company_id";
                using var cmd = new NpgsqlCommand(qry, _connection);
                await _connection.OpenAsync();
                cmd.Parameters.AddWithValue("@c_company_id", company.c_company_id);
                cmd.Parameters.AddWithValue("@c_name", company.c_name);
                cmd.Parameters.AddWithValue("@c_state", company.c_state);
                cmd.Parameters.AddWithValue("@c_city", company.c_city);
                cmd.Parameters.AddWithValue("@c_image", company.c_image ?? (Object)DBNull.Value);
                int affectedRows = await cmd.ExecuteNonQueryAsync();
                await _connection.CloseAsync();
                if (affectedRows > 0)
                {
                    return 1;
                }
                return 0;
            }
            catch (Exception error)
            {
                Console.WriteLine("Error in EditCompany : " + error.Message);
                return -1;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }



        public async Task<List<ViewApplicationVM>> ViewApplication(int jobId)
        {
            List<ViewApplicationVM> list = new();

            try
            {
                var qry = @"
            SELECT 
                j.c_job_id,
                j.c_role,
                j.c_location,

                a.c_application_id,
                a.c_status,
                a.c_resume_file,

                u.c_userid,
                a.c_name,
                a.c_email
            FROM t_job j
            INNER JOIN t_applications a 
                ON j.c_job_id = a.c_job_id
            INNER JOIN t_user u 
                ON a.c_userid = u.c_userid
            WHERE j.c_job_id = @jobId;
        ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@jobId", jobId);

                await _connection.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new ViewApplicationVM
                    {
                        c_job_id = Convert.ToInt32(reader["c_job_id"]),
                        c_role = reader["c_role"].ToString(),
                        c_location = reader["c_location"].ToString(),

                        c_application_id = Convert.ToInt32(reader["c_application_id"]),
                        c_status = reader["c_status"].ToString(),
                        c_resume_file = reader["c_resume_file"] == DBNull.Value
                                            ? null
                                            : reader["c_resume_file"].ToString(),

                        c_userid = Convert.ToInt32(reader["c_userid"]),
                        c_name = reader["c_name"].ToString(),
                        c_email = reader["c_email"].ToString()
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ViewApplication: " + ex.Message);
                return new List<ViewApplicationVM>();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }



        // public async Task<int> AcceptApplication(int applicationId)
        // {
        //     try
        //     {
        //         var qry = @"UPDATE t_applications
        //             SET c_status = 'accepted'
        //             WHERE c_application_id = @applicationId";

        //         using var cmd = new NpgsqlCommand(qry, _connection);
        //         cmd.Parameters.AddWithValue("@applicationId", applicationId);

        //         await _connection.OpenAsync();
        //         int affectedRows = await cmd.ExecuteNonQueryAsync();

        //         if (affectedRows > 0)
        //         {
        //             return 1;   // success
        //         }

        //         return 0;       // no rows affected
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine("Error in AcceptApplication : " + ex.Message);
        //         return -1;      // error
        //     }
        //     finally
        //     {
        //         await _connection.CloseAsync();
        //     }


        // }

        // public async Task<int> RejectApplication(int applicationId)
        // {
        //     try
        //     {
        //         var qry = @"UPDATE t_applications
        //             SET c_status = 'rejected'
        //             WHERE c_application_id = @c_application_id";

        //         using var cmd = new NpgsqlCommand(qry, _connection);
        //         cmd.Parameters.AddWithValue("@c_application_id", applicationId);

        //         await _connection.OpenAsync();
        //         int affectedRows = await cmd.ExecuteNonQueryAsync();

        //         if (affectedRows > 0)
        //         {
        //             return 1;   // success
        //         }

        //         return 0;       // no rows affected
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine("Error in RejectApplication : " + ex.Message);
        //         return -1;      // error
        //     }
        //     finally
        //     {
        //         await _connection.CloseAsync();
        //     }
        // }

        public async Task<int> UpdateApplicationStatus(int c_application_id, string c_status)
        {
            try
            {
                var qry = @"UPDATE t_applications
                    SET c_status = @c_status
                    WHERE c_application_id = @c_application_id";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@c_status", c_status);
                cmd.Parameters.AddWithValue("@c_application_id", c_application_id);

                await _connection.OpenAsync();
                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateApplicationStatus Error : " + ex.Message);
                return -1;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }



        public async Task<int> AddCompanyDetails(t_companydetails companydetails)
        {
            try
            {
                await _connection.OpenAsync();
                string checkQuery = @"SELECT * FROM t_companydetails 
                              WHERE c_company_email = @c_company_email;";

                using (var checkCmd = new NpgsqlCommand(checkQuery, _connection))
                {
                    checkCmd.Parameters.AddWithValue("c_company_email", companydetails.c_company_email);

                    using (var reader = await checkCmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            return 0;
                        }
                    }
                }
                string insertQuery = @"INSERT INTO t_companydetails (c_userid, c_company_email, c_name, c_image, c_state, c_city)
                                      VALUES (@c_userid, @c_company_email, @c_name, @c_image, @c_state, @c_city);";
                using (var insertCmd = new NpgsqlCommand(insertQuery, _connection))
                {
                    insertCmd.Parameters.AddWithValue("c_userid", companydetails.c_userid);
                    insertCmd.Parameters.AddWithValue("c_company_email", companydetails.c_company_email);
                    insertCmd.Parameters.AddWithValue("c_name", (object?)companydetails.c_name ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("c_image", (object?)companydetails.c_image ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("c_state", (object?)companydetails.c_state ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("c_city", (object?)companydetails.c_city ?? DBNull.Value);
                    await insertCmd.ExecuteNonQueryAsync();
                }
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in AddCompanyDetails: " + ex.Message);
                return 0;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        public async Task<int> CheckCompanyEmailExists(string? filter = null)
        {
            try
            {
                string qry = @"SELECT 1 FROM t_companydetails ";
                if (filter != null)
                {
                    qry += filter;
                }
                Console.WriteLine("Qry : " + qry);
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _connection);
                await _connection.CloseAsync();
                await _connection.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    return 1;
                }
                return 0;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("error in fetching company:", ex.Message);
                return -1;
            }
            finally
            {
                await _connection.CloseAsync();
                await _connection.CloseAsync();
            }
        }

        public async Task<int> GetCompanyIdByUserId(int userId)
        {
            try
            {
                await _connection.OpenAsync();

                string query = @"SELECT c_company_id
                         FROM t_companydetails
                         WHERE c_userid = @c_userid
                         LIMIT 1;";

                using (var cmd = new NpgsqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("c_userid", userId);

                    object? result = await cmd.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt32(result);
                }

                return 0; // company not found
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetCompanyIdByUserId: " + ex.Message);
                return 0;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }



        public async Task<mailDetAIL_VM> GetMailDetails(int id)
        {
            await _connection.CloseAsync();
            await _connection.OpenAsync();

            try
            {


                string query = @"
        SELECT 
            a.c_name       AS applicant_name,
            a.c_email      AS applicant_email,
            j.c_role       AS job_role,
            c.c_name       AS company_name
        FROM public.t_applications AS a
        INNER JOIN t_job AS j 
            ON a.c_job_id = j.c_job_id
        INNER JOIN t_companydetails AS c 
            ON j.c_company_id = c.c_company_id
        WHERE a.c_application_id = @c_userid;
        ";

                var cmd = new NpgsqlCommand(query, _connection);

                cmd.Parameters.AddWithValue("@c_userid", id);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    return new mailDetAIL_VM
                    {
                        name = reader["applicant_name"].ToString(),
                        email = reader["applicant_email"].ToString(),
                        job_role = reader["job_role"].ToString(),
                        companyName = reader["company_name"].ToString()
                    };
                }

                return null;



            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetMailDetails: " + ex.Message);
                return null;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }

        }
        //===================================================================================================================================================

        public async Task<List<ViewApplicantDetailsVM>> GetAllApplicants()
        {
            List<ViewApplicantDetailsVM> list = new List<ViewApplicantDetailsVM>();

            try
            {
                var qry = @"
                    SELECT 
                        u.c_userid,
                        u.c_name,
                        u.c_email,
                        ud.c_bio,
                        ud.c_skills,
                        ud.c_resume_file
                    FROM t_user u
                    LEFT JOIN t_userdetails ud
                        ON u.c_userid = ud.c_userid
                    WHERE u.c_role = 'applicant'
                ";


                using var cmd = new NpgsqlCommand(qry, _connection);

                await _connection.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new ViewApplicantDetailsVM
                    {
                        c_userid = Convert.ToInt32(reader["c_userid"]),
                        c_name = reader["c_name"]?.ToString(),
                        c_email = reader["c_email"]?.ToString(),

                        c_bio = reader["c_bio"] == DBNull.Value
                            ? null
                            : reader["c_bio"].ToString(),

                        c_skills = reader["c_skills"] == DBNull.Value
                            ? null
                            : reader["c_skills"].ToString(),

                        c_resume_file = reader["c_resume_file"] == DBNull.Value
                            ? null
                            : reader["c_resume_file"].ToString()
                    });


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllApplicants Error : " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }

            return list;
        }


        public async Task<List<ViewApplicantDetailsVM>> SearchApplicantsBySkill(string skill)
        {
            List<ViewApplicantDetailsVM> list = new List<ViewApplicantDetailsVM>();

            try
            {
                var qry = @"
            SELECT 
                u.c_userid,
                u.c_name,
                u.c_email,
                ud.c_bio,
                ud.c_skills,
                ud.c_resume_file
            FROM t_user u
            LEFT JOIN t_userdetails ud
                ON u.c_userid = ud.c_userid
            WHERE u.c_role = 'applicant'
              AND ud.c_skills ILIKE @skill
              or u.c_name ILIKE @skill1
        ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@skill", "%" + skill + "%");
                cmd.Parameters.AddWithValue("@skill1", "%" + skill + "%");


                await _connection.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new ViewApplicantDetailsVM
                    {
                        c_userid = Convert.ToInt32(reader["c_userid"]),
                        c_name = reader["c_name"]?.ToString(),
                        c_email = reader["c_email"]?.ToString(),
                        c_bio = reader["c_bio"] == DBNull.Value ? null : reader["c_bio"].ToString(),
                        c_skills = reader["c_skills"] == DBNull.Value ? null : reader["c_skills"].ToString(),
                        c_resume_file = reader["c_resume_file"] == DBNull.Value ? null : reader["c_resume_file"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SearchApplicantsBySkill Error : " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }

            return list;
        }







        public async Task<ViewApplicantFullDetailsVM> GetApplicantFullDetails(int userid)
        {
            var vm = new ViewApplicantFullDetailsVM();

            try
            {
                await _connection.OpenAsync();

                // ================= USER =================
                using (var cmd = new NpgsqlCommand(
                    "SELECT c_userid, c_name, c_email FROM t_user WHERE c_userid=@uid",
                    _connection))
                {
                    cmd.Parameters.AddWithValue("@uid", userid);

                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        vm.c_userid = userid;
                        vm.c_name = r["c_name"].ToString();
                        vm.c_email = r["c_email"].ToString();
                    }
                } // ✅ reader closed here

                // ================= EDUCATION =================
                using (var cmd = new NpgsqlCommand(
                    "SELECT * FROM t_educationdetails WHERE c_userid=@uid",
                    _connection))
                {
                    cmd.Parameters.AddWithValue("@uid", userid);

                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        vm.Educations.Add(new t_educationdetails
                        {
                            c_schoolname = r["c_schoolname"].ToString(),
                            c_degree = r["c_degree"].ToString(),
                            c_fieldofstudy = r["c_fieldofstudy"].ToString(),
                            c_startyear = Convert.ToInt32(r["c_startyear"]),
                            c_endyear = Convert.ToInt32(r["c_endyear"]),
                            c_state = r["c_state"].ToString(),
                            c_city = r["c_city"].ToString()
                        });
                    }
                } // ✅ reader closed here

                // ================= CERTIFICATES =================
                using (var cmd = new NpgsqlCommand(
                    "SELECT * FROM t_certificate WHERE c_userid=@uid",
                    _connection))
                {
                    cmd.Parameters.AddWithValue("@uid", userid);

                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        vm.Certificates.Add(new t_certificate
                        {
                            c_certificatename = r["c_certificatename"].ToString(),

                            // ⚠️ COLUMN NAME CHECK KARJO
                            c_certificatefile = r["c_certificatefile"].ToString()
                        });
                    }
                } // ✅ reader closed here
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetApplicantFullDetails Error: " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }

            return vm;
        }
    }
}