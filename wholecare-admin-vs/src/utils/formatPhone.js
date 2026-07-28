// Formatea el string crudo (11 dígitos: código de país + área + número, sin formato)
// que ya guarda User.Phone/Customer.Phone (§17.1) como "+1 (XXX) XXX-XXXX" — solo para
// mostrar, en base sigue crudo. Si no matchea el patrón esperado se devuelve tal cual,
// para no ocultar datos con un formato distinto al importado.
export function formatPhoneDisplay(phone) {
    if (!phone) return null;
    const digits = phone.replace(/\D/g, "");

    if (digits.length === 11 && digits[0] === "1")
        return `+1 (${digits.slice(1, 4)}) ${digits.slice(4, 7)}-${digits.slice(7, 11)}`;

    if (digits.length === 10)
        return `+1 (${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6, 10)}`;

    return phone;
}
