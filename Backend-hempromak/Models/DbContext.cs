using Microsoft.AspNetCore.Hosting.Server;
using MySql.Data.MySqlClient;

namespace Backend_hempromak.Models
{
    public class DbContext
    {
        public DbContext() {}

        public List<Dictionary<string, object>> executeSqlQuery (string query)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection("Server=localhost;Database=hempromak;Uid=root;Pwd="))
                {
                    con.Open();

                    MySqlCommand execute_query = new MySqlCommand(query, con);
                    MySqlDataReader reader = execute_query.ExecuteReader();

                    if (execute_query.LastInsertedId > 0)
                    {
                        var result = new Dictionary<string, object>{ { "id", execute_query.LastInsertedId } };

                        return new List<Dictionary<string, object>> { result };
                    }

                    var results = new List<Dictionary<string, object>>();
                    var row_number = 0;

                    while (reader.Read())
                    {
                        row_number++;
                        var row = new Dictionary<string, object>();

                        row["key"] = row_number;

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }

                        results.Add(row);
                    }
                  
                    return results;

                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


        }

    }
}
