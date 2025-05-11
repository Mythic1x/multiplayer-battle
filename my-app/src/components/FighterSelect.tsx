import {Fighter } from "../vite-env"
import FighterCard from "./FighterCard"

interface Props {
    fighters: Record<string, Fighter> 
    showFightersMenu: React.Dispatch<React.SetStateAction<boolean>> 
}

function FighterSelect({ fighters, showFightersMenu }: Props) {
    return (
        <>
            <div className="fighters-container">
                <span className="delete-button" onClick={() => {showFightersMenu(false)}}>X</span>
                {Object.values(fighters).map(item =>
                    <FighterCard fighter={item} showFightersMenu={showFightersMenu}/>
                )}
            </div>
        </>
    )
}

export default FighterSelect