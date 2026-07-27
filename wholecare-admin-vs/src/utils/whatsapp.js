// Link de click-to-chat de WhatsApp (§4.2, extendido en §17 a Customers) —
// limpia el teléfono de cualquier formato/máscara antes de armar la URL.
export function buildWhatsAppUrl(phone, message) {
    const digits = phone.replace(/\D/g, "");
    return `https://wa.me/${digits}?text=${encodeURIComponent(message)}`;
}
