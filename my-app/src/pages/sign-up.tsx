import { FormEvent, useState } from "react"
import { useAuth } from "../AuthContext"

export function SignUpPage() {
    const [username, setUsername] = useState("")
    const [password, setPassword] = useState("")
    const [message, setMessage] = useState("")
    const [starterFighter, setStarterFighter] = useState("")
    const [signingUp, setSigningUp] = useState(false)
    const { login } = useAuth()
    const handleSignUp = async (e: FormEvent) => {
        if (!username || !password || !starterFighter) {
            setMessage("All fields are required");
            return;
        }
        e.preventDefault()
        const payload = {
            username: username,
            password: password,
            //placeholder
            starterFighter: starterFighter
        }
        try {
            setMessage("Signing Up")
            setSigningUp(true)
            const res = await fetch(`http://${window.location.hostname}:5050/register`, { method: "POST", body: JSON.stringify(payload), headers: { "Content-type": "application/json" } })
            if (!res.ok) {
                setMessage(await res.text())
                setSigningUp(false)
            }
            setMessage("success")
            setSigningUp(false)
            await login(username, password)
        } catch (error: any) {
            console.log(error)
            setSigningUp(false)
        }
    }
    return (
        <div className="form-container">
            <span className="signup-submit-text">
                {message}
            </span>
            <form onSubmit={handleSignUp} className="signup-form">
                <input type="text" className="username-input" value={username} placeholder="Username" onChange={(e) => setUsername(e.target.value)} />
                <input type="password" className="password-input" value={password} placeholder="Password" onChange={(e) => setPassword(e.target.value)} />
                <select name="fighters" id="fighter-select" className="fighter-select" onChange={(e) => setStarterFighter(e.target.value)}>
                    <option value="">Select a fighter</option>
                    <option value="fireGuy">FireGuy</option>
                    <option value="waterGuy">WaterGuy</option>
                    <option value="iceGuy">IceGuy</option>
                    <option value="physicalGuy">PhysicalGuy</option>
                    <option value="lightningGuy">LightningGuy</option>
                    <option value="trashGuy">TrashGuy</option>
                </select>
                <button className="submit-button" type="submit" disabled={signingUp || !username || !password || !starterFighter}>
                    Sign Up
                </button>

            </form>
        </div>
    )
}