import { useState } from "react";
import { Fighter } from "../vite-env";
import usePlayer from "../hooks/useplayer";
import Loading from "../components/Loading";
import FighterProfile from "../components/FighterProfile";
import { useParams } from "react-router-dom";
import { useAuth } from "../AuthContext";

interface Props {
    own: boolean
}

export default function ProfilePage({ own }: Props) {
    const { user } = useAuth();
    const params = useParams<{ userId: string }>();
    const userId = own ? user?.id : params.userId;
    if (!userId) return;

    const { player, loading, error } = usePlayer(userId)
    const [fighterScreen, setFighterScreen] = useState<Fighter | null>(null)

    if (loading) {
        return <Loading></Loading>
    }
    if (error) {
        return (
            <div className="error">Error: {error.toString()}</div>
        )
    }
    const fighters = player!.fighters
    const items = player!.inventory

    return (
        <div className="info-container">
            {fighterScreen && (<div className="fighter-profile-container">
                <FighterProfile fighter={fighterScreen}></FighterProfile>
            </div>)}
            <div className="fighters-list-container">
                <span className="fighters-list-label">Fighters:</span>
                <ul className="fighters-list">
                    {Object.values(fighters).map((fighter => (
                        <li className="fighter-entry" onClick={() => setFighterScreen(fighter)} style={{ cursor: "pointer" }}>
                            {fighter.name}
                        </li>
                    )))}
                </ul>
                <span className="items-list-label">Items:</span>
                <ul className="items-list">
                    {Object.values(items).map(item => (
                        <li className="item-entry">
                            {item.name}
                        </li>
                    ))}
                </ul>
            </div>
        </div>
    )
}