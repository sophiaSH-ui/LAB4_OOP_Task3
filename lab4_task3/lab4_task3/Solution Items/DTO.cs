using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Text;

namespace lab4_task3.DTO
{
    internal class DB
    {
        public static string ConnectionString;

        static DB()
        {
            using (StreamReader reader = new StreamReader("Solution Items/access.txt"))
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
                using var selectCommand = new NpgsqlCommand();
                selectCommand.Connection = connection;

                selectCommand.CommandText = "SELECT id FROM localities;";
                using var localityReader = selectCommand.ExecuteReader();

                while (localityReader.Read())
                {
                    int localityID = localityReader.GetInt32(localityReader.GetOrdinal("id"));

                    localities[localityID] = new Locality(localityID);
                }

                command.CommandText = "SELECT id, owner, description, locality, usage FROM properties;";
            }
            else
            {
                command.CommandText = "SELECT id, owner, description, locality, usage FROM properties WHERE locality = @localityId;";
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

                int usageID = reader.GetInt32(reader.GetOrdinal("usage"));

                if (!owners.ContainsKey(ownerID))
                {
                    owners[ownerID] = new Owner(ownerID);
                }

                var description = new Description(descriptionID);
                var usage = new Usage(usageID);

                var property = new Property(owners[ownerID], description, localities[localityID], usage, propertyID);

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
            command.CommandText = "SELECT id FROM owners;";

            ObservableCollection<Owner> owners = new ObservableCollection<Owner>();

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int ownerID = reader.GetInt32(reader.GetOrdinal("id"));

                owners.Add(new Owner(ownerID));
            }

            return owners;
        }

        public ObservableCollection<Usage> GetUsages()
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            using var command = new NpgsqlCommand();

            command.Connection = connection;
            command.CommandText = "SELECT id FROM usages;";

