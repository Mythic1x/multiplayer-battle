using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static WebSocketHelpers;
public class BattleWebSocket {
    static readonly JsonSerializerOptions jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };
    public static async Task handleMessages(WebSocket webSocket, HttpContext context, bool? reconnect, SessionCache? session, ConcurrentDictionary<string, GameSession> battleSessions, ConcurrentDictionary<string, SessionCache> sessionConnections, string cookieId) {
        var buffer = new byte[1024 * 4];
        string? sessionId = null;
        var cts = new CancellationTokenSource();
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None
        );
        if (reconnect is true) {
            await HandleReconnect(webSocket, session!, cts);
        }
        while (!receiveResult.CloseStatus.HasValue) {
            if (session is not null) {
                session.lastSeen = DateTime.Now;
            }
            if (receiveResult.MessageType == WebSocketMessageType.Text) {
                var jsonString = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var message = JsonSerializer.Deserialize<SocketMessage>(jsonString, jsonOptions);
                if (message is null || message.id is null) {
                    return;
                }
                sessionId = message.id;
                switch (message.type) {
                    case "connect":
                        if (!sessionConnections.TryGetValue(cookieId, out _)) await HandleConnect(webSocket, message, battleSessions, context, sessionConnections, cookieId);
                        break;
                    case "action":
                        await HandleAction(message, battleSessions);
                        break;
                    default:
                        Console.WriteLine("Unknown message type received.");
                        break;
                }
            }
            receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None
        );
        }
        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None
        );
        if (sessionId is null) return;
        if (battleSessions.TryGetValue(sessionId, out var currentSession)) {
            var key = currentSession.connections.FirstOrDefault(kvp => kvp.Value == webSocket).Key;
            if (key != null) {
                currentSession.connections.TryRemove(key, out _);
                var messagePayload = new MessagePayload {
                    message = $"{key} disconnected"
                };
                var message = new ServerMessage<MessagePayload>("disconnect", messagePayload);
                await currentSession.Broadcast(message.ToJSON());
            }
            if (currentSession.connections.IsEmpty) {
                battleSessions.TryRemove(sessionId, out _);
                sessionConnections.TryRemove(cookieId, out _);
                return;
            }
        }

        var delayDeletion = Task.Delay(TimeSpan.FromSeconds(60), cts.Token);
        try {
            await delayDeletion;
            sessionConnections.TryRemove(cookieId, out _);
            if (currentSession is not null) {
                var messagePayload = new MessagePayload {
                    message = $"Opponent has forfeit. Ending session"
                };
                var message = new ServerMessage<MessagePayload>("opponentquit", messagePayload);
                await currentSession.Broadcast(message.ToJSON());
            }
        } catch (TaskCanceledException) {
            Console.WriteLine("player reconnected");
        } finally {
            cts.Dispose();
        }
        return;
    }
}

public record SessionCache {
    public required string assignment;
    public required string player;
    public required GameSession session;
    public required string id;
    public DateTime lastSeen;
}

public record SocketMessage {
    public required string id;
    public required string type;
    public JsonElement payload;
}
public record ConnectMessagePayload {
    public required string player;
    public required Player playerData;
}
public record ActionPayload {
    public required string action;
    public string? skill;
    public string? item;
    public string? fighter;
}
public class ServerMessage<TPayload>(string messageType, TPayload messagePayload) {
    public string type = messageType;
    public TPayload payload = messagePayload;
    public string ToJSON() {
        var jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };
        return JsonSerializer.Serialize(this, jsonOptions);
    }
}
public record ReconnectPayload {
    public required Battle state;
    public required string assignment;
}
public record StatePayload {
    public required Battle state;
    public string? message;
    public Dictionary<string, List<string>>? buffInfo;
}
public record MessagePayload {
    public required string message;
}
public record ErrorPayload {
    public required string errorType;
    public required string errorMessage;
}
public record EndPayload {
    public required string winner;
    public required string loser;
    public required string message;
    public required Battle state;
}
public class GameSession {
    public OrderedDictionary<string, Player> players = [];
    public ConcurrentDictionary<string, WebSocket> connections = [];
    public Battle? battle;
    public async Task Broadcast(string message) {
        var bytes = Encoding.UTF8.GetBytes(message);
        var arraySegment = new ArraySegment<byte>(bytes);
        foreach (var connection in connections.Values) {
            if (connection.State != WebSocketState.Open) continue;
            await connection.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
    public async Task SendToPlayer(string message, string player) {
        var bytes = Encoding.UTF8.GetBytes(message);
        var arraySegment = new ArraySegment<byte>(bytes);
        await connections[player].SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
    }
    public async Task SendEndNotification(Player winner, Player loser) {
        var endPayload = new EndPayload {
            winner = winner.name,
            loser = loser.name,
            message = $"{winner.name} has won the battle!",
            state = battle!,
        };
        var endMessage = new ServerMessage<EndPayload>("end", endPayload);
        await Broadcast(endMessage.ToJSON());
        players = [];
    }
    public async Task SendErrorToClient(string type, string message, WebSocket client) {
        var errorPayload = new ErrorPayload {
            errorType = type,
            errorMessage = message
        };
        var errorMsg = new ServerMessage<ErrorPayload>("error", errorPayload).ToJSON();
        var bytes = Encoding.UTF8.GetBytes(errorMsg);
        var arraySegment = new ArraySegment<byte>(bytes);
        await client.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
    }
    public async Task BroadcastAction(string message, Dictionary<string, List<string>>? buffInfo) {
        var statePayload = new StatePayload {
            state = battle!,
            message = message,
            buffInfo = buffInfo
        };
        var actionMessage = new ServerMessage<StatePayload>("stateUpdate", statePayload);
        await Broadcast(actionMessage.ToJSON());
    }
}
