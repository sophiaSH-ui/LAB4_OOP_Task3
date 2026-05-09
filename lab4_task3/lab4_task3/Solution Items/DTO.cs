using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;

namespace lab4_task3.DTO
{
    internal class DB
    {
        public static string ConnectionString;

        static DB()
        {
            using (StreamReader reader = new StreamReader("access.txt"))
            {
                ConnectionString = reader.ReadToEnd();
            }
        }

        public ObservableCollection<Property> GetProperties(Locality locality = null)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            var owners = new Dictionary<int, Owner>();

            ObservableCollection<Property> properties = new ObservableCollection<Property>();

            using var command = new NpgsqlCommand();
            command.Connection = connection;

            var localities = new Dictionary<int, Locality>();

            if (locality is null)
            {
                command.CommandText = "SELECT id FROM localities;";
                using var localityReader = command.ExecuteReader();

                while (localityReader.Read())
                {
                    int localityID = localityReader.GetInt32(localityReader.GetOrdinal("id"));

                    localities[localityID] = new Locality(localityID);
                }

                command.CommandText = "SELECT id, owner, description, locality FROM properties;";
            }
            else
            {
                command.CommandText = "SELECT id, owner, description, locality FROM properties WHERE locality = @localityId;";

                command.Parameters.AddWithValue("localityId", locality.ID);

                localities[locality.ID] = locality;
            }

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int propertyID = reader.GetInt32(reader.GetOrdinal("id"));
                int ownerID = reader.GetInt32(reader.GetOrdinal("owner"));
                int descriptionID = reader.GetInt32(reader.GetOrdinal("description"));

                int localityID = reader.GetInt32(reader.GetOrdinal("locality"));

                if (!owners.ContainsKey(ownerID))
                {
                    owners[ownerID] = new Owner(ownerID);
                }

                var description = new Description(descriptionID);

                var property = new Property(owners[ownerID], description, localities[localityID], propertyID);

                properties.Add(property);
            }

