(() => {
    "use strict";

    const focusWithoutJump = (element) => {
        if (!element) return;
        element.focus({ preventScroll: true });
        element.scrollIntoView({ behavior: "smooth", block: "start" });
    };

    function controlValue(control) {
        if (control instanceof HTMLSelectElement) {
            return control.selectedOptions[0]?.text?.trim() || "";
        }

        if (control instanceof HTMLInputElement && control.type === "file") {
            return control.files?.[0]?.name || "";
        }

        if (control.type === "checkbox" || control.type === "radio") {
            return control.checked ? "Yes" : "";
        }

        if (control.type === "date" && control.value) {
            const [year, month, day] = control.value.split("-").map(Number);
            return new Intl.DateTimeFormat(undefined, {
                year: "numeric",
                month: "short",
                day: "numeric"
            }).format(new Date(year, month - 1, day));
        }

        return control.value?.trim() || "";
    }

    function renderReview(form) {
        const review = form.querySelector("[data-wizard-review]");
        if (!review) return;

        const list = document.createElement("dl");
        list.className = "review-list mb-0";

        form.querySelectorAll("[data-review-label]").forEach((control) => {
            if (!(control instanceof HTMLInputElement ||
                  control instanceof HTMLSelectElement ||
                  control instanceof HTMLTextAreaElement)) return;

            const value = controlValue(control);
            if (!value || value.toLowerCase().startsWith("select ")) return;

            const row = document.createElement("div");
            row.className = "review-row";

            const term = document.createElement("dt");
            term.textContent = control.dataset.reviewLabel;

            const description = document.createElement("dd");
            description.textContent = value;

            row.append(term, description);
            list.append(row);
        });

        review.replaceChildren(list);
    }

    function initializeWizard(form) {
        if (form.dataset.wizardInitialized === "true") return;
        form.dataset.wizardInitialized = "true";

        const steps = [...form.querySelectorAll("[data-wizard-step]")];
        const indicators = [...form.querySelectorAll("[data-step-indicator]")];
        const back = form.querySelector("[data-wizard-back]");
        const next = form.querySelector("[data-wizard-next]");
        const submit = form.querySelector("[data-wizard-submit]");
        const stepCount = form.querySelector(".workflow-step-count");
        const progress = form.querySelector('[role="progressbar"]');
        const progressBar = progress?.querySelector(".progress-bar");

        if (!steps.length || !back || !next || !submit) return;

        const serverError = form.querySelector(".field-validation-error");
        const errorStep = serverError ? steps.findIndex((step) => step.contains(serverError)) : -1;
        let current = errorStep >= 0
            ? errorStep
            : Math.min(Number.parseInt(form.dataset.startStep || "0", 10), steps.length - 1);

        const showStep = (index, moveFocus = true) => {
            current = Math.max(0, Math.min(index, steps.length - 1));

            steps.forEach((step, stepIndex) => {
                step.hidden = stepIndex !== current;
            });

            indicators.forEach((indicator, stepIndex) => {
                indicator.classList.toggle("is-current", stepIndex === current);
                indicator.classList.toggle("is-complete", stepIndex < current);
                if (stepIndex === current) {
                    indicator.setAttribute("aria-current", "step");
                } else {
                    indicator.removeAttribute("aria-current");
                }
            });

            const humanStep = current + 1;
            const percent = Math.round((humanStep / steps.length) * 100);
            if (stepCount) stepCount.textContent = `Step ${humanStep} of ${steps.length}`;
            if (progress) progress.setAttribute("aria-valuenow", String(humanStep));
            if (progressBar) progressBar.style.width = `${percent}%`;

            back.hidden = current === 0;
            next.hidden = current === steps.length - 1;
            submit.hidden = current !== steps.length - 1;

            if (current === steps.length - 1) renderReview(form);

            if (moveFocus) {
                const heading = steps[current].querySelector("h2");
                if (heading) {
                    heading.setAttribute("tabindex", "-1");
                    focusWithoutJump(heading);
                }
            }
        };

        const validateCurrentStep = () => {
            const controls = [...steps[current].querySelectorAll("input, select, textarea")]
                .filter((control) => !control.disabled && control.type !== "hidden");
            let isValid = true;
            let firstInvalid = null;

            controls.forEach((control) => {
                let controlIsValid;
                if (window.jQuery?.validator && window.jQuery(control).rules) {
                    controlIsValid = window.jQuery(control).valid();
                } else {
                    controlIsValid = control.checkValidity();
                }

                control.classList.toggle("is-invalid", !controlIsValid);
                if (!controlIsValid) {
                    isValid = false;
                    firstInvalid ??= control;
                }
            });

            if (firstInvalid) {
                firstInvalid.focus();
                firstInvalid.reportValidity?.();
            }

            return isValid;
        };

        next.addEventListener("click", () => {
            if (validateCurrentStep()) showStep(current + 1);
        });
        back.addEventListener("click", () => showStep(current - 1));

        form.addEventListener("invalid", (event) => {
            const invalidStep = steps.findIndex((step) => step.contains(event.target));
            if (invalidStep >= 0 && invalidStep !== current) showStep(invalidStep, false);
        }, true);

        showStep(current, false);

        const summary = form.querySelector(".validation-summary-errors");
        if (summary) focusWithoutJump(summary);
    }

    async function populateSelect(select, url, placeholder, selectedValue) {
        select.disabled = true;
        select.replaceChildren(new Option(placeholder, ""));
        select.dispatchEvent(new CustomEvent("optionsloading"));

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (!response.ok) throw new Error(`Request failed with ${response.status}`);
            const options = await response.json();
            options.forEach((item) => select.add(new Option(item.name, item.id)));
            select.disabled = false;
            if (selectedValue && selectedValue !== "0") select.value = selectedValue;
            select.dispatchEvent(new CustomEvent("optionsloaded"));
        } catch {
            select.replaceChildren(new Option("Could not load options — try again", ""));
            select.disabled = false;
            select.dispatchEvent(new CustomEvent("optionsloaded"));
        }
    }

    function initializePatientPhoto(form) {
        const field = form.querySelector("[data-patient-photo]");
        if (!field) return;

        const input = field.querySelector("[data-photo-input]");
        const image = field.querySelector("[data-photo-image]");
        const placeholder = field.querySelector("[data-photo-placeholder]");
        const remove = field.querySelector("[data-photo-remove]");
        const removeValue = field.querySelector("[data-remove-photo-value]");
        const chooseText = field.querySelector("[data-photo-choose-text]");
        const status = field.querySelector("[data-photo-status]");
        let previewUrl = null;

        const showPlaceholder = () => {
            image?.classList.add("d-none");
            placeholder?.classList.remove("d-none");
        };

        if (removeValue?.value === "true") {
            remove?.classList.add("d-none");
            if (chooseText) chooseText.textContent = "Choose photo";
            showPlaceholder();
        }

        input?.addEventListener("change", () => {
            const file = input.files?.[0];
            input.setCustomValidity("");
            if (!file) return;

            if (!["image/jpeg", "image/png"].includes(file.type)) {
                input.setCustomValidity("Use a JPG or PNG image.");
                input.reportValidity();
                input.value = "";
                return;
            }

            if (file.size > 5 * 1024 * 1024) {
                input.setCustomValidity("The patient photo must be 5 MB or smaller.");
                input.reportValidity();
                input.value = "";
                return;
            }

            if (previewUrl) URL.revokeObjectURL(previewUrl);
            previewUrl = URL.createObjectURL(file);
            if (image) {
                image.src = previewUrl;
                image.alt = `Selected patient photo: ${file.name}`;
                image.classList.remove("d-none");
            }
            placeholder?.classList.add("d-none");
            remove?.classList.remove("d-none");
            if (removeValue) removeValue.value = "false";
            if (chooseText) chooseText.textContent = "Replace photo";
            if (status) {
                status.textContent = `${file.name} selected`;
                status.classList.remove("d-none", "text-secondary");
                status.classList.add("text-success");
            }
            form.dataset.dirty = "true";
        });

        remove?.addEventListener("click", () => {
            if (previewUrl) {
                URL.revokeObjectURL(previewUrl);
                previewUrl = null;
            }
            if (input) input.value = "";
            if (removeValue) removeValue.value = "true";
            if (chooseText) chooseText.textContent = "Choose photo";
            if (status) {
                status.textContent = "Patient photo will be removed when you save.";
                status.classList.remove("d-none", "text-success");
                status.classList.add("text-secondary");
            }
            remove.classList.add("d-none");
            showPlaceholder();
            form.dataset.dirty = "true";
        });
    }

    function initializeSearchableCity(form, select) {
        const field = select.closest("[data-city-field]");
        const cityOther = field?.querySelector("#CityOther");
        if (!field || !cityOther || field.classList.contains("city-field-enhanced")) return;

        field.classList.add("city-field-enhanced");

        const combobox = document.createElement("div");
        combobox.className = "city-combobox";

        const input = document.createElement("input");
        input.type = "search";
        input.className = "form-control city-combobox-input";
        input.placeholder = "Search or enter city";
        input.autocomplete = "off";
        input.required = true;
        input.setAttribute("role", "combobox");
        input.setAttribute("aria-autocomplete", "list");
        input.setAttribute("aria-expanded", "false");
        input.setAttribute("aria-controls", `${select.id}SearchResults`);
        input.setAttribute("aria-label", "City");

        const list = document.createElement("div");
        list.id = `${select.id}SearchResults`;
        list.className = "city-combobox-list";
        list.setAttribute("role", "listbox");
        list.hidden = true;

        combobox.append(input, list);
        select.insertAdjacentElement("afterend", combobox);

        let options = [];
        let activeIndex = -1;

        const close = () => {
            list.hidden = true;
            input.setAttribute("aria-expanded", "false");
            input.removeAttribute("aria-activedescendant");
            activeIndex = -1;
        };

        const chooseOption = (option) => {
            select.value = option.value;
            cityOther.value = "";
            input.value = option.text;
            input.setCustomValidity("");
            select.dispatchEvent(new Event("change", { bubbles: true }));
            close();
            form.dataset.dirty = "true";
        };

        const render = () => {
            const query = input.value.trim().toLocaleLowerCase();
            const matches = options
                .filter((option) => !query || option.text.toLocaleLowerCase().includes(query))
                .slice(0, 10);
            list.replaceChildren();
            activeIndex = -1;

            if (!matches.length) {
                const empty = document.createElement("div");
                empty.className = "city-combobox-empty";
                empty.textContent = query
                    ? `Use “${input.value.trim()}” as a city not listed`
                    : "No cities available for this district";
                list.append(empty);
            } else {
                matches.forEach((option, index) => {
                    const button = document.createElement("button");
                    button.type = "button";
                    button.id = `${list.id}Option${index}`;
                    button.className = "city-combobox-option";
                    button.setAttribute("role", "option");
                    button.textContent = option.text;
                    button.addEventListener("mousedown", (event) => event.preventDefault());
                    button.addEventListener("click", () => chooseOption(option));
                    list.append(button);
                });
            }

            list.hidden = false;
            input.setAttribute("aria-expanded", "true");
        };

        const setActive = (index) => {
            const items = [...list.querySelectorAll(".city-combobox-option")];
            if (!items.length) return;
            activeIndex = (index + items.length) % items.length;
            items.forEach((item, itemIndex) => item.classList.toggle("is-active", itemIndex === activeIndex));
            const active = items[activeIndex];
            input.setAttribute("aria-activedescendant", active.id);
            active.scrollIntoView({ block: "nearest" });
        };

        const syncOptions = () => {
            options = [...select.options]
                .filter((option) => option.value)
                .map((option) => ({ value: option.value, text: option.text.trim() }));
            const selected = options.find((option) => option.value === select.value);
            input.disabled = select.disabled;
            input.value = selected?.text || cityOther.value || "";
            input.setCustomValidity(input.value.trim() ? "" : "Select or enter a city.");
            close();
        };

        input.addEventListener("focus", render);
        input.addEventListener("input", () => {
            const value = input.value.trim();
            const exact = options.find((option) =>
                option.text.localeCompare(value, undefined, { sensitivity: "accent" }) === 0);
            select.value = exact?.value || "";
            cityOther.value = exact ? "" : value;
            input.setCustomValidity(value ? "" : "Select or enter a city.");
            render();
            form.dataset.dirty = "true";
        });
        input.addEventListener("blur", () => window.setTimeout(close, 120));
        input.addEventListener("keydown", (event) => {
            if (event.key === "ArrowDown") {
                event.preventDefault();
                if (list.hidden) render();
                setActive(activeIndex + 1);
            } else if (event.key === "ArrowUp") {
                event.preventDefault();
                if (list.hidden) render();
                setActive(activeIndex - 1);
            } else if (event.key === "Enter" && !list.hidden && activeIndex >= 0) {
                event.preventDefault();
                const item = list.querySelectorAll(".city-combobox-option")[activeIndex];
                item?.click();
            } else if (event.key === "Escape") {
                close();
            }
        });

        select.addEventListener("optionsloading", () => {
            input.disabled = true;
            input.value = "";
            close();
        });
        select.addEventListener("optionsloaded", syncOptions);
        syncOptions();
    }

    function initializePatientForm(form) {
        if (form.dataset.patientInitialized === "true") return;
        form.dataset.patientInitialized = "true";

        const category = form.querySelector("#Category");
        const nationalityField = form.querySelector("#nationalityField");
        const nationality = nationalityField?.querySelector("input");
        const referral = form.querySelector("#ReferralSourceId");
        const referralOther = form.querySelector("#referralOtherField");
        const province = form.querySelector("#ProvinceId");
        const district = form.querySelector("#DistrictId");
        const city = form.querySelector("#CityId");
        const contacts = form.querySelector("#contactsContainer");
        const assigneeEntry = form.querySelector("[data-assignee-entry]");
        const assigneeUserId = form.querySelector("[data-assignee-user-id]");
        initializePatientPhoto(form);
        if (city) initializeSearchableCity(form, city);

        const toggleNationality = () => {
            const isForeign = category?.value === "Foreign";
            if (nationalityField) nationalityField.hidden = !isForeign;
            if (nationality) nationality.required = isForeign;
        };

        const toggleReferralOther = () => {
            const isOther = referral?.selectedOptions[0]?.text?.trim().toLowerCase() === "other";
            if (referralOther) referralOther.hidden = !isOther;
        };

        category?.addEventListener("change", toggleNationality);
        referral?.addEventListener("change", toggleReferralOther);
        toggleNationality();
        toggleReferralOther();

        const syncAssignee = () => {
            if (!assigneeEntry || !assigneeUserId) return;
            const entered = assigneeEntry.value.trim().toLocaleLowerCase();
            const matched = [...form.querySelectorAll("#patientAssigneeOptions option")]
                .find((option) => option.value.trim().toLocaleLowerCase() === entered);
            assigneeUserId.value = matched?.dataset.userId || "";
        };
        assigneeEntry?.addEventListener("input", syncAssignee);
        assigneeEntry?.addEventListener("change", syncAssignee);
        form.addEventListener("submit", syncAssignee);
        syncAssignee();

        if (province && district && city) {
            const selectedDistrict = district.dataset.selected;
            const selectedCity = city.dataset.selected;

            province.addEventListener("change", async () => {
                city.disabled = true;
                city.replaceChildren(new Option("Select city", ""));
                city.dispatchEvent(new CustomEvent("optionsloaded"));
                const cityOther = form.querySelector("#CityOther");
                if (cityOther) cityOther.value = "";
                if (!province.value) {
                    district.disabled = true;
                    district.replaceChildren(new Option("Select district", ""));
                    return;
                }

                await populateSelect(
                    district,
                    `/Patients/GetDistrictsByProvince?provinceId=${encodeURIComponent(province.value)}`,
                    "Select district",
                    ""
                );
            });

            district.addEventListener("change", async () => {
                if (!district.value) {
                    city.disabled = true;
                    city.replaceChildren(new Option("Select city", ""));
                    city.dispatchEvent(new CustomEvent("optionsloaded"));
                    return;
                }

                await populateSelect(
                    city,
                    `/Patients/GetCitiesByDistrict?districtId=${encodeURIComponent(district.value)}`,
                    "Select city",
                    ""
                );
            });

            if (province.value) {
                populateSelect(
                    district,
                    `/Patients/GetDistrictsByProvince?provinceId=${encodeURIComponent(province.value)}`,
                    "Select district",
                    selectedDistrict
                ).then(() => {
                    if (!district.value) return;
                    return populateSelect(
                        city,
                        `/Patients/GetCitiesByDistrict?districtId=${encodeURIComponent(district.value)}`,
                        "Select city",
                        selectedCity
                    );
                });
            } else {
                district.disabled = true;
                city.disabled = true;
                city.dispatchEvent(new CustomEvent("optionsloaded"));
            }
        }

        const reindexContacts = () => {
            contacts?.querySelectorAll(".contact-row").forEach((row, index) => {
                const definitions = [
                    ["TelephoneNo", "contactTelephone", "Telephone number"],
                    ["DateConfirmed", "contactDate", "Date confirmed"],
                    ["PersonChecked", "contactPerson", "Confirmed by"]
                ];

                definitions.forEach(([field, idPrefix, labelText]) => {
                    const input = row.querySelector(`[name$=".${field}"]`);
                    if (!input) return;
                    const id = `${idPrefix}_${index}`;
                    input.name = `Contacts[${index}].${field}`;
                    input.id = id;
                    const label = [...row.querySelectorAll("label")].find((item) => item.textContent.trim() === labelText);
                    if (label) label.htmlFor = id;
                });

                const idInput = row.querySelector('[name$=".Id"]');
                if (idInput) idInput.name = `Contacts[${index}].Id`;
            });
        };

        form.querySelector("#addContactBtn")?.addEventListener("click", () => {
            if (!contacts) return;
            const index = contacts.querySelectorAll(".contact-row").length;
            const row = document.createElement("div");
            row.className = "contact-row";
            row.innerHTML = `
                <div class="row g-2 align-items-end">
                    <div class="col-md-4">
                        <label class="form-label" for="contactTelephone_${index}">Telephone number</label>
                        <input id="contactTelephone_${index}" name="Contacts[${index}].TelephoneNo" class="form-control" autocomplete="tel" inputmode="tel">
                    </div>
                    <div class="col-md-3">
                        <label class="form-label" for="contactDate_${index}">Date confirmed</label>
                        <input id="contactDate_${index}" name="Contacts[${index}].DateConfirmed" type="date" class="form-control">
                    </div>
                    <div class="col-md-4">
                        <label class="form-label" for="contactPerson_${index}">Confirmed by</label>
                        <input id="contactPerson_${index}" name="Contacts[${index}].PersonChecked" class="form-control" autocomplete="name">
                    </div>
                    <div class="col-md-1">
                        <button type="button" class="btn btn-outline-danger w-100" data-remove-contact aria-label="Remove telephone number">
                            <i class="fa-solid fa-trash" aria-hidden="true"></i>
                        </button>
                    </div>
                </div>`;
            contacts.append(row);
            row.querySelector("input")?.focus();
        });

        contacts?.addEventListener("click", (event) => {
            const remove = event.target.closest("[data-remove-contact]");
            if (!remove) return;
            const rows = contacts.querySelectorAll(".contact-row");
            const row = remove.closest(".contact-row");
            if (rows.length === 1) {
                row.querySelectorAll("input").forEach((input) => {
                    if (input.type !== "hidden") input.value = "";
                });
            } else {
                row.remove();
                reindexContacts();
            }
        });

        const idNumber = form.querySelector("#IdentificationNumber");
        const duplicateAlert = form.querySelector("#duplicateAlert");
        idNumber?.addEventListener("blur", async () => {
            if (!idNumber.value.trim() || !duplicateAlert) return;
            const query = new URLSearchParams({
                idType: form.querySelector("#IdentificationType")?.value || "",
                idNumber: idNumber.value,
                fullName: form.querySelector("#FullName")?.value || "",
                dob: form.querySelector("#Dob")?.value || ""
            });

            try {
                const response = await fetch(`/Patients/CheckDuplicateAjax?${query}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                const data = await response.json();
                duplicateAlert.classList.toggle("d-none", !data.isExactDuplicate && !data.hasSimilarNameOrDob);
                duplicateAlert.classList.toggle("alert-danger", data.isExactDuplicate);
                duplicateAlert.classList.toggle("alert-warning", !data.isExactDuplicate);
                duplicateAlert.textContent = data.isExactDuplicate
                    ? `Duplicate found: ${data.existingPatientNumber} — ${data.existingPatientName}`
                    : data.hasSimilarNameOrDob
                        ? `Possible match: ${data.existingPatientNumber} — ${data.existingPatientName}`
                        : "";
            } catch {
                duplicateAlert.classList.add("d-none");
            }
        });

        form.querySelector("[data-confirm-duplicate]")?.addEventListener("click", () => {
            const confirmation = form.querySelector("#confirmDuplicateWarning");
            if (confirmation) confirmation.value = "true";
            form.requestSubmit();
        });

        form.addEventListener("input", () => {
            form.dataset.dirty = "true";
        });

        const modal = form.closest("[data-workflow-modal]");
        if (modal && modal.dataset.closeGuardInitialized !== "true") {
            modal.dataset.closeGuardInitialized = "true";
            modal.addEventListener("hide.bs.modal", (event) => {
                const activeForm = modal.querySelector('[data-patient-form][data-dirty="true"]');
                if (!activeForm) return;
                if (!window.confirm("Close without saving? Your patient registration entries will be lost.")) {
                    event.preventDefault();
                }
            });
        }
    }

    function initializeAssessmentForm(form) {
        if (form.dataset.assessmentInitialized === "true") return;
        form.dataset.assessmentInitialized = "true";

        const assessmentType = form.querySelector("#AssessmentType");
        const limbCategory = form.querySelector("#LimbCategory");
        const side = form.querySelector("#Side");
        const cause = form.querySelector("#CauseReasonTypeId");
        const causeOtherField = form.querySelector("#causeReasonOtherField");
        const causeOther = causeOtherField?.querySelector("input");
        const singleBlock = form.querySelector("#singlePrescriptionBlock");
        const bilateralBlock = form.querySelector("#bilateralPrescriptionBlock");

        const toggleSpinal = () => {
            const spinal = limbCategory?.querySelector('option[value="Spinal"]');
            const allowsSpinal = assessmentType?.value === "Orthotic";
            if (spinal) spinal.disabled = !allowsSpinal;
            if (!allowsSpinal && limbCategory?.value === "Spinal") limbCategory.value = "UpperLimb";
        };

        const toggleCauseOther = () => {
            const isOther = cause?.selectedOptions[0]?.text?.trim().toLowerCase() === "other";
            if (causeOtherField) causeOtherField.hidden = !isOther;
            if (causeOther) causeOther.required = isOther;
        };

        const updatePrescriptionRequirements = () => {
            const bilateral = side?.value === "Bilateral";
            if (singleBlock) singleBlock.hidden = bilateral;
            if (bilateralBlock) bilateralBlock.hidden = !bilateral;

            const single = singleBlock?.querySelector(".prescription-select");
            const left = bilateralBlock?.querySelector('.prescription-select[data-side="left"]');
            const right = bilateralBlock?.querySelector('.prescription-select[data-side="right"]');
            if (single) single.required = !bilateral;
            if (left) left.required = bilateral;
            if (right) right.required = bilateral;
        };

        const updatePrescriptionDetails = (select) => {
            const selectedOption = select.selectedOptions[0];
            let subTypes = [];
            try {
                subTypes = JSON.parse(selectedOption?.dataset.subtypes || "[]");
            } catch {
                subTypes = [];
            }

            const sideName = select.dataset.side;
            const scope = sideName ? select.closest(".workflow-subsection") : singleBlock;
            const subtype = scope?.querySelector(".subtype-select");
            const otherField = scope?.querySelector(".other-text-field");
            const otherInput = otherField?.querySelector(".other-text");
            const selectedSubtype = subtype?.dataset.selected;

            if (subtype) {
                subtype.replaceChildren(new Option("Select subtype (optional)", ""));
                subTypes.forEach((item) => subtype.add(new Option(item, item)));
                subtype.hidden = subTypes.length === 0;
                if (selectedSubtype) subtype.value = selectedSubtype;
            }

            const isOther = select.value === "OTHER";
            if (otherField) otherField.hidden = !isOther;
            if (otherInput) otherInput.required = isOther;
        };

        const loadPrescriptions = async (select, preserveSelection = true) => {
            const selected = preserveSelection ? select.dataset.selected : "";
            select.disabled = true;
            select.replaceChildren(new Option("Loading prescriptions…", ""));

            try {
                const query = new URLSearchParams({
                    assessmentType: assessmentType?.value || "",
                    limbCategory: limbCategory?.value || ""
                });
                const response = await fetch(`/Assessments/GetPrescriptionOptions?${query}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (!response.ok) throw new Error(`Request failed with ${response.status}`);
                const options = await response.json();
                select.replaceChildren(new Option("Select prescription", ""));
                options.forEach((item) => {
                    const option = new Option(item.label, item.code);
                    option.dataset.subtypes = JSON.stringify(item.subTypes || []);
                    select.add(option);
                });
                select.disabled = false;
                if (selected) select.value = selected;
                updatePrescriptionDetails(select);
            } catch {
                select.replaceChildren(new Option("Could not load prescriptions — try again", ""));
                select.disabled = false;
            }
        };

        form.querySelectorAll(".prescription-select").forEach((select) => {
            select.addEventListener("change", () => updatePrescriptionDetails(select));
        });

        assessmentType?.addEventListener("change", () => {
            toggleSpinal();
            form.querySelectorAll(".prescription-select").forEach((select) => loadPrescriptions(select, false));
        });
        limbCategory?.addEventListener("change", () => {
            form.querySelectorAll(".prescription-select").forEach((select) => loadPrescriptions(select, false));
        });
        side?.addEventListener("change", updatePrescriptionRequirements);
        cause?.addEventListener("change", toggleCauseOther);

        toggleSpinal();
        toggleCauseOther();
        updatePrescriptionRequirements();
        form.querySelectorAll(".prescription-select").forEach((select) => loadPrescriptions(select));
    }

    function initializeContainedUi(root = document) {
        root.querySelectorAll("[data-workflow-wizard]").forEach(initializeWizard);
        root.querySelectorAll("[data-patient-form]").forEach(initializePatientForm);
        root.querySelectorAll("[data-assessment-form]").forEach(initializeAssessmentForm);
    }

    async function loadModal(trigger) {
        const selector = trigger.dataset.workflowModalTarget;
        const modalElement = document.querySelector(selector);
        const dialog = modalElement?.querySelector(".modal-dialog");
        if (!modalElement || !dialog || !window.bootstrap) return;

        const modal = window.bootstrap.Modal.getOrCreateInstance(modalElement, {
            backdrop: "static",
            keyboard: true
        });
        modal.show();

        dialog.innerHTML = `
            <div class="modal-content workflow-loading">
                <div class="modal-body text-center py-5" role="status">
                    <div class="spinner-border text-primary" aria-hidden="true"></div>
                    <p class="mt-3 mb-0">Opening patient registration…</p>
                </div>
            </div>`;

        try {
            const response = await fetch(trigger.dataset.workflowModalUrl, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (!response.ok) throw new Error(`Request failed with ${response.status}`);
            dialog.innerHTML = await response.text();
            window.jQuery?.validator?.unobtrusive?.parse(dialog);
            initializeContainedUi(dialog);
            focusWithoutJump(dialog.querySelector(".modal-title"));
        } catch {
            dialog.innerHTML = `
                <div class="modal-content">
                    <div class="modal-header">
                        <h2 class="modal-title h4">Registration could not be opened</h2>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p>Check your connection and try again.</p>
                        <a class="btn btn-primary" href="${trigger.href}">Open registration page</a>
                    </div>
                </div>`;
        }
    }

    document.addEventListener("click", (event) => {
        const trigger = event.target.closest("[data-workflow-modal-url]");
        if (!trigger) return;
        event.preventDefault();
        loadModal(trigger);
    });

    document.addEventListener("submit", async (event) => {
        const form = event.target.closest('form[data-modal-form="true"]');
        if (!form) return;
        event.preventDefault();

        const submit = form.querySelector("[data-wizard-submit]");
        if (submit) {
            submit.disabled = true;
            submit.setAttribute("aria-busy", "true");
        }

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (response.redirected) {
                window.location.assign(response.url);
                return;
            }

            if (!response.ok) throw new Error(`Request failed with ${response.status}`);
            const dialog = form.closest(".modal-dialog");
            dialog.innerHTML = await response.text();
            window.jQuery?.validator?.unobtrusive?.parse(dialog);
            initializeContainedUi(dialog);
        } catch {
            const summary = form.querySelector('[asp-validation-summary], .validation-summary-valid, .validation-summary-errors');
            if (summary) {
                summary.classList.remove("validation-summary-valid");
                summary.classList.add("validation-summary-errors");
                summary.textContent = "The patient could not be saved. Check your connection and try again.";
                focusWithoutJump(summary);
            }
            if (submit) {
                submit.disabled = false;
                submit.removeAttribute("aria-busy");
            }
        }
    });

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => initializeContainedUi());
    } else {
        initializeContainedUi();
    }
})();
