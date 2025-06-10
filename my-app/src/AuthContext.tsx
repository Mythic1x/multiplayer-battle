import { createContext, ReactNode, useContext, useEffect, useState } from "react";
import { User } from "./vite-env";
import { useNavigate } from "react-router-dom";
type AuthContextType = {
    user: User | undefined;
    login: (username: string, password: string) => Promise<void>
    logout: () => Promise<void>;
    isAuthenticated: boolean;
};


export const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | undefined>(undefined)
    const [loading, setLoading] = useState(true);
    const navigate = useNavigate()

    const checkAuthention = async () => {
        try {
            const response = await fetch(`http://${window.location.hostname}:5050/user`, { credentials: "include" })
            if (response.status === 200) {
                return response.json()
            } else {
                return false;
            }
        } catch (error: any) {
            console.log(error.toString())
            setLoading(false)
        }
    }

    useEffect(() => {
        checkAuthention().then(user => {
            if (user) setUser(user)
            setLoading(false)
        })
    }, [])

    const login = async (username: string, password: string) => {
        const payload = {
            username: username,
            password: password
        }
        try {
            const response = await fetch(`http://${window.location.hostname}:5050/login`, { method: "POST", body: JSON.stringify(payload), credentials: "include", headers: { "Content-type": "application/json" } })
            if (response.status !== 200) {
                const error = await response.text()
                throw new Error(error)
            }
            const userJson = await response.json()
            setUser(userJson)
            navigate("/")
        } catch (error: any) {
            console.log(error.toString())
            throw new Error(error.toString());
        }

    }

    const logout = async () => {
        try {
            const response = await fetch(`http://${window.location.hostname}:5050/logout`, { credentials: "include" })
            if (response.status === 200) {
                setUser(undefined)
            }
        } catch (error: any) {
            console.log(error.toString())
            throw new Error(error);

        }
    }
    const contextValue = {
        user,
        login,
        logout,
        isAuthenticated: !!user
    }
    if (loading) {
        return (
            <div className="loading">Loading...</div>
        )
    }
    return (
        <AuthContext.Provider value={contextValue}>
            {children}
        </AuthContext.Provider>
    );
}
export const useAuth = () => {
    const context = useContext(AuthContext)
    if (!context) {
        throw new Error("Undefined context")
    }
    return context
}