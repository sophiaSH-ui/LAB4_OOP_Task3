using System;
using Npgsql;
using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

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

                command.CommandText = "SELECT first_name, last_name, birth_day FROM owners WHERE id = @ownerId;";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("ownerId", ownerID);

                using var ownerReader = command.ExecuteReader();

                if (ownerReader.Read())
                {
                    string firstName = ownerReader.GetString(ownerReader.GetOrdinal("first_name"));
                    string lastName = ownerReader.GetString(ownerReader.GetOrdinal("last_name"));
                    DateTime birthDate = ownerReader.GetDateTime(ownerReader.GetOrdinal("birth_day"));
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

        public int GetPropertiesCountByLocation(string location)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            string sql = @"SELECT COUNT(*) FROM properties p
                   JOIN localities l ON p.locality = l.id
                   WHERE l.title ILIKE @loc";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("loc", "%" + location + "%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Owner> GetOwners()
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var command = new NpgsqlCommand();
            command.Connection = connection;
            command.CommandText = "SELECT * FROM owners;";

            List<Owner> owners = new List<Owner>();

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
        public List<List<int>>? IsOverlapping(List<List<int>> currentCoords, string locality, int excludeId = -1)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            string sql = @"SELECT d.coordinates, p.id FROM properties p
                   JOIN localities l ON p.locality = l.id
                   JOIN descriptions d ON p.description = d.id
                   WHERE l.title = @loc";

            if (excludeId != -1) sql += " AND p.id != @excludeId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("loc", locality);
            if (excludeId != -1) cmd.Parameters.AddWithValue("excludeId", excludeId);

            var currentPoly = ToPoints(currentCoords);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var jsonCoords = reader.GetString(0);
                var otherCoords = new List<List<int>>();
                var matches = Regex.Matches(jsonCoords, @"\d+");
                for (int i = 0; i + 1 < matches.Count; i += 2)
                    otherCoords.Add(new List<int> { int.Parse(matches[i].Value), int.Parse(matches[i + 1].Value) });

                if (AabbOverlaps(currentCoords, otherCoords) && PolygonsOverlap(currentPoly, ToPoints(otherCoords)))
                    return otherCoords; 
            }
            return null; 
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


        public Property? CheckOverlapping(Property property, List<Property> properties)
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

    class Owner
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

            using var connection = new NpgsqlConnection(DB.connectionString);
            connection.Open();

            string sql = "INSERT INTO owners (first_name, last_name, birth_day) VALUES (@fn, @ln, @bd) RETURNING id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("fn", FirstName);
            command.Parameters.AddWithValue("ln", LastName);
            command.Parameters.AddWithValue("bd", BirthDate);

            id = (int)command.ExecuteScalar();
        }
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

        public List<List<int>> Coordinates
        {
            get
            {
                return coordinates;
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
        public double GroundWater { get; set; }
        public string SoilType { get; set; }
        public string Description { get; set; }    
        public List<string> Coordinates { get; set; }

        [JsonIgnore]
        public List<Point> CoordinatePoints { get; set; }
    }



}