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
                            Speed INTEGER NOT NULL,
                            OwnerId TEXT NOT NUll
                        );
                        CREATE TABLE IF NOT EXISTS UserBalances (
                            UserId TEXT PRIMARY KEY NOT NULL,
                            Balance INTEGER NOT NULL
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
        public static void InsertUserRoll(string userId, int rarity, string imageUrl, string rollName, string element, int attack, int defense, int speed, string ownerId)
        {
            using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
            {
                connection.Open();

                string insertCmd = @"
                    INSERT INTO UserRolls (UserId, Rarity, ImageUrl, RollName, Element, Attack, Defense, Speed, OwnerId) 
                    VALUES (@UserId, @Rarity, @ImageUrl, @RollName, @Element, @Attack, @Defense, @Speed, @OwnerId);
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
                    command.Parameters.AddWithValue("@OwnerId", ownerId);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        //retrieve all rolls for a specific user
        public static List<Bot.UserRollEntry> GetUserRolls(string userId, string GuildId)
        {
            var rolls = new List<Bot.UserRollEntry>();

            using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
            {
                connection.Open();

                string selectCmd = "SELECT * FROM UserRolls WHERE UserId = @UserId";

                using (var command = new SqliteCommand(selectCmd, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@GuildId", GuildId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rolls.Add(new Bot.UserRollEntry
                            {
                                Id = reader.GetInt32(0),
                                Rarity = reader.GetInt32(2),
                                ImageUrl = reader.GetString(3),
                                RollName = reader.GetString(4),
                                Element = (Bot.Element)Enum.Parse(typeof(Bot.Element), reader.GetString(5)),
                                Attack = reader.GetInt32(6),
                                Defense = reader.GetInt32(7),
                                Speed = reader.GetInt32(8),
                                OwnerId = reader.GetString(9),
                            });
                        }
                    }
                }

                connection.Close();
            }

            return rolls;
        }
        public static List<Bot.UserRollEntry> QueryDatabase(string queryParams)
        {
            var results = new List<Bot.UserRollEntry>();

            using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
            {
                connection.Open();

                //example query: simple search by roll name or element
                string selectCmd = @"
                    SELECT * FROM UserRolls
                    WHERE RollName LIKE @QueryParam
                    OR Element LIKE @QueryParam
                    OR Id LIKE @QueryParam;
                ";

                using (var command = new SqliteCommand(selectCmd, connection))
                {
                    command.Parameters.AddWithValue("@QueryParam", $"%{queryParams}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Bot.UserRollEntry
                            {
                                Id = reader.GetInt32(0),
                                Rarity = reader.GetInt32(2),
                                ImageUrl = reader.GetString(3),
                                RollName = reader.GetString(4),
                                Element = (Bot.Element)Enum.Parse(typeof(Bot.Element), reader.GetString(5)),
                                Attack = reader.GetInt32(6),
                                Defense = reader.GetInt32(7),
                                Speed = reader.GetInt32(8),
                                OwnerId = reader.GetString(9)
                            });
                        }
                    }
                }

                connection.Close();
            }

            return results;
        }
        //initialize user balance
        public static void SetUserBalance(string userId, int balance)
        {
            using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
            {
                connection.Open();

                string upsertCmd = @"
                    INSERT INTO UserBalances (UserId, Balance) VALUES (@UserId, @Balance)
                    ON CONFLICT(UserId) DO UPDATE SET Balance = @Balance;
                ";

                using (var command = new SqliteCommand(upsertCmd, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Balance", balance);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }
        //retrieve user balance
        public static int GetUserBalance(string userId)
        {
            using (var connection = new SqliteConnection($"Data Source={DbFilePath}"))
            {
                connection.Open();

                string selectCmd = "SELECT Balance FROM UserBalances WHERE UserId = @UserId";

                using (var command = new SqliteCommand(selectCmd, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    object? result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }
        //deduct balance
        public static bool DeductUserBalance(string userId, int amount)
        {
            int currentBalance = GetUserBalance(userId);
            if (currentBalance < amount)
                return false;

            SetUserBalance(userId, currentBalance - amount);
            return true;
        }
    }
}
