/// <reference types="vite/client" />
type ElementType = "physical" | "fire" | "water" | "ice" | "lightning" | "support"
type FighterType = "physical" | "magic"

export interface Fighter {
    strength: number
    dexterity: number
    magic: number
    luck: number
    image: string
    skills: Record<string, skill>
    level: number
    name: string
    type: FighterType
    elementType: ElementType
}


interface Skill {
    damage: number[]
    name: string
    spCost?: number,
    hpCost?: number
    type: "physical" | "magic"
    elementType: ElementType
    method: "attack" | "heal" | "buff" | "debuff"
    description: string
    buffValues?: { amount: number, length: number, stat: string }
}

interface Player {
   id: number;
    name: string;
    level: number;
    xp: number;
    maxSp: number;
    maxHp: number;
    xpForLevelUp: number;
    money: number;
    fighters: Record<string, Fighter>;
    inventory: Record<string, Item>;
    selectedFighter: Fighter;
    sp: number;
    hp: number;
}

interface User {
    username: string
    //placeholder
}

interface GameState {
    player1: Player
    player2: Player
    turn: number
}

type Message = {
    id: number,
    type: "connect" | "action"
    payload: ConnectPayload | ActionPayload
}

type ConnectPayload = {
    playerName: string,
    playerData: Player,
}

type ActionPayload =
    | { action: "useSkill", skill: string }
    | { action: "defend" | "attack" }
    | { action: "useItem", item: string }
    | { action: "selectFighter", fighter: string }

type Item =
    | { type: "heal" | "damage" | "sp" | "debuff", effectAmount: number, description: string, owned: number, name: string, description: string }
    | { type: "reflect", reflectType: string, description: string, owned: number, name: string, description: string }


type ServerStatePayload = {
    state: Battle
    message?: string
    buffInfo?: Record<string, string[]>
}

type ServerMessagePayload = {
    message: string
}

type ServerErrorPayload = {
    errorType: string
    errorMessage: string
}

type ServerEndPayload = {
    winner: string
    loser: string
    message: string
    state: Battle
}
type ReconnectPayload = {
    state: Battle,
    assignment: string
}

type ServerMessage =
    | { type: 'assignment' | 'connection' | 'disconnect' | 'reconnection', payload: ServerMessagePayload }
    | { type: 'start' | 'stateUpdate', payload: ServerStatePayload }
    | { type: 'error', payload: ServerErrorPayload }
    | { type: 'end', payload: ServerEndPayload }
    | { type: "reconnect", payload: ReconnectPayload }

