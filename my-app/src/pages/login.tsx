import { FormEvent, useState } from "react"
import { useAuth } from "../AuthContext"

export function LoginPage() {
    const [username, setUsername] = useState("")
    const [password, setPassword] = useState("")
    const [message, setMessage] = useState("")
    const [loggingIn, setLoggingIn] = useState(false)
    const { login } = useAuth()
    const handleLogin = async (e: FormEvent) => {
        if (!username || !password) {
            setMessage("All fields are required")
            return
        }
        e.preventDefault()
        try {
            setMessage("Logging in")
            setLoggingIn(true)
            await login(username, password)
            setMessage("Login successful")
            setLoggingIn(false)
        } catch (error: any) {
            setMessage(error.toString())
            setLoggingIn(false)
        }
    }
    return (
        <div className="form-container">
            <span className="login-submit-text">
                {message}
            </span>
            <form onSubmit={handleLogin} className="login-form">
                <input type="text" className="username-input" value={username} placeholder="Username" onChange={(e) => setUsername(e.target.value)} />
                <input type="password" className="password-input" value={password} placeholder="Password" onChange={(e) => setPassword(e.target.value)} />
                <button className="submit-button" type="submit" disabled={loggingIn || !password || !username}>
                    Login
                </button>
            </form>
        </div>
    )
}