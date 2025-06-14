import { Routes, Route, BrowserRouter } from "react-router-dom"
import { LoginPage } from "./pages/login"
import { SignUpPage } from "./pages/sign-up"
import { BattlePage } from "./pages/battle"
import { ProtectedRoute } from "./components/ProtectedRoute"
import { AuthProvider } from "./AuthContext"
import { HomePage } from "./pages/home"
import ProfilePage from "./pages/profile"

const NotFound = () => {
  return (
    <div className="not-found">
      I think you're lost
    </div>
  )
}

function App() {
  return (
    <>
      <BrowserRouter>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<LoginPage></LoginPage>}></Route>
            <Route path="/sign-up" element={<SignUpPage></SignUpPage>}></Route>
            <Route path="/" element={<HomePage></HomePage>}></Route>
            <Route path="/battle/:roomId" element={
              <ProtectedRoute>
                <BattlePage />
              </ProtectedRoute>
            }
            ></Route>
            <Route path="/profile/:userId" element={
              <ProtectedRoute>
                <ProfilePage own={false}></ProfilePage>
              </ProtectedRoute>}>
            </Route>
            <Route path="/profile" element={
              <ProtectedRoute><ProfilePage own={true}></ProfilePage></ProtectedRoute>
            }>

            </Route>
            <Route path="*" element={<NotFound></NotFound>}></Route>
          </Routes>
        </AuthProvider>
      </BrowserRouter>
    </>
  )
}



export default App
