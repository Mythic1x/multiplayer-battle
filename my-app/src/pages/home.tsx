import { useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { useState } from "react";
export function HomePage() {
    const { user, logout, isAuthenticated } = useAuth()
    const [creatingRoom, setCreatingRoom] = useState(false)
    const navigate = useNavigate()
    async function createBattleRoom() {
        try {
            setCreatingRoom(true)
            const res = await fetch(`http://${window.location.hostname}:5050/createroom`, { credentials: "include" })
            if (!res.ok) {
                alert("Error creating room")
                throw new Error(await res.text())
            }
            const roomId = await res.json()
            console.log(roomId)
            navigate(`/battle/${roomId}`)
            setCreatingRoom(false)
        } catch (error: any) {
            alert(error.toString())
            setCreatingRoom(false)
        }
    }

    return (
        <div className="home">
            {!isAuthenticated ?
                <>
                    <div className="guest-container">
                        <button className="sign-up-button" onClick={() => navigate("/sign-up")}>Sign Up</button>
                        <button className="login-button" onClick={() => navigate("/login")}>Login</button>
                    </div>
                </>
                :
                <>
                    <h1 className="welcome-text">Welcome {user!.username}!</h1>
                    <div className="logged-in-container">
                        <button className="battle-button" onClick={createBattleRoom} disabled={creatingRoom}>Battle</button>
                        <button className="logout-button" onClick={() => {
                            logout()
                        }}>Log Out</button>
                    </div>
                </>}
        </div>
    )
}

