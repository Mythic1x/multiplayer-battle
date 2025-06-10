import { useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
export function HomePage() {
    const { user, logout, isAuthenticated } = useAuth()
    const navigate = useNavigate()


    return (
        <div className="home">
            {!isAuthenticated ?
                <><button className="sign-up-button" onClick={() => navigate("/sign-up")}>Sign Up</button>
                    <button className="login-button" onClick={() => navigate("/login")}>Login</button></>
                :
                <><h1 className="welcome-text">welcome {user!.username}</h1>
                    <button className="battle-button" onClick={() => navigate("/battle")}>Battle</button>
                    <button className="logout-button" onClick={() => {
                        logout()
                    }}>Log Out</button>
                </>}
        </div>
    )
}