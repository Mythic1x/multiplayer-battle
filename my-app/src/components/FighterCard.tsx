
import useWebSocket from "react-use-websocket"
import { useGameState } from "../GameContext"
import { ActionPayload, Fighter, Message } from "../vite-env"
import { gameId, socketUrl } from "../pages/battle.tsx"


interface Props {
    fighter: Fighter
    showFightersMenu: React.Dispatch<React.SetStateAction<boolean>> 
}



function FighterCard({ fighter, showFightersMenu }: Props) {
    const { sendJsonMessage } = useWebSocket(socketUrl, {share: true})
    const { player, turn, playerTurn } = useGameState()
    return (
        <>
            <button className="fighter-card" onClick={() => {
                const payload = handleSelectFighter(fighter)
                sendJsonMessage(payload)
                showFightersMenu(false)
             }}
                disabled={player !== playerTurn} style={player !== playerTurn ? { cursor: "not-allowed" } : undefined}>
                <div className="fighter-info-container">
                    <img src={fighter.image} className="fighter-pic" />
                    <span className="fighter-name">{fighter.name}</span>
                    <span className="level">Level: {fighter.level}</span>
                </div>
                <div className="skills-container">
                    <span className="skills">Skills: </span>
                    {Object.values(fighter.skills).map(item =>
                        <span className="skill">{item.name}</span>
                    )}
                </div>
            </button>
        </>
    )
}

function handleSelectFighter(fighter: Fighter) {
    const payload: ActionPayload = {
        action: "selectFighter",
        fighter: fighter.name
    }
    const message: Message = {
        id: gameId,
        type: "action",
        payload: payload
    }
    return message
}

export default FighterCard