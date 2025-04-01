using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using JMUcare.Pages.Dataclasses;
using Microsoft.AspNetCore.Identity;
using static JMUcare.Pages.Projects.EditTaskModel;
namespace JMUcare.Pages.DBclass
{
    public class DBClass
    {
        public static SqlConnection JMUcareDBConnection = new SqlConnection();

        //Orginial DB Connection
        
        //private static readonly string JMUcareDBConnString =
        // "Server=LocalHost;Database=JMU_CARE;Trusted_Connection=True";

        //private static readonly string? AuthConnString =
        //"Server=Localhost;Database=AUTH;Trusted_Connection=True";

        //private static readonly string JMUcareDBConnString =
        //    "Server=LOCALHOST\\MSSQLSERVER484;Database=JMU_CARE;Trusted_Connection=True";

        //private static readonly string? AuthConnString =
        //    "Server=LOCALHOST\\MSSQLSERVER484;Database=AUTH;Trusted_Connection=True";





        //Will's Connection Below


        //public static readonly string JMUcareDBConnString =
            //"Server=DESKTOP-LUH5RCB;Database=JMU_CARE;Trusted_Connection=True";

       //private static readonly string? AuthConnString =
            //"Server=DESKTOP-LUH5RCB;Database=AUTH;Trusted_Connection=True";




            //Dylan BELOW

      //private static readonly string JMUcareDBConnString =
           // "Server=LOCALHOST\\MSSQLSERVER01;Database=JMU_CARE;Trusted_Connection=True";

       // private static readonly string? AuthConnString =
            //"Server=LOCALHOST\\MSSQLSERVER01;Database=AUTH;Trusted_Connection=True";

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
            string loginQuery = "INSERT INTO HashedCredentials (Username,Password) values (@Username, @Password)";

            using (SqlConnection connection = new SqlConnection(AuthConnString))
            {
                SqlCommand cmdLogin = new SqlCommand(loginQuery, connection);
                cmdLogin.Parameters.AddWithValue("@Username", Username);
                cmdLogin.Parameters.AddWithValue("@Password", HashPassword(Password));

                connection.Open();
                cmdLogin.ExecuteNonQuery();
            }
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
                string checkQuery = @"
                SELECT COUNT(*) FROM Grant_Permission 
                WHERE GrantID = @GrantID AND UserID = @UserID";

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@GrantID", grantId);
                    checkCmd.Parameters.AddWithValue("@UserID", userId);

                    connection.Open();
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count == 0)
                    {
                        string insertQuery = @"
                        INSERT INTO Grant_Permission (GrantID, UserID, AccessLevel)
                        VALUES (@GrantID, @UserID, @AccessLevel)";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@GrantID", grantId);
                            insertCmd.Parameters.AddWithValue("@UserID", userId);
                            insertCmd.Parameters.AddWithValue("@AccessLevel", accessLevel);

