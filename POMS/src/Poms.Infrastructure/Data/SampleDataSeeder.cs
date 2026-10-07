using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;

namespace Poms.Infrastructure.Data;

public static class SampleDataSeeder
{
    public static async Task SeedDemoPatientsAsync(PomsDbContext context)
    {
        const string demoPrefix = "DEMO-";
        if (await context.Patients.AnyAsync(p => p.PatientNumber.StartsWith(demoPrefix))) return;

        var ragama = await context.Centers.FirstAsync(c => c.Code == "RAG");
        var colombo = await context.Centers.FirstAsync(c => c.Code == "COL");
        var gampaha = await context.Districts.FirstAsync(d => d.Code == "GM");
        var colomboDistrict = await context.Districts.FirstAsync(d => d.Code == "CO");
        var western = await context.Provinces.FirstAsync(p => p.Code == "WP");
        var ragamaCity = await context.Cities.FirstOrDefaultAsync(c => c.DistrictId == gampaha.Id);
        var colomboCity = await context.Cities.FirstOrDefaultAsync(c => c.DistrictId == colomboDistrict.Id);
        var doctorReferral = await context.ReferralSources.FirstAsync(r => r.Name == "Doctor referral");
        var walkIn = await context.ReferralSources.FirstAsync(r => r.Name == "Walk-in");
        var today = DateOnly.FromDateTime(DateTime.Today);

        Patient BuildPatient(
            string number, string name, string initials, DateOnly dob, Sex sex,
            Center center, District district, City? city, string phone, int registrationOffset)
        {
            var patient = new Patient
            {
                PatientNumber = number,
                FullName = name,
                NameWithInitials = initials,
                Dob = dob,
                Sex = sex,
                Employment = "Demo record",
                Address1 = "Demo address — not a real patient",
                ProvinceId = western.Id,
                DistrictId = district.Id,
                CityId = city?.Id,
                Email = $"{number.ToLowerInvariant()}@example.com",
                Nationality = "Sri Lankan",
                Category = PatientCategory.Local,
                IdentificationType = IdentificationType.NotApplicable,
                IdentificationNumber = "N/A",
                CenterId = center.Id,
                ReferralSourceId = registrationOffset % 2 == 0 ? doctorReferral.Id : walkIn.Id,
                AssignedClinicianName = "Demo Clinician",
                RegistrationDate = today.AddDays(registrationOffset),
                RegistrationProcessedBy = "Demo Data Seeder",
                Remarks = "Fictional demonstration record. Do not use for clinical decisions.",
                GuardianName = "Demo Guardian",
                GuardianRelationship = "Family member",
                GuardianMobile = phone,
                CreatedBy = "demo-seeder"
            };
            patient.Contacts.Add(new PatientContact
            {
                TelephoneNo = phone,
                DateConfirmed = today,
                PersonChecked = "Demo Data Seeder",
                CreatedBy = "demo-seeder"
            });
            return patient;
        }

        var patients = new[]
        {
            BuildPatient("DEMO-0001", "Nimali Perera", "N. Perera", new DateOnly(1988, 4, 12), Sex.Female, ragama, gampaha, ragamaCity, "0770001001", -45),
            BuildPatient("DEMO-0002", "Kasun Fernando", "K. Fernando", new DateOnly(1976, 9, 3), Sex.Male, colombo, colomboDistrict, colomboCity, "0770001002", -30),
            BuildPatient("DEMO-0003", "Ayesha Silva", "A. Silva", new DateOnly(1995, 1, 25), Sex.Female, ragama, gampaha, ragamaCity, "0770001003", -18),
            BuildPatient("DEMO-0004", "Ruwan Jayasinghe", "R. Jayasinghe", new DateOnly(1964, 7, 16), Sex.Male, colombo, colomboDistrict, colomboCity, "0770001004", -8),
            BuildPatient("DEMO-0005", "Samanthi De Alwis", "S. De Alwis", new DateOnly(2001, 11, 8), Sex.Female, ragama, gampaha, ragamaCity, "0770001005", -2)
        };

        for (var index = 0; index < patients.Length; index++)
        {
            var patient = patients[index];
            var episode = new Episode
            {
                Patient = patient,
                CenterId = patient.CenterId,
                RecordDate = today.AddDays(-(index * 6 + 2)),
                RecordTime = new TimeOnly(9 + index, 0),
                Status = index == 1 ? RecordStatus.Completed : RecordStatus.Active,
                Remarks = "Fictional demo clinical record.",
                CreatedBy = "demo-seeder"
            };
            patient.Episodes.Add(episode);
            patient.Appointments.Add(new Appointment
            {
                Patient = patient,
                Episode = episode,
                Type = index % 2 == 0 ? AppointmentType.Assessment : AppointmentType.Fitting,
                AppointmentDate = today.AddDays(index - 2),
                AppointmentTime = new TimeOnly(9 + index, 30),
                Status = index < 2 ? AppointmentStatus.Completed : AppointmentStatus.Scheduled,
                AssignedClinicianName = "Demo Clinician",
                Notes = "Fictional appointment for UI demonstration.",
                CreatedBy = "demo-seeder"
            });
        }

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();
    }

