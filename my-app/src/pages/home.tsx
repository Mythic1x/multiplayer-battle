import { useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { FormEvent, useRef, useState } from "react";
export function HomePage() {
    const { user, logout, isAuthenticated } = useAuth()
    const [creatingRoom, setCreatingRoom] = useState(false)
    const [roomCode, setRoomCode] = useState('')
    const navigate = useNavigate()
    const dialog = useRef<HTMLDialogElement | null>(null)
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

    function handleJoinRoom(e: FormEvent) {
        e.preventDefault()
        dialog.current?.close()
        navigate(`/battle/${roomCode}`)
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
                        <button className="join-button" onClick={() => dialog.current?.showModal()}>Join Room</button>
                        <button className="logout-button" onClick={() => {
                            logout()
                        }}>Log Out</button>
                        <dialog className="join-room" ref={dialog} onClick={(e) => {
                            if(e.target === dialog.current) dialog.current?.close()
                        }}>
                            <form onSubmit={handleJoinRoom} className="join-room-form">
                                <input type="text" value={roomCode} placeholder="Enter Room Code" onChange={(e) => setRoomCode(e.target.value)} />
                                <div className="dialog-button-container">
                                    <button className="dialog-buttons" type="submit" disabled={!roomCode}>Join Room</button>
                                    <button className="dialog-buttons" type="button" onClick={() => dialog.current?.close()}>Close</button>
                                </div>

                            </form>
                        </dialog>
                    </div>
                </>}
        </div>
    )
}

