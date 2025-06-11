using System.Text.Json;
using System.Net.WebSockets;
using System.Collections.Concurrent;
public static class WebSocketHelpers {
    static readonly JsonSerializerOptions jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };
    public static async Task HandleConnect(WebSocket webSocket, SocketMessage message, ConcurrentDictionary<string, GameSession> battleSessions, HttpContext context, ConcurrentDictionary<string, SessionCache> sessionConnections, string cookieId) {
        if (sessionConnections.TryGetValue(cookieId, out _)) {
            return;
        }
        
        var data = JsonSerializer.Deserialize<ConnectMessagePayload>(message.payload, jsonOptions);
        if (data == null) return;
        string player = data.player;
        Player? playerData = data.playerData;
        if (playerData == null) return;

        if (!battleSessions.TryGetValue(message.id, out GameSession? session)) {
            return;
        } else if (session.players.Count > 2) {
            await session.SendErrorToClient("FullSession", "session is full", webSocket);
            return;
        }
        session.connections.TryAdd(player, webSocket);
        var assignment = $"player{session.connections.Count}";
        session.players.TryAdd(player, playerData);
        sessionConnections[cookieId] = new SessionCache {
            assignment = assignment,
            player = player,
            session = session,
            lastSeen = DateTime.UtcNow,
            id = message.id
        };
        var messagePayload = new MessagePayload {
            message = $"{player} connected!"
        };

        var connectionMsg = new ServerMessage<MessagePayload>("connection", messagePayload);
        await session.Broadcast(connectionMsg.ToJSON());

        var assignmentPayload = new MessagePayload {
            message = assignment
        };
        var assignmentMsg = new ServerMessage<MessagePayload>("assignment", assignmentPayload);
        await session.SendToPlayer(assignmentMsg.ToJSON(), player);

        if (session.connections.Values.Count > 1) {
            var players = session.players.Values.ToList();
            session.battle = new Battle(players[0], players[1]);
            var statePayload = new StatePayload {
                state = session.battle
            };
            var stateMsg = new ServerMessage<StatePayload>("start", statePayload);
            await session.Broadcast(stateMsg.ToJSON());
        }
    }
    public static async Task HandleReconnect(WebSocket webSocket, SessionCache session, CancellationTokenSource cts) {
        cts.Cancel();
        session.lastSeen = DateTime.UtcNow;
        session.session.connections[session.player] = webSocket;
        var reconnectPayload = new ReconnectPayload {
            state = session.session.battle!,
            assignment = session.assignment,
        };
        var reconnectAlertPayload = new MessagePayload {
            message = $"{session.player} reconnected!"
        };
        var reconnectAlertMessage = new ServerMessage<MessagePayload>("reconnection", reconnectAlertPayload);
        var reconnectMsg = new ServerMessage<ReconnectPayload>("reconnect", reconnectPayload);
        await session.session.SendToPlayer(reconnectMsg.ToJSON(), session.player);
        await session.session.Broadcast(reconnectAlertMessage.ToJSON());
    }
    public static async Task HandleAction(SocketMessage message, ConcurrentDictionary<string, GameSession> battleSessions) {
        var data = JsonSerializer.Deserialize<ActionPayload>(message.payload, jsonOptions);
        if (data == null) return;
        Battle? battle = battleSessions[message.id].battle;
        if (battle == null) return;
        var player = battle.GoingPlayer;
        var opponent = battle.OpposingPlayer;
        var session = battleSessions[message.id];

        if (data.action == "attack") {
            var (battleMessage, ended, winner, buffInfo) = battle.HandleStrike();

            if (ended) {
                await session.SendEndNotification(winner!, opponent);
                return;
            }

            await session.BroadcastAction(battleMessage, buffInfo);
        } else if (data.action == "defend") {
            var (battleMessage, buffInfo) = battle.HandleDefend();
            await session.BroadcastAction(battleMessage, buffInfo);
            return;

        } else if (data.action == "useSkill") {
            if (data.skill == null) {
                await session.SendErrorToClient("MissingData", "Skill not provided", battleSessions[message.id].connections[player.name]);
                return;
            }
            if (!player.selectedFighter.skills.TryGetValue(data.skill.ToLower(), out Skill? skill)) {
                await session.SendErrorToClient("InvalidAction", "No Skill Found", battleSessions[message.id].connections[player.name]);
                return;
            }
            var (battleMessage, ended, winner, buffInfo) = battle.HandleSkill(skill);

            if (battleMessage.StartsWith("Error:")) {
                await session.SendErrorToClient("InvalidAction", battleMessage, battleSessions[message.id].connections[player.name]);
                return;
            }

            if (ended) {
                await session.SendEndNotification(winner!, opponent);
                return;
            }

            await session.BroadcastAction(battleMessage, buffInfo);

        } else if (data.action == "useItem") {
            if (data.item == null) {
                await session.SendErrorToClient("MissingData", "Item not provided", battleSessions[message.id].connections[player.name]);
                return;
            }
            if (!player.inventory.TryGetValue(data.item.ToLower(), out Item? item)) {
                await session.SendErrorToClient("InvalidAction", "No Item Found", battleSessions[message.id].connections[player.name]);
                return;
            }

            var (battleMessage, ended, winner, buffInfo) = battle.HandleItem(item);

            if (battleMessage.StartsWith("Error:")) {
                await session.SendErrorToClient("InvalidAction", battleMessage, battleSessions[message.id].connections[player.name]);
                return;
            }

            if (ended) {
                await session.SendEndNotification(winner!, opponent);
                return;
            }

            await session.BroadcastAction(battleMessage, buffInfo);

        } else if (data.action == "selectFighter") {
            if (data.fighter == null) {
                await session.SendErrorToClient("MissingData", "Fighter not provided", battleSessions[message.id].connections[player.name]);
                return;
            }
            var fighterName = data.fighter;
            var battleMessage = battle.HandleSelectFighter(fighterName);

            if (battleMessage.StartsWith("Error:")) {
                await session.SendErrorToClient("InvalidAction", battleMessage, battleSessions[message.id].connections[player.name]);
                return;
            }

            await session.BroadcastAction(battleMessage, null);
        } else {
            await session.SendErrorToClient("InvalidAction", "Invalid action type", battleSessions[message.id].connections[player.name]);
        }
    }
}
