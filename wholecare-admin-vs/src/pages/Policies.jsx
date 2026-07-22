import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useOutletContext } from "react-router-dom";
import { apiFetch, isAdmin } from "../api";
import { translateEnum } from "../i18n/translateEnum";
import CustomerFormFields from "../components/CustomerFormFields";
import LifeInsuranceFields from "../components/LifeInsuranceFields";
import MaskedInput from "../components/MaskedInput";
import MaskedText from "../components/MaskedText";
import { maskValue } from "../utils/maskValue";
import { emptyCustomerForm } from "../data/customerFormOptions";

const POLICY_TYPES = ["Obama Care", "Medicare", "Life Insurance", "Supplemental Plans", "Auto", "Otro"];
const BANK_ACCOUNT_TYPES = ["Cheque", "Ahorros"];
const AUTO_PAYMENT_DAYS = Array.from({ length: 28 }, (_, i) => i + 1);
const POLICY_STATUSES = ["Draft", "Pendiente", "Cancelado", "Por procesar", "En proceso", "Actualizado", "Procesado", "Cambio de agente"];
const PLAN_TYPES = ["Catastrophic", "Bronze", "Silver", "Gold", "Platinum"];
const ALLOWED_DOCUMENT_EXTENSIONS = [".pdf", ".docx", ".jpg", ".jpeg"];
const MAX_DOCUMENT_SIZE_BYTES = 5 * 1024 * 1024;

const formatFileSize = (bytes) => `${(bytes / 1024).toFixed(2)} KB`;

const formatDocumentDate = (iso) => {
    const d = new Date(iso);
    const pad = (n) => String(n).padStart(2, "0");
    const month = pad(d.getMonth() + 1);
    const day = pad(d.getDate());
    const year = d.getFullYear();
    let hours = d.getHours();
    const ampm = hours >= 12 ? "PM" : "AM";
    hours = hours % 12 || 12;
    const minutes = pad(d.getMinutes());
    return `${month}/${day}/${year} ${pad(hours)}:${minutes} ${ampm}`;
};

