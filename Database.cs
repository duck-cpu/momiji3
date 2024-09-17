using Microsoft.Data.Sqlite;

namespace momiji3
{
    public class Database
    {
        private const string DbFilePath = "botdata.sqlite";

        //initialize the SQLite database and create tables if they don't exist
        public static void InitializeDatabase()
        {
            if (!File.Exists(DbFilePath))
            {
                using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
                {
                    connection.Open();

                    string tableCmd = @"
                        CREATE TABLE IF NOT EXISTS UserRolls (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UserId TEXT NOT NULL,
                            Rarity INTEGER NOT NULL,
                            ImageUrl TEXT NOT NULL,
                            RollName TEXT NOT NULL,
                            Element TEXT NOT NULL,
                            Attack INTEGER NOT NULL,
                            Defense INTEGER NOT NULL,
                            Speed INTEGER NOT NULL
                        );
                    ";

                    using (var command = new SqliteCommand(tableCmd, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
        }

        //insert a new roll into the UserRolls table
        public static void InsertUserRoll(string userId, int rarity, string imageUrl, string rollName, string element, int attack, int defense, int speed)
        {
            using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
            {
                connection.Open();

                string insertCmd = @"
                    INSERT INTO UserRolls (UserId, Rarity, ImageUrl, RollName, Element, Attack, Defense, Speed) 
                    VALUES (@UserId, @Rarity, @ImageUrl, @RollName, @Element, @Attack, @Defense, @Speed);
                ";

                using (var command = new SqliteCommand(insertCmd, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Rarity", rarity);
                    command.Parameters.AddWithValue("@ImageUrl", imageUrl);
                    command.Parameters.AddWithValue("@RollName", rollName);
                    command.Parameters.AddWithValue("@Element", element);
                    command.Parameters.AddWithValue("@Attack", attack);
                    command.Parameters.AddWithValue("@Defense", defense);
                    command.Parameters.AddWithValue("@Speed", speed);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        //retrieve all rolls for a specific user
        public static List<Bot.UserRollEntry> GetUserRolls(string userId)
        {
            var rolls = new List<Bot.UserRollEntry>();

            using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
            {
                connection.Open();

                string selectCmd = "SELECT * FROM UserRolls WHERE UserId = @UserId";

                using (var command = new SqliteCommand(selectCmd, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rolls.Add(new Bot.UserRollEntry
                            {
                                Rarity = reader.GetInt32(2),
                                ImageUrl = reader.GetString(3),
                                RollName = reader.GetString(4),
                                Element = (Bot.Element)Enum.Parse(typeof(Bot.Element), reader.GetString(5)),
                                Attack = reader.GetInt32(6),
                                Defense = reader.GetInt32(7),
                                Speed = reader.GetInt32(8)
                            });
                        }
                    }
                }

                connection.Close();
            }

            return rolls;
        }
    }
}
