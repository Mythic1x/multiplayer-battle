import NumberFlow from "@number-flow/react"
import { ActionPayload, Fighter, Message, Player } from "../vite-env"
import { useState, useRef, useEffect, useContext, act } from "react"
import { useGameState } from "../GameContext"
import useWebSocket from "react-use-websocket"
import { socketUrl } from "../pages/battle.tsx"
import { SendJsonMessage } from "react-use-websocket/dist/lib/types"
import { useParams } from "react-router-dom"


interface Props {
    health: number
    sp: number
    showItemsMenu: React.Dispatch<React.SetStateAction<boolean>>
    showSkillsMenu: React.Dispatch<React.SetStateAction<boolean>>
    showFightersMenu: React.Dispatch<React.SetStateAction<boolean>>
    isP2: boolean
}

function BattleStats({ health, sp, showFightersMenu, showSkillsMenu, showItemsMenu, isP2 }: Props) {
    const { sendJsonMessage } = useWebSocket(socketUrl, { share: true })
    const getHealthColor = (health: number) => {
        if (health > 50) {
            return "limegreen"
        } else if (health > 25) {
            return "yellow"
        } else {
            return "red"
        }
    }
    const flash = flashWhenChange(health)
    return (
        <>
            <div className="heath-text-container">
                <span className={`health-text`}>Health: </span>
                <NumberFlow value={health} className={`health-value ${flash === 'decrease' ? "flash-red" : ""} ${flash === 'increase' ? "flash-green" : ""}`} />
            </div>
            <div className="health-bar-container" style={isP2 ? { position: "relative" } : undefined}>
                <div className="health-bar" style={{ width: `${health}px`, backgroundColor: getHealthColor(health), ...(isP2 ? { position: "absolute", right: 0 } : {}) }}></div>
            </div>
            <div className="sp-text-container">
                <span className="sp">SP: </span>
                <NumberFlow value={sp} />
            </div>
            <div className="sp-bar-container" style={isP2 ? { position: "relative" } : undefined}>
                <div className="sp-bar" style={{ width: `${sp}px`, ...(isP2 ? { position: "absolute", right: 0 } : {}) }}></div>
            </div>
            <div className="button-container">
                <AttackButton sendJsonMessage={sendJsonMessage} />
                <DefendButton sendJsonMessage={sendJsonMessage} />
                <button className="select-fighter" style={{ backgroundColor: "#6B7280" }} onClick={() => {
                    showFightersMenu(prev => !prev)
                    showSkillsMenu(false)
                    showItemsMenu(false)
                }}>Change Fighter</button>
                <button className="show-skills" style={{ backgroundColor: "#7C3AED" }} onClick={() => {
                    showSkillsMenu(prev => !prev)
                    showFightersMenu(false)
                    showItemsMenu(false)
                }}>Use Skill</button>
                <button className="show-items" style={{ backgroundColor: "#D97706" }} onClick={() => {
                    showItemsMenu(prev => !prev)
                    showFightersMenu(false)
                    showSkillsMenu(false)
                }}>Use Item</button>
            </div>
        </>
    )
}

function AttackButton({ sendJsonMessage }: { sendJsonMessage: SendJsonMessage }) {
    const { player, turn, playerTurn, roomId } = useGameState()
    return (
        <button className="attack" style={{ backgroundColor: "#B91C1C" }} onClick={() => {
            const payload = handleAttackOrDefend(player, playerTurn, "attack", roomId)
            sendJsonMessage(payload)
        }} disabled={player !== playerTurn}>Attack</button>
    )
}

function DefendButton({ sendJsonMessage }: { sendJsonMessage: SendJsonMessage }) {
    const { player, turn, playerTurn, roomId } = useGameState()
    return (
        <button className="defend" style={{ backgroundColor: "#059669" }} onClick={() => {
            const payload = handleAttackOrDefend(player, playerTurn, "defend", roomId)
            sendJsonMessage(payload)
        }} disabled={player !== playerTurn}>Defend</button>
    )
}

function handleAttackOrDefend(player: Player, playerTurn: Player, type: "attack" | "defend", roomId: string) {
    if (player !== playerTurn) {
        return alert("not your turn stop cheating")
    }
    const actionPayload: Message = {
        id: roomId,
        type: "action",
        payload: {
            action: type
        }
    }
    return actionPayload
}

export function flashWhenChange(value: number) {
    const [flash, setFlashType] = useState<"increase" | "decrease" | false>(false)
    const ref = useRef(value)
    useEffect(() => {
        if (ref.current !== value) {
            const type = ref.current > value ? "decrease" : "increase"
            setFlashType(type)
            setTimeout(() => setFlashType(false), 1000)
            ref.current = value
        }
    }, [value])
    return flash
}

export default BattleStats