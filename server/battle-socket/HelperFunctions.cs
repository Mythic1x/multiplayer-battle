public static class HelperFunctions {
    static readonly Random rand = new();
    public static int Range(int lowest, int highest) {
        return rand.Next(lowest, highest + 1);
    }
    public static bool RandomChanceCheck(double chance) {
        if (chance > 1 || chance < 0) {
            throw new Exception("chance must be between 0 and 1");
        }
        double result = rand.NextDouble();
        return chance >= result;
    }
    public static int CalculateDamage(Fighter fighter, Player attacker, Player defender, Skill skill) {
        int damage = Range(skill.damage[0], skill.damage[1]);
        int toAdd = 0;
        int toReduce = 0;

        //calculate toAdd
        if (attacker.buffs.attack.amount != 0) {
            toAdd += (int)(attacker.buffs.attack.amount * (Range(50, 200) / 100d));
        }
        if (fighter.type == skill.type) {
            toAdd += fighter.type switch {
                "magic" => (int)(fighter.magic * 0.5),
                "physical" => (int)(fighter.strength * 0.5),
                _ => 0
            };
        }
        if (attacker.debuffs.attack.amount != 0) {
            toAdd -= (int)(attacker.debuffs.attack.amount * (Range(50, 200) / 100d));
        }

        //calculate toReduce
        if (defender.buffs.defense.amount != 0) {
            toReduce += (int)(defender.buffs.defense.amount * (Range(50, 200) / 100d));
        }
        if (defender.defending) {
            toReduce += (int)(damage * 0.25);
        }
        if (defender.debuffs.defense.amount != 0) {
            toReduce -= (int)(defender.debuffs.defense.amount * (Range(50, 200) / 100d));
        }

        return damage + toAdd - toReduce;
    }
    public static string HandleAttack(Player player, Player opponent, Skill skill) {
        double dodgeChance = opponent.selectedFighter.dexterity * 0.5 / 100;
        double critChance = player.selectedFighter.luck * 0.3 / 100;
        bool isMagic = skill.type == "magic";
        bool isPhysical = skill.type == "physical";
        int cost;
        ref int pool = ref player.sp; // Default to player.sp
        string poolName;

        if (isMagic) {
            cost = skill.spCost ?? throw new InvalidOperationException("Magic cost is null");
            poolName = "SP";
        } else {
            cost = skill.hpCost ?? throw new InvalidOperationException("HP cost is null");
            pool = ref player.hp;
            poolName = "HP";
        }
       
        if (cost > pool)
            return $"Error: You do not have enough {poolName} to use {skill.name}";

        pool -= cost;
        int damage = CalculateDamage(player.selectedFighter, player, opponent, skill);

        if (opponent.defending) opponent.defending = false;

        if ((opponent.reflectingMagic && isMagic) || opponent.reflectingPhysical && isPhysical) {
            player.hp -= damage;
            if (isMagic) opponent.reflectingMagic = false;
            else opponent.reflectingPhysical = false;
            return $"{player.name} reflected {skill.name} back at {opponent.name} for {damage} damage";
        } else if (RandomChanceCheck(dodgeChance)) {
            return $"{player.name} missed {opponent.name} with {skill.name}";
        } else if (RandomChanceCheck(critChance)) {
            damage = (int)(damage * 1.5);
            opponent.hp -= damage;
            return $"Critical hit! {player.name} hit {opponent.name} with {skill.name} for {damage} damage";
        } else {
            opponent.hp -= damage;
            return $"{player.name} hit {opponent.name} with {skill.name} for {damage} damage";
        }
    }
    public static string HandleHeal(Player player, Skill skill) {
        if (skill.spCost == null) {
            throw new Exception("Skill cost is null");
        }
        if (skill.spCost > player.sp) {
            return $"Error: You do not have enough SP to use {skill.name}";
        }
        ;
        int healAmount = Range(skill.damage[0], skill.damage[1]);
        player.sp -= (int)skill.spCost;

        if ((player.hp + healAmount) > player.maxHp) {
            player.hp = player.maxHp;
        } else player.hp += healAmount;
        return $"{player.name} healed for {healAmount} with {skill.name}";
    }
    public static string HandleBuff(Player player, Skill skill) {
        if (skill.spCost == null) {
            throw new Exception("Skill cost is null");
        }
        if (skill.spCost > player.sp) return $"Error: You do not have enough SP to use this {skill.name}";
        player.sp -= (int)skill.spCost;
        Skill.BuffValues toBuff;

        if (skill.buffValues is not null) {
            toBuff = skill.buffValues.Value;
        } else throw new Exception("Buff values for support skill is null");

        switch (toBuff.StatToBuff) {
            case "attack":
                if (player.buffs.buffed && player.buffs.attack.length > 0) {
                    return "Error: Your attack is already buffed";
                }
                player.buffs.attack.amount = toBuff.Amount;
                player.buffs.attack.length = toBuff.Length;
                break;
            case "defense":
                if (player.buffs.buffed && player.buffs.defense.length > 0) {
                    return "Error: Your defense is already buffed";
                }
                player.buffs.defense.amount = toBuff.Amount;
                player.buffs.defense.length = toBuff.Length;
                break;
            case "dexterity":
                if (player.buffs.buffed && player.buffs.dexterity.length > 0) {
                    return "Error: Your dexterity is already buffed";
                }
                player.buffs.dexterity.amount = toBuff.Amount;
                player.buffs.dexterity.length = toBuff.Length;
                break;
        }

        return $"{player.name} buffed their {toBuff.StatToBuff} with {skill.name}";
    }
    public static string HandleDebuff(Player player, Player opponent, Skill skill) {
        if (skill.spCost == null) {
            throw new Exception("Skill cost is null");
        }
        if (skill.spCost > player.sp) return $"Error: You do not have enough SP to use this {skill.name}";
        player.sp -= (int)skill.spCost;
        Skill.BuffValues toDebuff;

        if (skill.buffValues is not null) {
            toDebuff = skill.buffValues.Value;
        } else throw new Exception("Debuff values for support skill is null");

        switch (toDebuff.StatToBuff) {
            case "attack":
                if (opponent.debuffs.attack.length > 0) {
                    return $"{opponent.name}'s attack is already debuffed";
                }
                opponent.debuffs.attack.amount = toDebuff.Amount;
                opponent.debuffs.attack.length = toDebuff.Length;
                break;
            case "defense":
                if (opponent.debuffs.defense.length > 0) {
                    return $"{opponent.name}'s defense is already debuffed";
                }
                opponent.debuffs.defense.amount = toDebuff.Amount;
                opponent.debuffs.defense.length = toDebuff.Length;
                break;
            case "dexterity":
                if (opponent.debuffs.dexterity.length > 0) {
                    return $"{opponent.name}'s dexterity is already debuffed";
                }
                opponent.debuffs.dexterity.amount = toDebuff.Amount;
                opponent.debuffs.dexterity.length = toDebuff.Length;
                break;
        }

        return $"{player.name} debuffed {opponent.name}'s {toDebuff.StatToBuff} with {skill.name}";
    }
}

