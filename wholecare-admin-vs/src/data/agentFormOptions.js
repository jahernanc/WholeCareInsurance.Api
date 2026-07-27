export const CONTRACT_INTERESTS = ["Medicare", "Obamacare", "Supplemental Plans", "Life Insurance"];

// Agencias reales del sistema anterior (§15.1) — solo 2 confirmadas, validadas server-side.
export const AGENCIES = ["Whole Care Insurance Group llC", "Preventive Health Insurance"];

export const emptyAgentForm = {
    nombre: "",
    email: "",
    password: "",
    rol: "Agente",
    isEncargado: false,
    isActive: true,
    middleName: "",
    gender: "",
    agency: "",
    address1: "",
    address2: "",
    city: "",
    zipCode: "",
    state: "",
    county: "",
    licensed: false,
    licenseNumber: "",
    npnNumber: "",
    npnOverride: false,
    hasCompanyContract: false,
    contractNumber: "",
    companyName: "",
    contractsWanted: [],
    additionalInformation: "",
    termsAccepted: false,
};
