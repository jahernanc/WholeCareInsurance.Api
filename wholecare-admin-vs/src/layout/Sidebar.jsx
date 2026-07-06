import { Link } from "react-router-dom";

const menu = [
    { path: "/", label: "Dashboard", icon: "📊" },
    { path: "/customers", label: "Customers", icon: "👤" },
    { path: "/policies", label: "Policies", icon: "📄" },
];

function Sidebar() {
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
            {menu.map(item => (
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