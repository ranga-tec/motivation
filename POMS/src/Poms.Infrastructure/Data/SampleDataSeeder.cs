using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;

namespace Poms.Infrastructure.Data;

public static class SampleDataSeeder
{
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
