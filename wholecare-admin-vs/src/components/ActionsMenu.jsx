import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";

const triggerStyle = {
    background: "transparent",
    border: "none",
    cursor: "pointer",
    fontSize: 18,
    padding: "4px 8px",
    borderRadius: 4,
    lineHeight: 1,
};

const menuStyle = {
    position: "fixed",
    minWidth: 180,
    background: "white",
    borderRadius: 8,
    boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
    border: "1px solid #e5e7eb",
    padding: 4,
    zIndex: 2000,
};

const itemStyle = {
    display: "flex",
    alignItems: "center",
    gap: 8,
    width: "100%",
    padding: "8px 12px",
    background: "transparent",
    border: "none",
    borderRadius: 6,
    cursor: "pointer",
    fontSize: 14,
    textAlign: "left",
    whiteSpace: "nowrap",
    color: "#111827",
    textDecoration: "none",
};

// Menú de acciones de fila (⋮), reemplaza los íconos sueltos de Policies/Customers/
// Agentes. Se posiciona con position:fixed + createPortal a document.body (primer uso
// de portal en este código) en vez de quedar anidado en el <td> — así el contenedor
// overflowX:auto de la tabla nunca lo recorta ni lo desalinea, sea cual sea el scroll
// horizontal activo al momento de abrirlo. Se renderiza off-screen en el primer paint
// y se reposiciona en un useLayoutEffect (mide el tamaño real del menú antes de
// mostrarlo, no asume un ancho/alto fijo), así el usuario nunca ve el salto.
//
// items: [{ icon, label, onClick? , href?, external?, disabled? }]
function ActionsMenu({ items }) {
    const [open, setOpen] = useState(false);
    const [position, setPosition] = useState(null);
    const triggerRef = useRef(null);
    const menuRef = useRef(null);

    useLayoutEffect(() => {
        if (!open) return;
        const trigger = triggerRef.current;
        const menu = menuRef.current;
        if (!trigger || !menu) return;

        const margin = 8;
        const triggerRect = trigger.getBoundingClientRect();
        const menuRect = menu.getBoundingClientRect();

        let left = triggerRect.left;
        if (left + menuRect.width > window.innerWidth - margin) {
            left = triggerRect.right - menuRect.width;
        }
        left = Math.max(margin, left);

        let top = triggerRect.bottom + 4;
        if (top + menuRect.height > window.innerHeight - margin) {
            top = triggerRect.top - menuRect.height - 4;
        }
        top = Math.max(margin, top);

        setPosition({ top, left });
    }, [open]);

    useEffect(() => {
        if (!open) return;

        const handleClickOutside = (e) => {
            if (menuRef.current?.contains(e.target) || triggerRef.current?.contains(e.target)) return;
            setOpen(false);
        };
        const handleKeyDown = (e) => {
            if (e.key === "Escape") setOpen(false);
        };
        // capture:true en scroll porque los eventos de scroll no burbujean — así se
        // detecta igual el scroll horizontal del contenedor overflowX:auto de la tabla.
        const handleScroll = () => setOpen(false);

        document.addEventListener("mousedown", handleClickOutside);
        document.addEventListener("keydown", handleKeyDown);
        document.addEventListener("scroll", handleScroll, true);
        window.addEventListener("resize", handleScroll);

        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
            document.removeEventListener("keydown", handleKeyDown);
            document.removeEventListener("scroll", handleScroll, true);
            window.removeEventListener("resize", handleScroll);
        };
    }, [open]);

    const toggle = () => {
        if (!open) setPosition(null);
        setOpen((prev) => !prev);
    };

    const runAction = (item) => {
        setOpen(false);
        item.onClick?.();
    };

    return (
        <>
            <button
                ref={triggerRef}
                type="button"
                onClick={toggle}
                style={triggerStyle}
                aria-haspopup="menu"
                aria-expanded={open}
            >
                ⋮
            </button>

            {open &&
                createPortal(
                    <div
                        ref={menuRef}
                        role="menu"
                        style={{
                            ...menuStyle,
                            top: position?.top ?? -9999,
                            left: position?.left ?? -9999,
                            visibility: position ? "visible" : "hidden",
                        }}
                    >
                        {items.map((item, idx) =>
                            item.href ? (
                                <a
                                    key={idx}
                                    role="menuitem"
                                    href={item.href}
                                    target={item.external ? "_blank" : undefined}
                                    rel={item.external ? "noopener noreferrer" : undefined}
                                    onClick={() => setOpen(false)}
                                    style={itemStyle}
                                    onMouseEnter={(e) => (e.currentTarget.style.background = "#f3f4f6")}
                                    onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
                                >
                                    <span>{item.icon}</span>
                                    <span>{item.label}</span>
                                </a>
                            ) : (
                                <button
                                    key={idx}
                                    type="button"
                                    role="menuitem"
                                    disabled={item.disabled}
                                    onClick={() => runAction(item)}
                                    style={{ ...itemStyle, opacity: item.disabled ? 0.5 : 1, cursor: item.disabled ? "default" : "pointer" }}
                                    onMouseEnter={(e) => !item.disabled && (e.currentTarget.style.background = "#f3f4f6")}
                                    onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
                                >
                                    <span>{item.icon}</span>
                                    <span>{item.label}</span>
                                </button>
                            )
                        )}
                    </div>,
                    document.body
                )}
        </>
    );
}

export default ActionsMenu;
