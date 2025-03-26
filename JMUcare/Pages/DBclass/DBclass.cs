using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using JMUcare.Pages.Dataclasses;
using Microsoft.AspNetCore.Identity;
namespace JMUcare.Pages.DBclass
{
    public class DBClass
    {
        public static SqlConnection JMUcareDBConnection = new SqlConnection();

        private static readonly string JMUcareDBConnString =
            "Server=LocalHost;Database=JMU_CARE;Trusted_Connection=True";

        private static readonly string? AuthConnString =
            "Server=Localhost;Database=AUTH;Trusted_Connection=True";

        public const int SaltByteSize = 24; // standard, secure size of salts
        public const int HashByteSize = 20; // to match the size of the PBKDF2-HMAC-SHA-1 hash (standard)
        public const int Pbkdf2Iterations = 1000; // higher number is more secure but takes longer
        public const int IterationIndex = 0; // used to find first section (number of iterations) of PasswordHash database field
        public const int SaltIndex = 1; // used to find second section (salt) of PasswordHash database field
        public const int Pbkdf2Index = 2; // used to find third section (hash) of PasswordHash database field


        public static string HashPassword(string password)
        {
            var cryptoProvider = new RNGCryptoServiceProvider(); // create a new crypto provider
            byte[] salt = new byte[SaltByteSize]; // creates a new random salt of a certain length
            cryptoProvider.GetBytes(salt); // fills array with cryptographically strong sequence of random values

            var hash = GetPbkdf2Bytes(password, salt, Pbkdf2Iterations, HashByteSize); // call method below to create the hash
            return Pbkdf2Iterations + ":" + Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash); // create string to store in database and return
        }

