using System.Data;
using System.Data.SqlClient;
using Seaboard.Intranet.Domain;

namespace Seaboard.Intranet.Data
{
    public class ConnectionDb
    {
        static SesionSql _ins = null;
        public static SqlConnection GetCon()
        {
            if (_ins == null)
                _ins = new SesionSql(new SqlConnection(Helpers.ConnectionStrings));
            return _ins.SqlCon;
        }
        public static DataTable GetDt(string sql)
        {
            SqlDataAdapter da = new SqlDataAdapter(sql, GetCon());
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
    }

    public class SesionSql
    {
        public SqlConnection SqlCon = null;
        public SesionSql(SqlConnection con)
        {
            con.Open();
            SqlCon = con;
        }
        ~SesionSql()
        {
            ConnectionDb.GetCon().Close();
        }
    }
}