

using System.Net.WebSockets;
using static HelperFunctions;

public class Player {
    public int level;
    public int xp;
    public required string name;
    public int maxSp;
    public int maxHp;
    public int xpForLevelUp;
    public int money;
    public required Dictionary<string, Fighter> fighters;
    public required Dictionary<string, Item> inventory;
    public required Fighter selectedFighter;
    public int sp;
    public int hp;

    /* public Player(int lvl, int xpNumber, int maxSpNumber, int maxHpNumber, int xpForLevelUpNumber, int heldMoney, List<Fighter> ownedFighters, Dictionary<string, Item> ownedItems, Fighter selected, int spNumber, int hpNumber) {
         level = lvl;
         xp = xpNumber;
         maxSp = maxSpNumber;
         maxHp = maxHpNumber;
         xpForLevelUp = xpForLevelUpNumber;
         money = heldMoney;
         fighters = ownedFighters;
         inventory = ownedItems;
         selectedFighter = selected;
         sp = spNumber;
         hp = hpNumber;
     }*/

    public string ToLevelUp {
        get => xp >= xpForLevelUp ? "Can level up!" : $"{xpForLevelUp - xp} XP until level up";
    }
    public Buffs buffs = new();
    public Buffs debuffs = new();
    public bool defending = false;
    public bool reflectingMagic = false;
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
    public required string method;
    public required string elementType;
    public required string description;
    public required string name;
    public readonly record struct BuffValues(int Amount, int Length, string StatToBuff);
    public BuffValues? buffValues;
    public string Property(Player player, Player opponent) {
        return method switch {
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
