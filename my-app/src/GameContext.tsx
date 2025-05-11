import { createContext, useContext, } from "react";
import { Player } from "./vite-env";

export const GameContext = createContext<{ player: Player, turn: number, playerTurn: Player } | undefined>(undefined)

//export function GameContextProvider({ children, player }: { children: ReactNode, player: "player1" | "player2" }) {
//
//}

export function useGameState() {
    const context = useContext(GameContext)
    if (!context) throw new Error('error using context')
    return context
}