using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;

namespace Poms.Infrastructure.Data;

public static class SampleDataSeeder
{
    public static async Task SeedLocationsAsync(PomsDbContext context)
    {
        if (await context.Provinces.AnyAsync()) return; // Already seeded

        var western = new Province { Code = "WP", Name = "Western Province" };
        var central = new Province { Code = "CP", Name = "Central Province" };
        var southern = new Province { Code = "SP", Name = "Southern Province" };
        var northern = new Province { Code = "NP", Name = "Northern Province" };
        var eastern = new Province { Code = "EP", Name = "Eastern Province" };
        context.Provinces.AddRange(western, central, southern, northern, eastern);
        await context.SaveChangesAsync();

        var gampaha = new District { ProvinceId = western.Id, Code = "GM", Name = "Gampaha" };
        var colombo = new District { ProvinceId = western.Id, Code = "CO", Name = "Colombo" };
        var kandy = new District { ProvinceId = central.Id, Code = "KA", Name = "Kandy" };
        var galle = new District { ProvinceId = southern.Id, Code = "GL", Name = "Galle" };
        var jaffna = new District { ProvinceId = northern.Id, Code = "JA", Name = "Jaffna" };
        var batticaloa = new District { ProvinceId = eastern.Id, Code = "BT", Name = "Batticaloa" };
        context.Districts.AddRange(gampaha, colombo, kandy, galle, jaffna, batticaloa);
        await context.SaveChangesAsync();

        // Current treatment locations (PRD 3.1): Ragama (unflagged) + Colombo (flagged "C")
        context.Centers.AddRange(
            new Center
            {
                DistrictId = gampaha.Id, Code = "RAG", Name = "Ragama", Address = "Ragama",
                IsActive = true, RequiresPatientNumberFlag = false
            },
            new Center
            {
                DistrictId = colombo.Id, Code = "COL", Name = "Colombo", Address = "Colombo",
                IsActive = true, RequiresPatientNumberFlag = true, PatientNumberFlagCode = "C"
            }
        );
        await context.SaveChangesAsync();

        // Cities for the patient-address dropdowns (separate from the 2 treatment Centers above)
        context.Cities.AddRange(
            new City { DistrictId = gampaha.Id, Name = "Ragama" },
            new City { DistrictId = gampaha.Id, Name = "Gampaha" },
            new City { DistrictId = gampaha.Id, Name = "Negombo" },
            new City { DistrictId = gampaha.Id, Name = "Wattala" },
            new City { DistrictId = colombo.Id, Name = "Colombo" },
            new City { DistrictId = colombo.Id, Name = "Dehiwala" },
            new City { DistrictId = colombo.Id, Name = "Moratuwa" },
            new City { DistrictId = colombo.Id, Name = "Maharagama" },
            new City { DistrictId = kandy.Id, Name = "Kandy" },
            new City { DistrictId = galle.Id, Name = "Galle" },
            new City { DistrictId = jaffna.Id, Name = "Jaffna" },
            new City { DistrictId = batticaloa.Id, Name = "Batticaloa" }
        );
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