    public static async Task SeedLocationsAsync(PomsDbContext context)
    {
        var provincesByCode = await context.Provinces
            .ToDictionaryAsync(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var provinceSeed in SriLankaLocationData.Provinces)
        {
            if (provincesByCode.ContainsKey(provinceSeed.Code)) continue;

            var province = new Province { Code = provinceSeed.Code, Name = provinceSeed.Name };
            context.Provinces.Add(province);
            provincesByCode[provinceSeed.Code] = province;
        }

        await context.SaveChangesAsync();

        var districtsByCode = await context.Districts
            .ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var provinceSeed in SriLankaLocationData.Provinces)
        {
            var province = provincesByCode[provinceSeed.Code];
            foreach (var districtSeed in provinceSeed.Districts)
            {
                if (districtsByCode.ContainsKey(districtSeed.Code)) continue;

                var district = new District
                {
                    ProvinceId = province.Id,
                    Code = districtSeed.Code,
                    Name = districtSeed.Name
                };
                context.Districts.Add(district);
                districtsByCode[districtSeed.Code] = district;
            }
        }

        await context.SaveChangesAsync();

        // Current treatment locations (PRD 3.1): Ragama (unflagged) + Colombo (flagged "C")
        var gampaha = districtsByCode["GM"];
        var colombo = districtsByCode["CO"];
        if (!await context.Centers.AnyAsync(c => c.Code == "RAG"))
        {
            context.Centers.Add(new Center
            {
                DistrictId = gampaha.Id, Code = "RAG", Name = "Ragama", Address = "Ragama",
                IsActive = true, RequiresPatientNumberFlag = false
            });
        }

        if (!await context.Centers.AnyAsync(c => c.Code == "COL"))
        {
            context.Centers.Add(new Center
            {
                DistrictId = colombo.Id, Code = "COL", Name = "Colombo", Address = "Colombo",
                IsActive = true, RequiresPatientNumberFlag = true, PatientNumberFlagCode = "C"
            });
        }

        await context.SaveChangesAsync();

        var existingCities = await context.Cities
            .Select(c => new { c.DistrictId, c.Name })
            .ToListAsync();
        var cityKeys = existingCities
            .Select(c => $"{c.DistrictId}|{c.Name.Trim().ToUpperInvariant()}")
            .ToHashSet(StringComparer.Ordinal);

