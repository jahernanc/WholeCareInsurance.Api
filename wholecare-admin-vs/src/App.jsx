import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import Login from "./Login";
import ChangePasswordForced from "./ChangePasswordForced";
import ForgotPassword from "./ForgotPassword";
import ResetPassword from "./ResetPassword";
import AppLayout from "./layout/AppLayout";
import Customers from "./pages/Customers";
import Policies from "./pages/Policies";
import Agentes from "./pages/Agentes";
import Profile from "./pages/Profile";
import { isAdmin } from "./api";

function Dashboard() {
    const { t } = useTranslation("common");
    return <h1>{t("dashboard.title")} ✅</h1>;
}

function App() {
    return (
        <BrowserRouter>
            <Routes>

                {/* login */}
                <Route path="/login" element={<Login />} />

                {/* gestión de contraseña: sin AppLayout, igual que /login */}
                <Route path="/change-password" element={<ChangePasswordForced />} />
                <Route path="/forgot-password" element={<ForgotPassword />} />
                <Route path="/reset-password" element={<ResetPassword />} />

                {/* redirect si no hay token, o si falta cambiar la contraseña asignada */}
                <Route
                    path="*"
                    element={
                        !localStorage.getItem("accessToken")
                            ? <Navigate to="/login" replace />
                            : localStorage.getItem("mustChangePassword") === "true"
                                ? <Navigate to="/change-password" replace />
                                : <AppLayout />
                    }
                >
                    {/* rutas internas */}
                    <Route index element={<Dashboard />} />
                    <Route path="customers" element={<Customers />} />
                    <Route path="policies" element={<Policies />} />
                    <Route
                        path="agentes"
                        element={isAdmin() ? <Agentes /> : <Navigate to="/" replace />}
                    />
                    <Route path="profile" element={<Profile />} />
                </Route>

            </Routes>
        </BrowserRouter>
    );
}

export default App;