                            insertCmd.ExecuteNonQuery();
                        }
                    }
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
            ((gp.UserID = @UserID AND gp.AccessLevel IN ('Read', 'Edit')) OR ur.RoleName = 'Admin')
            AND g.IsArchived = 0";  // Added this condition to filter out archived grants

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

        public static bool IsUserAdmin(int userId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
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

                    return adminResult != null;
                }
            }
        }

        public static void UpdateGrantPermission(int grantId, int userId, string accessLevel)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            MERGE Grant_Permission AS target
            USING (SELECT @GrantID, @UserID, @AccessLevel) AS source (GrantID, UserID, AccessLevel)
            ON target.GrantID = source.GrantID AND target.UserID = source.UserID
            WHEN MATCHED THEN
                UPDATE SET AccessLevel = source.AccessLevel
            WHEN NOT MATCHED THEN
                INSERT (GrantID, UserID, AccessLevel)
                VALUES (source.GrantID, source.UserID, source.AccessLevel);";

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

        public static List<(DbUserModel User, string AccessLevel)> GetGrantUserPermissions(int grantId)
        {
            var users = new List<(DbUserModel User, string AccessLevel)>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            SELECT u.*, gp.AccessLevel
            FROM DBUser u
            JOIN Grant_Permission gp ON u.UserID = gp.UserID
            WHERE gp.GrantID = @GrantID AND gp.AccessLevel != 'None'";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantID", grantId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new DbUserModel
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                // Add other user properties as needed
                            };
                            string accessLevel = reader.GetString(reader.GetOrdinal("AccessLevel"));
                            users.Add((user, accessLevel));
                        }
                    }
                }
            }

            return users;
        }
        public static int InsertPhase(PhaseModel phase)
        {
            int newPhaseId;

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
                INSERT INTO Phase (
                    PhaseName,
                    Description,
                    Status,
                    CreatedBy,
                    PhaseLeadID
                )
                OUTPUT INSERTED.PhaseID
                VALUES (
                    @PhaseName,
                    @Description,
                    @Status,
                    @CreatedBy,
                    @PhaseLeadID
                )";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseName", phase.PhaseName);
                    cmd.Parameters.AddWithValue("@Description", phase.Description ?? "");
                    cmd.Parameters.AddWithValue("@Status", phase.Status);
                    cmd.Parameters.AddWithValue("@CreatedBy", phase.CreatedBy);
                    cmd.Parameters.AddWithValue("@PhaseLeadID", phase.PhaseLeadID);

                    connection.Open();
                    newPhaseId = (int)cmd.ExecuteScalar();
                }
            }

            return newPhaseId;
        }

        public static void InsertPhasePermission(int phaseId, int userId, string accessLevel)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string checkQuery = @"
        SELECT COUNT(*) FROM Phase_Permission 
        WHERE PhaseID = @PhaseID AND UserID = @UserID";

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    checkCmd.Parameters.AddWithValue("@UserID", userId);

                    connection.Open();
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count == 0)
                    {
                        string insertQuery = @"
                INSERT INTO Phase_Permission (PhaseID, UserID, AccessLevel)
                VALUES (@PhaseID, @UserID, @AccessLevel)";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                            insertCmd.Parameters.AddWithValue("@UserID", userId);
                            insertCmd.Parameters.AddWithValue("@AccessLevel", accessLevel);

                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public static List<PhaseModel> GetPhasesForUser(int userId)
        {
            var phases = new List<PhaseModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
                SELECT p.*
                FROM Phase p
                LEFT JOIN Phase_Permission pp ON p.PhaseID = pp.PhaseID
                LEFT JOIN DBUser u ON u.UserID = @UserID
                LEFT JOIN UserRole ur ON u.UserRoleID = ur.UserRoleID
                WHERE 
                    (pp.UserID = @UserID AND pp.AccessLevel IN ('Read', 'Edit')) 
                    OR ur.RoleName = 'Admin'
                    OR EXISTS (
                        SELECT 1 
                        FROM Grant_Permission gp 
                        WHERE gp.UserID = @UserID AND gp.AccessLevel = 'Edit'
                    )";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            phases.Add(new PhaseModel
                            {
                                PhaseID = reader.GetInt32(reader.GetOrdinal("PhaseID")),
                                PhaseName = reader.GetString(reader.GetOrdinal("PhaseName")), // Updated here
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                                PhaseLeadID = reader.GetInt32(reader.GetOrdinal("PhaseLeadID"))
                            });
                        }
                    }
                }
            }

            return phases ?? new List<PhaseModel>();
        }

        public static PhaseModel GetPhaseById(int phaseId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        SELECT * FROM Phase
        WHERE PhaseID = @PhaseID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PhaseModel
                            {
                                PhaseID = reader.GetInt32(reader.GetOrdinal("PhaseID")),
                                PhaseName = reader.GetString(reader.GetOrdinal("PhaseName")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                                PhaseLeadID = reader.GetInt32(reader.GetOrdinal("PhaseLeadID"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public static void UpdatePhase(PhaseModel phase)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        UPDATE Phase SET
            PhaseName = @PhaseName,
            Description = @Description,
            Status = @Status,
            PhaseLeadID = @PhaseLeadID
        WHERE PhaseID = @PhaseID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phase.PhaseID);
                    cmd.Parameters.AddWithValue("@PhaseName", phase.PhaseName);
                    cmd.Parameters.AddWithValue("@Description", phase.Description ?? "");
                    cmd.Parameters.AddWithValue("@Status", phase.Status);
                    cmd.Parameters.AddWithValue("@PhaseLeadID", phase.PhaseLeadID);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdatePhasePermission(int phaseId, int userId, string accessLevel)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        MERGE Phase_Permission AS target
        USING (SELECT @PhaseID, @UserID, @AccessLevel) AS source (PhaseID, UserID, AccessLevel)
        ON target.PhaseID = source.PhaseID AND target.UserID = source.UserID
        WHEN MATCHED THEN
            UPDATE SET AccessLevel = source.AccessLevel
        WHEN NOT MATCHED THEN
            INSERT (PhaseID, UserID, AccessLevel)
            VALUES (source.PhaseID, source.UserID, source.AccessLevel);";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@AccessLevel", accessLevel);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<(DbUserModel User, string AccessLevel)> GetPhaseUserPermissions(int phaseId)
        {
            var users = new List<(DbUserModel User, string AccessLevel)>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        SELECT u.*, pp.AccessLevel
        FROM DBUser u
        JOIN Phase_Permission pp ON u.UserID = pp.UserID
        WHERE pp.PhaseID = @PhaseID AND pp.AccessLevel != 'None'";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new DbUserModel
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                // Add other user properties as needed
                            };
                            string accessLevel = reader.GetString(reader.GetOrdinal("AccessLevel"));
                            users.Add((user, accessLevel));
                        }
                    }
                }
            }

            return users;
        }
        // Add this method to DBClass.cs
        public static void InsertGrantPhase(int grantId, int phaseId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                connection.Open();

                // First, get the maximum position of existing phases
                int maxPosition = 0;
                string positionQuery = @"
            SELECT ISNULL(MAX(p.PhasePosition), 0)
            FROM Phase p
            JOIN Grant_Phase gp ON p.PhaseID = gp.PhaseID
            WHERE gp.GrantID = @GrantID";

                using (SqlCommand posCmd = new SqlCommand(positionQuery, connection))
                {
                    posCmd.Parameters.AddWithValue("@GrantID", grantId);
                    var result = posCmd.ExecuteScalar();
                    maxPosition = Convert.ToInt32(result);
                }

                // Set the new phase position to be after the last phase
                int newPosition = maxPosition + 1;
                string updateQuery = @"
            UPDATE Phase
            SET PhasePosition = @PhasePosition
            WHERE PhaseID = @PhaseID";

                using (SqlCommand updateCmd = new SqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    updateCmd.Parameters.AddWithValue("@PhasePosition", newPosition);
                    updateCmd.ExecuteNonQuery();
                }

                // Insert the grant-phase relationship
                string sqlQuery = @"
            INSERT INTO Grant_Phase (GrantID, PhaseID)
            VALUES (@GrantID, @PhaseID)";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantID", grantId);
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static string GetUserAccessLevelForPhase(int userId, int phaseId)
        {
            string accessLevel = "None";

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                connection.Open();

                // First check if user is an admin
                if (IsUserAdmin(userId))
                {
                    return "Edit"; // Admins get edit access to all phases
                }

                // Check if user has edit access to the parent grant
                int grantId = 0;
                string grantQuery = @"
            SELECT gp.GrantID
            FROM Grant_Phase gp
            WHERE gp.PhaseID = @PhaseID";

                using (SqlCommand grantCmd = new SqlCommand(grantQuery, connection))
                {
                    grantCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    var grantResult = grantCmd.ExecuteScalar();
                    if (grantResult != null && grantResult != DBNull.Value)
                    {
                        grantId = (int)grantResult;

                        // If user has edit access to the grant, they have edit access to this phase
                        string grantAccess = GetUserAccessLevelForGrant(userId, grantId);
                        if (grantAccess == "Edit")
                        {
                            return "Edit";
                        }
                    }
                }

                // Check specific phase permission if grant access didn't provide edit rights
                string permQuery = @"
            SELECT AccessLevel 
            FROM Phase_Permission 
            WHERE PhaseID = @PhaseID AND UserID = @UserID";

                using (SqlCommand permCmd = new SqlCommand(permQuery, connection))
                {
                    permCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    permCmd.Parameters.AddWithValue("@UserID", userId);

                    var result = permCmd.ExecuteScalar();
                    if (result != null)
                    {
                        accessLevel = result.ToString();
                    }
                }
            }

            return accessLevel;
        }

        public static string GetUserAccessLevelForProject(int userId, int projectId)
        {
            string accessLevel = "None";

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                connection.Open();

                // First check if user is an admin
                if (IsUserAdmin(userId))
                {
                    return "Edit"; // Admins get edit access to all projects
                }

                // Get the phase ID and grant ID for this project
                int phaseId = 0;
                int? grantId = null;

                string phaseQuery = @"
            SELECT pp.PhaseID, p.GrantID
            FROM Phase_Project pp
            JOIN Project p ON pp.ProjectID = p.ProjectID
            WHERE pp.ProjectID = @ProjectID";

                using (SqlCommand phaseCmd = new SqlCommand(phaseQuery, connection))
                {
                    phaseCmd.Parameters.AddWithValue("@ProjectID", projectId);
                    using (var reader = phaseCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            phaseId = reader.GetInt32(0);
                            if (!reader.IsDBNull(1))
                            {
                                grantId = reader.GetInt32(1);
                            }
                        }
                    }
                }

                // If project doesn't have a phase or grant, try to get the grant through the phase
                if (!grantId.HasValue && phaseId > 0)
                {
                    string grantQuery = @"
                SELECT GrantID
                FROM Grant_Phase
                WHERE PhaseID = @PhaseID";

                    using (SqlCommand grantCmd = new SqlCommand(grantQuery, connection))
                    {
                        grantCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                        var grantResult = grantCmd.ExecuteScalar();
                        if (grantResult != null && grantResult != DBNull.Value)
                        {
                            grantId = (int)grantResult;
                        }
                    }
                }

                // Check if user has edit access to the grant
                if (grantId.HasValue)
                {
                    string grantAccess = GetUserAccessLevelForGrant(userId, grantId.Value);
                    if (grantAccess == "Edit")
                    {
                        return "Edit"; // Grant editors get edit access to all projects in the grant
                    }
                }

                // Check if user has edit access to the phase
                if (phaseId > 0)
                {
                    string phaseAccess = GetUserAccessLevelForPhase(userId, phaseId);
                    if (phaseAccess == "Edit")
                    {
                        return "Edit"; // Phase editors get edit access to all projects in the phase
                    }
                }

                // Check specific project permission if grant/phase access didn't provide edit rights
                string permQuery = @"
            SELECT AccessLevel 
            FROM Project_Permission 
            WHERE ProjectID = @ProjectID AND UserID = @UserID";

                using (SqlCommand permCmd = new SqlCommand(permQuery, connection))
                {
                    permCmd.Parameters.AddWithValue("@ProjectID", projectId);
                    permCmd.Parameters.AddWithValue("@UserID", userId);

                    var result = permCmd.ExecuteScalar();
                    if (result != null)
                    {
                        accessLevel = result.ToString();
                    }
                }
            }

            return accessLevel;
        }


        public static int InsertProject(ProjectModel project)
        {
            int newProjectId;

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        INSERT INTO Project (
            Title,
            CreatedBy,
            GrantID,
            ProjectType,
            TrackingStatus,
            IsArchived,
            Project_Description
        )
        OUTPUT INSERTED.ProjectID
        VALUES (
            @Title,
            @CreatedBy,
            @GrantID,
            @ProjectType,
            @TrackingStatus,
            @IsArchived,
            @Project_Description
        )";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Title", project.Title);
                    cmd.Parameters.AddWithValue("@CreatedBy", project.CreatedBy);
                    cmd.Parameters.AddWithValue("@GrantID", project.GrantID ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProjectType", project.ProjectType);
                    cmd.Parameters.AddWithValue("@TrackingStatus", project.TrackingStatus ?? "");
                    cmd.Parameters.AddWithValue("@IsArchived", project.IsArchived);
                    cmd.Parameters.AddWithValue("@Project_Description", project.Project_Description ?? "");

                    connection.Open();
                    newProjectId = (int)cmd.ExecuteScalar();
                }
            }

            return newProjectId;
        }

        public static void InsertPhaseProject(int phaseId, int projectId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        INSERT INTO Phase_Project (PhaseID, ProjectID)
        VALUES (@PhaseID, @ProjectID)";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static List<ProjectModel> GetProjectsByPhaseId(int phaseId)
        {
            var projects = new List<ProjectModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
SELECT p.*, pp.PhaseID
FROM Project p
JOIN Phase_Project pp ON p.ProjectID = pp.ProjectID
WHERE pp.PhaseID = @PhaseID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            projects.Add(new ProjectModel
                            {
                                ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                                GrantID = reader.IsDBNull(reader.GetOrdinal("GrantID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GrantID")),
                                PhaseID = reader.GetInt32(reader.GetOrdinal("PhaseID")),
                                ProjectType = reader.GetString(reader.GetOrdinal("ProjectType")),
                                TrackingStatus = reader.GetString(reader.GetOrdinal("TrackingStatus")),
                                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                                Project_Description = reader.GetString(reader.GetOrdinal("Project_Description"))
                            });
                        }
                    }
                }
            }

            return projects ?? new List<ProjectModel>();
        }

        public static List<ProjectModel> GetProjectsByPhaseId(int phaseId, int userId)
        {
            var projects = new List<ProjectModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
                SELECT p.*, pp.PhaseID
                FROM Project p
                JOIN Phase_Project pp ON p.ProjectID = pp.ProjectID
                LEFT JOIN Project_Permission prp ON p.ProjectID = prp.ProjectID
                LEFT JOIN DBUser u ON u.UserID = @UserID
                LEFT JOIN UserRole ur ON u.UserRoleID = ur.UserRoleID
                WHERE pp.PhaseID = @PhaseID
                AND (
                    prp.UserID = @UserID AND prp.AccessLevel IN ('Read', 'Edit')
                    OR ur.RoleName = 'Admin'
                    OR EXISTS (
                        SELECT 1 
                        FROM Grant_Permission gp 
                        WHERE gp.UserID = @UserID AND gp.AccessLevel = 'Edit'
                    )
                )";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            projects.Add(new ProjectModel
                            {
                                ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                                GrantID = reader.IsDBNull(reader.GetOrdinal("GrantID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GrantID")),
                                PhaseID = reader.GetInt32(reader.GetOrdinal("PhaseID")),
                                ProjectType = reader.GetString(reader.GetOrdinal("ProjectType")),
                                TrackingStatus = reader.GetString(reader.GetOrdinal("TrackingStatus")),
                                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                                Project_Description = reader.GetString(reader.GetOrdinal("Project_Description"))
                            });
                        }
                    }
                }
            }

            return projects ?? new List<ProjectModel>();
        }


        public static void InsertProjectTask(ProjectTaskModel task)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            INSERT INTO Project_Task (ProjectID, TaskContent, DueDate, Status)
            VALUES (@ProjectID, @TaskContent, @DueDate, @Status)";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", task.ProjectID);
                    cmd.Parameters.AddWithValue("@TaskContent", task.TaskContent);
                    cmd.Parameters.AddWithValue("@DueDate", task.DueDate);
                    cmd.Parameters.AddWithValue("@Status", task.Status);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<ProjectTaskModel> GetTasksByProjectId(int projectId)
        {
            var tasks = new List<ProjectTaskModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        SELECT TaskID, ProjectID, TaskContent, DueDate, Status
        FROM Project_Task
        WHERE ProjectID = @ProjectID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new ProjectTaskModel
                            {
                                TaskID = reader.GetInt32(reader.GetOrdinal("TaskID")),
                                ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                                TaskContent = reader.GetString(reader.GetOrdinal("TaskContent")),
                                DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                AssignedUsers = new List<DbUserModel>() // Initialize empty list
                            });
                        }
                    }
                }

                // For each task, get the assigned users
                foreach (var task in tasks)
                {
                    string userQuery = @"
            SELECT u.UserID, u.FirstName, u.LastName, u.Email, ptu.Role
            FROM DBUser u
            JOIN Project_Task_User ptu ON u.UserID = ptu.UserID
            WHERE ptu.TaskID = @TaskID";

                    using (SqlCommand userCmd = new SqlCommand(userQuery, connection))
                    {
                        userCmd.Parameters.AddWithValue("@TaskID", task.TaskID);

                        using (SqlDataReader userReader = userCmd.ExecuteReader())
                        {
                            while (userReader.Read())
                            {
                                task.AssignedUsers.Add(new DbUserModel
                                {
                                    UserID = userReader.GetInt32(userReader.GetOrdinal("UserID")),
                                    FirstName = userReader.GetString(userReader.GetOrdinal("FirstName")),
                                    LastName = userReader.GetString(userReader.GetOrdinal("LastName")),
                                    Email = userReader.GetString(userReader.GetOrdinal("Email"))
                                    // Add other properties as needed
                                });
                            }
                        }
                    }
                }
            }

            return tasks ?? new List<ProjectTaskModel>();
        }


        public static List<ProjectTaskModel> GetTasksByProjectId(int projectId, int userId)
        {
            var tasks = new List<ProjectTaskModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
                SELECT t.*
                FROM Project_Task t
                LEFT JOIN Project_Permission pp ON t.ProjectID = pp.ProjectID
                LEFT JOIN DBUser u ON u.UserID = @UserID
                LEFT JOIN UserRole ur ON u.UserRoleID = ur.UserRoleID
                WHERE t.ProjectID = @ProjectID
                AND t.IsArchived = 0  -- Added filter for non-archived tasks
                AND (
                    pp.UserID = @UserID AND pp.AccessLevel IN ('Read', 'Edit')
                    OR ur.RoleName = 'Admin'
                    OR EXISTS (
                        SELECT 1 
                        FROM Grant_Permission gp 
                        WHERE gp.UserID = @UserID AND gp.AccessLevel = 'Edit'
                    )
                )";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new ProjectTaskModel
                            {
                                TaskID = reader.GetInt32(reader.GetOrdinal("TaskID")),
                                ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                                TaskContent = reader.GetString(reader.GetOrdinal("TaskContent")),
                                DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived"))
                            });
                        }
                    }
                }
            }

            return tasks ?? new List<ProjectTaskModel>();
        }

        public static List<ProjectModel> GetProjects()
        {
            var projects = new List<ProjectModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        SELECT ProjectID, Title
        FROM Project
        WHERE IsArchived = 0";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            projects.Add(new ProjectModel
                            {
                                ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                                Title = reader.GetString(reader.GetOrdinal("Title"))
                            });
                        }
                    }
                }
            }

            return projects ?? new List<ProjectModel>();
        }

        public static bool IsGrantEditor(int userId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string query = @"
                SELECT COUNT(*) 
                FROM Grant_Permission 
                WHERE UserID = @UserID AND AccessLevel = 'Edit'";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    connection.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public static bool IsPhaseEditor(int userId, int phaseId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string query = @"
                SELECT COUNT(*) 
                FROM Phase_Permission 
                WHERE UserID = @UserID AND PhaseID = @PhaseID AND AccessLevel = 'Edit'";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    connection.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public static bool CanUserAddProjectToPhase(int userId, int phaseId)
        {
            // Check if user is an admin (admins can add projects to any phase)
            if (IsUserAdmin(userId))
            {
                return true;
            }

            // Check if user is a grant editor (grant editors can add projects to any phase)
            if (IsGrantEditor(userId))
            {
                return true;
            }

            // Check if user has specific edit permissions for this phase
            string accessLevel = GetUserAccessLevelForPhase(userId, phaseId);
            return accessLevel == "Edit";
            {
                return true;
            }

            // If none of the above, user cannot add projects to this phase
            return false;
        }


        public static bool IsProjectEditor(int userId, int projectId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string query = @"
                SELECT COUNT(*) 
                FROM Project_Permission 
                WHERE UserID = @UserID AND ProjectID = @ProjectID AND AccessLevel = 'Edit'";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);
                    connection.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // Add these methods to DBClass.cs

        public static void UpdatePhasePosition(int phaseId, int newPosition)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
            UPDATE Phase SET PhasePosition = @PhasePosition
            WHERE PhaseID = @PhaseID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PhaseID", phaseId);
                    cmd.Parameters.AddWithValue("@PhasePosition", newPosition);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void InitializePhasePositions(int grantId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                // First check if positions are already set
                string checkQuery = @"
            SELECT COUNT(*) 
            FROM Phase p
            JOIN Grant_Phase gp ON p.PhaseID = gp.PhaseID
            WHERE gp.GrantID = @GrantID AND p.PhasePosition IS NULL";

                connection.Open();

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@GrantID", grantId);
                    int nullPositionCount = (int)checkCmd.ExecuteScalar();

                    if (nullPositionCount > 0)
                    {
                        // Positions need to be initialized
                        string updateQuery = @"
                    ;WITH OrderedPhases AS (
                        SELECT p.PhaseID, ROW_NUMBER() OVER (ORDER BY p.PhaseID) AS RowNum
                        FROM Phase p
                        JOIN Grant_Phase gp ON p.PhaseID = gp.PhaseID
                        WHERE gp.GrantID = @GrantID
                    )
                    UPDATE Phase
                    SET PhasePosition = op.RowNum
                    FROM Phase p
                    INNER JOIN OrderedPhases op ON p.PhaseID = op.PhaseID";

                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@GrantID", grantId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public static void UpdatePhaseStatusesForGrant(int grantId)
        {
            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                connection.Open();

                // Get all phases for the grant, ordered by position
                string selectQuery = @"
            SELECT p.PhaseID, p.PhasePosition, p.Status
            FROM Phase p
            JOIN Grant_Phase gp ON p.PhaseID = gp.PhaseID
            WHERE gp.GrantID = @GrantID
            ORDER BY p.PhasePosition";

                var phases = new List<(int PhaseId, int Position, string Status)>();

                using (SqlCommand cmd = new SqlCommand(selectQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantID", grantId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            phases.Add((
                                reader.GetInt32(reader.GetOrdinal("PhaseID")),
                                reader.GetInt32(reader.GetOrdinal("PhasePosition")),
                                reader.GetString(reader.GetOrdinal("Status"))
                            ));
                        }
                    }
                }

                if (phases.Count == 0) return;

                // First phase is always "In Progress" if not "Completed"
                var firstPhase = phases.OrderBy(p => p.Position).First();
                if (firstPhase.Status != "Completed")
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Phase SET Status = 'In Progress' WHERE PhaseID = @PhaseID",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@PhaseID", firstPhase.PhaseId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Set all phases after a completed phase to "Pending" if they aren't already "In Progress" or "Completed"
                bool previousPhaseCompleted = false;

                foreach (var phase in phases.OrderBy(p => p.Position))
                {
                    if (previousPhaseCompleted && phase.Status != "In Progress" && phase.Status != "Completed")
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE Phase SET Status = 'Pending' WHERE PhaseID = @PhaseID",
                            connection))
                        {
                            cmd.Parameters.AddWithValue("@PhaseID", phase.PhaseId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    previousPhaseCompleted = phase.Status == "Completed";
                }

                // Set phases that come after an "In Progress" phase to "Not Started" if they are not already in a specific state
                bool previousPhaseInProgress = false;

                foreach (var phase in phases.OrderBy(p => p.Position))
                {
                    if (previousPhaseInProgress && phase.Status != "In Progress" && phase.Status != "Completed" && phase.Status != "Pending")
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE Phase SET Status = 'Not Started' WHERE PhaseID = @PhaseID",
                            connection))
                        {
                            cmd.Parameters.AddWithValue("@PhaseID", phase.PhaseId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    previousPhaseInProgress = phase.Status == "In Progress";
                }
            }
        }

        // Replace the existing GetPhasesByGrantId method with this one
        public static List<PhaseModel> GetPhasesByGrantId(int grantId)
        {
            var phases = new List<PhaseModel>();

            using (SqlConnection connection = new SqlConnection(JMUcareDBConnString))
            {
                string sqlQuery = @"
        SELECT p.*
        FROM Phase p
        JOIN Grant_Phase gp ON p.PhaseID = gp.PhaseID
        WHERE gp.GrantID = @GrantID
        ORDER BY p.PhasePosition, p.PhaseID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@GrantID", grantId);
                    connection.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            phases.Add(new PhaseModel
                            {
                                PhaseID = reader.GetInt32(reader.GetOrdinal("PhaseID")),
                                PhaseName = reader.GetString(reader.GetOrdinal("PhaseName")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                                PhaseLeadID = reader.GetInt32(reader.GetOrdinal("PhaseLeadID")),
                                PhasePosition = reader.IsDBNull(reader.GetOrdinal("PhasePosition")) ? 0 : reader.GetInt32(reader.GetOrdinal("PhasePosition")),
                                GrantID = grantId,
 
                                IsArchived = reader.IsDBNull(reader.GetOrdinal("IsArchived")) ? false : reader.GetBoolean(reader.GetOrdinal("IsArchived"))
                            });
                        }
                    }
                }
            }

            return phases ?? new List<PhaseModel>();
        }




        public static ProjectModel GetProjectById(int projectId)
        {
            try
            {
                using var connection = new System.Data.SqlClient.SqlConnection(JMUcareDBConnString);
                var query = @"
            SELECT p.*, pp.PhaseID 
            FROM Project p
            LEFT JOIN Phase_Project pp ON p.ProjectID = pp.ProjectID
            WHERE p.ProjectID = @ProjectID";

                using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ProjectID", projectId);

                connection.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new ProjectModel
                    {
                        ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),
                        GrantID = reader.IsDBNull(reader.GetOrdinal("GrantID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("GrantID")),
                        PhaseID = reader.IsDBNull(reader.GetOrdinal("PhaseID")) ? 0 : reader.GetInt32(reader.GetOrdinal("PhaseID")),
                        ProjectType = reader.GetString(reader.GetOrdinal("ProjectType")),
                        TrackingStatus = reader.GetString(reader.GetOrdinal("TrackingStatus")),
                        IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                        Project_Description = reader.GetString(reader.GetOrdinal("Project_Description"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Database error in GetProjectById: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// Gets all user permissions for a specific project
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <returns>List of users with their permission levels for the project</returns>
        public static List<(DbUserModel User, string AccessLevel)> GetProjectUserPermissions(int projectId)
        {
            var permissions = new List<(DbUserModel User, string AccessLevel)>();

            using var connection = new System.Data.SqlClient.SqlConnection(JMUcareDBConnString);
            var query = @"
        SELECT u.UserID, u.FirstName, u.LastName, u.Email, u.UserRoleID, pp.AccessLevel
        FROM [DBUser] u
        INNER JOIN Project_Permission pp ON u.UserID = pp.UserID
        WHERE pp.ProjectID = @ProjectID AND pp.AccessLevel != 'None'
        ORDER BY u.LastName, u.FirstName";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ProjectID", projectId);

            connection.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var user = new DbUserModel
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    UserRoleID = reader.GetInt32(reader.GetOrdinal("UserRoleID"))
                };

                string accessLevel = reader.GetString(reader.GetOrdinal("AccessLevel"));
                permissions.Add((user, accessLevel));
            }

            return permissions;
        }
        /// <summary>
        /// Adds or updates a user's permission for a specific project
        /// </summary>
        /// <param name="projectId">The ID of the project</param>
        /// <param name="userId">The ID of the user</param>
        /// <param name="accessLevel">The access level (Edit, Read, or None)</param>
        /// <returns>True if operation was successful</returns>
        public static bool InsertProjectPermission(int projectId, int userId, string accessLevel)
        {
            try
            {
                using var connection = new System.Data.SqlClient.SqlConnection(JMUcareDBConnString);

                // First check if the permission already exists
                var checkQuery = "SELECT COUNT(*) FROM Project_Permission WHERE ProjectID = @ProjectID AND UserID = @UserID";
                using var checkCmd = new System.Data.SqlClient.SqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("@ProjectID", projectId);
                checkCmd.Parameters.AddWithValue("@UserID", userId);

                connection.Open();
                int existingCount = (int)checkCmd.ExecuteScalar();

                string query;
                if (existingCount > 0)
                {
                    // Update existing permission
                    if (accessLevel == "None")
                    {
                        // If setting to None, delete the record
                        query = "DELETE FROM Project_Permission WHERE ProjectID = @ProjectID AND UserID = @UserID";
                    }
                    else
                    {
                        // Otherwise update the access level
                        query = "UPDATE Project_Permission SET AccessLevel = @AccessLevel WHERE ProjectID = @ProjectID AND UserID = @UserID";
                    }
                }
                else
                {
                    // Insert new permission (only if not None)
                    if (accessLevel == "None")
                    {
                        return true; // Nothing to do if setting a non-existent permission to None
                    }

                    query = "INSERT INTO Project_Permission (ProjectID, UserID, AccessLevel) VALUES (@ProjectID, @UserID, @AccessLevel)";
                }

                using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ProjectID", projectId);
                cmd.Parameters.AddWithValue("@UserID", userId);

                if (accessLevel != "None")
                {
                    cmd.Parameters.AddWithValue("@AccessLevel", accessLevel);
                }

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        public static bool DeleteTaskAndUsers(int taskId)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(JMUcareDBConnString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // First delete the task user assignments
                var deleteTaskUsersQuery = "DELETE FROM Project_Task_User WHERE TaskID = @TaskID";
                using var deleteTaskUsersCmd = new System.Data.SqlClient.SqlCommand(deleteTaskUsersQuery, connection, transaction);
                deleteTaskUsersCmd.Parameters.AddWithValue("@TaskID", taskId);
                deleteTaskUsersCmd.ExecuteNonQuery();

                // Then delete the task itself
                var deleteTaskQuery = "DELETE FROM Project_Task WHERE TaskID = @TaskID";
                using var deleteTaskCmd = new System.Data.SqlClient.SqlCommand(deleteTaskQuery, connection, transaction);
                deleteTaskCmd.Parameters.AddWithValue("@TaskID", taskId);
                deleteTaskCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public static ProjectTaskModel GetTaskById(int taskId)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = @"
        SELECT * FROM Project_Task
        WHERE TaskID = @TaskID";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);

            connection.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new ProjectTaskModel
                {
                    TaskID = reader.GetInt32(reader.GetOrdinal("TaskID")),
                    ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                    TaskContent = reader.GetString(reader.GetOrdinal("TaskContent")),
                    DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                };
            }

            return null;
        }

        public static void UpdateTask(ProjectTaskModel task)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = @"
        UPDATE Project_Task
        SET TaskContent = @TaskContent,
            DueDate = @DueDate,
            Status = @Status
        WHERE TaskID = @TaskID";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", task.TaskID);
            cmd.Parameters.AddWithValue("@TaskContent", task.TaskContent);
            cmd.Parameters.AddWithValue("@DueDate", task.DueDate);
            cmd.Parameters.AddWithValue("@Status", task.Status);

            connection.Open();
            cmd.ExecuteNonQuery();
        }

        public static void LoadTaskAssignments(int taskId, List<TaskAssignmentViewModel> taskAssignments)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = @"
        SELECT u.UserID, u.FirstName, u.LastName, ptu.Role
        FROM DBUser u
        JOIN Project_Task_User ptu ON u.UserID = ptu.UserID
        WHERE ptu.TaskID = @TaskID";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);

            connection.Open();
            using var reader = cmd.ExecuteReader();

            taskAssignments.Clear();
            while (reader.Read())
            {
                taskAssignments.Add(new TaskAssignmentViewModel
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    AccessLevel = reader.GetString(reader.GetOrdinal("Role"))
                });
            }
        }

        public static void LoadAvailableUsers(int taskId, List<DbUserModel> availableUsers)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = @"
        SELECT u.UserID, u.FirstName, u.LastName, u.Email
        FROM DBUser u
        WHERE u.IsArchived = 0
        AND u.UserID NOT IN (
            SELECT ptu.UserID
            FROM Project_Task_User ptu
            WHERE ptu.TaskID = @TaskID
        )";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);

            connection.Open();
            using var reader = cmd.ExecuteReader();

            availableUsers.Clear();
            while (reader.Read())
            {
                availableUsers.Add(new DbUserModel
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                });
            }
        }

        public static void AddUserToTask(ProjectTaskUserModel taskUser)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = @"
        INSERT INTO Project_Task_User (TaskID, UserID, Role)
        VALUES (@TaskID, @UserID, @Role)";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskUser.TaskID);
            cmd.Parameters.AddWithValue("@UserID", taskUser.UserID);
            cmd.Parameters.AddWithValue("@Role", taskUser.Role);

            connection.Open();
            cmd.ExecuteNonQuery();
        }

        public static void RemoveUserFromTask(int taskId, int userId)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = @"
        DELETE FROM Project_Task_User
        WHERE TaskID = @TaskID AND UserID = @UserID";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);
            cmd.Parameters.AddWithValue("@UserID", userId);

            connection.Open();
            cmd.ExecuteNonQuery();
        }
        public static int AddTask(ProjectTaskModel task)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(JMUcareDBConnString);
            var query = @"
        INSERT INTO Project_Task (ProjectID, TaskContent, DueDate, Status)
        OUTPUT INSERTED.TaskID
        VALUES (@ProjectID, @TaskContent, @DueDate, @Status)";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ProjectID", task.ProjectID);
            cmd.Parameters.AddWithValue("@TaskContent", task.TaskContent);
            cmd.Parameters.AddWithValue("@DueDate", task.DueDate);
            cmd.Parameters.AddWithValue("@Status", task.Status);

            connection.Open();
            int taskId = (int)cmd.ExecuteScalar();
            return taskId;
        }
        public static int? GetGrantIdByPhaseId(int phaseId)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(JMUcareDBConnString);
            var query = "SELECT GrantID FROM Grant_Phase WHERE PhaseID = @PhaseID";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@PhaseID", phaseId);

            connection.Open();
            var result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return (int)result;
            }

            return null;
        }
        public static void UpdateProject(ProjectModel project)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(JMUcareDBConnString);
            var query = @"
        UPDATE Project SET
            Title = @Title,
            Project_Description = @Project_Description,
            TrackingStatus = @TrackingStatus,
            ProjectType = @ProjectType
        WHERE ProjectID = @ProjectID";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ProjectID", project.ProjectID);
            cmd.Parameters.AddWithValue("@Title", project.Title);
            cmd.Parameters.AddWithValue("@Project_Description", project.Project_Description ?? "");
            cmd.Parameters.AddWithValue("@TrackingStatus", project.TrackingStatus);
            cmd.Parameters.AddWithValue("@ProjectType", project.ProjectType);

            connection.Open();
            cmd.ExecuteNonQuery();
        }
        public static bool DeleteTask(int taskId)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = "DELETE FROM Project_Task WHERE TaskID = @TaskID";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);

            connection.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }
        public static bool ArchiveProjectAndTasks(int projectId)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Archive tasks associated with the project
                var archiveTasksQuery = "UPDATE Project_Task SET IsArchived = 1 WHERE ProjectID = @ProjectID";
                using var archiveTasksCmd = new SqlCommand(archiveTasksQuery, connection, transaction);
                archiveTasksCmd.Parameters.AddWithValue("@ProjectID", projectId);
                archiveTasksCmd.ExecuteNonQuery();

                // Archive the project
                var archiveProjectQuery = "UPDATE Project SET IsArchived = 1 WHERE ProjectID = @ProjectID";
                using var archiveProjectCmd = new SqlCommand(archiveProjectQuery, connection, transaction);
                archiveProjectCmd.Parameters.AddWithValue("@ProjectID", projectId);
                archiveProjectCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                // Log the exception (ex) for debugging purposes
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public static bool ArchiveTask(int taskId)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            var query = "UPDATE Project_Task SET IsArchived = 1 WHERE TaskID = @TaskID";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);

            connection.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }
        public static bool ArchivePhase(int phaseId)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // First check if the phase exists and get its current state
                var checkQuery = "SELECT IsArchived FROM Phase WHERE PhaseID = @PhaseID";
                using var checkCmd = new SqlCommand(checkQuery, connection, transaction);
                checkCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                object currentValue = checkCmd.ExecuteScalar();

                if (currentValue == null)
                {
                    Console.WriteLine($"Phase with ID {phaseId} doesn't exist");
                    transaction.Rollback();
                    return false;
                }

                Console.WriteLine($"Current IsArchived value for phase {phaseId}: {currentValue}");

                // Archive tasks associated with projects in the phase
                var archiveTasksQuery = @"
            UPDATE Project_Task 
            SET IsArchived = 1 
            WHERE ProjectID IN (
                SELECT ProjectID 
                FROM Phase_Project 
                WHERE PhaseID = @PhaseID
            )";
                using var archiveTasksCmd = new SqlCommand(archiveTasksQuery, connection, transaction);
                archiveTasksCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                int tasksAffected = archiveTasksCmd.ExecuteNonQuery();
                Console.WriteLine($"Tasks archived: {tasksAffected}");

                // Archive projects associated with the phase
                var archiveProjectsQuery = @"
            UPDATE Project 
            SET IsArchived = 1 
            WHERE ProjectID IN (
                SELECT ProjectID 
                FROM Phase_Project 
                WHERE PhaseID = @PhaseID
            )";
                using var archiveProjectsCmd = new SqlCommand(archiveProjectsQuery, connection, transaction);
                archiveProjectsCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                int projectsAffected = archiveProjectsCmd.ExecuteNonQuery();
                Console.WriteLine($"Projects archived: {projectsAffected}");

                // Archive the phase
                var archivePhaseQuery = "UPDATE Phase SET IsArchived = 1 WHERE PhaseID = @PhaseID";
                using var archivePhaseCmd = new SqlCommand(archivePhaseQuery, connection, transaction);
                archivePhaseCmd.Parameters.AddWithValue("@PhaseID", phaseId);
                int phaseAffected = archivePhaseCmd.ExecuteNonQuery();
                Console.WriteLine($"Phase affected rows: {phaseAffected}");

                if (phaseAffected == 0)
                {
                    throw new Exception($"Failed to update IsArchived for phase {phaseId}");
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Exception in ArchivePhase: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        public static bool ArchiveGrant(int grantId)
        {
            using var connection = new SqlConnection(JMUcareDBConnString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Get all phases associated with this grant
                var phasesQuery = @"
            SELECT PhaseID 
            FROM Grant_Phase 
            WHERE GrantID = @GrantID";

                List<int> phaseIds = new List<int>();

                using (var phasesCmd = new SqlCommand(phasesQuery, connection, transaction))
                {
                    phasesCmd.Parameters.AddWithValue("@GrantID", grantId);

                    using var reader = phasesCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        phaseIds.Add(reader.GetInt32(0));
                    }
                }

                // Archive all tasks associated with projects in all phases
                var archiveTasksQuery = @"
            UPDATE Project_Task 
            SET IsArchived = 1 
            WHERE ProjectID IN (
                SELECT p.ProjectID 
                FROM Project p
                JOIN Phase_Project pp ON p.ProjectID = pp.ProjectID
                WHERE pp.PhaseID IN (
                    SELECT PhaseID 
                    FROM Grant_Phase 
                    WHERE GrantID = @GrantID
                )
            )";

                using (var archiveTasksCmd = new SqlCommand(archiveTasksQuery, connection, transaction))
                {
                    archiveTasksCmd.Parameters.AddWithValue("@GrantID", grantId);
                    archiveTasksCmd.ExecuteNonQuery();
                }

                // Archive projects associated with all phases
                var archiveProjectsQuery = @"
            UPDATE Project 
            SET IsArchived = 1 
            WHERE ProjectID IN (
                SELECT pp.ProjectID 
                FROM Phase_Project pp
                JOIN Grant_Phase gp ON pp.PhaseID = gp.PhaseID
                WHERE gp.GrantID = @GrantID
            )";

                using (var archiveProjectsCmd = new SqlCommand(archiveProjectsQuery, connection, transaction))
                {
                    archiveProjectsCmd.Parameters.AddWithValue("@GrantID", grantId);
                    archiveProjectsCmd.ExecuteNonQuery();
                }

                // Archive all phases associated with the grant
                var archivePhaseQuery = @"
            UPDATE Phase 
            SET IsArchived = 1 
            WHERE PhaseID IN (
                SELECT PhaseID 
                FROM Grant_Phase 
                WHERE GrantID = @GrantID
            )";

                using (var archivePhaseCmd = new SqlCommand(archivePhaseQuery, connection, transaction))
                {
                    archivePhaseCmd.Parameters.AddWithValue("@GrantID", grantId);
                    archivePhaseCmd.ExecuteNonQuery();
                }

                // Finally, archive the grant itself
                var archiveGrantQuery = "UPDATE Grants SET IsArchived = 1 WHERE GrantID = @GrantID";
                using (var archiveGrantCmd = new SqlCommand(archiveGrantQuery, connection, transaction))
                {
                    archiveGrantCmd.Parameters.AddWithValue("@GrantID", grantId);
                    int rowsAffected = archiveGrantCmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        // Grant not found or already archived
                        transaction.Rollback();
                        return false;
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error archiving grant: {ex.Message}");
                return false;
            }
        }












    }
}

