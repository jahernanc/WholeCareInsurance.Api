import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import commonEn from "./locales/en/common.json";
import loginEn from "./locales/en/login.json";
import customersEn from "./locales/en/customers.json";
import policiesEn from "./locales/en/policies.json";
import agentesEn from "./locales/en/agentes.json";
import enumsEn from "./locales/en/enums.json";

import commonEs from "./locales/es/common.json";
import loginEs from "./locales/es/login.json";
import customersEs from "./locales/es/customers.json";
import policiesEs from "./locales/es/policies.json";
import agentesEs from "./locales/es/agentes.json";
import enumsEs from "./locales/es/enums.json";

// El idioma persiste en el backend (User.PreferredLanguage), pero para
// pintar la UI correcta antes de esperar cualquier respuesta de red, se
// arranca síncronamente desde este cache. AppLayout reconcilia contra el
// backend en segundo plano apenas monta.
const cachedLanguage = localStorage.getItem("preferredLanguage");

i18n.use(initReactI18next).init({
    lng: cachedLanguage === "es" ? "es" : "en",
    fallbackLng: "en",
    defaultNS: "common",
    ns: ["common", "login", "customers", "policies", "agentes", "enums"],
    resources: {
        en: {
            common: commonEn,
            login: loginEn,
            customers: customersEn,
            policies: policiesEn,
            agentes: agentesEn,
            enums: enumsEn,
        },
        es: {
            common: commonEs,
            login: loginEs,
            customers: customersEs,
            policies: policiesEs,
            agentes: agentesEs,
            enums: enumsEs,
        },
    },
    interpolation: { escapeValue: false },
});

export default i18n;