            ObservableCollection<Usage> usages = new ObservableCollection<Usage>();

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int usageID = reader.GetInt32(reader.GetOrdinal("id"));
                usages.Add(new Usage(usageID));
            }

            return usages;
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

        public string? Validate(ObservableCollection<Property> properties)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (Property property in properties)
            {
                var context = new ValidationContext(property);
                var results = new List<ValidationResult>();

                bool isValid = Validator.TryValidateObject(property, context, results, validateAllProperties: true);

                if (!isValid)
                {
                    stringBuilder.AppendLine($"№{property.ID} ({property.ToString()})");
                }
            }

            if (stringBuilder.Length > 0)
            {
                return stringBuilder.ToString();
            }
            else
            {
                return null;
            }
        }

        public string? Validate(ObservableCollection<Owner> owners)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (Owner owner in owners)
            {
                var context = new ValidationContext(owner);
                var results = new List<ValidationResult>();

                bool isValid = Validator.TryValidateObject(owner, context, results, validateAllProperties: true);

                if (!isValid)
                {
                    stringBuilder.AppendLine($"№{owner.ID} ({owner.FirstName} {owner.LastName})");
                }
            }

            if (stringBuilder.Length > 0)
            {
                return stringBuilder.ToString();
            }
            else
            {
                return null;
            }
        }

        enum PointPosition { Outside, OnBoundary, Inside }

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

        static bool SegmentsProperlyIntersect(
            (long X, long Y) a1, (long X, long Y) a2,
            (long X, long Y) b1, (long X, long Y) b2)
        {
            long Cross((long X, long Y) o, (long X, long Y) a, (long X, long Y) b) =>
                (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

            long d1 = Cross(b1, b2, a1), d2 = Cross(b1, b2, a2);
            long d3 = Cross(a1, a2, b1), d4 = Cross(a1, a2, b2);

            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                   ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        static PointPosition PointInPolygon(
            (long X, long Y) p,
            List<(long X, long Y)> polygon)
        {
            int winding = 0;
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % n];

                long cross = (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

                if (cross == 0 &&
                    Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) &&
                    Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y))
                    return PointPosition.OnBoundary;

                if (a.Y <= p.Y)
                {
                    if (b.Y > p.Y && cross > 0)
                        winding++;
                }
                else
                {
                    if (b.Y <= p.Y && cross < 0)
                        winding--;
                }
            }

            return winding != 0 ? PointPosition.Inside : PointPosition.Outside;
        }

        static bool PolygonsOverlap(
            List<(long X, long Y)> a,
            List<(long X, long Y)> b)
        {
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                    if (SegmentsProperlyIntersect(
                        a[i], a[(i + 1) % a.Count],
                        b[j], b[(j + 1) % b.Count]))
                        return true;

            if (a.Any(p => PointInPolygon(p, b) == PointPosition.Inside)) return true;
            if (b.Any(p => PointInPolygon(p, a) == PointPosition.Inside)) return true;

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

        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Ім'я: від 2 до 50 символів")]

        [RegularExpression(@"^[\p{L}'\-]+$",
        ErrorMessage = "Ім'я може містити лише літери, дефіс та апостроф")]

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

        [Required(ErrorMessage = "Прізвище є обов'язковим")]
        [StringLength(50, MinimumLength = 2,
        ErrorMessage = "Прізвище: від 2 до 50 символів")]

        [RegularExpression(@"^[\p{L}'\-]+$",
        ErrorMessage = "Прізвище може містити лише літери, дефіс та апостроф")]

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

        [Required(ErrorMessage = "Дата народження є обов'язковою")]
        [DataType(DataType.Date, ErrorMessage = "Невірний формат дати")]
        [Range(typeof(DateTime), "1/1/1900", "12/31/2100", ErrorMessage = "Дата народження повинна бути між 01.01.1900 та 31.12.2100")]

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

            string sql = "SELECT first_name, last_name, birth_day FROM owners WHERE id = @id;";

            using var command = new NpgsqlCommand( sql, connection);

            command.Parameters.AddWithValue("id", ownerID);

            var reader = command.ExecuteReader();

            reader.Read();

            FirstName = reader.GetString(0);
            LastName = reader.GetString(1);
            BirthDate = reader.GetDateTime(2);
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

    public class Usage
    {
        private string title;
        private int id;

        public Usage(string title)
        {
            this.title = title;

            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            using var command = new NpgsqlCommand("INSERT INTO usages (title) VALUES (@t) RETURNING id;", connection);

            command.Parameters.AddWithValue("t", title);

            id = (int)command.ExecuteScalar();
        }

        public Usage(int usageID)
        {
            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "SELECT title FROM usages WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", usageID);

            var reader = command.ExecuteReader();

            reader.Read();

            this.title = reader.GetString(0);
            this.id = usageID;
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

    public class Property
    {
        private Owner owner;
        private Description description;
        private Locality locality;
        private Usage usage;
        private double price;
        private int id;

        public Property(Owner owner, Description description, Locality locality, Usage usage, double price)
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
            command.Parameters.AddWithValue("u", usage.ID);
            command.Parameters.AddWithValue("p", price);

            id = (int)command.ExecuteScalar();
        }

        public Property(Owner owner, Description description, Locality locality, Usage usage, int propertyId)
        {
            this.owner = owner;
            this.description = description;
            this.locality = locality;
            this.usage = usage;

            using var connection = new NpgsqlConnection(DB.ConnectionString);
            connection.Open();

            string sql = "SELECT price FROM properties WHERE id = @id;";

            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", propertyId);

            var reader = command.ExecuteReader();

            reader.Read();

            this.price = reader.GetDouble(0);
            this.id = propertyId;
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

        public Usage Usage
        {
            get
            {
                return usage;
            }
        }

        [Required(ErrorMessage = "Ціна є обов'язковою")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ціна повинна бути більше 0")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Ціна повинна бути числом з максимум двома знаками після коми")]

        public double Price
        {
            get
            {
                return price;
            }
        }

        public void Update(Owner owner, Description description, Locality locality, Usage usage, double price)
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
            command.Parameters.AddWithValue("u", usage.ID);
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

            command.Parameters.Add(new NpgsqlParameter("c", NpgsqlTypes.NpgsqlDbType.Json)
            {
                Value = JsonSerializer.Serialize(coordinates)
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

        [Required(ErrorMessage = "Рівень ґрунтових вод є обов'язковим")]
        [Range(0, int.MaxValue, ErrorMessage = "Рівень ґрунтових вод не може бути від'ємним")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Рівень ґрунтових вод має бути цілим числом")]

        public int Water
        {
            get
            {
                return water;
            }
        }

        [Required(ErrorMessage = "Тип ґрунту є обов'язковим")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Тип ґрунту повинен містити від 2 до 100 символів")]
        [RegularExpression(@"^[\p{L}\s\-]+$", ErrorMessage = "Тип ґрунту може містити тільки літери, пробіли та дефіси")]

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

            command.Parameters.Add(new NpgsqlParameter("c", NpgsqlTypes.NpgsqlDbType.Json)
            {
                Value = JsonSerializer.Serialize(coordinates)
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

        [Required(ErrorMessage = "Назва є обов'язковою")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Назва повинна містити від 2 до 100 символів")]
        [RegularExpression(@"^[\p{L}\s\-]+$", ErrorMessage = "Назва може містити тільки літери, пробіли та дефіси")]

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
}