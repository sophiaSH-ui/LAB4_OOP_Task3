using System;
using Npgsql;
using System.Collections.ObjectModel;

namespace lab4_task3.DTO
{
    class DB
    {
        string connectionString = "Host=78.137.52.42;Username=socksof;Password=вуафгде;Database=properties";

        public ObservableCollection<Property> GetProperties(Locality locality = null)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var owners = new Dictionary<int, Owner>();

            using var command = new NpgsqlCommand();
            command.Connection = connection;

            if (locality is null)
            {
                command.CommandText = "SELECT id, name, surname, birthdate FROM properties;";
            }
            else
            {
                command.CommandText = "SELECT id, name, surname, birthdate FROM properties WHERE locality = @localityId;";

                command.Parameters.AddWithValue("localityId", locality.ID);
            }

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int ownerID = reader.GetInt32(reader.GetOrdinal("id"));

                var owner = new Owner(
                    reader.GetString(reader.GetOrdinal("name")),
                    reader.GetString(reader.GetOrdinal("surname")),
                    reader.GetDateTime(reader.GetOrdinal("birthdate"))
                );

                owners[ownerID] = owner;
            }
        }

        public long GetPropertiesCount(Locality locality = null)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var command = new NpgsqlCommand();
            command.Connection = connection;

            if (locality is null)
            {
                command.CommandText = "SELECT COUNT(*) FROM properties;";
            }
            else
            {
                command.CommandText = "SELECT COUNT(*) FROM properties WHERE locality = @localityId;";

                command.Parameters.AddWithValue("localityId", locality.ID);
            }

            long count = (long)command.ExecuteScalar();

            return count;
        }
    }

    class Owner
    {

    }

    class Property
    {

    }

    class Description
    {

    }

    class Locality
    {

    }
}