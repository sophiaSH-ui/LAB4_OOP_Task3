using Npgsql;
using System.Text.Json;
using lab4_task3.DTO;
using System.Windows;
using System;

namespace lab4_task3
{
    public static class DatabaseSyncService
    {
        private static readonly string connString = "Host=78.137.52.42;Username=socksof;Password=вуафгде;Database=properties";

        private static void ExecuteDbAction(Action<NpgsqlConnection> dbAction, string errorContext)
        {
            try
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                using var transaction = conn.BeginTransaction();
                try
                {
                    dbAction(conn);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка {errorContext}: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }

        private static NpgsqlCommand CreateCommand(NpgsqlConnection conn, string sql, Action<NpgsqlParameterCollection> setupParameters = null)
        {
            var cmd = new NpgsqlCommand(sql, conn);
            setupParameters?.Invoke(cmd.Parameters);
            return cmd;
        }

        public static void UpdateInDatabase(int propertyId, Plot plot)
        {
            ExecuteDbAction(conn =>
            {
                int locId, descId;

                using (var cmdGet = CreateCommand(conn, "SELECT locality, description FROM properties WHERE id = @id",
                    p => p.AddWithValue("id", propertyId)))
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

                using (var cmdLoc = CreateCommand(conn, "UPDATE localities SET title = @t WHERE id = @id", p => {
                    p.AddWithValue("t", plot.Location);
                    p.AddWithValue("id", locId);
                })) { cmdLoc.ExecuteNonQuery(); }

                using (var cmdDesc = CreateCommand(conn, "UPDATE descriptions SET water = @w, soil = @s, coordinates = @c::json WHERE id = @id", p => {
                    p.AddWithValue("w", (int)plot.GroundWater);
                    p.AddWithValue("s", plot.SoilType);
                    p.AddWithValue("c", JsonSerializer.Serialize(plot.Coordinates));
                    p.AddWithValue("id", descId);
                })) { cmdDesc.ExecuteNonQuery(); }

                using (var cmdProp = CreateCommand(conn, "UPDATE properties SET owner = @o, usage = @u, price = @p::money WHERE id = @id", p => {
                    p.AddWithValue("o", plot.OwnerId);
                    p.AddWithValue("u", plot.Purpose);
                    p.AddWithValue("p", (decimal)plot.MarketValue);
                    p.AddWithValue("id", propertyId);
                })) { cmdProp.ExecuteNonQuery(); }

                Console.WriteLine("Запис успішно оновлено!");

            }, "оновлення");
        }

        public static void PushToDatabase(Plot plot)
        {
            ExecuteDbAction(conn =>
            {
                using var cmdLoc = CreateCommand(conn, "INSERT INTO localities (title) VALUES (@t) RETURNING id",
                    p => p.AddWithValue("t", plot.Location));
                int locId = (int)cmdLoc.ExecuteScalar();

                using var cmdDesc = CreateCommand(conn, "INSERT INTO descriptions (water, soil, coordinates) VALUES (@w, @s, @c::json) RETURNING id", p => {
                    p.AddWithValue("w", (int)plot.GroundWater);
                    p.AddWithValue("s", plot.SoilType);
                    p.AddWithValue("c", JsonSerializer.Serialize(plot.Coordinates));
                });
                int descId = (int)cmdDesc.ExecuteScalar();

                using var cmdProp = CreateCommand(conn, "INSERT INTO properties (owner, locality, description, usage, price) VALUES (@o, @l, @d, @u, @p::money)", p => {
                    p.AddWithValue("o", plot.OwnerId);
                    p.AddWithValue("l", locId);
                    p.AddWithValue("d", descId);
                    p.AddWithValue("u", plot.Purpose);
                    p.AddWithValue("p", (decimal)plot.MarketValue);
                });
                cmdProp.ExecuteNonQuery();

                Console.WriteLine("Запис успішно додано!");

            }, "синхронізації");
        }
    }
}