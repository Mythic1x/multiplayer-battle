import NumberFlow from "@number-flow/react"
import { flashWhenChange } from "./BattleStats"


interface Props {
    health: number
    sp: number
    isP2: boolean
}

function OpponentStats({ health, sp, isP2 }: Props) {
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
                <div className="health-bar" style={{ width: `${health}%`, backgroundColor: getHealthColor(health), ...(isP2 ? { position: "absolute", right: 0 } : {}) }}></div>
            </div>
            <div className="sp-text-container">
                <span className="sp">SP: </span>
                <NumberFlow value={sp} />
            </div>
            <div className="sp-bar-container" style={isP2 ? { position: "relative" } : undefined}>
                <div className="sp-bar" style={{ width: `${sp}%`, ...(isP2 ? { position: "absolute", right: 0 } : {}) }}></div>
            </div>
        </>
    )
}

export default OpponentStats