function Policies() {
    const { t } = useTranslation(["policies", "common"]);
    const { period } = useOutletContext();
    const [policies, setPolicies] = useState([]);
    const [customers, setCustomers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);

    const [policyNumber, setPolicyNumber] = useState("");
    const [type, setType] = useState("");
    const [insuranceCompanies, setInsuranceCompanies] = useState([]);
    const [insuranceCompanyId, setInsuranceCompanyId] = useState("");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [premium, setPremium] = useState("");
    const [status, setStatus] = useState("Draft");
    const [customerId, setCustomerId] = useState("");
    // Período no es editable en el formulario: se toma del selector del header al
    // crear, y se conserva el valor ya guardado de la póliza al editar.
    const [formPeriod, setFormPeriod] = useState(period);
    const [numberOfApplicants, setNumberOfApplicants] = useState("");

    // Campos confirmados por el análisis del archivo real de migración (Health/Obamacare).
    const [planType, setPlanType] = useState("");
    const [insurancePlan, setInsurancePlan] = useState("");
    const [effectiveDate, setEffectiveDate] = useState("");
    const [taxCreditSubsidy, setTaxCreditSubsidy] = useState("");
    const [monthlyPremiumAmount, setMonthlyPremiumAmount] = useState("");

    // Campos específicos de Medicare (§12.10), visibles solo cuando type === "Medicare".
    // Se guardan como "" | "true" | "false" para poder representar el estado "sin elegir".
    const [hasMedicaid, setHasMedicaid] = useState("");
    const [medicaidLevel, setMedicaidLevel] = useState("");
    const [referredToMedicalCorporation, setReferredToMedicalCorporation] = useState("");
    const [medicalCorporation, setMedicalCorporation] = useState("");

    // Campos específicos de Life Insurance (§12.6), visibles solo cuando type === "Life Insurance".
    const [additionalOrAlternatePolicy, setAdditionalOrAlternatePolicy] = useState(false);
    const [additionalOrAlternatePolicyDetail, setAdditionalOrAlternatePolicyDetail] = useState("");
    const [underwritingRequirements, setUnderwritingRequirements] = useState("");
    const [needsMedicalRequirements, setNeedsMedicalRequirements] = useState(false);
    const [billingType, setBillingType] = useState("");
    const [premiumFrequency, setPremiumFrequency] = useState("");
    const [plannedPeriodicModalPremium, setPlannedPeriodicModalPremium] = useState("");
    const [sourceOfFunds, setSourceOfFunds] = useState("");
    const [hasExistingLifeInsurance, setHasExistingLifeInsurance] = useState(false);
    const [isReplacingExistingPolicy, setIsReplacingExistingPolicy] = useState(false);
    const [usingFundsFromInforcePolicy, setUsingFundsFromInforcePolicy] = useState(false);
    const [provideComparativeInfoForm, setProvideComparativeInfoForm] = useState(false);
    const [physicianName, setPhysicianName] = useState("");
    const [physicianAddress, setPhysicianAddress] = useState("");
    const [additionalInformation, setAdditionalInformation] = useState("");
    const [consentSigned, setConsentSigned] = useState(false);

    // "Datos Life Insurance del titular": sección inline en Policies.jsx que edita
    // directamente el Customer titular ya seleccionado (PUT /api/customers/{id}),
    // ya que el titular se elige por dropdown y no se crea/edita inline como los
    // dependientes (ver decisión de la sesión: nueva sección inline para el titular).
    const [titularLifeForm, setTitularLifeForm] = useState({
        age: "", countryOfBirth: "", height: "", weight: "",
        backDateToSaveAge: false, spentMoreThan4MonthsAbroad: false, militaryOrganizationMember: false,
        currentlyEmployed: "", hasDriverLicense: false, driverLicenseNumber: "",
        netWorth: "", householdIncome: "", householdNetWorth: "",
    });
    const [titularLifeError, setTitularLifeError] = useState("");
    const [savingTitularLife, setSavingTitularLife] = useState(false);

    // Beneficiarios (§12.6): sección propia de Policy, sin vínculo con Customer.
    const [beneficiaries, setBeneficiaries] = useState([]);
    const [showCreateBeneficiaryForm, setShowCreateBeneficiaryForm] = useState(false);
    const [newBeneficiaryForm, setNewBeneficiaryForm] = useState({
        typeOfRelationship: "", firstName: "", lastName: "", dateOfBirth: "",
        gender: "", phone: "", email: "", socialSecurityNumber: "",
    });
    const [beneficiaryError, setBeneficiaryError] = useState("");
    const [creatingBeneficiary, setCreatingBeneficiary] = useState(false);

    // Campos específicos de Supplemental Plans (§12.9), visibles solo cuando
    // type === "Supplemental Plans". EffectiveDate/InsuranceCompanyId/InsurancePlan/
    // MonthlyPremiumAmount ya existen y se reusan, no se duplican acá.
    const [hasExistingDentalCoverage, setHasExistingDentalCoverage] = useState(false);
    const [eligibleForMedicare, setEligibleForMedicare] = useState(false);
    const [isReplacingDentalCoverage, setIsReplacingDentalCoverage] = useState(false);
    const [insuredPaysThePremium, setInsuredPaysThePremium] = useState(false);
    const [bankAccountType, setBankAccountType] = useState("");
    const [routingNumber, setRoutingNumber] = useState("");
    const [accountNumber, setAccountNumber] = useState("");
    const [insuredIsAccountHolder, setInsuredIsAccountHolder] = useState(false);
    const [authorizedAutomaticPayment, setAuthorizedAutomaticPayment] = useState(false);
    const [autoPaymentDay, setAutoPaymentDay] = useState("");
    const [authorizeMarketingInfo, setAuthorizeMarketingInfo] = useState(false);
    const [representativeName, setRepresentativeName] = useState("");
    const [representativeRelationship, setRepresentativeRelationship] = useState("");

    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [editingId, setEditingId] = useState(null);

    const [dependents, setDependents] = useState([]);
    const [dependentQuery, setDependentQuery] = useState("");
    const [showDependentPicker, setShowDependentPicker] = useState(false);

    // Alta de un Customer nuevo directamente desde Dependientes (§2) — paridad de
    // campos con el formulario de Customers vía el mismo CustomerFormFields.
    const userIsAdmin = isAdmin();
    const [dependentAgents, setDependentAgents] = useState([]);
    const [showCreateDependentForm, setShowCreateDependentForm] = useState(false);
    const [newDependentForm, setNewDependentForm] = useState(emptyCustomerForm);
    const [newDependentError, setNewDependentError] = useState("");
    const [creatingDependent, setCreatingDependent] = useState(false);

    const [filterPolicyNumber, setFilterPolicyNumber] = useState("");
    const [filterFirstName, setFilterFirstName] = useState("");
    const [filterLastName, setFilterLastName] = useState("");
    const [filterStatus, setFilterStatus] = useState("");
    const [filterType, setFilterType] = useState("");
    const [filterInsuranceCompanyId, setFilterInsuranceCompanyId] = useState("");

    const [viewingPolicy, setViewingPolicy] = useState(null);
    const [detailDependents, setDetailDependents] = useState([]);
    const [detailBeneficiaries, setDetailBeneficiaries] = useState([]);
    const [detailDocuments, setDetailDocuments] = useState([]);
    const [documentError, setDocumentError] = useState("");
    const [uploadingDocument, setUploadingDocument] = useState(false);
    const [openDocMenuId, setOpenDocMenuId] = useState(null);

    const isMedicare = type === "Medicare";
    const isLifeInsurance = type === "Life Insurance";
    const isSupplemental = type === "Supplemental Plans";

    const getCustomer = (id) => customers.find((c) => c.id === Number(id));

    const getCustomerName = (id) => {
        const customer = customers.find((c) => c.id === Number(id));
        return customer ? `${customer.firstName} ${customer.lastName}` : "Unknown";
    };

    const getCustomerPhone = (id) => {
        const customer = customers.find((c) => c.id === Number(id));
        return customer ? customer.phone : null;
    };

    const buildWhatsAppUrl = (phone) => {
        const digits = phone.replace(/\D/g, "");
        const message = encodeURIComponent(t("whatsappMessage"));
        return `https://wa.me/${digits}?text=${message}`;
    };

    // Precarga los campos de Life Insurance del titular seleccionado cada vez que
    // cambia el customerId (alta o edición), para que "Guardar datos del titular"
    // parta siempre de los valores ya guardados en ese Customer.
    useEffect(() => {
        const c = getCustomer(customerId);
        setTitularLifeForm({
            age: c?.age ?? "",
            countryOfBirth: c?.countryOfBirth ?? "",
            height: c?.height ?? "",
            weight: c?.weight ?? "",
            backDateToSaveAge: c?.backDateToSaveAge ?? false,
            spentMoreThan4MonthsAbroad: c?.spentMoreThan4MonthsAbroad ?? false,
            militaryOrganizationMember: c?.militaryOrganizationMember ?? false,
            currentlyEmployed: c?.currentlyEmployed === null || c?.currentlyEmployed === undefined ? "" : String(c.currentlyEmployed),
            hasDriverLicense: c?.hasDriverLicense ?? false,
            driverLicenseNumber: c?.driverLicenseNumber ?? "",
            netWorth: c?.netWorth ?? "",
            householdIncome: c?.householdIncome ?? "",
            householdNetWorth: c?.householdNetWorth ?? "",
        });
        setTitularLifeError("");
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [customerId, customers]);

    const handleTitularLifeField = (e) => {
        const { name, value, type: inputType, checked } = e.target;
        setTitularLifeForm((f) => ({ ...f, [name]: inputType === "checkbox" ? checked : value }));
    };

    const handleSaveTitularLife = async () => {
        const base = getCustomer(customerId);
        if (!base) return;

        setTitularLifeError("");
        setSavingTitularLife(true);

        try {
            const body = {
                socialSecurityNumber: base.socialSecurityNumber,
                firstName: base.firstName,
                lastName: base.lastName,
                dateOfBirth: base.dateOfBirth?.slice(0, 10),
                email: base.email,
                address1: base.address1,
                phone: base.phone,
                migrationStatus: base.migrationStatus,
                relacionConPrincipal: base.relacionConPrincipal,
                zipCode: base.zipCode ?? "",
                state: base.state ?? "",
                city: base.city ?? "",
                county: base.county ?? "",
                maritalStatus: base.maritalStatus ?? "",
                occupation: base.occupation ?? "",
                agentId: base.agentId ?? null,
                assistantAgentId: base.assistantAgentId ?? null,
                recordAgentId: base.recordAgentId ?? null,
                middleName: base.middleName ?? "",
                gender: base.gender ?? "",
                greenCard: base.greenCard ?? "",
                workPermit: base.workPermit ?? "",
                address2: base.address2 ?? "",
                employerName: base.employerName ?? "",
                companyPhone: base.companyPhone ?? "",
                annualIncome: base.annualIncome ?? 0,
                tags: base.tags ?? "",
                contactLanguage: base.contactLanguage ?? "",
                age: titularLifeForm.age === "" ? null : Number(titularLifeForm.age),
                countryOfBirth: titularLifeForm.countryOfBirth,
                height: titularLifeForm.height,
                weight: titularLifeForm.weight,
                backDateToSaveAge: titularLifeForm.backDateToSaveAge,
                spentMoreThan4MonthsAbroad: titularLifeForm.spentMoreThan4MonthsAbroad,
                militaryOrganizationMember: titularLifeForm.militaryOrganizationMember,
                currentlyEmployed: titularLifeForm.currentlyEmployed === "" ? null : titularLifeForm.currentlyEmployed === "true",
                hasDriverLicense: titularLifeForm.hasDriverLicense,
                driverLicenseNumber: titularLifeForm.hasDriverLicense ? titularLifeForm.driverLicenseNumber : "",
                netWorth: titularLifeForm.netWorth === "" ? null : Number(titularLifeForm.netWorth),
                householdIncome: titularLifeForm.householdIncome === "" ? null : Number(titularLifeForm.householdIncome),
                householdNetWorth: titularLifeForm.householdNetWorth === "" ? null : Number(titularLifeForm.householdNetWorth),
            };

            const res = await apiFetch(`/api/customers/${customerId}`, {
                method: "PUT",
                body: JSON.stringify(body),
            });

            if (!res.ok) {
                setTitularLifeError(t("form.titularLifeSaveError"));
                return;
            }

            await loadData();
        } catch (error) {
            console.error(error);
            setTitularLifeError(t("form.titularLifeSaveError"));
        } finally {
            setSavingTitularLife(false);
        }
    };

    const loadData = async (filterOverrides = {}) => {
        const filters = {
            policyNumber: filterPolicyNumber,
            firstName: filterFirstName,
            lastName: filterLastName,
            status: filterStatus,
            type: filterType,
            insuranceCompanyId: filterInsuranceCompanyId,
            period,
            ...filterOverrides,
        };

        const params = new URLSearchParams();
        Object.entries(filters).forEach(([key, value]) => {
            if (value) params.set(key, value);
        });
        const query = params.toString();

        try {
            setLoading(true);

            const [policiesRes, customersRes] = await Promise.all([
                apiFetch(`/api/policies${query ? `?${query}` : ""}`),
                apiFetch("/api/customers"),
            ]);

            if (!policiesRes.ok) {
                throw new Error("Could not load policies");
            }

            if (!customersRes.ok) {
                throw new Error("Could not load customers");
            }

            const policiesData = await policiesRes.json();
            const customersData = await customersRes.json();

            setPolicies(policiesData);
            setCustomers(customersData);
        } catch (error) {
            console.error("ERROR loading policies data:", error);
        } finally {
            setLoading(false);
        }
    };

    const handleSearch = () => loadData();

    const handleClearFilters = () => {
        setFilterPolicyNumber("");
        setFilterFirstName("");
        setFilterLastName("");
        setFilterStatus("");
        setFilterType("");
        setFilterInsuranceCompanyId("");
        loadData({ policyNumber: "", firstName: "", lastName: "", status: "", type: "", insuranceCompanyId: "" });
    };
    const loadDependents = async (policyId) => {
        try {
            const res = await apiFetch(`/api/policies/${policyId}/dependents`);
            if (!res.ok) throw new Error();
            setDependents(await res.json());
        } catch (error) {
            console.error("Error loading dependents:", error);
        }
    };

    const loadBeneficiaries = async (policyId) => {
        try {
            const res = await apiFetch(`/api/policies/${policyId}/beneficiaries`);
            if (!res.ok) throw new Error();
            setBeneficiaries(await res.json());
        } catch (error) {
            console.error("Error loading beneficiaries:", error);
        }
    };

    const openDetail = async (policy) => {
        setViewingPolicy(policy);
        setDetailDependents([]);
        setDetailBeneficiaries([]);
        setDetailDocuments([]);
        setDocumentError("");
        setOpenDocMenuId(null);
        try {
            const res = await apiFetch(`/api/policies/${policy.id}/dependents`);
            if (!res.ok) throw new Error();
            setDetailDependents(await res.json());
        } catch (error) {
            console.error("Error loading dependents for detail view:", error);
        }
        try {
            const res = await apiFetch(`/api/policies/${policy.id}/beneficiaries`);
            if (!res.ok) throw new Error();
            setDetailBeneficiaries(await res.json());
        } catch (error) {
            console.error("Error loading beneficiaries for detail view:", error);
        }
        await loadDocuments(policy.id);
    };

    const closeDetail = () => {
        setViewingPolicy(null);
        setDetailDependents([]);
        setDetailBeneficiaries([]);
        setDetailDocuments([]);
        setDocumentError("");
        setOpenDocMenuId(null);
    };

    const loadDocuments = async (policyId) => {
        try {
            const res = await apiFetch(`/api/policies/${policyId}/documents`);
            if (!res.ok) throw new Error();
            setDetailDocuments(await res.json());
        } catch (error) {
            console.error("Error loading documents:", error);
        }
    };

    const handleUploadDocument = async (e) => {
        const file = e.target.files[0];
        e.target.value = "";
        if (!file) return;

        setDocumentError("");

        const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
        if (!ALLOWED_DOCUMENT_EXTENSIONS.includes(extension)) {
            setDocumentError(t("documents.invalidExtension"));
            return;
        }

        if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
            setDocumentError(t("documents.tooLarge"));
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        try {
            setUploadingDocument(true);
            const res = await apiFetch(`/api/policies/${viewingPolicy.id}/documents`, {
                method: "POST",
                body: formData,
            });

            if (!res.ok) {
                setDocumentError(res.errorMessage ?? t("documents.uploadError"));
                return;
            }

            await loadDocuments(viewingPolicy.id);
        } catch (error) {
            console.error(error);
            setDocumentError(t("documents.uploadError"));
        } finally {
            setUploadingDocument(false);
        }
    };

    const handleDownloadDocument = async (doc) => {
        try {
            const res = await apiFetch(`/api/policies/${viewingPolicy.id}/documents/${doc.id}`);
            if (!res.ok) throw new Error();

            const blob = await res.blob();
            const url = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = url;
            link.download = doc.originalFileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            URL.revokeObjectURL(url);
        } catch (error) {
            console.error(error);
            alert(t("documents.downloadError"));
        }
    };

    const handleDeleteDocument = async (doc) => {
        if (!confirm(t("documents.deleteConfirm", { name: doc.originalFileName }))) return;

        try {
            const res = await apiFetch(`/api/policies/${viewingPolicy.id}/documents/${doc.id}`, {
                method: "DELETE",
            });
            if (!res.ok) throw new Error();
            await loadDocuments(viewingPolicy.id);
        } catch (error) {
            console.error(error);
            alert(t("documents.deleteError"));
        }
    };

    const handleAddDependent = async (depCustomerId) => {
        try {
            const response = await apiFetch(`/api/policies/${editingId}/dependents`, {
                method: "POST",
                body: JSON.stringify({ customerId: depCustomerId }),
            });
            if (!response.ok) throw new Error("Error adding dependent");
            setDependentQuery("");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert(t("dependents.addError"));
        }
    };

    const handleNewDependentField = (e) => {
        const { name, value, type, checked } = e.target;
        if (name === "state") {
            // el condado depende del estado: si cambia el estado, se resetea
            setNewDependentForm((f) => ({ ...f, state: value, county: "" }));
            return;
        }
        setNewDependentForm((f) => ({ ...f, [name]: type === "checkbox" ? checked : value }));
    };

    const handleCreateDependent = async (e) => {
        e.preventDefault();
        setNewDependentError("");

        const body = {
            ...newDependentForm,
            agentId: newDependentForm.agentId ? Number(newDependentForm.agentId) : null,
            assistantAgentId: newDependentForm.assistantAgentId ? Number(newDependentForm.assistantAgentId) : null,
            recordAgentId: newDependentForm.recordAgentId ? Number(newDependentForm.recordAgentId) : null,
            annualIncome: newDependentForm.annualIncome === "" ? 0 : Number(newDependentForm.annualIncome),
            age: newDependentForm.age === "" ? null : Number(newDependentForm.age),
            currentlyEmployed: newDependentForm.currentlyEmployed === "" ? null : newDependentForm.currentlyEmployed === "true",
            driverLicenseNumber: newDependentForm.hasDriverLicense ? newDependentForm.driverLicenseNumber : "",
            netWorth: newDependentForm.netWorth === "" ? null : Number(newDependentForm.netWorth),
            householdIncome: newDependentForm.householdIncome === "" ? null : Number(newDependentForm.householdIncome),
            householdNetWorth: newDependentForm.householdNetWorth === "" ? null : Number(newDependentForm.householdNetWorth),
        };

        try {
            setCreatingDependent(true);

            const res = await apiFetch("/api/customers", {
                method: "POST",
                body: JSON.stringify(body),
            });

            if (!res.ok) {
                setNewDependentError(res.errorMessage ?? t("dependents.createError"));
                return;
            }

            const created = await res.json();

            // El nuevo Customer queda guardado como cualquier otro y se vincula
            // automáticamente a esta póliza vía PolicyDependents.
            await handleAddDependent(created.id);
            await loadData(); // refresca la lista de customers para que el nuevo aparezca en el resto de la página

            setNewDependentForm(emptyCustomerForm);
            setShowCreateDependentForm(false);
        } catch {
            setNewDependentError(t("dependents.createError"));
        } finally {
            setCreatingDependent(false);
        }
    };

    const handleToggleAplicante = async (depCustomerId, isAplicante) => {
        try {
            const response = await apiFetch(`/api/policies/${editingId}/dependents/${depCustomerId}`, {
                method: "PUT",
                body: JSON.stringify({ isAplicante }),
            });
            if (!response.ok) throw new Error("Error updating dependent");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert(t("dependents.updateError"));
        }
    };

    const handleRemoveDependent = async (depCustomerId) => {
        try {
            const response = await apiFetch(`/api/policies/${editingId}/dependents/${depCustomerId}`, {
                method: "DELETE",
            });
            if (!response.ok) throw new Error("Error removing dependent");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert(t("dependents.removeError"));
        }
    };

    const handleNewBeneficiaryField = (e) => {
        const { name, value } = e.target;
        setNewBeneficiaryForm((f) => ({ ...f, [name]: value }));
    };

    const handleCreateBeneficiary = async (e) => {
        e.preventDefault();
        setBeneficiaryError("");

        try {
            setCreatingBeneficiary(true);

            const res = await apiFetch(`/api/policies/${editingId}/beneficiaries`, {
                method: "POST",
                body: JSON.stringify(newBeneficiaryForm),
            });

            if (!res.ok) {
                setBeneficiaryError(t("beneficiaries.createError"));
                return;
            }

            setNewBeneficiaryForm({
                typeOfRelationship: "", firstName: "", lastName: "", dateOfBirth: "",
                gender: "", phone: "", email: "", socialSecurityNumber: "",
            });
            setShowCreateBeneficiaryForm(false);
            await loadBeneficiaries(editingId);
        } catch (error) {
            console.error(error);
            setBeneficiaryError(t("beneficiaries.createError"));
        } finally {
            setCreatingBeneficiary(false);
        }
    };

    const handleRemoveBeneficiary = async (beneficiaryId) => {
        try {
            const response = await apiFetch(`/api/policies/${editingId}/beneficiaries/${beneficiaryId}`, {
                method: "DELETE",
            });
            if (!response.ok) throw new Error("Error removing beneficiary");
            await loadBeneficiaries(editingId);
        } catch (error) {
            console.error(error);
            alert(t("beneficiaries.removeError"));
        }
    };

    const handleEdit = (policy) => {
        setEditingId(policy.id);

        setPolicyNumber(policy.policyNumber);
        setType(policy.type);
        setInsuranceCompanyId(policy.insuranceCompanyId);
        setStartDate(policy.startDate.slice(0, 10));
        setEndDate(policy.endDate.slice(0, 10));
        setPremium(policy.premium);
        setStatus(policy.status);
        setCustomerId(policy.customerId);
        setFormPeriod(policy.period);
        setNumberOfApplicants(policy.numberOfApplicants ?? "");

        setPlanType(policy.planType ?? "");
        setInsurancePlan(policy.insurancePlan ?? "");
        setEffectiveDate(policy.effectiveDate ? policy.effectiveDate.slice(0, 10) : "");
        setTaxCreditSubsidy(policy.taxCreditSubsidy ?? "");
        setMonthlyPremiumAmount(policy.monthlyPremiumAmount ?? "");

        setHasMedicaid(policy.hasMedicaid === null || policy.hasMedicaid === undefined ? "" : String(policy.hasMedicaid));
        setMedicaidLevel(policy.medicaidLevel ?? "");
        setReferredToMedicalCorporation(
            policy.referredToMedicalCorporation === null || policy.referredToMedicalCorporation === undefined
                ? ""
                : String(policy.referredToMedicalCorporation)
        );
        setMedicalCorporation(policy.medicalCorporation ?? "");

        setAdditionalOrAlternatePolicy(policy.additionalOrAlternatePolicy ?? false);
        setAdditionalOrAlternatePolicyDetail(policy.additionalOrAlternatePolicyDetail ?? "");
        setUnderwritingRequirements(policy.underwritingRequirements ?? "");
        setNeedsMedicalRequirements(policy.needsMedicalRequirements ?? false);
        setBillingType(policy.billingType ?? "");
        setPremiumFrequency(policy.premiumFrequency ?? "");
        setPlannedPeriodicModalPremium(policy.plannedPeriodicModalPremium ?? "");
        setSourceOfFunds(policy.sourceOfFunds ?? "");
        setHasExistingLifeInsurance(policy.hasExistingLifeInsurance ?? false);
        setIsReplacingExistingPolicy(policy.isReplacingExistingPolicy ?? false);
        setUsingFundsFromInforcePolicy(policy.usingFundsFromInforcePolicy ?? false);
        setProvideComparativeInfoForm(policy.provideComparativeInfoForm ?? false);
        setPhysicianName(policy.physicianName ?? "");
        setPhysicianAddress(policy.physicianAddress ?? "");
        setAdditionalInformation(policy.additionalInformation ?? "");
        setConsentSigned(policy.consentSigned ?? false);

        setHasExistingDentalCoverage(policy.hasExistingDentalCoverage ?? false);
        setEligibleForMedicare(policy.eligibleForMedicare ?? false);
        setIsReplacingDentalCoverage(policy.isReplacingDentalCoverage ?? false);
        setInsuredPaysThePremium(policy.insuredPaysThePremium ?? false);
        setBankAccountType(policy.bankAccountType ?? "");
        setRoutingNumber(policy.routingNumber ?? "");
        setAccountNumber(policy.accountNumber ?? "");
        setInsuredIsAccountHolder(policy.insuredIsAccountHolder ?? false);
        setAuthorizedAutomaticPayment(policy.authorizedAutomaticPayment ?? false);
        setAutoPaymentDay(policy.autoPaymentDay ?? "");
        setAuthorizeMarketingInfo(policy.authorizeMarketingInfo ?? false);
        setRepresentativeName(policy.representativeName ?? "");
        setRepresentativeRelationship(policy.representativeRelationship ?? "");

        setDependentQuery("");
        setShowDependentPicker(false);
        setShowCreateDependentForm(false);
        setNewDependentForm(emptyCustomerForm);
        setNewDependentError("");
        loadDependents(policy.id);

        setShowCreateBeneficiaryForm(false);
        setBeneficiaryError("");
        loadBeneficiaries(policy.id);

        setShowForm(true);
    };

    const handleDelete = async (id) => {
        if (!confirm(t("deleteConfirm"))) return;

        try {
            const response = await apiFetch(`/api/policies/${id}`, { method: "DELETE" });

            if (!response.ok) {
                throw new Error("Error deleting policy");
            }

            await loadData(); // refresca tabla

        } catch (error) {
            console.error(error);
            alert(t("deleteError"));
        }
    };


    useEffect(() => {
        if (!localStorage.getItem("accessToken")) return;
        // Re-carga cuando cambia el Período activo del header (filtra la lista).
        loadData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [period]);

    useEffect(() => {
        const loadInsuranceCompanies = async () => {
            try {
                const res = await apiFetch("/api/insurance-companies");
                if (!res.ok) throw new Error();
                setInsuranceCompanies(await res.json());
            } catch (error) {
                console.error("Error loading insurance companies:", error);
            }
        };

        loadInsuranceCompanies();
    }, []);

    useEffect(() => {
        if (!userIsAdmin) return;

        const loadDependentAgents = async () => {
            try {
                const res = await apiFetch("/users?role=Agente");
                if (!res.ok) throw new Error();
                setDependentAgents(await res.json());
            } catch (error) {
                console.error("Error loading agents:", error);
            }
        };

        loadDependentAgents();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        if (
            !policyNumber.trim() ||
            !type.trim() ||
            !insuranceCompanyId ||
            !startDate ||
            !endDate ||
            !premium ||
            !status.trim() ||
            !customerId
        ) {
            setFormError(t("form.requiredError"));
            return;
        }

        try {
            setSubmitting(true);

            const url = editingId
                ? `/api/policies/${editingId}`
                : "/api/policies";

            const method = editingId ? "PUT" : "POST";

            const response = await apiFetch(url, {
                method: method,
                body: JSON.stringify({
                    policyNumber,
                    type,
                    insuranceCompanyId: Number(insuranceCompanyId),
                    startDate,
                    endDate,
                    premium: Number(premium),
                    status,
                    customerId: Number(customerId),
                    period: formPeriod,
                    numberOfApplicants: numberOfApplicants === "" ? null : Number(numberOfApplicants),
                    planType: planType || null,
                    insurancePlan: insurancePlan || null,
                    effectiveDate: effectiveDate || null,
                    taxCreditSubsidy: taxCreditSubsidy === "" ? null : Number(taxCreditSubsidy),
                    monthlyPremiumAmount: monthlyPremiumAmount === "" ? null : Number(monthlyPremiumAmount),
                    hasMedicaid: isMedicare && hasMedicaid !== "" ? hasMedicaid === "true" : null,
                    medicaidLevel: isMedicare && medicaidLevel ? medicaidLevel : null,
                    referredToMedicalCorporation:
                        isMedicare && referredToMedicalCorporation !== "" ? referredToMedicalCorporation === "true" : null,
                    medicalCorporation: isMedicare && medicalCorporation ? medicalCorporation : null,
                    additionalOrAlternatePolicy: isLifeInsurance ? additionalOrAlternatePolicy : null,
                    additionalOrAlternatePolicyDetail:
                        isLifeInsurance && additionalOrAlternatePolicy && additionalOrAlternatePolicyDetail
                            ? additionalOrAlternatePolicyDetail
                            : null,
                    underwritingRequirements: isLifeInsurance && underwritingRequirements ? underwritingRequirements : null,
                    needsMedicalRequirements: isLifeInsurance ? needsMedicalRequirements : null,
                    billingType: isLifeInsurance && billingType ? billingType : null,
                    premiumFrequency: isLifeInsurance && premiumFrequency ? premiumFrequency : null,
                    plannedPeriodicModalPremium:
                        isLifeInsurance && plannedPeriodicModalPremium !== "" ? Number(plannedPeriodicModalPremium) : null,
                    sourceOfFunds: isLifeInsurance && sourceOfFunds ? sourceOfFunds : null,
                    hasExistingLifeInsurance: isLifeInsurance ? hasExistingLifeInsurance : null,
                    isReplacingExistingPolicy: isLifeInsurance ? isReplacingExistingPolicy : null,
                    usingFundsFromInforcePolicy: isLifeInsurance ? usingFundsFromInforcePolicy : null,
                    provideComparativeInfoForm: isLifeInsurance ? provideComparativeInfoForm : null,
                    physicianName: isLifeInsurance && physicianName ? physicianName : null,
                    physicianAddress: isLifeInsurance && physicianAddress ? physicianAddress : null,
                    additionalInformation: isLifeInsurance && additionalInformation ? additionalInformation : null,
                    consentSigned: isLifeInsurance ? consentSigned : null,
                    hasExistingDentalCoverage: isSupplemental ? hasExistingDentalCoverage : null,
                    eligibleForMedicare: isSupplemental ? eligibleForMedicare : null,
                    isReplacingDentalCoverage: isSupplemental ? isReplacingDentalCoverage : null,
                    insuredPaysThePremium: isSupplemental ? insuredPaysThePremium : null,
                    bankAccountType: isSupplemental && bankAccountType ? bankAccountType : null,
                    routingNumber: isSupplemental && routingNumber ? routingNumber : null,
                    accountNumber: isSupplemental && accountNumber ? accountNumber : null,
                    insuredIsAccountHolder: isSupplemental ? insuredIsAccountHolder : null,
                    authorizedAutomaticPayment: isSupplemental ? authorizedAutomaticPayment : null,
                    autoPaymentDay: isSupplemental && autoPaymentDay !== "" ? Number(autoPaymentDay) : null,
                    authorizeMarketingInfo: isSupplemental ? authorizeMarketingInfo : null,
                    representativeName: isSupplemental && representativeName ? representativeName : null,
                    representativeRelationship: isSupplemental && representativeRelationship ? representativeRelationship : null,
                }),
            });


            if (!response.ok) {
                throw new Error("Error saving policy");
            }


            // ✅ limpiar
            setEditingId(null);
            setShowForm(false);


            setPolicyNumber("");
            setType("");
            setInsuranceCompanyId("");
            setStartDate("");
            setEndDate("");
            setPremium("");
            setStatus("Draft");
            setCustomerId("");
            setNumberOfApplicants("");

            setPlanType("");
            setInsurancePlan("");
            setEffectiveDate("");
            setTaxCreditSubsidy("");
            setMonthlyPremiumAmount("");

            setHasMedicaid("");
            setMedicaidLevel("");
            setReferredToMedicalCorporation("");
            setMedicalCorporation("");

            setAdditionalOrAlternatePolicy(false);
            setAdditionalOrAlternatePolicyDetail("");
            setUnderwritingRequirements("");
            setNeedsMedicalRequirements(false);
            setBillingType("");
            setPremiumFrequency("");
            setPlannedPeriodicModalPremium("");
            setSourceOfFunds("");
            setHasExistingLifeInsurance(false);
            setIsReplacingExistingPolicy(false);
            setUsingFundsFromInforcePolicy(false);
            setProvideComparativeInfoForm(false);
            setPhysicianName("");
            setPhysicianAddress("");
            setAdditionalInformation("");
            setConsentSigned(false);

            setHasExistingDentalCoverage(false);
            setEligibleForMedicare(false);
            setIsReplacingDentalCoverage(false);
            setInsuredPaysThePremium(false);
            setBankAccountType("");
            setRoutingNumber("");
            setAccountNumber("");
            setInsuredIsAccountHolder(false);
            setAuthorizedAutomaticPayment(false);
            setAutoPaymentDay("");
            setAuthorizeMarketingInfo(false);
            setRepresentativeName("");
            setRepresentativeRelationship("");

            setDependents([]);
            setDependentQuery("");
            setShowDependentPicker(false);
            setShowCreateDependentForm(false);
            setNewDependentForm(emptyCustomerForm);
            setNewDependentError("");

            setBeneficiaries([]);
            setShowCreateBeneficiaryForm(false);
            setBeneficiaryError("");
            setNewBeneficiaryForm({
                typeOfRelationship: "", firstName: "", lastName: "", dateOfBirth: "",
                gender: "", phone: "", email: "", socialSecurityNumber: "",
            });

            await loadData();
        } catch (error) {

            console.error(error);
            setFormError(t("form.saveError"));

        } finally {
            setSubmitting(false);
        }
    };

    const dependentCandidates = customers.filter(
        (c) =>
            c.id !== Number(customerId) &&
            !dependents.some((d) => d.customerId === c.id) &&
            `${c.firstName} ${c.lastName}`.toLowerCase().includes(dependentQuery.toLowerCase())
    );

    if (loading) {
        return <p>{t("loading")}</p>;
    }

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>{t("title")}</h2>

            {/* ✅ BOTÓN */}
            <button
                onClick={() => {
                    // Al abrir el formulario para crear (no editar), el Período
                    // se toma del selector activo del header en ese momento.
                    if (!showForm && !editingId) setFormPeriod(period);
                    setShowForm(!showForm);
                }}
                type="button"
                style={{
                    marginBottom: 20,
                    background: "#2563eb",
                    color: "white",
                    padding: "8px 12px",
                    border: "none",
                    borderRadius: 6,
                    cursor: "pointer"
                }}
            >
                {showForm ? t("closeFormButton") : t("newButton")}
            </button>

            {/* ✅ FORMULARIO CONDICIONAL */}
            {
                showForm && (
                    <div
                        style={{
                            border: "1px solid #ddd",
                            borderRadius: 10,
                            padding: 20,
                            marginBottom: 30,
                            background: "#fafafa",
                            maxWidth: 600,
                        }}
                    >
                        <h3 style={{ marginTop: 0 }}>{editingId ? t("form.titleEdit") : t("form.title")}</h3>

                        <form onSubmit={handleSubmit}>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.policyNumber")}</label>
                                <input
                                    type="text"
                                    value={policyNumber}
                                    onChange={(e) => setPolicyNumber(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.type")}</label>
                                <select
                                    value={type}
                                    onChange={(e) => {
                                        const nextType = e.target.value;
                                        setType(nextType);
                                        if (nextType !== "Medicare") {
                                            setHasMedicaid("");
                                            setMedicaidLevel("");
                                            setReferredToMedicalCorporation("");
                                            setMedicalCorporation("");
                                        }
                                        if (nextType !== "Life Insurance") {
                                            setAdditionalOrAlternatePolicy(false);
                                            setAdditionalOrAlternatePolicyDetail("");
                                            setUnderwritingRequirements("");
                                            setNeedsMedicalRequirements(false);
                                            setBillingType("");
                                            setPremiumFrequency("");
                                            setPlannedPeriodicModalPremium("");
                                            setSourceOfFunds("");
                                            setHasExistingLifeInsurance(false);
                                            setIsReplacingExistingPolicy(false);
                                            setUsingFundsFromInforcePolicy(false);
                                            setProvideComparativeInfoForm(false);
                                            setPhysicianName("");
                                            setPhysicianAddress("");
                                            setAdditionalInformation("");
                                            setConsentSigned(false);
                                        }
                                        if (nextType !== "Supplemental Plans") {
                                            setHasExistingDentalCoverage(false);
                                            setEligibleForMedicare(false);
                                            setIsReplacingDentalCoverage(false);
                                            setInsuredPaysThePremium(false);
                                            setBankAccountType("");
                                            setRoutingNumber("");
                                            setAccountNumber("");
                                            setInsuredIsAccountHolder(false);
                                            setAuthorizedAutomaticPayment(false);
                                            setAutoPaymentDay("");
                                            setAuthorizeMarketingInfo(false);
                                            setRepresentativeName("");
                                            setRepresentativeRelationship("");
                                        }
                                    }}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">{t("form.selectType")}</option>
                                    {POLICY_TYPES.map((t2) => (
                                        <option key={t2} value={t2}>{translateEnum("policyType", t2)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.insuranceCompany")}</label>
                                <select
                                    value={insuranceCompanyId}
                                    onChange={(e) => setInsuranceCompanyId(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">{t("form.selectInsuranceCompany")}</option>
                                    {insuranceCompanies.map((ic) => (
                                        <option key={ic.id} value={ic.id}>
                                            {ic.name}{!ic.isActive ? ` ${t("form.inactiveSuffix")}` : ""}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.planType")}</label>
                                <select
                                    value={planType}
                                    onChange={(e) => setPlanType(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">{t("form.selectPlanType")}</option>
                                    {PLAN_TYPES.map((pt) => (
                                        <option key={pt} value={pt}>{translateEnum("planType", pt)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.insurancePlan")}</label>
                                <input
                                    type="text"
                                    value={insurancePlan}
                                    onChange={(e) => setInsurancePlan(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.startDate")}</label>
                                <input
                                    type="date"
                                    value={startDate}
                                    onChange={(e) => setStartDate(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.endDate")}</label>
                                <input
                                    type="date"
                                    value={endDate}
                                    onChange={(e) => setEndDate(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.effectiveDate")}</label>
                                <input
                                    type="date"
                                    value={effectiveDate}
                                    onChange={(e) => setEffectiveDate(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.premium")}</label>
                                <input
                                    type="number"
                                    value={premium}
                                    onChange={(e) => setPremium(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.monthlyPremiumAmount")}</label>
                                <input
                                    type="number"
                                    min="0"
                                    step="0.01"
                                    value={monthlyPremiumAmount}
                                    onChange={(e) => setMonthlyPremiumAmount(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.taxCreditSubsidy")}</label>
                                <input
                                    type="number"
                                    min="0"
                                    step="0.01"
                                    value={taxCreditSubsidy}
                                    onChange={(e) => setTaxCreditSubsidy(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            {isMedicare && (
                                <>
                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.hasMedicaid")}</label>
                                        <select
                                            value={hasMedicaid}
                                            onChange={(e) => setHasMedicaid(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        >
                                            <option value="">{t("form.selectOption")}</option>
                                            <option value="true">{t("yes")}</option>
                                            <option value="false">{t("no")}</option>
                                        </select>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.medicaidLevel")}</label>
                                        <input
                                            type="text"
                                            value={medicaidLevel}
                                            onChange={(e) => setMedicaidLevel(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.referredToMedicalCorporation")}</label>
                                        <select
                                            value={referredToMedicalCorporation}
                                            onChange={(e) => setReferredToMedicalCorporation(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        >
                                            <option value="">{t("form.selectOption")}</option>
                                            <option value="true">{t("yes")}</option>
                                            <option value="false">{t("no")}</option>
                                        </select>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.medicalCorporation")}</label>
                                        <input
                                            type="text"
                                            value={medicalCorporation}
                                            onChange={(e) => setMedicalCorporation(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>
                                </>
                            )}

                            {isLifeInsurance && (
                                <>
                                    <div style={{ marginBottom: 12 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={additionalOrAlternatePolicy}
                                                onChange={(e) => setAdditionalOrAlternatePolicy(e.target.checked)}
                                            />
                                            {t("form.fields.additionalOrAlternatePolicy")}
                                        </label>
                                    </div>

                                    {additionalOrAlternatePolicy && (
                                        <div style={{ marginBottom: 12 }}>
                                            <label>{t("form.fields.additionalOrAlternatePolicyDetail")}</label>
                                            <input
                                                type="text"
                                                value={additionalOrAlternatePolicyDetail}
                                                onChange={(e) => setAdditionalOrAlternatePolicyDetail(e.target.value)}
                                                style={{ width: "100%", padding: 8, marginTop: 4 }}
                                            />
                                        </div>
                                    )}

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.underwritingRequirements")}</label>
                                        <input
                                            type="text"
                                            value={underwritingRequirements}
                                            onChange={(e) => setUnderwritingRequirements(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={needsMedicalRequirements}
                                                onChange={(e) => setNeedsMedicalRequirements(e.target.checked)}
                                            />
                                            {t("form.fields.needsMedicalRequirements")}
                                        </label>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.billingType")}</label>
                                        <input
                                            type="text"
                                            value={billingType}
                                            onChange={(e) => setBillingType(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.premiumFrequency")}</label>
                                        <input
                                            type="text"
                                            value={premiumFrequency}
                                            onChange={(e) => setPremiumFrequency(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.plannedPeriodicModalPremium")}</label>
                                        <input
                                            type="number"
                                            min="0"
                                            step="0.01"
                                            value={plannedPeriodicModalPremium}
                                            onChange={(e) => setPlannedPeriodicModalPremium(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.sourceOfFunds")}</label>
                                        <input
                                            type="text"
                                            value={sourceOfFunds}
                                            onChange={(e) => setSourceOfFunds(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12, display: "flex", flexDirection: "column", gap: 6 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={hasExistingLifeInsurance}
                                                onChange={(e) => setHasExistingLifeInsurance(e.target.checked)}
                                            />
                                            {t("form.fields.hasExistingLifeInsurance")}
                                        </label>

                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={isReplacingExistingPolicy}
                                                onChange={(e) => setIsReplacingExistingPolicy(e.target.checked)}
                                            />
                                            {t("form.fields.isReplacingExistingPolicy")}
                                        </label>

                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={usingFundsFromInforcePolicy}
                                                onChange={(e) => setUsingFundsFromInforcePolicy(e.target.checked)}
                                            />
                                            {t("form.fields.usingFundsFromInforcePolicy")}
                                        </label>

                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={provideComparativeInfoForm}
                                                onChange={(e) => setProvideComparativeInfoForm(e.target.checked)}
                                            />
                                            {t("form.fields.provideComparativeInfoForm")}
                                        </label>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.physicianName")}</label>
                                        <input
                                            type="text"
                                            value={physicianName}
                                            onChange={(e) => setPhysicianName(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.physicianAddress")}</label>
                                        <input
                                            type="text"
                                            value={physicianAddress}
                                            onChange={(e) => setPhysicianAddress(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.additionalInformation")}</label>
                                        <textarea
                                            value={additionalInformation}
                                            onChange={(e) => setAdditionalInformation(e.target.value)}
                                            rows={3}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={consentSigned}
                                                onChange={(e) => setConsentSigned(e.target.checked)}
                                            />
                                            {t("form.fields.consentSigned")}
                                        </label>
                                    </div>
                                </>
                            )}

                            {isSupplemental && (
                                <>
                                    <div style={{ marginBottom: 12, display: "flex", flexDirection: "column", gap: 6 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={hasExistingDentalCoverage}
                                                onChange={(e) => setHasExistingDentalCoverage(e.target.checked)}
                                            />
                                            {t("form.fields.hasExistingDentalCoverage")}
                                        </label>

                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={eligibleForMedicare}
                                                onChange={(e) => setEligibleForMedicare(e.target.checked)}
                                            />
                                            {t("form.fields.eligibleForMedicare")}
                                        </label>

                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={isReplacingDentalCoverage}
                                                onChange={(e) => setIsReplacingDentalCoverage(e.target.checked)}
                                            />
                                            {t("form.fields.isReplacingDentalCoverage")}
                                        </label>
                                    </div>

                                    <h4 style={{ marginBottom: 4 }}>{t("form.bankDataSection")}</h4>

                                    <div style={{ marginBottom: 12 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={insuredPaysThePremium}
                                                onChange={(e) => setInsuredPaysThePremium(e.target.checked)}
                                            />
                                            {t("form.fields.insuredPaysThePremium")}
                                        </label>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.bankAccountType")}</label>
                                        <select
                                            value={bankAccountType}
                                            onChange={(e) => setBankAccountType(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        >
                                            <option value="">{t("form.selectOption")}</option>
                                            {BANK_ACCOUNT_TYPES.map((b) => (
                                                <option key={b} value={b}>{translateEnum("bankAccountType", b)}</option>
                                            ))}
                                        </select>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.routingNumber")}</label>
                                        <MaskedInput
                                            value={routingNumber}
                                            onChange={(e) => setRoutingNumber(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.accountNumber")}</label>
                                        <MaskedInput
                                            value={accountNumber}
                                            onChange={(e) => setAccountNumber(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12, display: "flex", flexDirection: "column", gap: 6 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={insuredIsAccountHolder}
                                                onChange={(e) => setInsuredIsAccountHolder(e.target.checked)}
                                            />
                                            {t("form.fields.insuredIsAccountHolder")}
                                        </label>

                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={authorizedAutomaticPayment}
                                                onChange={(e) => setAuthorizedAutomaticPayment(e.target.checked)}
                                            />
                                            {t("form.fields.authorizedAutomaticPayment")}
                                        </label>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.autoPaymentDay")}</label>
                                        <select
                                            value={autoPaymentDay}
                                            onChange={(e) => setAutoPaymentDay(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        >
                                            <option value="">{t("form.selectOption")}</option>
                                            {AUTO_PAYMENT_DAYS.map((day) => (
                                                <option key={day} value={day}>{day}</option>
                                            ))}
                                        </select>
                                    </div>

                                    <h4 style={{ marginBottom: 4 }}>{t("form.hipaaSection")}</h4>

                                    <div style={{ marginBottom: 12 }}>
                                        <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
                                            <input
                                                type="checkbox"
                                                checked={authorizeMarketingInfo}
                                                onChange={(e) => setAuthorizeMarketingInfo(e.target.checked)}
                                            />
                                            {t("form.fields.authorizeMarketingInfo")}
                                        </label>
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.representativeName")}</label>
                                        <input
                                            type="text"
                                            value={representativeName}
                                            onChange={(e) => setRepresentativeName(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    <div style={{ marginBottom: 12 }}>
                                        <label>{t("form.fields.representativeRelationship")}</label>
                                        <input
                                            type="text"
                                            value={representativeRelationship}
                                            onChange={(e) => setRepresentativeRelationship(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>
                                </>
                            )}

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.status")}</label>
                                <select
                                    value={status}
                                    onChange={(e) => setStatus(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    {POLICY_STATUSES.map((s) => (
                                        <option key={s} value={s}>{translateEnum("policyStatus", s)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.customer")}</label>
                                <select
                                    value={customerId}
                                    onChange={(e) => setCustomerId(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">{t("form.selectCustomer")}</option>
                                    {customers.map((c) => (
                                        <option key={c.id} value={c.id}>
                                            {c.firstName} {c.lastName} ({maskValue(c.socialSecurityNumber)})
                                        </option>
                                    ))}
                                </select>
                            </div>

                            {isLifeInsurance && customerId && (
                                <div style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, marginBottom: 12, background: "white" }}>
                                    <label style={{ fontWeight: "bold" }}>{t("form.titularLifeSection")}</label>

                                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginTop: 8 }}>
                                        <LifeInsuranceFields form={titularLifeForm} onFieldChange={handleTitularLifeField} />
                                    </div>

                                    {titularLifeError && (
                                        <p style={{ color: "red", marginTop: 8 }}>{titularLifeError}</p>
                                    )}

                                    <button
                                        type="button"
                                        onClick={handleSaveTitularLife}
                                        disabled={savingTitularLife}
                                        style={{ marginTop: 8 }}
                                    >
                                        {savingTitularLife ? t("form.savingTitularLife") : t("form.saveTitularLife")}
                                    </button>
                                </div>
                            )}

                            {formError && (
                                <p style={{ color: "red", marginBottom: 12 }}>
                                    {formError}
                                </p>
                            )}

                            <button type="submit" disabled={submitting}>
                                {editingId
                                    ? (submitting ? t("form.submitUpdating") : t("form.submitUpdate"))
                                    : (submitting ? t("form.submitCreating") : t("form.submitCreate"))}
                            </button>

                        </form>

                        {/* Fuera del <form> a propósito: los campos "required" de
                            CustomerFormFields (usados al crear un dependiente nuevo)
                            bloquearían la validación nativa del formulario de Policy
                            si quedaran anidados dentro del mismo <form>. */}
                        {editingId && (
                            <div style={{ marginTop: 12, borderTop: "1px solid #ddd", paddingTop: 12 }}>
                                <label style={{ fontWeight: "bold" }}>{t("dependents.title")}</label>

                                <div style={{ margin: "10px 0" }}>
                                    <label>{t("dependents.numberOfApplicants")}</label>
                                    <input
                                        type="number"
                                        min="0"
                                        value={numberOfApplicants}
                                        onChange={(e) => setNumberOfApplicants(e.target.value)}
                                        style={{ width: "100%", padding: 8, marginTop: 4 }}
                                    />
                                </div>

                                {dependents.length === 0 ? (
                                    <p style={{ color: "#666", margin: "8px 0" }}>{t("dependents.empty")}</p>
                                ) : (
                                    <ul style={{ listStyle: "none", padding: 0, margin: "8px 0" }}>
                                        {dependents.map((d) => (
                                            <li
                                                key={d.customerId}
                                                style={{
                                                    display: "flex",
                                                    justifyContent: "space-between",
                                                    alignItems: "center",
                                                    padding: "6px 0",
                                                }}
                                            >
                                                <span>{d.firstName} {d.lastName}</span>
                                                <label style={{ display: "flex", alignItems: "center", gap: 4, fontSize: 13 }}>
                                                    <input
                                                        type="checkbox"
                                                        checked={d.isAplicante}
                                                        onChange={(e) => handleToggleAplicante(d.customerId, e.target.checked)}
                                                    />
                                                    {t("dependents.isAplicante")}
                                                </label>
                                                <button
                                                    type="button"
                                                    onClick={() => handleRemoveDependent(d.customerId)}
                                                    title={t("dependents.removeTitle")}
                                                    style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 16 }}
                                                >
                                                    🗑
                                                </button>
                                            </li>
                                        ))}
                                    </ul>
                                )}

                                <div style={{ display: "flex", gap: 8, marginBottom: 8 }}>
                                    <button
                                        type="button"
                                        onClick={() => {
                                            setShowCreateDependentForm(false);
                                            setShowDependentPicker(!showDependentPicker);
                                        }}
                                        style={{
                                            background: "#2563eb",
                                            color: "white",
                                            padding: "6px 10px",
                                            border: "none",
                                            borderRadius: 6,
                                            cursor: "pointer",
                                        }}
                                    >
                                        {showDependentPicker ? t("dependents.cancelButton") : t("dependents.addButton")}
                                    </button>

                                    <button
                                        type="button"
                                        onClick={() => {
                                            setShowDependentPicker(false);
                                            setNewDependentError("");
                                            setShowCreateDependentForm(!showCreateDependentForm);
                                        }}
                                        style={{
                                            background: "#16a34a",
                                            color: "white",
                                            padding: "6px 10px",
                                            border: "none",
                                            borderRadius: 6,
                                            cursor: "pointer",
                                        }}
                                    >
                                        {showCreateDependentForm ? t("dependents.cancelButton") : t("dependents.createButton")}
                                    </button>
                                </div>

                                {showCreateDependentForm && (
                                    <div style={{ border: "1px solid #ddd", borderRadius: 8, padding: 16, marginBottom: 12, background: "white" }}>
                                        <h4 style={{ marginTop: 0 }}>{t("dependents.createTitle")}</h4>
                                        <CustomerFormFields
                                            form={newDependentForm}
                                            onFieldChange={handleNewDependentField}
                                            agents={dependentAgents}
                                            userIsAdmin={userIsAdmin}
                                            showLifeInsuranceFields={isLifeInsurance}
                                        />
                                        {newDependentError && <p style={{ color: "red", marginTop: 12 }}>{newDependentError}</p>}
                                        <button
                                            type="button"
                                            onClick={handleCreateDependent}
                                            disabled={creatingDependent}
                                            style={{ marginTop: 16, background: "#16a34a", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                                        >
                                            {creatingDependent ? t("common:actions.creating") : t("dependents.createSubmitButton")}
                                        </button>
                                    </div>
                                )}

                                {showDependentPicker && (
                                    <div>
                                        <input
                                            type="text"
                                            placeholder={t("dependents.searchPlaceholder")}
                                            value={dependentQuery}
                                            onChange={(e) => setDependentQuery(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginBottom: 8 }}
                                        />
                                        <ul style={{ listStyle: "none", padding: 0, maxHeight: 160, overflowY: "auto" }}>
                                            {dependentCandidates.map((c) => (
                                                <li
                                                    key={c.id}
                                                    style={{
                                                        display: "flex",
                                                        justifyContent: "space-between",
                                                        alignItems: "center",
                                                        padding: "6px 0",
                                                        borderBottom: "1px solid #eee",
                                                    }}
                                                >
                                                    <span>{c.firstName} {c.lastName}</span>
                                                    <button
                                                        type="button"
                                                        onClick={() => handleAddDependent(c.id)}
                                                        style={{
                                                            background: "#16a34a",
                                                            color: "white",
                                                            border: "none",
                                                            borderRadius: 4,
                                                            padding: "4px 8px",
                                                            cursor: "pointer",
                                                        }}
                                                    >
                                                        {t("dependents.addAction")}
                                                    </button>
                                                </li>
                                            ))}
                                            {dependentCandidates.length === 0 && (
                                                <li style={{ color: "#666", padding: "6px 0" }}>{t("dependents.noMatches")}</li>
                                            )}
                                        </ul>
                                    </div>
                                )}

                                {isLifeInsurance && (
                                    <div style={{ marginTop: 20, borderTop: "1px solid #ddd", paddingTop: 12 }}>
                                        <label style={{ fontWeight: "bold" }}>{t("beneficiaries.title")}</label>

                                        {beneficiaries.length === 0 ? (
                                            <p style={{ color: "#666", margin: "8px 0" }}>{t("beneficiaries.empty")}</p>
                                        ) : (
                                            <ul style={{ listStyle: "none", padding: 0, margin: "8px 0" }}>
                                                {beneficiaries.map((b) => (
                                                    <li
                                                        key={b.id}
                                                        style={{
                                                            display: "flex",
                                                            justifyContent: "space-between",
                                                            alignItems: "center",
                                                            padding: "6px 0",
                                                        }}
                                                    >
                                                        <span>{b.firstName} {b.lastName} ({b.typeOfRelationship})</span>
                                                        <button
                                                            type="button"
                                                            onClick={() => handleRemoveBeneficiary(b.id)}
                                                            title={t("beneficiaries.removeTitle")}
                                                            style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 16 }}
                                                        >
                                                            🗑
                                                        </button>
                                                    </li>
                                                ))}
                                            </ul>
                                        )}

                                        <button
                                            type="button"
                                            onClick={() => {
                                                setBeneficiaryError("");
                                                setShowCreateBeneficiaryForm(!showCreateBeneficiaryForm);
                                            }}
                                            style={{
                                                background: "#16a34a",
                                                color: "white",
                                                padding: "6px 10px",
                                                border: "none",
                                                borderRadius: 6,
                                                cursor: "pointer",
                                                marginBottom: 8,
                                            }}
                                        >
                                            {showCreateBeneficiaryForm ? t("beneficiaries.cancelButton") : t("beneficiaries.createButton")}
                                        </button>

                                        {showCreateBeneficiaryForm && (
                                            <div style={{ border: "1px solid #ddd", borderRadius: 8, padding: 16, marginBottom: 12, background: "white" }}>
                                                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.typeOfRelationship")}</label>
                                                        <input
                                                            name="typeOfRelationship"
                                                            value={newBeneficiaryForm.typeOfRelationship}
                                                            onChange={handleNewBeneficiaryField}
                                                            required
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.firstName")}</label>
                                                        <input
                                                            name="firstName"
                                                            value={newBeneficiaryForm.firstName}
                                                            onChange={handleNewBeneficiaryField}
                                                            required
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.lastName")}</label>
                                                        <input
                                                            name="lastName"
                                                            value={newBeneficiaryForm.lastName}
                                                            onChange={handleNewBeneficiaryField}
                                                            required
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.dateOfBirth")}</label>
                                                        <input
                                                            type="date"
                                                            name="dateOfBirth"
                                                            value={newBeneficiaryForm.dateOfBirth}
                                                            onChange={handleNewBeneficiaryField}
                                                            required
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.gender")}</label>
                                                        <input
                                                            name="gender"
                                                            value={newBeneficiaryForm.gender}
                                                            onChange={handleNewBeneficiaryField}
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.phone")}</label>
                                                        <input
                                                            name="phone"
                                                            value={newBeneficiaryForm.phone}
                                                            onChange={handleNewBeneficiaryField}
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.email")}</label>
                                                        <input
                                                            type="email"
                                                            name="email"
                                                            value={newBeneficiaryForm.email}
                                                            onChange={handleNewBeneficiaryField}
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                    <div>
                                                        <label>{t("beneficiaries.fields.socialSecurityNumber")}</label>
                                                        <MaskedInput
                                                            name="socialSecurityNumber"
                                                            value={newBeneficiaryForm.socialSecurityNumber}
                                                            onChange={handleNewBeneficiaryField}
                                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                                        />
                                                    </div>
                                                </div>
                                                {beneficiaryError && <p style={{ color: "red", marginTop: 12 }}>{beneficiaryError}</p>}
                                                <button
                                                    type="button"
                                                    onClick={handleCreateBeneficiary}
                                                    disabled={creatingBeneficiary}
                                                    style={{ marginTop: 16, background: "#16a34a", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                                                >
                                                    {creatingBeneficiary ? t("common:actions.creating") : t("beneficiaries.createSubmitButton")}
                                                </button>
                                            </div>
                                        )}
                                    </div>
                                )}
                            </div>
                        )}
                    </div>
                )
            }

            <div
                style={{
                    border: "1px solid #ddd",
                    borderRadius: 10,
                    padding: 16,
                    marginBottom: 20,
                    background: "#fafafa",
                    display: "flex",
                    flexWrap: "wrap",
                    gap: 10,
                    alignItems: "flex-end",
                }}
            >
                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.policyNumber")}</label>
                    <input
                        type="text"
                        value={filterPolicyNumber}
                        onChange={(e) => setFilterPolicyNumber(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.firstName")}</label>
                    <input
                        type="text"
                        value={filterFirstName}
                        onChange={(e) => setFilterFirstName(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.lastName")}</label>
                    <input
                        type="text"
                        value={filterLastName}
                        onChange={(e) => setFilterLastName(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.status")}</label>
                    <select
                        value={filterStatus}
                        onChange={(e) => setFilterStatus(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">{t("filters.all")}</option>
                        {POLICY_STATUSES.map((s) => (
                            <option key={s} value={s}>{translateEnum("policyStatus", s)}</option>
                        ))}
                    </select>
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.type")}</label>
                    <select
                        value={filterType}
                        onChange={(e) => setFilterType(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">{t("filters.all")}</option>
                        {POLICY_TYPES.map((t2) => (
                            <option key={t2} value={t2}>{translateEnum("policyType", t2)}</option>
                        ))}
                    </select>
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.insuranceCompany")}</label>
                    <select
                        value={filterInsuranceCompanyId}
                        onChange={(e) => setFilterInsuranceCompanyId(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">{t("filters.all")}</option>
                        {insuranceCompanies.map((ic) => (
                            <option key={ic.id} value={ic.id}>{ic.name}</option>
                        ))}
                    </select>
                </div>

                <button
                    type="button"
                    onClick={handleSearch}
                    style={{
                        background: "#2563eb",
                        color: "white",
                        padding: "8px 12px",
                        border: "none",
                        borderRadius: 6,
                        cursor: "pointer",
                    }}
                >
                    {t("common:actions.search")}
                </button>

                <button
                    type="button"
                    onClick={handleClearFilters}
                    style={{
                        background: "transparent",
                        border: "1px solid #ccc",
                        padding: "8px 12px",
                        borderRadius: 6,
                        cursor: "pointer",
                    }}
                >
                    {t("common:actions.clear")}
                </button>
            </div>

            {/* ✅ TABLA SIEMPRE VISIBLE */}
            {
                policies.length === 0 ? (
                    <p>{t("empty")}</p>
                ) : (

                    <div style={{ overflowX: "auto" }}>
                        <table style={{ width: "100%", borderCollapse: "collapse" }}>

                            <thead>
                                <tr style={{ background: "#f3f4f6", textAlign: "left" }}>
                                    <th style={{ padding: 10 }}>{t("table.policy")}</th>
                                    <th style={{ padding: 10 }}>{t("table.type")}</th>
                                    <th style={{ padding: 10 }}>{t("table.insuranceCompany")}</th>
                                    <th style={{ padding: 10 }}>{t("table.status")}</th>
                                    <th style={{ padding: 10 }}>{t("table.period")}</th>
                                    <th style={{ padding: 10 }}>{t("table.premium")}</th>
                                    <th style={{ padding: 10 }}>{t("table.customer")}</th>
                                    <th style={{ padding: 10 }}>{t("table.actions")}</th>
                                </tr>
                            </thead>

                            <tbody>
                                {policies.map((p) => (
                                    <tr key={p.id}>
                                        <td style={{ padding: 10 }}>{p.policyNumber}</td>
                                        <td style={{ padding: 10 }}>{translateEnum("policyType", p.type)}</td>
                                        <td style={{ padding: 10 }}>{p.insuranceCompanyName}</td>
                                        <td style={{ padding: 10 }}>{translateEnum("policyStatus", p.status)}</td>
                                        <td style={{ padding: 10 }}>{p.period}</td>
                                        <td style={{ padding: 10 }}>{p.premium}</td>
                                        <td style={{ padding: 10 }}>
                                            {getCustomerName(p.customerId)}
                                        </td>
                                        <td style={{ padding: 10 }}>

                                            <button
                                                onClick={() => openDetail(p)}
                                                title={t("actionTitles.viewDetails")}
                                                style={{
                                                    marginRight: 8,
                                                    background: "transparent",
                                                    border: "none",
                                                    cursor: "pointer",
                                                    fontSize: 16
                                                }}
                                            >
                                                🔍
                                            </button>

                                            <button
                                                onClick={() => handleEdit(p)}
                                                title={t("actionTitles.editPolicy")}
                                                style={{
                                                    marginRight: 8,
                                                    background: "transparent",
                                                    border: "none",
                                                    cursor: "pointer",
                                                    fontSize: 16
                                                }}
                                            >
                                                ✏️
                                            </button>



                                            <button
                                                onClick={() => handleDelete(p.id)}
                                                title={t("actionTitles.deletePolicy")}
                                                style={{
                                                    background: "transparent",
                                                    border: "none",
                                                    cursor: "pointer",
                                                    fontSize: 16
                                                }}
                                            >
                                                🗑
                                            </button>

                                            {getCustomerPhone(p.customerId) && (
                                                <a
                                                    href={buildWhatsAppUrl(getCustomerPhone(p.customerId))}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    title={t("actionTitles.chatWhatsapp")}
                                                    style={{ marginLeft: 8, fontSize: 16, textDecoration: "none" }}
                                                >
                                                    💬
                                                </a>
                                            )}

                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )
            }

            {viewingPolicy && (
                <div
                    style={{
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
                    }}
                    onClick={closeDetail}
                >
                    <div
                        onClick={(e) => { e.stopPropagation(); setOpenDocMenuId(null); }}
                        style={{
                            background: "white",
                            borderRadius: 10,
                            padding: 24,
                            width: "90%",
                            maxWidth: 500,
                            maxHeight: "85vh",
                            overflowY: "auto",
                        }}
                    >
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
                            <h3 style={{ margin: 0 }}>{t("detail.policyTitle", { number: viewingPolicy.policyNumber })}</h3>
                            <button
                                onClick={closeDetail}
                                style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 18 }}
                            >
                                ✕
                            </button>
                        </div>

                        <h4 style={{ marginBottom: 6 }}>{t("detail.policySection")}</h4>
                        <p style={{ margin: "2px 0" }}>{t("detail.type")}: {translateEnum("policyType", viewingPolicy.type)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.insuranceCompany")}: {viewingPolicy.insuranceCompanyName}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.status")}: {translateEnum("policyStatus", viewingPolicy.status)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.period")}: {viewingPolicy.period}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.planType")}: {viewingPolicy.planType ? translateEnum("planType", viewingPolicy.planType) : "-"}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.insurancePlan")}: {viewingPolicy.insurancePlan ?? "-"}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.startDate")}: {viewingPolicy.startDate?.slice(0, 10)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.endDate")}: {viewingPolicy.endDate?.slice(0, 10)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.effectiveDate")}: {viewingPolicy.effectiveDate ? viewingPolicy.effectiveDate.slice(0, 10) : "-"}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.premium")}: {viewingPolicy.premium}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.monthlyPremiumAmount")}: {viewingPolicy.monthlyPremiumAmount ?? "-"}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.taxCreditSubsidy")}: {viewingPolicy.taxCreditSubsidy ?? "-"}</p>

                        {viewingPolicy.type === "Medicare" && (
                            <>
                                <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.medicareSection")}</h4>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.hasMedicaid")}: {viewingPolicy.hasMedicaid === null || viewingPolicy.hasMedicaid === undefined ? "-" : (viewingPolicy.hasMedicaid ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.medicaidLevel")}: {viewingPolicy.medicaidLevel ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.referredToMedicalCorporation")}: {viewingPolicy.referredToMedicalCorporation === null || viewingPolicy.referredToMedicalCorporation === undefined ? "-" : (viewingPolicy.referredToMedicalCorporation ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.medicalCorporation")}: {viewingPolicy.medicalCorporation ?? "-"}</p>
                            </>
                        )}

                        {viewingPolicy.type === "Life Insurance" && (
                            <>
                                <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.lifeInsuranceSection")}</h4>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.additionalOrAlternatePolicy")}: {viewingPolicy.additionalOrAlternatePolicy === null || viewingPolicy.additionalOrAlternatePolicy === undefined ? "-" : (viewingPolicy.additionalOrAlternatePolicy ? t("yes") : t("no"))}
                                    {viewingPolicy.additionalOrAlternatePolicy && viewingPolicy.additionalOrAlternatePolicyDetail ? ` (${viewingPolicy.additionalOrAlternatePolicyDetail})` : ""}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.underwritingRequirements")}: {viewingPolicy.underwritingRequirements ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.needsMedicalRequirements")}: {viewingPolicy.needsMedicalRequirements === null || viewingPolicy.needsMedicalRequirements === undefined ? "-" : (viewingPolicy.needsMedicalRequirements ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.billingType")}: {viewingPolicy.billingType ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>{t("detail.premiumFrequency")}: {viewingPolicy.premiumFrequency ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>{t("detail.plannedPeriodicModalPremium")}: {viewingPolicy.plannedPeriodicModalPremium ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>{t("detail.sourceOfFunds")}: {viewingPolicy.sourceOfFunds ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.hasExistingLifeInsurance")}: {viewingPolicy.hasExistingLifeInsurance === null || viewingPolicy.hasExistingLifeInsurance === undefined ? "-" : (viewingPolicy.hasExistingLifeInsurance ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.isReplacingExistingPolicy")}: {viewingPolicy.isReplacingExistingPolicy === null || viewingPolicy.isReplacingExistingPolicy === undefined ? "-" : (viewingPolicy.isReplacingExistingPolicy ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.usingFundsFromInforcePolicy")}: {viewingPolicy.usingFundsFromInforcePolicy === null || viewingPolicy.usingFundsFromInforcePolicy === undefined ? "-" : (viewingPolicy.usingFundsFromInforcePolicy ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.provideComparativeInfoForm")}: {viewingPolicy.provideComparativeInfoForm === null || viewingPolicy.provideComparativeInfoForm === undefined ? "-" : (viewingPolicy.provideComparativeInfoForm ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.physicianName")}: {viewingPolicy.physicianName ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>{t("detail.physicianAddress")}: {viewingPolicy.physicianAddress ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>{t("detail.additionalInformation")}: {viewingPolicy.additionalInformation ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.consentSigned")}: {viewingPolicy.consentSigned === null || viewingPolicy.consentSigned === undefined ? "-" : (viewingPolicy.consentSigned ? t("yes") : t("no"))}
                                </p>
                            </>
                        )}

                        {viewingPolicy.type === "Supplemental Plans" && (
                            <>
                                <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.supplementalSection")}</h4>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.hasExistingDentalCoverage")}: {viewingPolicy.hasExistingDentalCoverage === null || viewingPolicy.hasExistingDentalCoverage === undefined ? "-" : (viewingPolicy.hasExistingDentalCoverage ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.eligibleForMedicare")}: {viewingPolicy.eligibleForMedicare === null || viewingPolicy.eligibleForMedicare === undefined ? "-" : (viewingPolicy.eligibleForMedicare ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.isReplacingDentalCoverage")}: {viewingPolicy.isReplacingDentalCoverage === null || viewingPolicy.isReplacingDentalCoverage === undefined ? "-" : (viewingPolicy.isReplacingDentalCoverage ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.insuredPaysThePremium")}: {viewingPolicy.insuredPaysThePremium === null || viewingPolicy.insuredPaysThePremium === undefined ? "-" : (viewingPolicy.insuredPaysThePremium ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.bankAccountType")}: {viewingPolicy.bankAccountType ? translateEnum("bankAccountType", viewingPolicy.bankAccountType) : "-"}</p>
                                <p style={{ margin: "2px 0" }}>{t("detail.routingNumber")}: <MaskedText value={viewingPolicy.routingNumber} /></p>
                                <p style={{ margin: "2px 0" }}>{t("detail.accountNumber")}: <MaskedText value={viewingPolicy.accountNumber} /></p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.insuredIsAccountHolder")}: {viewingPolicy.insuredIsAccountHolder === null || viewingPolicy.insuredIsAccountHolder === undefined ? "-" : (viewingPolicy.insuredIsAccountHolder ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.authorizedAutomaticPayment")}: {viewingPolicy.authorizedAutomaticPayment === null || viewingPolicy.authorizedAutomaticPayment === undefined ? "-" : (viewingPolicy.authorizedAutomaticPayment ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.autoPaymentDay")}: {viewingPolicy.autoPaymentDay ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>
                                    {t("detail.authorizeMarketingInfo")}: {viewingPolicy.authorizeMarketingInfo === null || viewingPolicy.authorizeMarketingInfo === undefined ? "-" : (viewingPolicy.authorizeMarketingInfo ? t("yes") : t("no"))}
                                </p>
                                <p style={{ margin: "2px 0" }}>{t("detail.representativeName")}: {viewingPolicy.representativeName ?? "-"}</p>
                                <p style={{ margin: "2px 0" }}>{t("detail.representativeRelationship")}: {viewingPolicy.representativeRelationship ?? "-"}</p>
                            </>
                        )}

                        <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.titularSection")}</h4>
                        {(() => {
                            const titular = getCustomer(viewingPolicy.customerId);
                            if (!titular) return <p>{t("detail.unknown")}</p>;
                            return (
                                <>
                                    <p style={{ margin: "2px 0" }}>{titular.firstName} {titular.lastName}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.ssn")}: <MaskedText value={titular.socialSecurityNumber} /></p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.email")}: {titular.email}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.phone")}: {titular.phone}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.address1")}: {titular.address1}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.migrationStatus")}: {translateEnum("migrationStatus", titular.migrationStatus)}</p>
                                </>
                            );
                        })()}

                        <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.dependentsSection")}</h4>
                        <p style={{ margin: "2px 0" }}>{t("detail.numberOfApplicants")}: {viewingPolicy.numberOfApplicants ?? "-"}</p>
                        {detailDependents.length === 0 ? (
                            <p style={{ color: "#666" }}>{t("detail.noDependents")}</p>
                        ) : (
                            <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                                {detailDependents.map((d) => (
                                    <li key={d.customerId} style={{ padding: "4px 0" }}>
                                        {d.firstName} {d.lastName} (<MaskedText value={d.socialSecurityNumber} />)
                                    </li>
                                ))}
                            </ul>
                        )}

                        {viewingPolicy.type === "Life Insurance" && (
                            <>
                                <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.beneficiariesSection")}</h4>
                                {detailBeneficiaries.length === 0 ? (
                                    <p style={{ color: "#666" }}>{t("detail.noBeneficiaries")}</p>
                                ) : (
                                    <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                                        {detailBeneficiaries.map((b) => (
                                            <li key={b.id} style={{ padding: "4px 0" }}>
                                                {b.firstName} {b.lastName} ({b.typeOfRelationship})
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </>
                        )}

                        <div
                            style={{
                                border: "1px solid #ddd",
                                borderRadius: 10,
                                padding: 16,
                                marginTop: 16,
                            }}
                        >
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
                                <h4 style={{ margin: 0 }}>{t("documents.title")}</h4>
                                <label
                                    style={{
                                        display: "inline-flex",
                                        alignItems: "center",
                                        gap: 6,
                                        background: "#2563eb",
                                        color: "white",
                                        padding: "6px 12px",
                                        borderRadius: 6,
                                        cursor: uploadingDocument ? "default" : "pointer",
                                        opacity: uploadingDocument ? 0.6 : 1,
                                        fontSize: 14,
                                    }}
                                >
                                    📎 {uploadingDocument ? t("documents.uploading") : t("documents.newButton")}
                                    <input
                                        type="file"
                                        accept={ALLOWED_DOCUMENT_EXTENSIONS.join(",")}
                                        onChange={handleUploadDocument}
                                        disabled={uploadingDocument}
                                        style={{ display: "none" }}
                                    />
                                </label>
                            </div>

                            {documentError && (
                                <p style={{ color: "red", margin: "0 0 8px" }}>{documentError}</p>
                            )}

                            {detailDocuments.length === 0 ? (
                                <p style={{ color: "#666" }}>{t("documents.empty")}</p>
                            ) : (
                                <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                                    {detailDocuments.map((doc) => (
                                        <li
                                            key={doc.id}
                                            style={{
                                                display: "flex",
                                                justifyContent: "space-between",
                                                alignItems: "flex-start",
                                                padding: "10px 0",
                                                borderBottom: "1px solid #eee",
                                                position: "relative",
                                            }}
                                        >
                                            <div>
                                                <span style={{ color: "#2563eb", fontWeight: 500 }}>
                                                    {doc.originalFileName}
                                                </span>
                                                <div style={{ color: "#666", fontSize: 12, marginTop: 2 }}>
                                                    {formatFileSize(doc.sizeBytes)} - {formatDocumentDate(doc.uploadedAt)}
                                                </div>
                                            </div>

                                            <div style={{ position: "relative" }}>
                                                <button
                                                    type="button"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        setOpenDocMenuId(openDocMenuId === doc.id ? null : doc.id);
                                                    }}
                                                    title={t("documents.optionsTitle")}
                                                    style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 18, padding: "0 6px", lineHeight: 1 }}
                                                >
                                                    ⋮
                                                </button>

                                                {openDocMenuId === doc.id && (
                                                    <div
                                                        style={{
                                                            position: "absolute",
                                                            right: 0,
                                                            top: "100%",
                                                            background: "white",
                                                            border: "1px solid #ddd",
                                                            borderRadius: 6,
                                                            boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
                                                            zIndex: 10,
                                                            minWidth: 130,
                                                            overflow: "hidden",
                                                        }}
                                                    >
                                                        <button
                                                            type="button"
                                                            onClick={() => {
                                                                setOpenDocMenuId(null);
                                                                handleDownloadDocument(doc);
                                                            }}
                                                            style={{
                                                                display: "block",
                                                                width: "100%",
                                                                textAlign: "left",
                                                                background: "transparent",
                                                                border: "none",
                                                                padding: "8px 12px",
                                                                cursor: "pointer",
                                                                fontSize: 14,
                                                            }}
                                                        >
                                                            {t("documents.download")}
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() => {
                                                                setOpenDocMenuId(null);
                                                                handleDeleteDocument(doc);
                                                            }}
                                                            style={{
                                                                display: "block",
                                                                width: "100%",
                                                                textAlign: "left",
                                                                background: "transparent",
                                                                border: "none",
                                                                padding: "8px 12px",
                                                                cursor: "pointer",
                                                                fontSize: 14,
                                                                color: "#dc2626",
                                                            }}
                                                        >
                                                            {t("documents.delete")}
                                                        </button>
                                                    </div>
                                                )}
                                            </div>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div >
    );

}

export default Policies;
