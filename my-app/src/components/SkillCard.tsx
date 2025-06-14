
import useWebSocket from "react-use-websocket"
import { useGameState } from "../GameContext"
import { ActionPayload, Message, Skill } from "../vite-env"
import { socketUrl } from "../pages/battle.tsx"

interface Props {
    skill: Skill
    sp: number
    hp: number
    showSkillsMenu: React.Dispatch<React.SetStateAction<boolean>>
}

function SkillCard({ skill, sp, hp, showSkillsMenu }: Props) {
    const { sendJsonMessage } = useWebSocket(socketUrl, { share: true })
    const { player, playerTurn, roomId } = useGameState()
    const resource = skill.type === "magic"
        ? { cost: skill.spCost, pool: sp, poolName: "SP" }
        : { cost: skill.hpCost, pool: hp, poolName: "HP" }

    const invalid = (resource.cost! > resource.pool || player !== playerTurn)
    return (
        <>
            <button className="skill-card" disabled={invalid} style={invalid ? { cursor: "not-allowed" } : undefined} title={skill.description} onClick={() => {
                const payload = handleSkill(skill, roomId)
                sendJsonMessage(payload)
                showSkillsMenu(false)
            }}>
                <span className="skill-name">{skill.name}</span>
                <span className="cost">Required {resource.poolName}: {resource.cost}</span>
            </button>
        </>
    )
}

function handleSkill(skill: Skill, roomId: string) {
    const payload: ActionPayload = {
        action: "useSkill",
        skill: skill.name
    }
    const message: Message = {
        id: roomId,
        type: "action",
        payload: payload
    }
    return message
}

export default SkillCard