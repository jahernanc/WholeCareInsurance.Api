import { useState } from "react";
import { maskValue } from "../utils/maskValue";

const buttonStyle = {
    background: "transparent",
    border: "none",
    cursor: "pointer",
    fontSize: 13,
    padding: 0,
    marginLeft: 6,
};

// Versión de solo lectura de MaskedInput — muestra el valor enmascarado (últimos
// 4 caracteres visibles) con botón de mostrar/ocultar, para vistas de detalle y
// listados (tarjetas de Customers, detalle de Policy, lista de dependientes).
function MaskedText({ value }) {
    const [visible, setVisible] = useState(false);

    return (
        <span>
            {visible ? (value ?? "-") : maskValue(value)}
            <button type="button" onClick={() => setVisible((v) => !v)} style={buttonStyle} tabIndex={-1}>
                {visible ? "🙈" : "👁"}
            </button>
        </span>
    );
}

export default MaskedText;