        foreach (var provinceSeed in SriLankaLocationData.Provinces)
        {
            foreach (var districtSeed in provinceSeed.Districts)
            {
                var district = districtsByCode[districtSeed.Code];
                foreach (var cityName in districtSeed.Cities.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var key = $"{district.Id}|{cityName.Trim().ToUpperInvariant()}";
                    if (!cityKeys.Add(key)) continue;

                    context.Cities.Add(new City
                    {
                        DistrictId = district.Id,
                        Name = cityName,
                        IsActive = true
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedReferralSourcesAsync(PomsDbContext context)
    {
        if (await context.ReferralSources.AnyAsync()) return;

        context.ReferralSources.AddRange(
            new ReferralSource { Name = "Doctor referral" },
            new ReferralSource { Name = "Hospital referral" },
            new ReferralSource { Name = "NGO referral" },
            new ReferralSource { Name = "Friend / family" },
            new ReferralSource { Name = "Social media" },
            new ReferralSource { Name = "Website" },
            new ReferralSource { Name = "Walk-in" },
            new ReferralSource { Name = "Other" }
        );
        await context.SaveChangesAsync();
    }

    public static async Task SeedMainProblemTypesAsync(PomsDbContext context)
    {
        if (await context.MainProblemTypes.AnyAsync()) return;

        context.MainProblemTypes.AddRange(
            new MainProblemType { Name = "Gait abnormality" },
            new MainProblemType { Name = "Pain" },
            new MainProblemType { Name = "Limb absence" },
            new MainProblemType { Name = "Deformity" },
            new MainProblemType { Name = "Instability" },
            new MainProblemType { Name = "Skin/Pressure issue" },
            new MainProblemType { Name = "Other" }
        );
        await context.SaveChangesAsync();
    }

    public static async Task SeedCauseReasonTypesAsync(PomsDbContext context)
    {
        if (await context.CauseReasonTypes.AnyAsync()) return;

        context.CauseReasonTypes.AddRange(
            new CauseReasonType { Name = "Congenital" },
            new CauseReasonType { Name = "Trauma" },
            new CauseReasonType { Name = "Disease" },
            new CauseReasonType { Name = "Amputation" },
            new CauseReasonType { Name = "Stroke" },
            new CauseReasonType { Name = "Diabetes-related" },
            new CauseReasonType { Name = "Other" }
        );
        await context.SaveChangesAsync();
    }

    public static async Task SeedDeviceCatalogAsync(PomsDbContext context)
    {
        if (await context.DeviceCatalogs.AnyAsync()) return;

        // Starter catalogue so deliveries can be recorded on a fresh database.
        // Administration > Devices is the place to extend or deactivate these.
        var typesByCode = await context.DeviceTypes
            .ToDictionaryAsync(t => t.Code, StringComparer.OrdinalIgnoreCase);

        if (typesByCode.Count == 0) return;

        var devices = new (string TypeCode, string Code, string Name)[]
        {
            ("PROS", "PROS-PP", "Partial foot prosthesis"),
            ("PROS", "PROS-TT", "Trans-tibial prosthesis"),
            ("PROS", "PROS-KD", "Knee disarticulation prosthesis"),
            ("PROS", "PROS-TF", "Trans-femoral prosthesis"),
            ("PROS", "PROS-HD", "Hip disarticulation prosthesis"),
            ("PROS", "PROS-TR", "Trans-radial prosthesis"),
            ("PROS", "PROS-TH", "Trans-humeral prosthesis"),
            ("ORTH", "ORTH-FO", "Foot orthosis"),
            ("ORTH", "ORTH-AFO", "Ankle foot orthosis (AFO)"),
            ("ORTH", "ORTH-KAFO", "Knee ankle foot orthosis (KAFO)"),
            ("ORTH", "ORTH-KO", "Knee orthosis"),
            ("ORTH", "ORTH-HKAFO", "Hip knee ankle foot orthosis (HKAFO)"),
            ("ORTH", "ORTH-WHO", "Wrist hand orthosis"),
            ("ORTH", "ORTH-EO", "Elbow orthosis"),
            ("SPIN", "SPIN-CO", "Cervical orthosis"),
            ("SPIN", "SPIN-TLSO", "Thoraco-lumbo-sacral orthosis (TLSO)"),
            ("SPIN", "SPIN-LSO", "Lumbo-sacral orthosis (LSO)")
        };

        foreach (var device in devices)
        {
            if (!typesByCode.TryGetValue(device.TypeCode, out var deviceType)) continue;

            context.DeviceCatalogs.Add(new DeviceCatalog
            {
                DeviceTypeId = deviceType.Id,
                Code = device.Code,
                Name = device.Name,
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedNationalitiesAsync(PomsDbContext context)
    {
        if (await context.Nationalities.AnyAsync()) return;

        context.Nationalities.AddRange(
            new Nationality { Name = "Sri Lankan" },
            new Nationality { Name = "Indian" },
            new Nationality { Name = "British" },
            new Nationality { Name = "Other" }
        );
        await context.SaveChangesAsync();
    }
}
