using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;



using Repositories.Models;

namespace Repositories.Implementations
{
    public class ApplicantRepository : IApplicantInterface
    {
        private readonly NpgsqlConnection _connection;

        public ApplicantRepository(NpgsqlConnection connection)
        {
            _connection = connection;
            
        }

//Appy page PRANAV===================================================================================================================================================

        public async Task<object?> GetApplyJobDetailsAsync(int jobId)
        {
            const string jobQuery = @"
        SELECT j.*, c.*
        FROM t_job j
        INNER JOIN t_companydetails c
            ON j.c_company_id = c.c_company_id
        WHERE j.c_job_id = @jobId";

            const string ratingQuery = @"
        SELECT 
            COALESCE(AVG(c_stars), 0) AS avg_rating,
            COUNT(*) AS total_reviews
        FROM t_company_review
        WHERE c_company_id = @companyId";

            // UPDATED: Include user details in the query
            const string reviewsQuery = @"
        SELECT 
            cr.c_stars, 
            cr.c_description,
            u.c_name as user_name,
            u.c_profile_image as user_profile_image
        FROM t_company_review cr
        INNER JOIN t_user u ON cr.c_userid = u.c_userid
        WHERE cr.c_company_id = @companyId
        ORDER BY cr.c_review_id DESC";

            try
            {
                await _connection.OpenAsync();

                t_job job = null;
                t_companydetails company = null;
                double avgRating = 0;
                int totalReviews = 0;
                var reviews = new List<object>();

                // ===== Job + Company =====
                using (var cmd = new NpgsqlCommand(jobQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@jobId", jobId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return null;

                    job = new t_job
                    {
                        c_job_id = reader.GetInt32(reader.GetOrdinal("c_job_id")),
                        c_userid = reader.GetInt32(reader.GetOrdinal("c_userid")),
                        c_company_id = reader.GetInt32(reader.GetOrdinal("c_company_id")),
                        c_job_type = reader.GetString(reader.GetOrdinal("c_job_type")),
                        c_skills = reader.GetString(reader.GetOrdinal("c_skills")),
                        c_role = reader.GetString(reader.GetOrdinal("c_role")),
                        c_location = reader.GetString(reader.GetOrdinal("c_location")),
                        c_experience = reader.GetString(reader.GetOrdinal("c_experience")),
                        c_description = reader.GetString(reader.GetOrdinal("c_description")),
                        c_salary = reader.GetString(reader.GetOrdinal("c_salary")),
                        c_jd_file = reader["c_jd_file"] as string
                    };

                    company = new t_companydetails
                    {
                        c_company_id = reader.GetInt32(reader.GetOrdinal("c_company_id")),
                        c_userid = reader.GetInt32(reader.GetOrdinal("c_userid")),
                        c_company_email = reader.GetString(reader.GetOrdinal("c_company_email")),
                        c_name = reader.GetString(reader.GetOrdinal("c_name")),
                        c_state = reader.GetString(reader.GetOrdinal("c_state")),
                        c_city = reader.GetString(reader.GetOrdinal("c_city")),
                        c_image = reader["c_image"] as string
                    };
                }

                // ===== Company Rating =====
                using (var cmd = new NpgsqlCommand(ratingQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@companyId", company.c_company_id);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        avgRating = reader.IsDBNull(0) ? 0 : Convert.ToDouble(reader.GetValue(0));
                        totalReviews = reader.GetInt32(1);
                    }
                }

                // ===== Company Reviews =====
                using (var cmd = new NpgsqlCommand(reviewsQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@companyId", company.c_company_id);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        reviews.Add(new
                        {
                            stars = reader.GetInt32(0),
                            description = reader.GetString(1),
                            userName = reader.IsDBNull(2) ? "Anonymous" : reader.GetString(2),
                            userProfileImage = reader.IsDBNull(3) ? null : reader.GetString(3)
                        });
                    }
                }

                return new
                {
                    jobDetails = job,
                    companyDetails = company,
                    companyRating = new
                    {
                        averageRating = Math.Round(avgRating), // Integer rating
                        originalRating = Math.Round(avgRating, 1), // Decimal for display
                        totalReviews = totalReviews
                    },
                    companyReviews = reviews
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching apply job details", ex);
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }


        public async Task<string?> GetLatestApplicationStatusAsync(int userId, int jobId)
        {
            const string query = @"
        SELECT c_status
        FROM t_applications
        WHERE c_userid = @userid AND c_job_id = @jobid
        ORDER BY c_application_id DESC
        LIMIT 1";

            try
            {
                await _connection.OpenAsync();

                using var cmd = new NpgsqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@userid", userId);
                cmd.Parameters.AddWithValue("@jobid", jobId);

                var result = await cmd.ExecuteScalarAsync();

                // Return null if no application exists
                return result?.ToString()?.ToLower();
            }
            catch (Exception ex)
            {
                // Return null instead of throwing, so application can proceed
                Console.WriteLine($"Error checking status for user {userId}, job {jobId}: {ex.Message}");
                return null;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<int> ApplyJobAsync(t_applications model)
        {
           
            const string query = @"
        INSERT INTO t_applications
        (c_userid, c_job_id, c_name, c_email, c_resume_file)
        VALUES
        (@userid, @jobid, @name, @email, @resume)
        RETURNING c_application_id";

            try
            {
                
                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }

                using var cmd = new NpgsqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@userid", model.c_userid);
                cmd.Parameters.AddWithValue("@jobid", model.c_job_id);
                cmd.Parameters.AddWithValue("@name", model.c_name);
                cmd.Parameters.AddWithValue("@email", model.c_email);
                cmd.Parameters.AddWithValue("@resume", model.c_resume_file ?? (object)DBNull.Value);

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while applying job for user {model.c_userid}, job {model.c_job_id}", ex);
            }
        }

        public async Task<t_applications?> GetApplicationPreviewByIdAsync(int applicationId)
        {
            const string query = @"
                SELECT c_name, c_email, c_resume_file
                FROM t_applications
                WHERE c_application_id = @appid";

            try
            {
                await _connection.OpenAsync();

                using var cmd = new NpgsqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@appid", applicationId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return null;

                return new t_applications
                {
                    c_name = reader.GetString(0),
                    c_email = reader.GetString(1),
                    c_resume_file = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching application preview", ex);
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

//===================================================================================================================================================
  

        public async Task<int> GiveCompanyReview(t_company_review review)
        {
             try
            {
                await _connection.OpenAsync();
                string qry = @"INSERT INTO t_company_review (c_company_id, c_userid, c_description, c_stars)
                               VALUES (@c_company_id, @c_userid, @c_description, @c_stars);";

                using (var cmd = new NpgsqlCommand(qry, _connection))
                {
                    cmd.Parameters.AddWithValue("c_company_id", review.c_company_id);
                    cmd.Parameters.AddWithValue("c_userid", review.c_userid);
                    cmd.Parameters.AddWithValue("c_description", review.c_description);
                    cmd.Parameters.AddWithValue("c_stars", review.c_stars);
                    var affectedRow = await cmd.ExecuteNonQueryAsync();
                    if (affectedRow > 0)
                        return 1;
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GiveCompanyReview: " + ex.Message);
                return -1;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }
   

        // DEV UserDetails POPUP S=============================================================

        public async Task<int> AddUserDetails(t_userdetails model)
        {
            try
            {
                string qry = @"
                INSERT INTO t_userdetails
                (c_userid, c_bio, c_skills, c_resume_file, c_job_role, c_job_type, c_location)
                VALUES
                (@userid, @bio, @skills, @resume, @role, @type, @location);";

                using var cmd = new NpgsqlCommand(qry, _connection);

                cmd.Parameters.AddWithValue("@userid", model.c_userid);
                cmd.Parameters.AddWithValue("@bio", (object?)model.c_bio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@skills", (object?)model.c_skills ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@resume", (object?)model.c_resume_file ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@role", (object?)model.c_job_role ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@type", (object?)model.c_job_type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@location", (object?)model.c_location ?? DBNull.Value);

                await _connection.OpenAsync();
                int rows = await cmd.ExecuteNonQueryAsync();
                await _connection.CloseAsync();

                return rows > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddUserDetails Error: " + ex.Message);
                return -1;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<bool> IsDetailsExists(int userid)
        {
            try
            {
                string qry = "SELECT 1 FROM t_userdetails WHERE c_userid=@id;";
                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@id", userid);

                await _connection.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                await _connection.CloseAsync();

                return result != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("IsDetailsExists Error: " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        // DEV UserDetails POPUP E =============================================================


        // Dev Profile Management S ============================================================

        // =====================================================
        // USER (READ ONLY + ALLOWED UPDATE)
        // =====================================================

        public async Task<vm_user?> GetUserForProfileAsync(int userid)
        {
            try
            {
                string sql = @"SELECT
                                c_userid,
                                c_name,
                                c_gender,
                                c_contactno,
                                c_email,
                                c_address,
                                c_role,
                                c_status,
                                c_dob,
                                c_profile_image
                            FROM t_user
                            WHERE c_userid = @uid";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", userid);

                if (_connection.State != System.Data.ConnectionState.Open)
                    await _connection.OpenAsync();

                using var dr = await cmd.ExecuteReaderAsync();

                if (await dr.ReadAsync())
                {
                    return new vm_user
                    {
                        c_userid = dr.GetInt32(0),
                        c_name = dr.GetString(1),
                        c_gender = dr.GetString(2),
                        c_contactno = dr.GetString(3),
                        c_email = dr.GetString(4),
                        c_address = dr.IsDBNull(5) ? null : dr.GetString(5),
                        c_role = dr.GetString(6),
                        c_status = dr.GetString(7),
                        c_dob = dr.GetDateTime(8),
                        c_profile_image = dr.IsDBNull(9) ? null : dr.GetString(9)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching user profile", ex);
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }


        public async Task UpdateUserProfileAsync(vm_user u)
        {
            try
            {
                // First get current user to preserve fields not being updated
                var currentUser = await GetUserForProfileAsync(u.c_userid);
                
                // Build SQL dynamically
                var setClauses = new List<string>();
                var parameters = new List<NpgsqlParameter>();
                
                // Always include these fields
                setClauses.Add("c_address = @address");
                parameters.Add(new NpgsqlParameter("@address", u.c_address ?? (currentUser?.c_address ?? "")));
                
                // Profile image
                if (!string.IsNullOrEmpty(u.c_profile_image))
                {
                    setClauses.Add("c_profile_image = @image");
                    parameters.Add(new NpgsqlParameter("@image", u.c_profile_image));
                }
                
                // Contact number if provided
                if (!string.IsNullOrEmpty(u.c_contactno))
                {
                    setClauses.Add("c_contactno = @contact");
                    parameters.Add(new NpgsqlParameter("@contact", u.c_contactno));
                }
                
                // Gender if provided
                if (!string.IsNullOrEmpty(u.c_gender))
                {
                    setClauses.Add("c_gender = @gender");
                    parameters.Add(new NpgsqlParameter("@gender", u.c_gender));
                }
                
                // DOB if provided
                if (u.c_dob != default)
                {
                    setClauses.Add("c_dob = @dob");
                    parameters.Add(new NpgsqlParameter("@dob", u.c_dob));
                }
                
                // Add user ID
                parameters.Add(new NpgsqlParameter("@uid", u.c_userid));
                
                if (setClauses.Count == 0)
                {
                    Console.WriteLine("No fields to update");
                    return;
                }
                
                string sql = $@"UPDATE t_user SET {string.Join(", ", setClauses)} WHERE c_userid = @uid";
                
                Console.WriteLine($"Executing SQL: {sql}");
                Console.WriteLine($"Parameters: {string.Join(", ", parameters.Select(p => $"{p.ParameterName}={p.Value}"))}");
                
                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddRange(parameters.ToArray());

                if (_connection.State != System.Data.ConnectionState.Open)
                    await _connection.OpenAsync();

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"Rows affected: {rowsAffected}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUserProfileAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw new Exception("Error while updating user profile", ex);
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        // =====================================================
        // USER DETAILS
        // =====================================================

        public async Task<t_userdetails?> GetUserDetailsAsync(int userid)
        {
            try
            {
                string sql = @"SELECT c_details_id,c_userid,c_bio,c_skills,
                                      c_resume_file,c_job_role,c_job_type,c_location
                               FROM t_userdetails WHERE c_userid=@uid";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", userid);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var dr = await cmd.ExecuteReaderAsync();
                if (await dr.ReadAsync())
                {
                    return new t_userdetails
                    {
                        c_details_id = dr.GetInt32(0),
                        c_userid = dr.GetInt32(1),
                        c_bio = dr.IsDBNull(2) ? null : dr.GetString(2),
                        c_skills = dr.IsDBNull(3) ? null : dr.GetString(3),
                        c_resume_file = dr.IsDBNull(4) ? null : dr.GetString(4),
                        c_job_role = dr.IsDBNull(5) ? null : dr.GetString(5),
                        c_job_type = dr.IsDBNull(6) ? null : dr.GetString(6),
                        c_location = dr.IsDBNull(7) ? null : dr.GetString(7)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching user details", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        public async Task SaveUserDetailsAsync(t_userdetails d)
{
    try
    {
        var existing = await GetUserDetailsAsync(d.c_userid);
        
        string sql;
        if (existing == null)
        {
            // INSERT
            sql = @"INSERT INTO t_userdetails
                   (c_userid, c_bio, c_skills, c_resume_file,
                    c_job_role, c_job_type, c_location)
                   VALUES
                   (@uid, @bio, @skills, @resume, @role, @type, @loc)";
        }
        else
        {
            // UPDATE
            sql = @"UPDATE t_userdetails SET
                   c_bio = @bio,
                   c_skills = @skills,
                   c_resume_file = @resume,
                   c_job_role = @role,
                   c_job_type = @type,
                   c_location = @loc
                   WHERE c_userid = @uid";
        }
        
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@uid", d.c_userid);
        cmd.Parameters.AddWithValue("@bio", d.c_bio ?? "");
        cmd.Parameters.AddWithValue("@skills", d.c_skills ?? "");
        cmd.Parameters.AddWithValue("@resume", d.c_resume_file ?? "");
        cmd.Parameters.AddWithValue("@role", d.c_job_role ?? "");
        cmd.Parameters.AddWithValue("@type", d.c_job_type ?? "");
        cmd.Parameters.AddWithValue("@loc", d.c_location ?? "");
        
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();
            
        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        throw new Exception("Error saving user details", ex);
    }
    finally
    {
        if (_connection.State == ConnectionState.Open)
            await _connection.CloseAsync();
    }
}
 /////checkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkk
        // =====================================================
        // EXPERIENCE
        // =====================================================

        public async Task<List<t_userexperience>> GetExperiencesAsync(int userid)
        {
            var list = new List<t_userexperience>();
            try
            {
                string sql = @"SELECT * FROM t_userexperience
                               WHERE c_userid=@uid ORDER BY c_start_date DESC";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", userid);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var dr = await cmd.ExecuteReaderAsync();
                while (await dr.ReadAsync())
                {
                    list.Add(new t_userexperience
                    {
                        c_experience_id = dr.GetInt32(0),
                        c_userid = dr.GetInt32(1),
                        c_title = dr.GetString(2),
                        c_employment_type = dr.GetString(3),
                        c_role = dr.GetString(4),
                        c_company = dr.GetString(5),
                        c_start_date = dr.GetDateTime(6),
                        c_end_date = dr.GetDateTime(7),
                        c_state = dr.GetString(8),
                        c_city = dr.GetString(9)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching experience", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
            return list;
        }

        public async Task AddExperienceAsync(t_userexperience e)
        {
            try
            {
                string sql = @"INSERT INTO t_userexperience
                               (c_userid,c_title,c_employment_type,c_role,
                                c_company,c_start_date,c_end_date,c_state,c_city)
                               VALUES
                               (@uid,@title,@type,@role,@company,@start,@end,@state,@city)";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", e.c_userid);
                cmd.Parameters.AddWithValue("@title", e.c_title);
                cmd.Parameters.AddWithValue("@type", e.c_employment_type);
                cmd.Parameters.AddWithValue("@role", e.c_role);
                cmd.Parameters.AddWithValue("@company", e.c_company);
                cmd.Parameters.AddWithValue("@start", e.c_start_date);
                cmd.Parameters.AddWithValue("@end", e.c_end_date);
                cmd.Parameters.AddWithValue("@state", e.c_state);
                cmd.Parameters.AddWithValue("@city", e.c_city);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding experience", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        public async Task DeleteExperienceAsync(int experienceId)
        {
            await ExecuteDeleteAsync("DELETE FROM t_userexperience WHERE c_experience_id=@id", experienceId);
        }

        public async Task UpdateExperienceAsync(t_userexperience e)
        {
            try
            {
                string sql = @"UPDATE t_userexperience SET
                            c_title = @title,
                            c_employment_type = @type,
                            c_role = @role,
                            c_company = @company,
                            c_start_date = @start,
                            c_end_date = @end,
                            c_state = @state,
                            c_city = @city
                            WHERE c_experience_id = @id
                            AND c_userid = @uid";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@title", e.c_title);
                cmd.Parameters.AddWithValue("@type", e.c_employment_type);
                cmd.Parameters.AddWithValue("@role", e.c_role);
                cmd.Parameters.AddWithValue("@company", e.c_company);
                cmd.Parameters.AddWithValue("@start", e.c_start_date);
                cmd.Parameters.AddWithValue("@end", e.c_end_date);
                cmd.Parameters.AddWithValue("@state", e.c_state);
                cmd.Parameters.AddWithValue("@city", e.c_city);
                cmd.Parameters.AddWithValue("@id", e.c_experience_id);
                cmd.Parameters.AddWithValue("@uid", e.c_userid);

                if (_connection.State != System.Data.ConnectionState.Open)
                    await _connection.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating experience", ex);
            }
            finally
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }
        // =====================================================
        // EDUCATION
        // =====================================================

        public async Task<List<t_educationdetails>> GetEducationsAsync(int userid)
        {
            var list = new List<t_educationdetails>();
            try
            {
                string sql = @"SELECT * FROM t_educationdetails WHERE c_userid=@uid";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", userid);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var dr = await cmd.ExecuteReaderAsync();
                while (await dr.ReadAsync())
                {
                    list.Add(new t_educationdetails
                    {
                        c_education_detail_id = dr.GetInt32(0),
                        c_userid = dr.GetInt32(1),
                        c_schoolname = dr.GetString(2),
                        c_degree = dr.GetString(3),
                        c_fieldofstudy = dr.GetString(4),
                        c_startyear = dr.GetInt32(5),
                        c_endyear = dr.GetInt32(6),
                        c_state = dr.GetString(7),
                        c_city = dr.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching education", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
            return list;
        }

        public async Task AddEducationAsync(t_educationdetails e)
        {
            try
            {
                string sql = @"INSERT INTO t_educationdetails
                               (c_userid,c_schoolname,c_degree,c_fieldofstudy,
                                c_startyear,c_endyear,c_state,c_city)
                               VALUES
                               (@uid,@school,@degree,@field,@start,@end,@state,@city)";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", e.c_userid);
                cmd.Parameters.AddWithValue("@school", e.c_schoolname);
                cmd.Parameters.AddWithValue("@degree", e.c_degree);
                cmd.Parameters.AddWithValue("@field", e.c_fieldofstudy);
                cmd.Parameters.AddWithValue("@start", e.c_startyear);
                cmd.Parameters.AddWithValue("@end", e.c_endyear);
                cmd.Parameters.AddWithValue("@state", e.c_state);
                cmd.Parameters.AddWithValue("@city", e.c_city);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding education", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }


        // ****
        public async Task UpdateEducationAsync(t_educationdetails e)
        {
            try
            {
                string sql = @"UPDATE t_educationdetails SET
                               c_schoolname = @school,
                               c_degree = @degree,
                               c_fieldofstudy = @field,
                               c_startyear = @start,
                               c_endyear = @end,
                               c_state = @state,
                               c_city = @city
                               WHERE c_education_detail_id = @id AND c_userid = @uid";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@id", e.c_education_detail_id);
                cmd.Parameters.AddWithValue("@uid", e.c_userid);
                cmd.Parameters.AddWithValue("@school", (object?)e.c_schoolname ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@degree", (object?)e.c_degree ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@field", (object?)e.c_fieldofstudy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@start", e.c_startyear);
                cmd.Parameters.AddWithValue("@end", e.c_endyear);
                cmd.Parameters.AddWithValue("@state", (object?)e.c_state ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@city", (object?)e.c_city ?? DBNull.Value);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating education", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        public async Task DeleteEducationAsync(int educationId)
        {
            await ExecuteDeleteAsync("DELETE FROM t_educationdetails WHERE c_education_detail_id=@id", educationId);
        }
        // =====================================================
        // CERTIFICATE
        // =====================================================

        public async Task<List<t_certificate>> GetCertificatesAsync(int userid)
        {
            var list = new List<t_certificate>();
            try
            {
                string sql = @"SELECT * FROM t_certificate WHERE c_userid=@uid";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", userid);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var dr = await cmd.ExecuteReaderAsync();
                while (await dr.ReadAsync())
                {
                    list.Add(new t_certificate
                    {
                        c_certificate_id = dr.GetInt32(0),
                        c_userid = dr.GetInt32(1),
                        c_certificatename = dr.GetString(2),
                        c_certificatefile = dr.IsDBNull(3) ? null : dr.GetString(3)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching certificates", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
            return list;
        }

        public async Task AddCertificateAsync(t_certificate c)
        {
            try
            {
                string sql = @"INSERT INTO t_certificate
                            (c_userid, c_certificatename, c_certificatefile)
                            VALUES (@uid, @name, @file)";

                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@uid", c.c_userid);
                cmd.Parameters.AddWithValue("@name", c.c_certificatename);
                cmd.Parameters.AddWithValue("@file", c.c_certificatefile ?? "");

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding certificate", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        public async Task DeleteCertificateAsync(int certificateId)
        {
            try
            {
                string sql = "DELETE FROM t_certificate WHERE c_certificate_id = @id";
                
                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@id", certificateId);
                
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();
                    
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting certificate", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        // =====================================================
        // COMMON DELETE HELPER
        // =====================================================
        private async Task ExecuteDeleteAsync(string sql, int id)
        {
            try
            {
                using var cmd = new NpgsqlCommand(sql, _connection);
                cmd.Parameters.AddWithValue("@id", id);

                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting record", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }
       
        // =============================
        // GET ALL JOBS
        // =============================
        public async Task<List<ApplicantJobVM>> GetAllJobs()
        {
            try
            {
                List<ApplicantJobVM> jobs = new();

                var qry = @"
               SELECT 
    j.c_job_id, 
    j.c_role, 
    j.c_location, 
    j.c_job_type, 
    j.c_experience, 
    j.c_salary, 
    j.c_skills, 
    c.c_name AS company_name,
    cr.c_stars,
    cr.c_description AS review_description
FROM t_job j
LEFT JOIN t_companydetails c ON j.c_company_id = c.c_company_id
LEFT JOIN t_company_review cr ON c.c_company_id = cr.c_company_id
ORDER BY cr.c_stars DESC NULLS LAST, j.c_job_id DESC;
                ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                await _connection.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    jobs.Add(new ApplicantJobVM
                    {
                        c_job_id = Convert.ToInt32(reader["c_job_id"]),
                        c_role = reader["c_role"]?.ToString(),
                        c_location = reader["c_location"]?.ToString(),
                        c_job_type = reader["c_job_type"]?.ToString(),
                        c_experience = reader["c_experience"]?.ToString(),
                        c_salary = reader["c_salary"]?.ToString(),
                        c_skills = reader["c_skills"]?.ToString(),
                        c_company_name = reader["company_name"] == DBNull.Value
                            ? "Company Not Available"
                            : reader["company_name"].ToString()
                    });
                }

                return jobs;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllJobs Error: " + ex.Message);
                return new List<ApplicantJobVM>();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        // =============================
        // RECOMMENDED JOBS
        // =============================
        public async Task<List<ApplicantJobVM>> GetRecommendedJobs(int userid)
        {
            try
            {
                List<ApplicantJobVM> jobs = new();

        //       var qry = @"
        //     SELECT DISTINCT
        //         j.c_job_id,
        //         j.c_role,
        //         j.c_location,
        //         j.c_job_type,
        //         j.c_experience,
        //         j.c_salary,
        //         j.c_skills,
        //         c.c_name AS company_name,
        //         -- Calculate match score (higher is better)
        //         -- Priority: Skills (3) > Job Role (2) > Location (1)
        //         (
        //             CASE WHEN LOWER(j.c_skills) LIKE '%' || LOWER(COALESCE(u.c_skills, '')) || '%' THEN 3 ELSE 0 END +
        //             CASE WHEN LOWER(j.c_role) LIKE '%' || LOWER(COALESCE(u.c_job_role, '')) || '%' THEN 2 ELSE 0 END +
        //             CASE WHEN LOWER(j.c_location) LIKE '%' || LOWER(COALESCE(u.c_location, '')) || '%' THEN 1 ELSE 0 END
        //         ) as match_score
        //     FROM t_job j
        //     LEFT JOIN t_companydetails c ON j.c_company_id = c.c_company_id
        //     INNER JOIN t_userdetails u ON u.c_userid = @userid
        //     WHERE 
        //         -- Match by skills (highest priority)
        //         LOWER(j.c_skills) LIKE '%' || LOWER(COALESCE(u.c_skills, '')) || '%'
        //         OR
        //         -- Match by job role (medium priority)
        //         LOWER(j.c_role) LIKE '%' || LOWER(COALESCE(u.c_job_role, '')) || '%'
        //         OR
        //         -- Match by location (lower priority)
        //         LOWER(j.c_location) LIKE '%' || LOWER(COALESCE(u.c_location, '')) || '%'
        //     ORDER BY match_score DESC, j.c_job_id DESC
        //     LIMIT 10;
        // ";


        var qry = @"SELECT DISTINCT
    j.c_job_id,
    j.c_role,
    j.c_location,
    j.c_job_type,
    j.c_experience,
    j.c_salary,
    j.c_skills,
    c.c_name AS company_name,
    (
        CASE 
            -- Skills match (Highest priority, for each skill in the user's skill list)
            WHEN EXISTS (
                SELECT 1
                FROM unnest(string_to_array(LOWER(COALESCE(u.c_skills, '')), ',')) AS user_skill
                WHERE LOWER(j.c_skills) LIKE '%' || user_skill || '%'
            ) THEN 3 
            ELSE 0 
        END +
        -- Job role match (Medium priority, for each job role in the user's job role list)
        CASE WHEN EXISTS (
            SELECT 1
            FROM unnest(string_to_array(LOWER(COALESCE(u.c_job_role, '')), ',')) AS user_role
            WHERE LOWER(j.c_role) LIKE '%' || user_role || '%'
        ) THEN 2 ELSE 0 END +
        -- Location match (Lowest priority)
        CASE WHEN LOWER(j.c_location) LIKE '%' || LOWER(COALESCE(u.c_location, '')) || '%' THEN 1 ELSE 0 END
    ) AS match_score
FROM t_job j
LEFT JOIN t_companydetails c ON j.c_company_id = c.c_company_id
INNER JOIN t_userdetails u ON u.c_userid = @userid
WHERE 
    -- Match by skills (highest priority)
    EXISTS (
        SELECT 1
        FROM unnest(string_to_array(LOWER(COALESCE(u.c_skills, '')), ',')) AS user_skill
        WHERE LOWER(j.c_skills) LIKE '%' || user_skill || '%'
    )
    OR
    -- Match by job role (medium priority)
    EXISTS (
        SELECT 1
        FROM unnest(string_to_array(LOWER(COALESCE(u.c_job_role, '')), ',')) AS user_role
        WHERE LOWER(j.c_role) LIKE '%' || user_role || '%'
    )
    OR
    -- Match by location (lowest priority)
    LOWER(j.c_location) LIKE '%' || LOWER(COALESCE(u.c_location, '')) || '%'
ORDER BY match_score DESC, j.c_job_id DESC
LIMIT 10;";
                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@userid", userid);

                await _connection.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    jobs.Add(new ApplicantJobVM
                    {
                        c_job_id = Convert.ToInt32(reader["c_job_id"]),
                        c_role = reader["c_role"]?.ToString(),
                        c_location = reader["c_location"]?.ToString(),
                        c_job_type = reader["c_job_type"]?.ToString(),
                        c_experience = reader["c_experience"]?.ToString(),
                        c_salary = reader["c_salary"]?.ToString(),
                        c_skills = reader["c_skills"]?.ToString(),
                        c_company_name = reader["company_name"] == DBNull.Value
                            ? "Company Not Available"
                            : reader["company_name"].ToString()
                    });
                }

                return jobs;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetRecommendedJobs Error: " + ex.Message);
                return new List<ApplicantJobVM>();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
        // =============================
        // GET SAVED JOBS
        // =============================
        public async Task<List<ApplicantJobVM>> GetSavedJobs(int userId)
        {
            try
            {
                List<ApplicantJobVM> jobs = new();

                var qry = @"
                   SELECT 
                    a.c_saveid,
                    a.c_status,

                    j.c_job_id,
                    j.c_role,
                    j.c_location,
                    j.c_job_type,
                    j.c_experience,
                    j.c_salary,
                    j.c_skills,

                    c.c_company_id,
                    c.c_name AS company_name,
                    c.c_city,
                    c.c_state,
                    c.c_company_email,
                    c.c_image
                FROM t_savejob a
                INNER JOIN t_job j 
                    ON j.c_job_id = a.c_job_id
                INNER JOIN t_companydetails c 
                    ON c.c_company_id = j.c_company_id
                WHERE a.c_userid = @userid
                AND a.c_status = 'saved'
                ORDER BY a.c_saveid DESC;
                ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@userid", userId);

                await _connection.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    jobs.Add(new ApplicantJobVM
                    {
                        c_job_id = Convert.ToInt32(reader["c_job_id"]),
                        c_role = reader["c_role"]?.ToString(),
                        c_location = reader["c_location"]?.ToString(),
                        c_job_type = reader["c_job_type"]?.ToString(),
                        c_experience = reader["c_experience"]?.ToString(),
                        c_salary = reader["c_salary"]?.ToString(),
                        c_skills = reader["c_skills"]?.ToString(),
                        c_company_name = reader["company_name"]?.ToString() ?? "Company Not Available",
                        c_status = reader["c_status"]?.ToString()
                    });
                }

                return jobs;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetSavedJobs Error: " + ex.Message);
                return new List<ApplicantJobVM>();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        // =============================
        // GET APPLIED JOBS
        // =============================
        public async Task<List<ApplicantJobVM>> GetAppliedJobs(int userId)
        {
            try
            {
                List<ApplicantJobVM> jobs = new();

                var qry = @"
                    SELECT 
                        j.c_job_id,
                        j.c_role,
                        j.c_location,
                        j.c_job_type,
                        j.c_experience,
                        j.c_salary,
                        j.c_skills,
                        c.c_name AS company_name,
                        a.c_status,
                        a.c_application_id
                    FROM t_applications a
                    INNER JOIN t_job j ON j.c_job_id = a.c_job_id
                    LEFT JOIN t_companydetails c ON j.c_company_id = c.c_company_id
                    WHERE a.c_userid = @userid
                      AND a.c_status != 'saved'
                    ORDER BY a.c_application_id DESC;
                ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@userid", userId);

                await _connection.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    jobs.Add(new ApplicantJobVM
                    {
                        c_job_id = Convert.ToInt32(reader["c_job_id"]),
                        c_role = reader["c_role"]?.ToString(),
                        c_location = reader["c_location"]?.ToString(),
                        c_job_type = reader["c_job_type"]?.ToString(),
                        c_experience = reader["c_experience"]?.ToString(),
                        c_salary = reader["c_salary"]?.ToString(),
                        c_skills = reader["c_skills"]?.ToString(),
                        c_company_name = reader["company_name"]?.ToString() ?? "Company Not Available",
                        c_status = reader["c_status"]?.ToString()
                    });
                }

                return jobs;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAppliedJobs Error: " + ex.Message);
                return new List<ApplicantJobVM>();
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        // =============================
        // SAVE JOB
        // =============================
        public async Task<bool> SaveJob(int userId, int jobId)
        {
            try
            {

                var qry = @"
                    INSERT INTO t_savejob (c_userid, c_job_id, c_status)
                    VALUES (@userid, @jobid, 'saved')
                    ON CONFLICT (c_userid, c_job_id) DO NOTHING;
                ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@userid", userId);
                cmd.Parameters.AddWithValue("@jobid", jobId);

                await _connection.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveJob Error: " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<bool> IsJobSaved(int userId, int jobId)
        {
            try
            {
                var qry = @"
            SELECT 1
            FROM t_savejob
            WHERE c_userid = @userid
              AND c_job_id = @jobid
              AND c_status = 'saved'
            LIMIT 1;
        ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@userid", userId);
                cmd.Parameters.AddWithValue("@jobid", jobId);

                await _connection.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                return result != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("IsJobSaved Error: " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }


        // =============================
        // APPLY TO JOB
        // =============================
        public async Task<bool> ApplyJob(int userId, int jobId, string name, string email, string resumeFile)
        {
            try
            {
                var qry = @"
                    INSERT INTO t_applications (c_userid, c_job_id, c_name, c_email, c_resume_file, c_status)
                    VALUES (@userid, @jobid, @name, @email, @resume, 'pending');
                ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@userid", userId);
                cmd.Parameters.AddWithValue("@jobid", jobId);
                cmd.Parameters.AddWithValue("@name", name ?? "");
                cmd.Parameters.AddWithValue("@email", email ?? "");
                cmd.Parameters.AddWithValue("@resume", resumeFile ?? "");

                await _connection.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ApplyJob Error: " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        // =============================
        // REMOVE SAVED JOB
        // =============================
        public async Task<bool> RemoveSavedJob(int userId, int jobId)
        {
            try
            {
                var qry = @"
            DELETE FROM t_savejob 
            WHERE c_userid = @userid 
              AND c_job_id = @jobid 
              AND c_status = 'saved';
        ";

                using var cmd = new NpgsqlCommand(qry, _connection);
                cmd.Parameters.AddWithValue("@userid", userId);
                cmd.Parameters.AddWithValue("@jobid", jobId);

                await _connection.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RemoveSavedJob Error: " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }



        public async Task<bool> IsJobAccepted(int userId, int jobId)
{
    try
    {
        await _connection.OpenAsync();

        string query = @"
            SELECT 1
            FROM t_applications
            WHERE c_userid = @userid
              AND c_job_id = @jobid
              AND c_status = 'accepted'
            LIMIT 1;
        ";

        using var cmd = new NpgsqlCommand(query, _connection);
        cmd.Parameters.AddWithValue("userid", userId);
        cmd.Parameters.AddWithValue("jobid", jobId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }
    finally
    {
        if (_connection.State == ConnectionState.Open)
            await _connection.CloseAsync();
    }
}


public async Task<int> GetCompanyIdByJobId(int jobId)
{
    try
    {
        await _connection.OpenAsync();

        string query = @"
            SELECT c_company_id
            FROM t_job
            WHERE c_job_id = @jobid;
        ";

        using var cmd = new NpgsqlCommand(query, _connection);
        cmd.Parameters.AddWithValue("jobid", jobId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }
    finally
    {
        if (_connection.State == ConnectionState.Open)
            await _connection.CloseAsync();
    }
}

    }
}