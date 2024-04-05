using Microsoft.Data.SqlClient;

namespace pesada_hacienda.Core
{
    public class SqlClient
    {
        private const string CONNECTION_STRING = "##-<SQL CONNECTION STRING>-##";

        public void Test()
        {
            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                conn.Open();
                SqlCommand cmd = new("SELECT * FROM pesada.HistorialPesos", conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(String.Format("{0} \t | {1} \t | {2} \t | {3} \t | {4}",
                            reader[0], reader[1], reader[2], reader[3], reader[4]));
                    }
                }
                conn.Close();
            }
        }

        public async Task Post(string frameName, int kilos, int tropa)
        {
            using (SqlConnection conn = new(CONNECTION_STRING))
            {
                SqlCommand cmd = new("INSERT INTO pesada.HistorialPesos VALUES (@kilos, @tropa, @imagen, GETDATE())", conn);
                cmd.Parameters.AddWithValue("kilos", kilos);
                cmd.Parameters.AddWithValue("tropa", tropa);
                cmd.Parameters.AddWithValue("imagen", frameName);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                await conn.CloseAsync();
            }
        }
    }
}
