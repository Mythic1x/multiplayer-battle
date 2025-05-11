using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using static HelperFunctions;

public class Battle(Player p1, Player p2) {
    public int turn = 1;
    public Player player1 = p1;
    public Player player2 = p2;
    [JsonIgnore]
    public Player GoingPlayer {
        get => turn % 2 == 0 ? player2 : player1;
    }
    [JsonIgnore]
    public Player OpposingPlayer {
        get => GoingPlayer == player1 ? player2 : player1;
    }
    Dictionary<string, List<string>> EndTurn() {
        var buffInfo = new Dictionary<string, List<string>>();
        var buffList = HandleBuffsAndDebuffs(GoingPlayer, "buff");
        var debuffList = HandleBuffsAndDebuffs(GoingPlayer, "debuff");
        if (buffList.Count > 0) {
            buffInfo.Add(GoingPlayer.name, buffList);
        }
        if (debuffList.Count > 0) {
            buffInfo.Add(GoingPlayer.name, debuffList);
        }
        turn++;
        return buffInfo;
    }
    public (bool, Player?) CheckForEnd() {
        if (player1.hp <= 0) {
            //not finished
            return (true, player2);
        } else if (player2.hp <= 0) {
            //not finished
            return (true, player1);
        } else return (false, null);
    }
    private void resetBattle() {
        player1.hp = player1.maxHp;
        player1.sp = player1.maxSp;
        player2.hp = player2.maxHp;
        player2.sp = player2.maxSp;
        player1.buffs = new Buffs();
        player1.reflectingMagic = false;
        player1.reflectingPhysical = false;
        player2.buffs = new Buffs();
        player2.reflectingMagic = false;
        player2.reflectingPhysical = false;
    }

    static List<string> HandleBuffsAndDebuffs(Player player, string type) {
        var expired = new List<string>();

        if (type == "buff") {
            if (!player.buffs.buffed) return expired;
            foreach (var buff in player.buffs) {
                if (buff.amount > 0) {
                    buff.length--;
                    if (buff.length <= 0) {
                        buff.amount = 0;
                        expired.Add($"{player.name}'s {buff.stat} buff has expired");
                    }
                }
            }
            bool buffed = player.buffs.Any(b => b.amount > 0);
            if (!buffed) player.buffs.buffed = false;
        }

        if (type == "debuff") {
            if (!player.debuffs.buffed) return expired;
            foreach (var debuff in player.debuffs) {
                if (debuff.amount > 0) {
                    debuff.length--;
                    if (debuff.length <= 0) {
                        debuff.amount = 0;
                        expired.Add($"{player.name}'s {debuff.stat} debuff has expired");
                    }
                }
            }
            bool debuffed = player.debuffs.Any(b => b.amount > 0);
            if (!debuffed) player.debuffs.buffed = false;
        }

        return expired;
    }

    public (string message, bool ended, Player? winner, Dictionary<string, List<string>>? buffInfo) HandleStrike() {
        //todo make better when weapons are added
        OpposingPlayer.hp -= GoingPlayer.selectedFighter.strength;
        string message = $"{GoingPlayer.name} attacks {OpposingPlayer.name} for {GoingPlayer.selectedFighter.strength} damage";
        var (endCheck, winner) = CheckForEnd();
        if (endCheck) {
            return ($"{winner!.name} has won", true, winner, null);
        }
        var buffInfo = EndTurn();
        return (message, false, null, buffInfo);
    }

    public (string message, Dictionary<string, List<string>>? buffInfo) HandleDefend() {
        GoingPlayer.defending = true;
        string message = $"{GoingPlayer.name} defends";
        var buffInfo = EndTurn();
        return (message, buffInfo);
    }

    public (string message, bool ended, Player? winner, Dictionary<string, List<string>>? buffInfo) HandleSkill(Skill skill) {
        string message = skill.Property(GoingPlayer, OpposingPlayer);
        CheckForError(message);
        if (CheckForError(message)) {
            return (message, false, null, null);
        }
        var (endCheck, winner) = CheckForEnd();
        if (endCheck) {
            return ($"{winner!.name} has won", true, winner, null);
        }
        var buffInfo = EndTurn();
        return (message, false, null, buffInfo);
    }

    public (string message, bool ended, Player? winner, Dictionary<string, List<string>>? buffInfo) HandleItem(Item item) {
        string message = item.Property(GoingPlayer, OpposingPlayer);
        CheckForError(message);
        if (CheckForError(message)) {
            return (message, false, null, null);
        }
        GoingPlayer.DeleteFromInventory(item.name.ToLower());
        var (endCheck, winner) = CheckForEnd();
        if (endCheck) {
            return ($"{winner!.name} has won", true, winner, null);
        }
        var buffInfo = EndTurn();
        return (message, false, null, buffInfo);
    }
    public string HandleSelectFighter(string name) {
        if (GoingPlayer.fighters.TryGetValue(name, out Fighter? fighter)) {
            GoingPlayer.selectedFighter = fighter;
            return $"{GoingPlayer.name} has selected {fighter.name}";
        } else {
            Console.WriteLine(name);
            return $"Error: {name} is not a valid fighter/you do not own {name}";
        }
    }
    public bool CheckForError(string message) {
        if (message.StartsWith("Error:")) {
            return true;
        }
        return false;
    }
}
