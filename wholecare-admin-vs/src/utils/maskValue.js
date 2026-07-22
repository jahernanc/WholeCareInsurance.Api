// Enmascara un valor sensible (SSN, número de cuenta, número de ruta) dejando
// visibles solo los últimos 4 caracteres — mismo criterio en MaskedInput/MaskedText
// y en el texto estático del <option> del dropdown de titular (que no admite
// componentes interactivos, ver Policies.jsx).
export function maskValue(value) {
    if (!value) return "-";
    const str = String(value);
    if (str.length <= 4) return "•".repeat(str.length);
    return "•".repeat(str.length - 4) + str.slice(-4);
}
