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

        // ... rest of your code ...
    }

}