        private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputBytes)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt); // create a new key
            pbkdf2.IterationCount = iterations; // assign number of iterations that the function is run
            return pbkdf2.GetBytes(outputBytes); // return pseudo-random hash of certain length
        }

        public static bool ValidatePassword(string password, string correctHash)
        {
            char[] delimiter = { ':' }; // this section takes the whole stored string and splits it up into the 3 parts
            var split = correctHash.Split(delimiter); // splits the long string at the : character
            var iterations = Int32.Parse(split[IterationIndex]); // picks out the first section and assigns the stored number of iterations to new variable
            var salt = Convert.FromBase64String(split[SaltIndex]); // picks out the second section and assign stored salt to new variable
            var hash = Convert.FromBase64String(split[Pbkdf2Index]); // picks out the third section and assign stored password hash to new variable

            var testHash = GetPbkdf2Bytes(password, salt, iterations, hash.Length); // creates the hash for the entered password
            return SlowEquals(hash, testHash); // compare the stored password (hash) to the entered password (testhash) and return true (matches) or false (doesn't)
        }

        private static bool SlowEquals(byte[] a, byte[] b) // optional method -> increases security/makes password cracking take longer
        {
            var diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        public static bool HashedParameterLogin(string Username, string Password)
        {
            using (SqlConnection conn = new SqlConnection(AuthConnString))
            {
                SqlCommand cmd = new SqlCommand("sp_Lab3Login", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", Username);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string correctHash = reader["Password"].ToString();
                    conn.Close();

                    if (ValidatePassword(Password, correctHash))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void CreateHashedUser(string Username, string Password)
        {
            string loginQuery =
                "INSERT INTO HashedCredentials (Username,Password) values (@Username, @Password)";

            SqlCommand cmdLogin = new SqlCommand();
            cmdLogin.Connection = JMUcareDBConnection;
            cmdLogin.Connection.ConnectionString = AuthConnString;

            cmdLogin.CommandText = loginQuery;
            cmdLogin.Parameters.AddWithValue("@Username", Username);
            cmdLogin.Parameters.AddWithValue("@Password", HashPassword(Password));

            cmdLogin.Connection.Open();

            // ExecuteScalar() returns back data type Object
            // Use a typecast to convert this to an int.
            // Method returns first column of first row.
            cmdLogin.ExecuteNonQuery();

        }

        public static void InsertDBUser(DbUserModel dbUser)
        {
            int newUserId = 0;

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"INSERT INTO DBUser (FirstName, LastName, Email, Username, UserRoleID, UpdatedAt, IsArchived) 
                            OUTPUT INSERTED.UserID 
                            VALUES (@FirstName, @LastName, @Email, @Username, @UserRoleID, @UpdatedAt, @IsArchived)";

                using (SqlCommand cmdUserInsert = new SqlCommand(sqlQuery, connection))
                {
                    cmdUserInsert.Parameters.AddWithValue("@FirstName", dbUser.FirstName);
                    cmdUserInsert.Parameters.AddWithValue("@LastName", dbUser.LastName);
                    cmdUserInsert.Parameters.AddWithValue("@Email", dbUser.Email);
                    cmdUserInsert.Parameters.AddWithValue("@Username", dbUser.Username);
                    cmdUserInsert.Parameters.AddWithValue("@UserRoleID", dbUser.UserRoleID);
                    cmdUserInsert.Parameters.AddWithValue("@UpdatedAt", dbUser.UpdatedAt);
                    cmdUserInsert.Parameters.AddWithValue("@IsArchived", dbUser.IsArchived);

                    connection.Open();
                    newUserId = (int)cmdUserInsert.ExecuteScalar();
                }

            }
            CreateHashedUser(dbUser.Username, dbUser.Password);
        }


        public static List<DbUserModel> GetUsers()
        {
            var users = new List<DbUserModel>();

            using var connection = new SqlConnection(JMUcareDBConnString);
            const string sqlQuery = @"
        SELECT UserID, FirstName, LastName 
        FROM DBUser 
        WHERE IsArchived = 0";

            using var cmd = new SqlCommand(sqlQuery, connection);
            connection.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new DbUserModel
                {
                    UserID = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2)
                    // Optionally add: FullName = reader.GetString(1) + " " + reader.GetString(2)
                });
            }

            return users;
        }


        public static int InsertGrant(GrantModel grant)
        {
            int newGrantId;

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            INSERT INTO Grants (
                GrantTitle,
                Category,
                FundingSource,
                Amount,
                Status,
                CreatedBy,
                GrantLeadID,
                Description,
                TrackingStatus,
                IsArchived
            )
            OUTPUT INSERTED.GrantID
            VALUES (
                @GrantTitle,
                @Category,
                @FundingSource,
                @Amount,
                @Status,
                @CreatedBy,
                @GrantLeadID,
                @Description,
                @TrackingStatus,
                @IsArchived
            )";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantTitle", grant.GrantTitle);
                    cmd.Parameters.AddWithValue("@Category", grant.Category);
                    cmd.Parameters.AddWithValue("@FundingSource", grant.FundingSource);
                    cmd.Parameters.AddWithValue("@Amount", grant.Amount);
                    cmd.Parameters.AddWithValue("@Status", grant.Status);
                    cmd.Parameters.AddWithValue("@CreatedBy", grant.CreatedBy);
                    cmd.Parameters.AddWithValue("@GrantLeadID", grant.GrantLeadID);
                    cmd.Parameters.AddWithValue("@Description", grant.Description ?? "");
                    cmd.Parameters.AddWithValue("@TrackingStatus", grant.TrackingStatus ?? "");
                    cmd.Parameters.AddWithValue("@IsArchived", grant.IsArchived);

                    connection.Open();
                    newGrantId = (int)cmd.ExecuteScalar();
                }
            }

            return newGrantId;
        }

        public static void InsertGrantPermission(int grantId, int userId, string accessLevel)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            INSERT INTO Grant_Permission (GrantID, UserID, AccessLevel)
            VALUES (@GrantID, @UserID, @AccessLevel)";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantID", grantId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@AccessLevel", accessLevel);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static List<GrantModel> GetGrantsForUser(int userId)
        {
            var grants = new List<GrantModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        SELECT DISTINCT g.*
        FROM Grants g
        LEFT JOIN Grant_Permission gp ON g.GrantID = gp.GrantID
        LEFT JOIN DBUser u ON u.UserID = @UserID
        LEFT JOIN UserRole ur ON u.UserRoleID = ur.UserRoleID
        WHERE 
            (gp.UserID = @UserID AND gp.AccessLevel IN ('Read', 'Edit')) OR ur.RoleName = 'Admin'";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            grants.Add(new GrantModel
                            {
                                // Your existing property mappings
                                GrantID = reader.GetInt32(reader.GetOrdinal("GrantID")),
                                GrantTitle = reader.GetString(reader.GetOrdinal("GrantTitle")),
                                // ... rest of your properties
                            });
                        }
                    }
                }
            }

            return grants ?? new List<GrantModel>();
        }



        public static int GetUserIdByUsername(string username)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string query = "SELECT UserID FROM DBUser WHERE Username = @Username";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    connection.Open();
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }


        public static string GetUserAccessLevelForGrant(int userId, int grantId)
        {
            string accessLevel = "None";

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                // First check if user is an admin
                string adminQuery = @"
            SELECT ur.RoleName 
            FROM DBUser u
            JOIN UserRole ur ON u.UserRoleID = ur.UserRoleID
            WHERE u.UserID = @UserID AND ur.RoleName = 'Admin'";

                using (SqlCommand adminCmd = new SqlCommand(adminQuery, connection))
                {
                    adminCmd.Parameters.AddWithValue("@UserID", userId);
                    connection.Open();
                    var adminResult = adminCmd.ExecuteScalar();

                    if (adminResult != null)
                    {
                        return "Edit"; // Admins get edit access to all grants
                    }

                    // Check specific grant permission
                    string permQuery = @"
                SELECT AccessLevel 
                FROM Grant_Permission 
                WHERE GrantID = @GrantID AND UserID = @UserID";

                    using (SqlCommand permCmd = new SqlCommand(permQuery, connection))
                    {
                        permCmd.Parameters.AddWithValue("@GrantID", grantId);
                        permCmd.Parameters.AddWithValue("@UserID", userId);

                        var result = permCmd.ExecuteScalar();
                        if (result != null)
                        {
                            accessLevel = result.ToString();
                        }
                    }
                }
            }

            return accessLevel;
        }

        public static GrantModel GetGrantById(int grantId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            SELECT * FROM Grants
            WHERE GrantID = @GrantID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantID", grantId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new GrantModel
                            {
                                GrantID = reader.GetInt32(reader.GetOrdinal("GrantID")),
                                GrantTitle = reader.GetString(reader.GetOrdinal("GrantTitle")),
                                Category = reader.GetString(reader.GetOrdinal("Category")),
                                FundingSource = reader.GetString(reader.GetOrdinal("FundingSource")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                                GrantLeadID = reader.GetInt32(reader.GetOrdinal("GrantLeadID")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                                TrackingStatus = reader.IsDBNull(reader.GetOrdinal("TrackingStatus")) ? "" : reader.GetString(reader.GetOrdinal("TrackingStatus")),
                                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public static void UpdateGrant(GrantModel grant)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            UPDATE Grants SET
                GrantTitle = @GrantTitle,
                Category = @Category,
                FundingSource = @FundingSource,
                Amount = @Amount,
                Status = @Status,
                GrantLeadID = @GrantLeadID,
                Description = @Description,
                TrackingStatus = @TrackingStatus
            WHERE GrantID = @GrantID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantID", grant.GrantID);
                    cmd.Parameters.AddWithValue("@GrantTitle", grant.GrantTitle);
                    cmd.Parameters.AddWithValue("@Category", grant.Category);
                    cmd.Parameters.AddWithValue("@FundingSource", grant.FundingSource);
                    cmd.Parameters.AddWithValue("@Amount", grant.Amount);
                    cmd.Parameters.AddWithValue("@Status", grant.Status);
                    cmd.Parameters.AddWithValue("@GrantLeadID", grant.GrantLeadID);
                    cmd.Parameters.AddWithValue("@Description", grant.Description ?? "");
                    cmd.Parameters.AddWithValue("@TrackingStatus", grant.TrackingStatus ?? "");

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }



    }
}