            return properties;
        }

        public ObservableCollection<Owner> GetOwners()
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            using var command = new NpgsqlCommand();
            command.Connection = connection;
            command.CommandText = "SELECT * FROM owners;";

            ObservableCollection<Owner> owners = new ObservableCollection<Owner>();

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int ownerID = reader.GetInt32(reader.GetOrdinal("id"));
                string firstName = reader.GetString(reader.GetOrdinal("first_name"));
                string lastName = reader.GetString(reader.GetOrdinal("last_name"));
                DateTime birthDate = reader.GetDateTime(reader.GetOrdinal("birth_day"));
                owners.Add(new Owner(firstName, lastName, birthDate));
            }

            return owners;
        }

        public long GetPropertiesCount(Locality locality = null)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
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


        public Property? CheckOverlapping(Property property, ObservableCollection<Property> properties)
        {
            var currentPoly = ToPoints(property.Description.Coordinates);

            return properties
                .Where(p => p.Locality.ID == property.Locality.ID)
                .Where(p => p.ID != property.ID)
                .FirstOrDefault(p =>
                    AabbOverlaps(property.Description.Coordinates, p.Description.Coordinates)
                    && PolygonsOverlap(currentPoly, ToPoints(p.Description.Coordinates)));
        }

        static bool SegmentsIntersect(
    (long X, long Y) a1, (long X, long Y) a2,
    (long X, long Y) b1, (long X, long Y) b2)
        {
            long Cross((long X, long Y) o, (long X, long Y) a, (long X, long Y) b) =>
                (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

            bool OnSegment((long X, long Y) p, (long X, long Y) a, (long X, long Y) b) =>
                Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) &&
                Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);

            long d1 = Cross(b1, b2, a1), d2 = Cross(b1, b2, a2);
            long d3 = Cross(a1, a2, b1), d4 = Cross(a1, a2, b2);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            if (d1 == 0 && OnSegment(a1, b1, b2)) return true;
            if (d2 == 0 && OnSegment(a2, b1, b2)) return true;
            if (d3 == 0 && OnSegment(b1, a1, a2)) return true;
            if (d4 == 0 && OnSegment(b2, a1, a2)) return true;

            return false;
        }

        static bool PointInPolygon(
            (long X, long Y) p,
            List<(long X, long Y)> polygon)
        {
            bool inside = false;
            int n = polygon.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.Y > p.Y) != (b.Y > p.Y))
                {
                    long lhs = (b.X - a.X) * (p.Y - a.Y);
                    long rhs = (b.Y - a.Y) * (p.X - a.X);
                    if ((lhs > rhs) == (a.Y < b.Y))
                        inside = !inside;
                }
            }
            return inside;
        }

        static bool PolygonsOverlap(
            List<(long X, long Y)> a,
            List<(long X, long Y)> b)
        {
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    if (SegmentsIntersect(
                        a[i], a[(i + 1) % a.Count],
                        b[j], b[(j + 1) % b.Count]))
                        return true;

            if (PointInPolygon(a[0], b)) return true;
            if (PointInPolygon(b[0], a)) return true;

            return false;
        }

        static bool AabbOverlaps(List<List<int>> a, List<List<int>> b)
        {
            if (a.Count == 0 || b.Count == 0) return false;

            int aMinX = a.Min(p => p[0]), aMaxX = a.Max(p => p[0]);
            int aMinY = a.Min(p => p[1]), aMaxY = a.Max(p => p[1]);
            int bMinX = b.Min(p => p[0]), bMaxX = b.Max(p => p[0]);
            int bMinY = b.Min(p => p[1]), bMaxY = b.Max(p => p[1]);

            return aMinX <= bMaxX && aMaxX >= bMinX &&
                   aMinY <= bMaxY && aMaxY >= bMinY;
        }

        static List<(long X, long Y)> ToPoints(List<List<int>> coords) =>
            coords.ConvertAll(p => ((long)p[0], (long)p[1]));
    }

    public class Owner
    {
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
                birthDate = value;
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

            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "INSERT INTO owners (first_name, last_name, birth_day) VALUES (@fn, @ln, @bd) RETURNING id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("fn", FirstName);
            command.Parameters.AddWithValue("ln", LastName);
            command.Parameters.AddWithValue("bd", BirthDate);

            id = (int)command.ExecuteScalar();
        }

        public Owner(int ownerID)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "SELECT first_name, last_name, birth_date FROM owners WHERE id = @id;";

            using var command = new NpgsqlCommand( sql, connection);

            command.Parameters.AddWithValue("id", ownerID);

            var reader = command.ExecuteReader();

            reader.Read();

            FirstName = reader.GetString(0);
            LastName = reader.GetString(1);
            BirthDate = DateTime.Parse(reader.GetString(2));
            this.id = ownerID;
        }

        public void Update(string fn, string ln, DateTime bd)
        {
            FirstName = fn;
            LastName = ln;
            BirthDate = bd;

            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "UPDATE owners SET first_name = @fn, last_name = @ln, birth_day = @bd WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("fn", FirstName);
            command.Parameters.AddWithValue("ln", LastName);
            command.Parameters.AddWithValue("bd", BirthDate);
            command.Parameters.AddWithValue("id", id);

            command.ExecuteNonQuery();
        }

        public void Delete()
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "DELETE FROM owners WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", id);

            command.ExecuteNonQuery();
        }
    }

    public class Property
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

            using var connection = new NpgsqlConnection(DB.ConnectionString);
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

        public Property(Owner owner, Description description, Locality locality, int propertyId)
        {
            this.owner = owner;
            this.description = description;
            this.locality = locality;

            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "SELECT usage, price FROM properties WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", propertyId);

            var reader = command.ExecuteReader();

            reader.Read();

            this.usage = reader.GetString(0);
            this.price = reader.GetInt32(1);
            this.id = propertyId;
        }

        //додати в інформації про ділянку

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

        public Owner Owner
        {
            get
            {
                return owner;
            }
        }

        public Description Description
        {
            get
            {
                return description;
            }
        }

        public Locality Locality
        {
            get
            {
                return locality;
            }
        }

        public string Usage
        {
            get
            {
                return usage;
            }
        }

        public double Price
        {
            get
            {
                return price;
            }
        }

        public void Update(Owner owner, Description description, Locality locality, string usage, double price)
        {
            this.owner = owner;
            this.description = description;
            this.locality = locality;
            this.usage = usage;
            this.price = price;

            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "UPDATE properties SET owner = @o, locality = @l, description = @d, usage = @u, price = @p WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("o", owner.ID);
            command.Parameters.AddWithValue("l", locality.ID);
            command.Parameters.AddWithValue("d", description.ID);
            command.Parameters.AddWithValue("u", usage);
            command.Parameters.AddWithValue("p", price);
            command.Parameters.AddWithValue("id", id);

            command.ExecuteNonQuery();
        }

        public void Delete()
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "DELETE FROM properties WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", id);

            command.ExecuteNonQuery();
        }
    }

    public class Description
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

            using var connection = new NpgsqlConnection(DB.ConnectionString);
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

        public Description(int descriptionId)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "SELECT water, soil, coordinates FROM descriptions WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", descriptionId);

            var reader = command.ExecuteReader();

            reader.Read();

            this.water = reader.GetInt32(0);
            this.soil = reader.GetString(1);
            this.coordinates = JsonSerializer.Deserialize<List<List<int>>>(reader.GetString(reader.GetOrdinal("coordinates")));
            this.id = descriptionId;
        }

        public int ID
        {
            get
            {
                return id;
            }
        }

        public List<List<int>> Coordinates
        {
            get
            {
                return coordinates;
            }
        }

        public int Water
        {
            get
            {
                return water;
            }
        }

        public string Soil
        {
            get
            {
                return soil;
            }
        }

        public void Update(int descriptionId, int water, string soil, List<List<int>> coordinates)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "UPDATE descriptions SET water = @w, soil = @s, coordinates = @c WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.Add(new NpgsqlParameter("c", NpgsqlTypes.NpgsqlDbType.Jsonb)
            {
                Value = coordinates
            });

            command.Parameters.AddWithValue("w", water);
            command.Parameters.AddWithValue("s", soil);
            command.Parameters.AddWithValue("id", descriptionId);

            command.ExecuteNonQuery();
        }

        public void Delete()
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "DELETE FROM descriptions WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", id);

            command.ExecuteNonQuery();
        }
    }

    public class Locality
    {
        private string title;
        private int id;

        public Locality(string title)
        {
            this.title = title;

            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            using var command = new NpgsqlCommand("INSERT INTO localities (title) VALUES (@t) RETURNING id;", connection);

            command.Parameters.AddWithValue("t", title);

            id = (int)command.ExecuteScalar();
        }

        public Locality(int localityID)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "SELECT title FROM localities WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", localityID);

            var reader = command.ExecuteReader();

            reader.Read();

            this.title = reader.GetString(0);
            this.id = localityID;
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

        public void Update(int localityId, string title)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "UPDATE localities SET title = @t WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("t", title);
            command.Parameters.AddWithValue("id", localityId);

            command.ExecuteNonQuery();
        }

        public void Delete()
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "DELETE FROM localities WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", id);

            command.ExecuteNonQuery();
        }
    }

    public class Plot
    {
        public int Id { get; set; }
        public string OwnerName { get; set; }
        public int OwnerId { get; set; }
        public string Location { get; set; }
        public string Purpose { get; set; }
        public string Pryznachennya  
        {
            get => Purpose;
            set => Purpose = value;
        }
        public double MarketValue { get; set; }
        public string MarketValueFormatted { get; set; }
        public int GroundWater { get; set; }
        public string SoilType { get; set; }
        public string Description { get; set; }    
        public List<string> Coordinates { get; set; }

        [JsonIgnore]
        public List<Point> CoordinatePoints { get; set; }
    }
}