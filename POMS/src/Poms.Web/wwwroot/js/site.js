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

    // The photo is the one field a reviewer cannot check by reading text, so the
    // review step shows the actual image at a size worth looking at, and full size
    // on click.
    function renderReviewPhoto(form) {
        const source = form.querySelector("[data-photo-image]");
        if (!source || source.classList.contains("d-none") || !source.getAttribute("src")) return null;

        const figure = document.createElement("figure");
        figure.className = "review-photo";

        const button = document.createElement("button");
        button.type = "button";
        button.className = "review-photo-button";
        button.setAttribute("aria-label", "Enlarge patient photo");

        const image = document.createElement("img");
        image.src = source.src;
        image.alt = "Patient photo to be saved with this record";
        button.append(image);

        const caption = document.createElement("figcaption");
        caption.textContent = "Patient photo — select to enlarge";

        button.addEventListener("click", () => openPhotoZoom(image.src));
        figure.append(button, caption);
        return figure;
    }

    function openPhotoZoom(src) {
        const existing = document.querySelector(".photo-zoom-overlay");
        existing?.remove();

        const overlay = document.createElement("div");
        overlay.className = "photo-zoom-overlay";
        overlay.setAttribute("role", "dialog");
        overlay.setAttribute("aria-modal", "true");
        overlay.setAttribute("aria-label", "Patient photo");
        overlay.tabIndex = -1;

        const image = document.createElement("img");
        image.src = src;
        image.alt = "Patient photo, enlarged";

        const close = document.createElement("button");
        close.type = "button";
        close.className = "btn btn-light photo-zoom-close";
        close.textContent = "Close";

        const dismiss = () => {
            overlay.remove();
            document.removeEventListener("keydown", onKey);
        };
        const onKey = (event) => {
            if (event.key === "Escape") dismiss();
        };

        close.addEventListener("click", dismiss);
        overlay.addEventListener("click", (event) => {
            if (event.target === overlay) dismiss();
        });
        document.addEventListener("keydown", onKey);

        overlay.append(image, close);
        document.body.append(overlay);
        close.focus();
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

            // The chosen photo is shown as an image below, so its file name
            // would only repeat it.
            if (control instanceof HTMLInputElement && control.type === "file") return;

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

        const photo = renderReviewPhoto(form);
        review.replaceChildren(...(photo ? [photo, list] : [list]));
    }

    // Visually-hidden controls (the photo file input) and off-screen fields are
    // focusable but would strand the caret somewhere the user cannot see.
    function firstFocusableControl(container) {
        return [...container.querySelectorAll("input, select, textarea")].find((control) => {
            if (control.disabled || control.type === "hidden" || control.readOnly) return false;
            if (control.closest(".visually-hidden") || control.classList.contains("visually-hidden")) return false;
            if (control.closest("[hidden]")) return false;
            return control.getClientRects().length > 0;
        }) ?? null;
    }

    function initializeWizard(form) {
        if (form.dataset.wizardInitialized === "true") return;
        form.dataset.wizardInitialized = "true";

        const steps = [...form.querySelectorAll("[data-wizard-step]")];
        const indicators = [...form.querySelectorAll("[data-step-indicator]")];
        const back = form.querySelector("[data-wizard-back]");
        const next = form.querySelector("[data-wizard-next]");
        const submit = form.querySelector("[data-form-submit], [data-wizard-submit]");
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

            // Focus the first field of the step, not its heading. The step count is
            // an aria-live region, so screen readers still hear which step opened
            // while keyboard users land where they actually have to type.
            if (moveFocus) {
                const target = firstFocusableControl(steps[current]);
                if (target) focusWithoutJump(target);
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

        form.addEventListener("keydown", (event) => {
            // Alt + arrow moves between steps from anywhere in the form.
            if (event.altKey && (event.key === "ArrowRight" || event.key === "ArrowLeft")) {
                event.preventDefault();
                if (event.key === "ArrowLeft") {
                    if (current > 0) showStep(current - 1);
                } else if (current < steps.length - 1 && validateCurrentStep()) {
                    showStep(current + 1);
                }
                return;
            }

            if (event.key !== "Enter" || event.altKey || event.ctrlKey || event.metaKey) return;

            // Enter advances the wizard the way it advances a paper form. Controls
            // that use Enter themselves keep it: multi-line text, buttons, links,
            // and the city combobox where Enter picks the highlighted option.
            const target = event.target;
            if (target.matches("textarea, button, a, [data-searchable-city]") ||
                target.closest(".city-combobox")) {
                return;
            }
            if (current >= steps.length - 1) return;

            event.preventDefault();
            if (validateCurrentStep()) showStep(current + 1);
        });

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

        // An N/A identification type has no number to enter, so the field is
        // emptied and disabled rather than left as a blank the user must ignore.
        const idType = form.querySelector("[data-identification-type]");
        const syncIdentificationNumber = () => {
            if (!idType || !idNumber) return;
            const notApplicable = idType.value === "NotApplicable";
            idNumber.disabled = notApplicable;
            idNumber.required = !notApplicable;
            idNumber.closest(".col-lg-6, .col-md-4, [class*='col-']")
                ?.classList.toggle("is-disabled-field", notApplicable);
            idNumber.previousElementSibling?.classList.toggle("required-label", !notApplicable);
            if (notApplicable) {
                idNumber.value = "";
                idNumber.classList.remove("is-invalid");
                duplicateAlert?.classList.add("d-none");
            }
        };
        idType?.addEventListener("change", syncIdentificationNumber);
        syncIdentificationNumber();

        idNumber?.addEventListener("blur", async () => {
            if (idNumber.disabled || !idNumber.value.trim() || !duplicateAlert) return;
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
                const hasSubTypes = subTypes.length > 0;
                subtype.hidden = !hasSubTypes;
                // Hide the label with its select, otherwise a lone "Subtype"
                // heading sits above nothing.
                const subtypeField = subtype.closest("[data-subtype-field]");
                if (subtypeField) subtypeField.hidden = !hasSubTypes;
                if (!hasSubTypes) subtype.value = "";
                else if (selectedSubtype) subtype.value = selectedSubtype;
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

    /**
     * Turns a <select> into a type-to-search dropdown, keeping the select as the
     * posted value so nothing downstream changes. With data-combobox-endpoint the
     * options come from the server as the user types; without it the select's own
     * options are filtered in the browser.
     *
     * The endpoint returns [{ value, text, detail }] and `detail` is shown as a
     * second line, which is how the record dropdown shows more than a date.
     */
    function initializeCombobox(select) {
        if (select.dataset.comboboxInitialized === "true") return;
        select.dataset.comboboxInitialized = "true";

        const endpoint = select.dataset.comboboxEndpoint || "";
        const placeholder = select.dataset.comboboxPlaceholder || "Type to search";
        const listId = `${select.id || `combobox${Math.random().toString(36).slice(2)}`}Results`;

        const wrapper = document.createElement("div");
        wrapper.className = "combobox";

        const input = document.createElement("input");
        input.type = "search";
        input.className = "form-control combobox-input";
        input.placeholder = placeholder;
        input.autocomplete = "off";
        input.setAttribute("role", "combobox");
        input.setAttribute("aria-autocomplete", "list");
        input.setAttribute("aria-expanded", "false");
        input.setAttribute("aria-controls", listId);
        const labelText = select.labels?.[0]?.textContent?.trim();
        if (labelText) input.setAttribute("aria-label", labelText);

        const list = document.createElement("div");
        list.id = listId;
        list.className = "combobox-list";
        list.setAttribute("role", "listbox");
        list.hidden = true;

        wrapper.append(input, list);
        select.insertAdjacentElement("afterend", wrapper);
        select.classList.add("visually-hidden");
        select.setAttribute("tabindex", "-1");
        select.setAttribute("aria-hidden", "true");

        const localOptions = () => [...select.options]
            .filter((option) => option.value)
            .map((option) => ({ value: option.value, text: option.text, detail: option.dataset.detail || "" }));

        // Show the current selection when the page loads with one already set.
        const selected = select.selectedOptions[0];
        if (selected?.value) input.value = selected.text;

        let matches = [];
        let activeIndex = -1;
        let debounce = null;

        const close = () => {
            list.hidden = true;
            input.setAttribute("aria-expanded", "false");
            input.removeAttribute("aria-activedescendant");
            activeIndex = -1;
        };

        // Some pickers must still accept a name that is not in the list (staff
        // without an account), so the raw text is mirrored into a companion field.
        const textTarget = select.dataset.comboboxTextTarget
            ? document.getElementById(select.dataset.comboboxTextTarget)
            : null;
        const syncTextTarget = () => {
            if (textTarget) textTarget.value = input.value.trim();
        };

        const choose = (option) => {
            if (![...select.options].some((existing) => existing.value === option.value)) {
                select.add(new Option(option.text, option.value));
            }
            select.value = option.value;
            input.value = option.text;
            syncTextTarget();
            select.dispatchEvent(new Event("change", { bubbles: true }));
            close();
        };

        const render = () => {
            list.replaceChildren();
            activeIndex = -1;

            if (!matches.length) {
                const empty = document.createElement("div");
                empty.className = "combobox-empty";
                empty.textContent = input.value.trim() ? "No matches" : "Start typing to search";
                list.append(empty);
            } else {
                matches.forEach((option, index) => {
                    const button = document.createElement("button");
                    button.type = "button";
                    button.id = `${listId}Option${index}`;
                    button.className = "combobox-option";
                    button.setAttribute("role", "option");
                    button.append(document.createTextNode(option.text));
                    if (option.detail) {
                        const detail = document.createElement("small");
                        detail.textContent = option.detail;
                        button.append(detail);
                    }
                    button.addEventListener("mousedown", (event) => event.preventDefault());
                    button.addEventListener("click", () => choose(option));
                    list.append(button);
                });
            }

            list.hidden = false;
            input.setAttribute("aria-expanded", "true");
        };

        const search = async () => {
            const query = input.value.trim();
            if (!endpoint) {
                const needle = query.toLocaleLowerCase();
                matches = localOptions()
                    .filter((option) => !needle ||
                        option.text.toLocaleLowerCase().includes(needle) ||
                        option.detail.toLocaleLowerCase().includes(needle))
                    .slice(0, 15);
                render();
                return;
            }

            if (query.length < 2) {
                matches = [];
                render();
                return;
            }

            try {
                const response = await fetch(`${endpoint}?term=${encodeURIComponent(query)}`, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (!response.ok) throw new Error(`Request failed with ${response.status}`);
                matches = await response.json();
            } catch {
                matches = [];
            }
            render();
        };

        const setActive = (index) => {
            const items = [...list.querySelectorAll(".combobox-option")];
            if (!items.length) return;
            activeIndex = (index + items.length) % items.length;
            items.forEach((item, i) => item.classList.toggle("is-active", i === activeIndex));
            input.setAttribute("aria-activedescendant", items[activeIndex].id);
        };

        input.addEventListener("input", () => {
            // Clearing the box clears the selection, so a stale id is never posted.
            select.value = "";
            select.dispatchEvent(new Event("change", { bubbles: true }));
            syncTextTarget();
            clearTimeout(debounce);
            debounce = setTimeout(search, endpoint ? 250 : 0);
        });
        input.addEventListener("focus", search);
        input.addEventListener("blur", () => setTimeout(close, 120));
        input.addEventListener("keydown", (event) => {
            if (event.key === "ArrowDown") {
                event.preventDefault();
                if (list.hidden) search(); else setActive(activeIndex + 1);
            } else if (event.key === "ArrowUp") {
                event.preventDefault();
                setActive(activeIndex - 1);
            } else if (event.key === "Enter" && !list.hidden && activeIndex >= 0) {
                event.preventDefault();
                choose(matches[activeIndex]);
            } else if (event.key === "Escape") {
                close();
            }
        });
    }

    function initializeContainedUi(root = document) {
        root.querySelectorAll("[data-workflow-wizard]").forEach(initializeWizard);
        root.querySelectorAll("[data-patient-form]").forEach(initializePatientForm);
        root.querySelectorAll("[data-assessment-form]").forEach(initializeAssessmentForm);
        root.querySelectorAll("select[data-combobox]").forEach(initializeCombobox);
        root.querySelectorAll("table.table").forEach((table) => {
            if (table.parentElement?.classList.contains("table-responsive")) return;
            const wrapper = document.createElement("div");
            wrapper.className = "table-responsive";
            wrapper.tabIndex = 0;
            wrapper.setAttribute("role", "region");
            wrapper.setAttribute("aria-label", table.getAttribute("aria-label") || "Scrollable data table");
            table.before(wrapper);
            wrapper.append(table);
        });
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
            const firstField = firstFocusableControl(dialog);
            focusWithoutJump(firstField ?? dialog.querySelector(".modal-title"));
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

        const submit = form.querySelector("[data-form-submit], [data-wizard-submit]");
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
