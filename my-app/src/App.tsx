import { Routes, Route, BrowserRouter } from "react-router-dom"
import { LoginPage } from "./pages/login"
import { SignUpPage } from "./pages/sign-up"
import { BattlePage } from "./pages/battle"
import { ProtectedRoute } from "./components/ProtectedRoute"
import { AuthProvider } from "./AuthContext"
import { HomePage } from "./pages/home"

function App() {

  return (
    <>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<LoginPage></LoginPage>}></Route>
            <Route path="/sign-up" element={<SignUpPage></SignUpPage>}></Route>
            <Route path="/" element={<HomePage></HomePage>}></Route>
            <Route path="/battle" element={
              <ProtectedRoute>
                <BattlePage />
              </ProtectedRoute>
            }
            ></Route>
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </>
  )
}



export default App
