using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;

namespace Poms.Infrastructure.Data;

public static class SampleDataSeeder
{
    public static async Task SeedSampleConditionsAsync(PomsDbContext context)
    {
        if (await context.Conditions.AnyAsync()) return; // Already seeded

        var conditions = new[]
        {
            new Condition { Code = "BKA", Name = "Below Knee Amputation", BodyRegion = BodyRegion.LowerLimb, Description = "Amputation below the knee joint", CreatedBy = "System" },
            new Condition { Code = "AKA", Name = "Above Knee Amputation", BodyRegion = BodyRegion.LowerLimb, Description = "Amputation above the knee joint", CreatedBy = "System" },
            new Condition { Code = "BEA", Name = "Below Elbow Amputation", BodyRegion = BodyRegion.UpperLimb, Description = "Amputation below the elbow joint", CreatedBy = "System" },
            new Condition { Code = "AEA", Name = "Above Elbow Amputation", BodyRegion = BodyRegion.UpperLimb, Description = "Amputation above the elbow joint", CreatedBy = "System" },
            new Condition { Code = "PFFD", Name = "Proximal Femoral Focal Deficiency", BodyRegion = BodyRegion.LowerLimb, Description = "Congenital limb deficiency", CreatedBy = "System" },
            new Condition { Code = "SCO", Name = "Scoliosis", BodyRegion = BodyRegion.Spine, Description = "Spinal curvature condition", CreatedBy = "System" },
            new Condition { Code = "CP", Name = "Cerebral Palsy", BodyRegion = BodyRegion.Other, Description = "Neurological condition affecting movement", CreatedBy = "System" },
            new Condition { Code = "MS", Name = "Multiple Sclerosis", BodyRegion = BodyRegion.Other, Description = "Progressive neurological condition", CreatedBy = "System" },
            new Condition { Code = "SCI", Name = "Spinal Cord Injury", BodyRegion = BodyRegion.Spine, Description = "Injury to the spinal cord", CreatedBy = "System" },
            new Condition { Code = "CVA", Name = "Cerebrovascular Accident (Stroke)", BodyRegion = BodyRegion.Other, Description = "Stroke resulting in limb weakness", CreatedBy = "System" }
        };

        context.Conditions.AddRange(conditions);
        await context.SaveChangesAsync();
    }
}