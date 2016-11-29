using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Tetrapak.ToCommon
{
    // tocommon
    public class SqlConnectionString
    {
        private readonly SqlConnectionStringBuilder _csb;

        public string Server => DataSource;

        public string DataSource => _csb.DataSource;

        public string Database => InitialCatalog;

        public string InitialCatalog => _csb.InitialCatalog;

        #region .  Casting  .
        public static implicit operator string(SqlConnectionString sqlConnectionString)
        {
            return sqlConnectionString._csb.ToString();
        }

        public static explicit operator SqlConnectionString(string connectionString)
        {
            return new SqlConnectionString(connectionString);
        }
        #endregion

        public static SqlConnectionString Configured(string configKey)
        {
#if DEBUG
            if (string.IsNullOrEmpty(configKey)) throw new ArgumentNullException(nameof(configKey));
#endif
            return new SqlConnectionString(ConfigurationManager.ConnectionStrings[configKey].ConnectionString);
        }

        private SqlConnectionString(string connectionString)
        {
#if DEBUG
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
#endif
            _csb = new SqlConnectionStringBuilder(connectionString);
        }
    }
}
