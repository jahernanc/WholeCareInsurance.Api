import { Link } from "react-router-dom";
import { isAdmin } from "../api";

const menu = [
    { path: "/", label: "Dashboard", icon: "📊" },
    { path: "/customers", label: "Customers", icon: "👤" },
    { path: "/policies", label: "Policies", icon: "📄" },
    { path: "/agentes", label: "Agentes", icon: "🧑‍💼", adminOnly: true },
];

function Sidebar() {
    const userIsAdmin = isAdmin();

    return (
        <div style={{
            width: 70,
            background: "#111",
            color: "white",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            paddingTop: 10
        }}>
            {menu.filter(item => !item.adminOnly || userIsAdmin).map(item => (
                <Link
                    key={item.path}
                    to={item.path}
                    title={item.label} // ✅ tooltip nativo
                    style={{
                        color: "white",
                        textDecoration: "none",
                        marginBottom: 20,
                        fontSize: 20
                    }}
                >
                    {item.icon}
                </Link>
            ))}
        </div>
    );
}

export default Sidebar;