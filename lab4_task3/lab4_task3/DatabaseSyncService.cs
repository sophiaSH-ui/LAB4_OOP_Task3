using Npgsql;
using System.Text.Json;
using lab4_task3.DTO;
using System.Windows; 
using System;

namespace lab4_task3
{
    public static class DatabaseSyncService
    {
        private static string connString = "Host=78.137.52.42;Username=socksof;Password=вуафгде;Database=properties";

        public static void UpdateInDatabase(int propertyId, Plot plot)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    var cmdGet = new NpgsqlCommand(
                        "SELECT locality, description FROM properties WHERE id = @id", conn);
                    cmdGet.Parameters.AddWithValue("id", propertyId);

                    int locId, descId;
                    using (var reader = cmdGet.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            MessageBox.Show("Запис не знайдено в БД.");
                            return;
                        }
                        locId = reader.GetInt32(0);
                        descId = reader.GetInt32(1);
                    }
                    var cmdLoc = new NpgsqlCommand(
                        "UPDATE localities SET title = @t WHERE id = @id", conn);
                    cmdLoc.Parameters.AddWithValue("t", plot.Location);
                    cmdLoc.Parameters.AddWithValue("id", locId);
                    cmdLoc.ExecuteNonQuery();

                    var cmdDesc = new NpgsqlCommand(
                        "UPDATE descriptions SET water = @w, soil = @s, coordinates = @c::json WHERE id = @id", conn);
                    cmdDesc.Parameters.AddWithValue("w", (int)plot.GroundWater);
                    cmdDesc.Parameters.AddWithValue("s", plot.SoilType);
                    cmdDesc.Parameters.AddWithValue("c", JsonSerializer.Serialize(plot.Coordinates));
                    cmdDesc.Parameters.AddWithValue("id", descId);
                    cmdDesc.ExecuteNonQuery();

                    var cmdProp = new NpgsqlCommand(
                        "UPDATE properties SET owner = @o, usage = @u, price = @p::money WHERE id = @id", conn);
                    cmdProp.Parameters.AddWithValue("o", plot.OwnerId);
                    cmdProp.Parameters.AddWithValue("u", plot.Purpose);
                    cmdProp.Parameters.AddWithValue("p", (decimal)plot.MarketValue);
                    cmdProp.Parameters.AddWithValue("id", propertyId);
                    cmdProp.ExecuteNonQuery();

                    Console.WriteLine("Запис успішно оновлено!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }
        public static void PushToDatabase(Plot plot)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    //Locality
                    var cmdLoc = new NpgsqlCommand("INSERT INTO localities (title) VALUES (@t) RETURNING id", conn);
                    cmdLoc.Parameters.AddWithValue("t", plot.Location);
                    int locId = (int)cmdLoc.ExecuteScalar();

                    //Description
                    var cmdDesc = new NpgsqlCommand("INSERT INTO descriptions (water, soil, coordinates) VALUES (@w, @s, @c::json) RETURNING id", conn);
                    cmdDesc.Parameters.AddWithValue("w", (int)plot.GroundWater);
                    cmdDesc.Parameters.AddWithValue("s", plot.SoilType);
                    cmdDesc.Parameters.AddWithValue("c", JsonSerializer.Serialize(plot.Coordinates));
                    int descId = (int)cmdDesc.ExecuteScalar();

                    //Properties
                    var cmdProp = new NpgsqlCommand("INSERT INTO properties (owner, locality, description, usage, price) VALUES (@o, @l, @d, @u, @p::money)", conn);
                    cmdProp.Parameters.AddWithValue("o", plot.OwnerId);
                    cmdProp.Parameters.AddWithValue("l", locId);
                    cmdProp.Parameters.AddWithValue("d", descId);
                    cmdProp.Parameters.AddWithValue("u", plot.Purpose);
                    cmdProp.Parameters.AddWithValue("p", (decimal)plot.MarketValue);

                    int affectedRows = cmdProp.ExecuteNonQuery();

                    Console.WriteLine("Запис успішно додано!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка синхронізації: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}