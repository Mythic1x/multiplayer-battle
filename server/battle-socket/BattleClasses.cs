using System.Text.Json.Serialization;
using static HelperFunctions;

public class Player {
    public long id;
    public int level;
    public int xp;
    public string name;
    public int maxSp;
    public int maxHp;
    public int xpForLevelUp;
    public int money;
    public Dictionary<string, Fighter> fighters;
    public  Dictionary<string, Item> inventory;
    public  Fighter selectedFighter;
    public int sp;
    public int hp;

    public Player(long id, string name, int level, int xp, int maxSp, int maxHp, int xpForLevelUp, int money, Dictionary<string, Fighter> fighters, Dictionary<string, Item> inventory, Fighter selectedFighter, int sp, int hp) {
        this.id = id;
        this.name = name;
        this.level = level;
        this.xp = xp;
        this.maxSp = maxSp;
        this.maxHp = maxHp;
        this.xpForLevelUp = xpForLevelUp;
        this.money = money;
        this.fighters = fighters;
        this.inventory = inventory;
        this.selectedFighter = selectedFighter;
        this.sp = sp;
        this.hp = hp;
    }

    public string ToLevelUp {
        get => xp >= xpForLevelUp ? "Can level up!" : $"{xpForLevelUp - xp} XP until level up";
    }
    [JsonIgnore]
    public Buffs buffs = new();
    [JsonIgnore]
    public Buffs debuffs = new();
    [JsonIgnore]
    public bool defending = false;
    [JsonIgnore]
    public bool reflectingMagic = false;
    [JsonIgnore]
    public bool reflectingPhysical = false;

    public bool LevelUp() {
        if (xp < xpForLevelUp) {
            return false;
        }
        maxHp += 20;
        maxSp += 20;
        level++;
        xpForLevelUp *= 2;
        return true;
    }
    public void AddToInventory(Item item) {
        var itemName = item.name.ToLower();
        if (inventory.TryGetValue(itemName, out Item? value)) {
            value.owned++;
            return;
        }
        inventory.Add(itemName, item);
        inventory[itemName].owned = 1;
    }
    public void DeleteFromInventory(string itemName) {
        if (inventory.TryGetValue(itemName, out Item? value)) {
            value.owned--;
            if (value.owned <= 0) {
                inventory.Remove(itemName);
            }
        }
    }
}

public class Buffs : IEnumerable<Buffs.StatEffects> {
    public bool buffed = false;
    public class StatEffects {
        public int amount = 0;
        public int length = 0;
        public string stat = string.Empty;
    };
    public StatEffects attack = new() { stat = "attack" };
    public StatEffects defense = new() { stat = "defense" };
    public StatEffects dexterity = new() { stat = "dexterity" };

    public IEnumerator<StatEffects> GetEnumerator() {
        yield return attack;
        yield return defense;
        yield return dexterity;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}

public class Fighter {
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
    public required Dictionary<string, Skill> skills;
    public Queue<int>? newSkillLevels;
    public Queue<Skill>? learnableSkills;
    public static bool LevelUp(Fighter fighter) {
        if (fighter.newSkillLevels == null || fighter.learnableSkills == null) {
            throw new Exception("Learnable skills are not initialized");
        }
        if (fighter.xp < fighter.xpForLevelUp) return false;
        if (fighter.type == "physical") {
            fighter.strength += 5;
            fighter.magic += 2;
            fighter.dexterity += 5;
            fighter.luck += 2;

        } else {
            fighter.magic += 5;
            fighter.dexterity += 2;
            fighter.luck += 5;
            fighter.strength += 2;
        }
        fighter.level++;
        fighter.xpForLevelUp *= 2;
        if (fighter.newSkillLevels.Count > 0 && fighter.level == fighter.newSkillLevels.Peek()) {
            fighter.newSkillLevels.Dequeue();
            if (fighter.learnableSkills.TryDequeue(out Skill? value)) {
                fighter.skills[value.name] = value;
            }
        } else {
            throw new Exception("Learnable skills amount does not match learnable skill levels");
        }
        return true;
    }
}

public class Skill {
    public required int[] damage;
    public int? spCost;
    public int? hpCost;
    public required string type;
    public required string action;
    public required string elementType;
    public required string description;
    public required string name;
    public readonly record struct BuffValues(int Amount, int Length, string StatToBuff);
    public BuffValues? buffValues;
    public string Property(Player player, Player opponent) {
        return action switch {
            "attack" => HandleAttack(player, opponent, this),
            "heal" => HandleHeal(player, this),
            "buff" => HandleBuff(player, this),
            "debuff" => HandleDebuff(player, opponent, this),
            _ => "Error: Invalid method",
        };
    }
}

public class Item {
    public required string name;
    public required string description;
    public int price;
    public required string type;
    public int maxAmount;
    public int? effectAmount;
    public string? reflectType;
    public int owned;
    public string Property(Player player, Player opponent) {
        if (owned <= 0) {
            return "Error: You do not have this item";
        }
        switch (type) {
            case "heal" when effectAmount is int amount:
                if (player.hp + amount > player.maxHp) {
                    amount = player.maxHp - player.hp;
                }
                player.hp += amount;
                return $"{player.name} used {name} and healed for {amount} HP";
            case "attack" when effectAmount is int amount:
                opponent.hp -= amount;
                return $"{player.name} used {name} and dealt {amount} damage to {opponent.name}";
            case "sp" when effectAmount is int amount:
                if (player.sp + amount > player.maxSp) {
                    amount = player.maxSp - player.sp;
                }
                player.sp += amount;
                return $"{player.name} used {name} and restored {amount} SP";
            case "reflect" when reflectType is string reflect:
                if (reflect == "physical") {
                    player.reflectingPhysical = true;
                } else if (reflect == "magic") {
                    player.reflectingMagic = true;
                }
                return $"{player.name} used {name} and is now reflecting {reflect}";
            default: return "Error: Invalid item type";
        }
    }
}
