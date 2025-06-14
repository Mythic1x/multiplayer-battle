import { Fighter } from "../vite-env"

interface Props {
    fighter: Fighter
}
export default function FighterProfile({ fighter }: Props) {
    return (
        <div className="fighter-profile-container">
            <div className="fighter-info-container">
                <span className="fighter-name">{fighter.name} • </span>
                <span className="fighter-level">Level: {fighter.level} • </span>
                <span className="fighter-element">Type: {fighter.type}</span>
            </div>
            {fighter.image ?
                <img src={fighter.image} alt="" className="fighter-image" />
                :
                <span className="no-image">No Fighter Image</span>
            }
            <span className="skills-text">Skills:</span>
            <ol className="skills-list">{Object.values(fighter.skills).sort((a, b) => b.damage[0] - a.damage[0]).map(skill => (
                <li className="skill" title={skill.description}>{skill.name}</li>
            ))}</ol>
        </div>
    )
}