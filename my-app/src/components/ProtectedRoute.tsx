import { ReactNode } from "react";
import { useAuth } from "../AuthContext";
import { Navigate } from "react-router-dom";

export function ProtectedRoute({ children }: { children: ReactNode }) {
    const { user } = useAuth()
    if(!user) {
        return <Navigate to={"/login"} replace></Navigate>
    }
    return children;
}