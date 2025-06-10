import { useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
export function HomePage() {
    const { user, logout, isAuthenticated } = useAuth()
    const navigate = useNavigate()


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
                        <button className="battle-button" onClick={() => navigate("/battle")}>Battle</button>
                        <button className="logout-button" onClick={() => {
                            logout()
                        }}>Log Out</button>
                    </div>
                </>}
        </div>
    )
}