using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using WholeCareInsurance.api.DTOs.Policies;
using WholeCareInsurance.api.Models;
using WholeCareInsurance.api.Services;
using WholeCareInsurance.api.Utils;

namespace WholeCareInsurance.api.Controllers
{
    [ApiController]
    [Route("api/policies")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policies;
        private readonly ICustomerService _customers;
        private readonly IInsuranceCompanyService _insuranceCompanies;
        private readonly IPolicyDocumentStorage _documentStorage;
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        public PoliciesController(IPolicyService policies, ICustomerService customers, IInsuranceCompanyService insuranceCompanies, IPolicyDocumentStorage documentStorage)
        {
            _policies = policies;
            _customers = customers;
            _insuranceCompanies = insuranceCompanies;
            _documentStorage = documentStorage;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? customerId = null,
            [FromQuery] string? firstName = null,
            [FromQuery] string? lastName = null,
            [FromQuery] string? policyNumber = null,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] int? insuranceCompanyId = null,
            [FromQuery] int? period = null)
        {
            var found = await _policies.Search(customerId, firstName, lastName, policyNumber, status, type, insuranceCompanyId, period);
            return Ok(found.Select(ToResponse));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();
            return Ok(ToResponse(policy));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PolicyCreateDto dto)
        {
            var customer = await _customers.GetById(dto.CustomerId);
            if (customer == null)
                return BadRequest(new ProblemDetails { Title = $"CustomerId {dto.CustomerId} no existe." });

            var insuranceCompany = await _insuranceCompanies.GetById(dto.InsuranceCompanyId);
            if (insuranceCompany == null)
                return BadRequest(new ProblemDetails { Title = $"InsuranceCompanyId {dto.InsuranceCompanyId} no existe." });

            var policy = new Policy
            {
                PolicyNumber = dto.PolicyNumber,
                Type = dto.Type,
                InsuranceCompanyId = dto.InsuranceCompanyId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Premium = dto.Premium,
                Status = dto.Status,
                Period = dto.Period,
                NumberOfApplicants = dto.NumberOfApplicants,
                CustomerId = dto.CustomerId,
                PlanType = dto.PlanType,
                InsurancePlan = dto.InsurancePlan,
                EffectiveDate = dto.EffectiveDate,
                TaxCreditSubsidy = dto.TaxCreditSubsidy,
                MonthlyPremiumAmount = dto.MonthlyPremiumAmount,
                HasMedicaid = dto.HasMedicaid,
                MedicaidLevel = dto.MedicaidLevel,
                ReferredToMedicalCorporation = dto.ReferredToMedicalCorporation,
                MedicalCorporation = dto.MedicalCorporation,
                AdditionalOrAlternatePolicy = dto.AdditionalOrAlternatePolicy,
                AdditionalOrAlternatePolicyDetail = dto.AdditionalOrAlternatePolicyDetail,
                UnderwritingRequirements = dto.UnderwritingRequirements,
                NeedsMedicalRequirements = dto.NeedsMedicalRequirements,
                BillingType = dto.BillingType,
                PremiumFrequency = dto.PremiumFrequency,
                PlannedPeriodicModalPremium = dto.PlannedPeriodicModalPremium,
                SourceOfFunds = dto.SourceOfFunds,
                HasExistingLifeInsurance = dto.HasExistingLifeInsurance,
                IsReplacingExistingPolicy = dto.IsReplacingExistingPolicy,
                UsingFundsFromInforcePolicy = dto.UsingFundsFromInforcePolicy,
                ProvideComparativeInfoForm = dto.ProvideComparativeInfoForm,
                PhysicianName = dto.PhysicianName,
                PhysicianAddress = dto.PhysicianAddress,
                AdditionalInformation = dto.AdditionalInformation,
                ConsentSigned = dto.ConsentSigned,
                HasExistingDentalCoverage = dto.HasExistingDentalCoverage,
                EligibleForMedicare = dto.EligibleForMedicare,
                IsReplacingDentalCoverage = dto.IsReplacingDentalCoverage,
                InsuredPaysThePremium = dto.InsuredPaysThePremium,
                BankAccountType = dto.BankAccountType,
                RoutingNumber = dto.RoutingNumber,
                AccountNumber = dto.AccountNumber,
                InsuredIsAccountHolder = dto.InsuredIsAccountHolder,
                AuthorizedAutomaticPayment = dto.AuthorizedAutomaticPayment,
                AutoPaymentDay = dto.AutoPaymentDay,
                AuthorizeMarketingInfo = dto.AuthorizeMarketingInfo,
                RepresentativeName = dto.RepresentativeName,
                RepresentativeRelationship = dto.RepresentativeRelationship
            };

            var created = await _policies.Create(policy);
            created = await _policies.GetById(created.Id) ?? created;

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PolicyUpdateDto dto)
        {
            var existing = await _policies.GetById(id);
            if (existing == null) return NotFound();

            if (dto.CustomerId != existing.CustomerId)
            {
                var customer = await _customers.GetById(dto.CustomerId);
                if (customer == null)
                    return BadRequest(new ProblemDetails { Title = $"CustomerId {dto.CustomerId} no existe." });
                existing.CustomerId = dto.CustomerId;
            }

            if (dto.InsuranceCompanyId != existing.InsuranceCompanyId)
            {
                var insuranceCompany = await _insuranceCompanies.GetById(dto.InsuranceCompanyId);
                if (insuranceCompany == null)
                    return BadRequest(new ProblemDetails { Title = $"InsuranceCompanyId {dto.InsuranceCompanyId} no existe." });
                existing.InsuranceCompanyId = dto.InsuranceCompanyId;
            }

            existing.PolicyNumber = dto.PolicyNumber;
            existing.Type = dto.Type;
            existing.StartDate = dto.StartDate;
            existing.EndDate = dto.EndDate;
            existing.Premium = dto.Premium;
            existing.Status = dto.Status;
            existing.Period = dto.Period;
            existing.NumberOfApplicants = dto.NumberOfApplicants;
            existing.PlanType = dto.PlanType;
            existing.InsurancePlan = dto.InsurancePlan;
            existing.EffectiveDate = dto.EffectiveDate;
            existing.TaxCreditSubsidy = dto.TaxCreditSubsidy;
            existing.MonthlyPremiumAmount = dto.MonthlyPremiumAmount;
            existing.HasMedicaid = dto.HasMedicaid;
            existing.MedicaidLevel = dto.MedicaidLevel;
            existing.ReferredToMedicalCorporation = dto.ReferredToMedicalCorporation;
            existing.MedicalCorporation = dto.MedicalCorporation;
            existing.AdditionalOrAlternatePolicy = dto.AdditionalOrAlternatePolicy;
            existing.AdditionalOrAlternatePolicyDetail = dto.AdditionalOrAlternatePolicyDetail;
            existing.UnderwritingRequirements = dto.UnderwritingRequirements;
            existing.NeedsMedicalRequirements = dto.NeedsMedicalRequirements;
            existing.BillingType = dto.BillingType;
            existing.PremiumFrequency = dto.PremiumFrequency;
            existing.PlannedPeriodicModalPremium = dto.PlannedPeriodicModalPremium;
            existing.SourceOfFunds = dto.SourceOfFunds;
            existing.HasExistingLifeInsurance = dto.HasExistingLifeInsurance;
            existing.IsReplacingExistingPolicy = dto.IsReplacingExistingPolicy;
            existing.UsingFundsFromInforcePolicy = dto.UsingFundsFromInforcePolicy;
            existing.ProvideComparativeInfoForm = dto.ProvideComparativeInfoForm;
            existing.PhysicianName = dto.PhysicianName;
            existing.PhysicianAddress = dto.PhysicianAddress;
            existing.AdditionalInformation = dto.AdditionalInformation;
            existing.ConsentSigned = dto.ConsentSigned;
            existing.HasExistingDentalCoverage = dto.HasExistingDentalCoverage;
            existing.EligibleForMedicare = dto.EligibleForMedicare;
            existing.IsReplacingDentalCoverage = dto.IsReplacingDentalCoverage;
            existing.InsuredPaysThePremium = dto.InsuredPaysThePremium;
            existing.BankAccountType = dto.BankAccountType;
            existing.RoutingNumber = dto.RoutingNumber;
            existing.AccountNumber = dto.AccountNumber;
            existing.InsuredIsAccountHolder = dto.InsuredIsAccountHolder;
            existing.AuthorizedAutomaticPayment = dto.AuthorizedAutomaticPayment;
            existing.AutoPaymentDay = dto.AutoPaymentDay;
            existing.AuthorizeMarketingInfo = dto.AuthorizeMarketingInfo;
            existing.RepresentativeName = dto.RepresentativeName;
            existing.RepresentativeRelationship = dto.RepresentativeRelationship;

            var updated = await _policies.Update(existing);
            updated = await _policies.GetById(updated.Id) ?? updated;

            return Ok(ToResponse(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            await _policies.Delete(policy);
            return NoContent();
        }

        [HttpGet("{id:int}/dependents")]
        public async Task<IActionResult> GetDependents(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            var dependents = (await _policies.GetDependents(id))
                .Select(pd => new DependentResponseDto
                {
                    CustomerId = pd.Customer.Id,
                    FirstName = pd.Customer.FirstName,
                    LastName = pd.Customer.LastName,
                    SocialSecurityNumber = pd.Customer.SocialSecurityNumber,
                    IsAplicante = pd.IsAplicante
                });

            return Ok(dependents);
        }

        [HttpPost("{id:int}/dependents")]
        public async Task<IActionResult> AddDependent(int id, [FromBody] DependentCreateDto dto)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            var customer = await _customers.GetById(dto.CustomerId);
            if (customer == null)
                return BadRequest(new ProblemDetails { Title = $"CustomerId {dto.CustomerId} no existe." });

            if (dto.CustomerId == policy.CustomerId)
                return BadRequest(new ProblemDetails { Title = "El titular no puede ser su propio dependiente." });

            if (await _policies.GetDependent(id, dto.CustomerId) != null)
                return BadRequest(new ProblemDetails { Title = "Ya es dependiente de esta póliza." });

            var created = await _policies.AddDependent(new PolicyDependent
            {
                PolicyId = id,
                CustomerId = dto.CustomerId,
                IsAplicante = dto.IsAplicante
            });

            return CreatedAtAction(nameof(GetDependents), new { id }, new DependentResponseDto
            {
                CustomerId = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                SocialSecurityNumber = customer.SocialSecurityNumber,
                IsAplicante = created.IsAplicante
            });
        }

        [HttpPut("{id:int}/dependents/{customerId:int}")]
        public async Task<IActionResult> UpdateDependent(int id, int customerId, [FromBody] DependentUpdateDto dto)
        {
            var dependent = await _policies.GetDependent(id, customerId);
            if (dependent == null) return NotFound();

            dependent.IsAplicante = dto.IsAplicante;
            var updated = await _policies.UpdateDependent(dependent);

            return Ok(new DependentResponseDto
            {
                CustomerId = updated.Customer.Id,
                FirstName = updated.Customer.FirstName,
                LastName = updated.Customer.LastName,
                SocialSecurityNumber = updated.Customer.SocialSecurityNumber,
                IsAplicante = updated.IsAplicante
            });
        }

        [HttpDelete("{id:int}/dependents/{customerId:int}")]
        public async Task<IActionResult> RemoveDependent(int id, int customerId)
        {
            var dependent = await _policies.GetDependent(id, customerId);
            if (dependent == null) return NotFound();

            await _policies.RemoveDependent(dependent);
            return NoContent();
        }

        [HttpGet("{id:int}/documents")]
        public async Task<IActionResult> GetDocuments(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            var documents = (await _policies.GetDocuments(id))
                .Select(ToDocumentResponse);

            return Ok(documents);
        }

        [HttpPost("{id:int}/documents")]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = FileValidationHelper.MaxFileSizeBytes)]
        public async Task<IActionResult> UploadDocument(int id, IFormFile file)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest(new ProblemDetails { Title = "Debe adjuntar un archivo." });

            if (file.Length > FileValidationHelper.MaxFileSizeBytes)
                return BadRequest(new ProblemDetails { Title = "El archivo supera el tamaño máximo permitido (5 MB)." });

            if (!FileValidationHelper.HasAllowedExtension(file.FileName))
                return BadRequest(new ProblemDetails { Title = "Tipo de archivo no permitido. Se aceptan: .pdf, .docx, .jpg, .jpeg." });

            var extension = Path.GetExtension(file.FileName);

            using var stream = file.OpenReadStream();
            if (!await FileValidationHelper.MatchesContentAsync(stream, extension))
                return BadRequest(new ProblemDetails { Title = "El contenido del archivo no coincide con su extensión." });

            var storedFileName = await _documentStorage.SaveAsync(id, stream, extension);

            if (!ContentTypeProvider.TryGetContentType(file.FileName, out var contentType))
                contentType = "application/octet-stream";

            var created = await _policies.AddDocument(new PolicyDocument
            {
                PolicyId = id,
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                ContentType = contentType,
                SizeBytes = file.Length,
                UploadedAt = DateTime.UtcNow
            });

            return CreatedAtAction(nameof(GetDocuments), new { id }, ToDocumentResponse(created));
        }

        [HttpGet("{id:int}/documents/{documentId:int}")]
        public async Task<IActionResult> DownloadDocument(int id, int documentId)
        {
            var document = await _policies.GetDocument(id, documentId);
            if (document == null) return NotFound();

            var path = _documentStorage.GetPhysicalPath(id, document.StoredFileName);
            if (!System.IO.File.Exists(path))
                return NotFound("El archivo ya no está disponible en el servidor.");

            return PhysicalFile(path, document.ContentType, document.OriginalFileName);
        }

        [HttpDelete("{id:int}/documents/{documentId:int}")]
        public async Task<IActionResult> DeleteDocument(int id, int documentId)
        {
            var document = await _policies.GetDocument(id, documentId);
            if (document == null) return NotFound();

            await _policies.RemoveDocument(document);
            return NoContent();
        }

        [HttpGet("{id:int}/beneficiaries")]
        public async Task<IActionResult> GetBeneficiaries(int id)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            var beneficiaries = (await _policies.GetBeneficiaries(id)).Select(ToBeneficiaryResponse);
            return Ok(beneficiaries);
        }

        [HttpPost("{id:int}/beneficiaries")]
        public async Task<IActionResult> AddBeneficiary(int id, [FromBody] BeneficiaryCreateDto dto)
        {
            var policy = await _policies.GetById(id);
            if (policy == null) return NotFound();

            var created = await _policies.AddBeneficiary(new PolicyBeneficiary
            {
                PolicyId = id,
                TypeOfRelationship = dto.TypeOfRelationship,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Phone = dto.Phone,
                Email = dto.Email,
                SocialSecurityNumber = dto.SocialSecurityNumber
            });

            return CreatedAtAction(nameof(GetBeneficiaries), new { id }, ToBeneficiaryResponse(created));
        }

        [HttpDelete("{id:int}/beneficiaries/{beneficiaryId:int}")]
        public async Task<IActionResult> RemoveBeneficiary(int id, int beneficiaryId)
        {
            var beneficiary = await _policies.GetBeneficiary(id, beneficiaryId);
            if (beneficiary == null) return NotFound();

            await _policies.RemoveBeneficiary(beneficiary);
            return NoContent();
        }

        private static PolicyResponseDto ToResponse(Policy p) => new()
        {
            Id = p.Id,
            PolicyNumber = p.PolicyNumber,
            Type = p.Type,
            InsuranceCompanyId = p.InsuranceCompanyId,
            InsuranceCompanyName = p.InsuranceCompany.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Premium = p.Premium,
            Status = p.Status,
            Period = p.Period,
            NumberOfApplicants = p.NumberOfApplicants,
            CustomerId = p.CustomerId,
            PlanType = p.PlanType,
            InsurancePlan = p.InsurancePlan,
            EffectiveDate = p.EffectiveDate,
            TaxCreditSubsidy = p.TaxCreditSubsidy,
            MonthlyPremiumAmount = p.MonthlyPremiumAmount,
            HasMedicaid = p.HasMedicaid,
            MedicaidLevel = p.MedicaidLevel,
            ReferredToMedicalCorporation = p.ReferredToMedicalCorporation,
            MedicalCorporation = p.MedicalCorporation,
            AdditionalOrAlternatePolicy = p.AdditionalOrAlternatePolicy,
            AdditionalOrAlternatePolicyDetail = p.AdditionalOrAlternatePolicyDetail,
            UnderwritingRequirements = p.UnderwritingRequirements,
            NeedsMedicalRequirements = p.NeedsMedicalRequirements,
            BillingType = p.BillingType,
            PremiumFrequency = p.PremiumFrequency,
            PlannedPeriodicModalPremium = p.PlannedPeriodicModalPremium,
            SourceOfFunds = p.SourceOfFunds,
            HasExistingLifeInsurance = p.HasExistingLifeInsurance,
            IsReplacingExistingPolicy = p.IsReplacingExistingPolicy,
            UsingFundsFromInforcePolicy = p.UsingFundsFromInforcePolicy,
            ProvideComparativeInfoForm = p.ProvideComparativeInfoForm,
            PhysicianName = p.PhysicianName,
            PhysicianAddress = p.PhysicianAddress,
            AdditionalInformation = p.AdditionalInformation,
            ConsentSigned = p.ConsentSigned,
            HasExistingDentalCoverage = p.HasExistingDentalCoverage,
            EligibleForMedicare = p.EligibleForMedicare,
            IsReplacingDentalCoverage = p.IsReplacingDentalCoverage,
            InsuredPaysThePremium = p.InsuredPaysThePremium,
            BankAccountType = p.BankAccountType,
            RoutingNumber = p.RoutingNumber,
            AccountNumber = p.AccountNumber,
            InsuredIsAccountHolder = p.InsuredIsAccountHolder,
            AuthorizedAutomaticPayment = p.AuthorizedAutomaticPayment,
            AutoPaymentDay = p.AutoPaymentDay,
            AuthorizeMarketingInfo = p.AuthorizeMarketingInfo,
            RepresentativeName = p.RepresentativeName,
            RepresentativeRelationship = p.RepresentativeRelationship
        };

        private static PolicyDocumentResponseDto ToDocumentResponse(PolicyDocument d) => new()
        {
            Id = d.Id,
            OriginalFileName = d.OriginalFileName,
            ContentType = d.ContentType,
            SizeBytes = d.SizeBytes,
            UploadedAt = d.UploadedAt
        };

        private static BeneficiaryResponseDto ToBeneficiaryResponse(PolicyBeneficiary b) => new()
        {
            Id = b.Id,
            TypeOfRelationship = b.TypeOfRelationship,
            FirstName = b.FirstName,
            LastName = b.LastName,
            DateOfBirth = b.DateOfBirth,
            Gender = b.Gender,
            Phone = b.Phone,
            Email = b.Email,
            SocialSecurityNumber = b.SocialSecurityNumber
        };
    }
}
