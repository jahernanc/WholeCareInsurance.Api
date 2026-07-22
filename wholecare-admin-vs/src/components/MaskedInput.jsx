import { useState } from "react";

const wrapperStyle = { position: "relative" };
const eyeButtonStyle = {
    position: "absolute",
    right: 6,
    top: "50%",
    transform: "translateY(-50%)",
    background: "transparent",
    border: "none",
    cursor: "pointer",
    fontSize: 14,
    padding: 2,
};

// Input de texto sensible (SSN, número de ruta, número de cuenta) con botón de
// mostrar/ocultar — arranca oculto (type="password") y alterna a type="text".
// No existía ningún patrón previo de este tipo en el proyecto; se diseñó para
// Supplemental (§12.9, RoutingNumber/AccountNumber) y se retrofiteó a SSN.
function MaskedInput({ name, value, onChange, required = false, style }) {
    const [visible, setVisible] = useState(false);

    return (
        <div style={wrapperStyle}>
            <input
                type={visible ? "text" : "password"}
                name={name}
                value={value}
                onChange={onChange}
                required={required}
                style={{ ...style, paddingRight: 32 }}
            />
            <button
                type="button"
                onClick={() => setVisible((v) => !v)}
                style={eyeButtonStyle}
                tabIndex={-1}
            >
                {visible ? "🙈" : "👁"}
            </button>
        </div>
    );
}

export default MaskedInput;
