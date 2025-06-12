using System.Text.Json;
using Microsoft.Data.Sqlite;
public static class Database {
    private static readonly TaskCompletionSource _initializationTcs = new();
    private static readonly Task _initializationTask = _initializationTcs.Task;
    static readonly JsonSerializerOptions jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };
    [Flags]
    public enum UserPermissions {
        None = 0,
        CanEditPlayers = 1 << 0,
        CanBanPlayers = 1 << 1,
        CanEditAccounts = 1 << 2,
        Moderator = CanEditPlayers | CanBanPlayers,
        Admin = Moderator | CanEditAccounts,
    }
    static readonly string file = "Data source=../data/users.db";
    static Dictionary<string, Fighter>? _fighters;
    public static Dictionary<string, Fighter> Fighters =>
        _fighters ?? [];


    public static async Task InitializeDatabase() {
        await CreateUserTable();
        await CreatePlayerDataTable();
        using StreamReader fr = new("../data/fighter.json");
        string fighterJson = await fr.ReadToEndAsync();
        using StreamReader sr = new("../data/skills.json");
        string skillsJson = await sr.ReadToEndAsync();

        var fighters = JsonSerializer.Deserialize<Dictionary<string, StoredFighter>>(fighterJson, jsonOptions) ?? throw new Exception("Fighter database is null");
        var skills = JsonSerializer.Deserialize<Dictionary<string, Skill>>(skillsJson, jsonOptions) ?? throw new Exception("Skills database is null");
        _fighters = fighters.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ConvertToFighter(skills)
        );
        _initializationTcs.SetResult();
    }
    static async Task CreateUserTable() {
        using var connection = new SqliteConnection(file);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS Users (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Username TEXT NOT NULL UNIQUE,
        Password TEXT NOT NULL,
        Permissions INTEGER NOT NULL DEFAULT 0
        )
        ";
        await command.ExecuteNonQueryAsync();
    }
    static async Task CreatePlayerDataTable() {
        using var connection = new SqliteConnection(file);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS PlayerData (
            UserId INTEGER PRIMARY KEY,                  
            PlayerName TEXT NOT NULL,                 
            Level INTEGER NOT NULL DEFAULT 1,        
            XP INTEGER NOT NULL DEFAULT 0,           
            MaxSP INTEGER NOT NULL DEFAULT 100,         
            MaxHP INTEGER NOT NULL DEFAULT 100,         
            SP INTEGER NOT NULL DEFAULT 100,    
            HP INTEGER NOT NULL DEFAULT 100,     
            XPForLevelUp INTEGER NOT NULL DEFAULT 50,
            Money INTEGER NOT NULL DEFAULT 100,         
            FightersJson TEXT NOT NULL,                        
            InventoryJson TEXT NOT NULL DEFAULT '{}',                       
            SelectedFighterKey TEXT NOT NULL,                 
            
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE                    
        );";
        await command.ExecuteNonQueryAsync();
    }

    public static Fighter? GetFighterByName(string name) {
        if (Fighters.TryGetValue(name, out Fighter? fighter)) {
            return fighter;
        } else return null;
    }
  
    public static async Task<User?> GetUser(string username) {
        await _initializationTask;
        using var connection = new SqliteConnection(file);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Users WHERE Username = @Username;";
        command.Parameters.AddWithValue("@Username", username);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync()) {
            long id = reader.GetInt64(reader.GetOrdinal("Id"));
            string name = reader.GetString(reader.GetOrdinal("Username"));
            string password = reader.GetString(reader.GetOrdinal("Password"));
            UserPermissions permissions = (UserPermissions)reader.GetInt32(reader.GetOrdinal("Permissions"));
            return new User(id, name, password, permissions);
        }
        return null;
    }
    public static async Task<User?> GetUserById(long userid) {
        await _initializationTask;
        using var connection = new SqliteConnection(file);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Users WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", userid);
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync()) {
            long id = reader.GetInt64(reader.GetOrdinal("Id"));
            string name = reader.GetString(reader.GetOrdinal("Username"));
            string password = reader.GetString(reader.GetOrdinal("Password"));
            UserPermissions permissions = (UserPermissions)reader.GetInt32(reader.GetOrdinal("Permissions"));
            return new User(id, name, password, permissions);
        }
        return null;
    }
    public static async Task<Player?> GetPlayerById(long id) {
        await _initializationTask;
        using var connection = new SqliteConnection(file);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM PlayerData WHERE UserId = @Id";
        command.Parameters.AddWithValue("@Id", id);
        try {
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                string playerName = reader.GetString(reader.GetOrdinal("PlayerName"));
                int level = reader.GetInt32(reader.GetOrdinal("Level"));
                int xp = reader.GetInt32(reader.GetOrdinal("XP"));
                int maxSp = reader.GetInt32(reader.GetOrdinal("MaxSP"));
                int maxHp = reader.GetInt32(reader.GetOrdinal("MaxHP"));
                int Sp = reader.GetInt32(reader.GetOrdinal("SP"));
                int Hp = reader.GetInt32(reader.GetOrdinal("HP"));
                int xpForLevelUp = reader.GetInt32(reader.GetOrdinal("XPForLevelUp"));
                int money = reader.GetInt32(reader.GetOrdinal("Money"));
                string fightersJson = reader.GetString(reader.GetOrdinal("FightersJson"));
                string inventoryJson = reader.GetString(reader.GetOrdinal("InventoryJson"));
                string selectedFighterKey = reader.GetString(reader.GetOrdinal("SelectedFighterKey"));
                var fighters = JsonSerializer.Deserialize<Dictionary<string, Fighter>>(fightersJson, jsonOptions) ?? [];
                var inventory = JsonSerializer.Deserialize<Dictionary<string, Item>>(inventoryJson, jsonOptions) ?? [];
                if (!fighters.TryGetValue(selectedFighterKey, out Fighter? fighter)) {
                    Console.WriteLine("Invalid selected fighter");
                    return null;
                }
                return new Player(id, playerName, level, xp, maxSp, maxHp, xpForLevelUp, money, fighters, inventory, fighter, Sp, Hp);
            }
        } catch (Exception e) {
            Console.WriteLine($"Error getting user {e}");
            return null;
        }
        Console.WriteLine("No player data found");
        return null;
    }
    public static async Task<bool> RegisterUser(string username, string password, string starterFighter) {
        await _initializationTask;
        var fighters = Fighters;
        if (!fighters.TryGetValue(starterFighter, out Fighter? selectedFighter)) {
            Console.WriteLine("Error with chosen fighter");
            return false;
        }
        var initialFighters = new Dictionary<string, Fighter> {
            { starterFighter, selectedFighter }
        };
        string fightersToSaveJson = JsonSerializer.Serialize(initialFighters, jsonOptions);
        using var connection = new SqliteConnection(file);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();
        try {
            var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction?)transaction;
            command.CommandText =
            @"
        INSERT INTO Users (Username, Password) VALUES (@Username, @Password)
        ";
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", PasswordHasher.HashPassword(password));
            await command.ExecuteNonQueryAsync();
            command.CommandText = " SELECT last_insert_rowid()";
            var userId = await command.ExecuteScalarAsync();
            if (userId is null) {
                Console.WriteLine("Error getting User id, cancelling registration");
                await transaction.RollbackAsync();
                return false;
            }

            var playerCommand = connection.CreateCommand();
            playerCommand.Transaction = (SqliteTransaction?)transaction;
            playerCommand.CommandText = @"
            INSERT INTO PlayerData
              (UserId, PlayerName, FightersJson, SelectedFighterKey)
            VALUES
              (@id, @name, @fighters, @selected);
        ";
            playerCommand.Parameters.AddWithValue("@id", (long)userId);
            playerCommand.Parameters.AddWithValue("@name", username);
            playerCommand.Parameters.AddWithValue("@fighters", fightersToSaveJson);
            playerCommand.Parameters.AddWithValue("@selected", starterFighter);
            await playerCommand.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        } catch (Exception e) {
            await transaction.RollbackAsync();
            Console.WriteLine(e);
            return false;
        }
        return true;
    }
    public static async Task<bool> UpdatePlayer(Player player) {
        await _initializationTask;
        using var connection = new SqliteConnection(file);
        var fightersJson = JsonSerializer.Serialize(player.fighters ?? [], jsonOptions);
        var inventoryJson = JsonSerializer.Serialize(player.inventory ?? [], jsonOptions);
        if (player.selectedFighter?.key is null) {
            Console.WriteLine("Selected fighter is null somehow");
            return false;
        }
        try {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE PlayerData
                SET 
                    PlayerName = @PlayerName,
                    Level = @Level,
                    XP = @XP,
                    MaxSP = @MaxSP,
                    MaxHP = @MaxHP,
                    SP = @CurrentSP,
                    HP = @CurrentHP,
                    XPForLevelUp = @XPForLevelUp,
                    Money = @Money,
                    FightersJson = @FightersJson,
                    InventoryJson = @InventoryJson,
                    SelectedFighterKey = @SelectedFighterKey
                WHERE UserId = @UserId; 
            ";

            command.Parameters.AddWithValue("@PlayerName", player.name);
            command.Parameters.AddWithValue("@Level", player.level);
            command.Parameters.AddWithValue("@XP", player.xp);
            command.Parameters.AddWithValue("@MaxSP", player.maxSp);
            command.Parameters.AddWithValue("@MaxHP", player.maxHp);
            command.Parameters.AddWithValue("@CurrentSP", player.sp);
            command.Parameters.AddWithValue("@CurrentHP", player.hp);
            command.Parameters.AddWithValue("@XPForLevelUp", player.xpForLevelUp);
            command.Parameters.AddWithValue("@Money", player.money);
            command.Parameters.AddWithValue("@FightersJson", fightersJson);
            command.Parameters.AddWithValue("@InventoryJson", inventoryJson);
            command.Parameters.AddWithValue("@SelectedFighterKey", player.selectedFighter.key);
            command.Parameters.AddWithValue("@UserId", player.id);
            await command.ExecuteNonQueryAsync();
            return true;
        } catch (Exception e) {
            Console.WriteLine(e);
            return false;
        }
    }
    public static async Task<SqliteDataReader> CustomQuery(SqliteCommand command) {
        var connection = new SqliteConnection(file);
        try {
            await connection.OpenAsync();
            command.Connection = connection;
            var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
            return reader;
        } catch (Exception e) {
            Console.WriteLine(e);
            await connection.DisposeAsync();
            throw;
        }
    }
}
public record User(long id, string username, string hashedPassword, Database.UserPermissions permissions);
public class StoredFighter {
    public required string name;
    public required string key;
    public string? image;
    public required string type;
    public required int level;
    public int xp;
    public int xpForLevelUp;
    public required int strength;
    public required int dexterity;
    public required int magic;
    public required int luck;
    public required List<string> skills;
    public Queue<int>? newSkillLevels;
    public Queue<string>? learnableSkills;
    public Fighter ConvertToFighter(Dictionary<string, Skill> skillJson) {
        Dictionary<string, Skill> skillDictionary = [];
        Queue<Skill>? skillQueue = [];
        foreach (string skillKey in skills) {
            if (!skillJson.TryGetValue(skillKey, out Skill? value)) {
                throw new Exception("Error transferring skills");
            }
            skillDictionary.Add(skillKey, value);
            skillQueue.Enqueue(value);
        }
        return new Fighter {
            name = name,
            key = key,
            image = image,
            type = type,
            level = level,
            xp = xp,
            xpForLevelUp = xpForLevelUp,
            strength = strength,
            dexterity = dexterity,
            magic = magic,
            luck = luck,
            skills = skillDictionary,
            newSkillLevels = newSkillLevels ?? [],
            learnableSkills = skillQueue ?? []
        };
    }
}