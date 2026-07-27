import { useEffect, useRef } from "react";

const backdropStyle = {
    position: "fixed",
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    background: "rgba(0,0,0,0.5)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    zIndex: 1000,
};

const contentStyle = {
    background: "white",
    borderRadius: 10,
    padding: 24,
    width: "90%",
    maxHeight: "85vh",
    overflowY: "auto",
};

const FOCUSABLE_SELECTOR =
    'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

// Modal/Dialog genérico (§16) — mismo estilo visual (backdrop + centrado) que el
// modal de detalle ya existente en Policies.jsx, sumando lo que ese no tenía:
// cierre con Escape, scroll lock del body, foco atrapado y restaurado al cerrar.
function Modal({ open, onClose, maxWidth = 500, children }) {
    const contentRef = useRef(null);
    const previouslyFocusedRef = useRef(null);
    // onClose normalmente se pasa como arrow function inline (identidad nueva en
    // cada render del padre) — se guarda en un ref para que el efecto de abajo
    // no dependa de ella y no se re-ejecute (robando el foco) en cada tecleo.
    const onCloseRef = useRef(onClose);
    useEffect(() => {
        onCloseRef.current = onClose;
    });

    useEffect(() => {
        if (!open) return;

        previouslyFocusedRef.current = document.activeElement;

        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";

        const focusables = () =>
            contentRef.current
                ? Array.from(contentRef.current.querySelectorAll(FOCUSABLE_SELECTOR))
                : [];

        const first = focusables()[0];
        (first ?? contentRef.current)?.focus();

        const handleKeyDown = (e) => {
            if (e.key === "Escape") {
                e.stopPropagation();
                onCloseRef.current();
                return;
            }

            if (e.key !== "Tab") return;

            const items = focusables();
            if (items.length === 0) return;

            const firstItem = items[0];
            const lastItem = items[items.length - 1];

            if (e.shiftKey && document.activeElement === firstItem) {
                e.preventDefault();
                lastItem.focus();
            } else if (!e.shiftKey && document.activeElement === lastItem) {
                e.preventDefault();
                firstItem.focus();
            }
        };

        document.addEventListener("keydown", handleKeyDown, true);

        return () => {
            document.removeEventListener("keydown", handleKeyDown, true);
            document.body.style.overflow = previousOverflow;
            previouslyFocusedRef.current?.focus?.();
        };
    }, [open]);

    if (!open) return null;

    return (
        <div style={backdropStyle} onClick={onClose}>
            <div
                ref={contentRef}
                role="dialog"
                aria-modal="true"
                tabIndex={-1}
                onClick={(e) => e.stopPropagation()}
                style={{ ...contentStyle, maxWidth }}
            >
                {children}
            </div>
        </div>
    );
}

export default Modal;
