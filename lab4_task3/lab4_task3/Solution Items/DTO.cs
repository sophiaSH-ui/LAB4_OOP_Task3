using System;
using Npgsql;
using System.Collections.ObjectModel;

namespace lab4_task3.DTO
{
    class DB
    {
        public static string connectionString = "Host=78.137.52.42;Username=socksof;Password=вуафгде;Database=properties";

        public ObservableCollection<Property> GetProperties(Locality locality = null)
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            var owners = new Dictionary<int, Owner>();

            ObservableCollection<Property> properties = new ObservableCollection<Property>();

            using var command = new NpgsqlCommand();
            command.Connection = connection;

            var localities = new Dictionary<int, Locality>();

            if (locality is null)
            {
                command.CommandText = "SELECT id, title FROM localities;";
                using var localityReader = command.ExecuteReader();

                while (localityReader.Read())
                {
                    int localityID = localityReader.GetInt32(localityReader.GetOrdinal("id"));
                    string title = localityReader.GetString(localityReader.GetOrdinal("title"));
                    localities[localityID] = new Locality(title);
                }

                command.CommandText = "SELECT id, owner, description, usage, price, locality FROM properties;";
            }
            else
            {
                command.CommandText = "SELECT id, owner, description, usage, price, locality FROM properties WHERE locality = @localityId;";

                command.Parameters.AddWithValue("localityId", locality.ID);

                localities[locality.ID] = locality;
            }

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int propertyID = reader.GetInt32(reader.GetOrdinal("id"));
                int ownerID = reader.GetInt32(reader.GetOrdinal("owner"));
                int descriptionID = reader.GetInt32(reader.GetOrdinal("description"));
                string usage = reader.GetString(reader.GetOrdinal("usage"));
                double price = reader.GetDouble(reader.GetOrdinal("price"));

                int localityID = reader.GetInt32(reader.GetOrdinal("locality"));

                command.CommandText = "SELECT first_name, last_name, birth_date FROM users WHERE id = @ownerId;";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("ownerId", ownerID);

                using var ownerReader = command.ExecuteReader();

                if (ownerReader.Read())
                {
                    string firstName = ownerReader.GetString(ownerReader.GetOrdinal("first_name"));
                    string lastName = ownerReader.GetString(ownerReader.GetOrdinal("last_name"));
                    DateTime birthDate = ownerReader.GetDateTime(ownerReader.GetOrdinal("birth_date"));
                    if (!owners.ContainsKey(ownerID))
                    {
                        owners[ownerID] = new Owner(firstName, lastName, birthDate);
                    }
                }

                command.CommandText = "SELECT water, soil, coordinates FROM descriptions WHERE id = @descriptionId;";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("descriptionId", descriptionID);

                using var descriptionReader = command.ExecuteReader();

                int water = descriptionReader.GetInt32(descriptionReader.GetOrdinal("water"));
                string soil = descriptionReader.GetString(descriptionReader.GetOrdinal("soil"));
                var coordinates = descriptionReader.GetFieldValue<List<List<int>>>(descriptionReader.GetOrdinal("coordinates"));

                var description = new Description(water, soil, coordinates);

                var property = new Property(owners[ownerID], description, localities[localityID], usage, price);

                properties.Add(property);
            }

            return properties;
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
<<<<<<< HEAD

=======
        private string firstName;
        private string lastName;
        private DateTime birthDate;
        private int id;

        public string FirstName
        {
            get
            {
                return firstName;
            }
            set
            {
                if (value is null)
                {
                    throw new ArgumentException("First name can not be null");
                }

                firstName = value;
            }
        }

        public string LastName
        {
            get
            {
                return lastName;
            }
            set
            {
                if (value is null)
                {
                    throw new ArgumentException("Last name can not be null");
                }

                lastName = value;
            }
        }

        public DateTime BirthDate
        {
            get
            {
                return birthDate;
            }
            set
            {
                if (value > DateTime.Now)
                {
                    throw new ArgumentException("Birthdays can not be in future");
                }
            }
        }

        public int ID
        {
            get
            {
                return id;
            }
        }

        public Owner(string fn, string ln, DateTime bd)
        {
            FirstName = fn;
            LastName = ln;
            BirthDate = bd;

            using var connection = new NpgsqlConnection(DB.connectionString);
            connection.Open();

            string sql = "INSERT INTO users (first_name, last_name, birth_date) VALUES (@fn, @ln, @bd) RETURNING id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("fn", FirstName);
            command.Parameters.AddWithValue("ln", LastName);
            command.Parameters.AddWithValue("bd", BirthDate);

            id = (int)command.ExecuteScalar();
        }
>>>>>>> origin/artiktheonlyone
    }

    class Property
    {
        private Owner owner;
        private Description description;
        private Locality locality;
        private string usage;
        private double price;
        private int id;

        public Property(Owner owner, Description description, Locality locality, string usage, double price)
        {
            this.owner = owner;
            this.description = description;
            this.locality = locality;
            this.usage = usage;
            this.price = price;

            using var connection = new NpgsqlConnection(DB.connectionString);
            connection.Open();

            string sql = "INSERT INTO properties (owner, locality, description, usage, price) VALUES (@o, @l, @d, @u, @p) RETURNING id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("o", owner.ID);
            command.Parameters.AddWithValue("l", locality.ID);
            command.Parameters.AddWithValue("d", description.ID);
            command.Parameters.AddWithValue("u", usage);
            command.Parameters.AddWithValue("p", price);

            id = (int)command.ExecuteScalar();
        }

        public override string ToString()
        {
            return $"Земельна ділянка №{id} вартістю {price} у. о., знаходиться в {locality.Title}, тип використання - {usage}, власник - {owner.FirstName} {owner.LastName}.";
        }

        public int ID
        {
            get
            {
                return id;
            }
        }
    }

    class Description
    {
        private int water;
        private string soil;
        private List<List<int>> coordinates;
        private int id;

        public Description(int water, string soil, List<List<int>> coordinates)
        {
            this.water = water;
            this.soil = soil;
            this.coordinates = coordinates;

            using var connection = new NpgsqlConnection(DB.connectionString);
            connection.Open();

            using var command = new NpgsqlCommand("INSERT INTO descriptions (water, soil, coordinates) VALUES (@w, @s, @c) RETURNING id;", connection);

            command.Parameters.Add(new NpgsqlParameter("c", NpgsqlTypes.NpgsqlDbType.Jsonb)
            {
                Value = coordinates
            });

            command.Parameters.AddWithValue("w", water);
            command.Parameters.AddWithValue("s", soil);

            id = (int)command.ExecuteScalar();
        }

        public int ID
        {
            get
            {
                return id;
            }
        }
    }

    class Locality
    {
        private string title;
        private int id;

        public Locality(string title)
        {
            this.title = title;

            using var connection = new NpgsqlConnection(DB.connectionString);
            connection.Open();

            using var command = new NpgsqlCommand("INSERT INTO localities (title) VALUES (@t) RETURNING id;", connection);

            command.Parameters.AddWithValue("t", title);

            id = (int)command.ExecuteScalar();
        }

        public int ID
        {
            get
            {
                return id;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }
    }
}