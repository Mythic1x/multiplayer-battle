
import { Fighter } from "../vite-env"
import SkillCard from "./SkillCard"

interface Props {
    fighter: Fighter
    sp: number
    hp: number
    showSkillsMenu: React.Dispatch<React.SetStateAction<boolean>>
}

function SkillMenu({ fighter, sp, hp, showSkillsMenu }: Props) {
    return (
        <>
            <div className="skill-menu-container">
                <span className="delete-button" onClick={() => { showSkillsMenu(false) }}>X</span>
                {Object.values(fighter.skills).sort((a, b) => b.damage[0] - a.damage[0]).map(item =>
                    <SkillCard skill={item} sp={sp} hp={hp} showSkillsMenu={showSkillsMenu} />
                )}
            </div>
        </>
    )
}

export default SkillMenu