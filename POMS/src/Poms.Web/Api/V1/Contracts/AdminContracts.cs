using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Api.V1.Contracts;

public sealed record AdminItem(int Id, string Code, string Name, bool IsActive, int? ParentId = null, string? ParentName = null);
public sealed record AdminCenter(int Id, int DistrictId, string DistrictName, string Code, string Name, string? Address, string? Phone, bool IsActive, bool RequiresPatientNumberFlag, string? PatientNumberFlagCode);
public sealed record AdminDevice(int Id, int DeviceTypeId, string DeviceTypeName, string Code, string Name, bool IsActive);
public sealed record AdminCatalogResponse(IReadOnlyList<AdminItem> Provinces, IReadOnlyList<AdminItem> Districts, IReadOnlyList<AdminItem> Cities, IReadOnlyList<AdminCenter> Centers, IReadOnlyDictionary<string, IReadOnlyList<AdminItem>> Lookups, IReadOnlyList<AdminItem> DeviceTypes, IReadOnlyList<AdminDevice> Devices);
public class SaveNamedItemRequest { [Required, StringLength(150)] public string Name { get; init; } = ""; public bool IsActive { get; init; } = true; }
public class SaveCodedItemRequest : SaveNamedItemRequest { [Required, StringLength(20)] public string Code { get; init; } = ""; public int? ParentId { get; init; } }
public sealed class SaveCenterRequest : SaveCodedItemRequest { [Required] public int DistrictId { get; init; } [StringLength(300)] public string? Address { get; init; } [StringLength(30)] public string? Phone { get; init; } public bool RequiresPatientNumberFlag { get; init; } [StringLength(10)] public string? PatientNumberFlagCode { get; init; } }
public sealed class SaveDeviceRequest : SaveCodedItemRequest { [Required] public int DeviceTypeId { get; init; } }
public sealed record AdminUser(string Id, string Email, string FullName, string EmployeeNumber, string Designation, string? Department, string MobileNumber, string? WorkPhoneNumber, bool CanAccessRestrictedClinicalData, IReadOnlyList<string> Roles, bool IsLocked, bool IsCurrentUser);
public sealed record AdminUsersResponse(IReadOnlyList<string> AvailableRoles, IReadOnlyList<AdminUser> Users);
public sealed class CreateAdminUserRequest { [Required, EmailAddress] public string Email { get; init; } = ""; [Required] public string FullName { get; init; } = ""; [Required] public string EmployeeNumber { get; init; } = ""; [Required] public string Designation { get; init; } = ""; public string? Department { get; init; } [Required] public string MobileNumber { get; init; } = ""; public string? WorkPhoneNumber { get; init; } public bool CanAccessRestrictedClinicalData { get; init; } public List<string> Roles { get; init; } = []; }
public sealed class UpdateAdminProfileRequest { [Required] public string FullName { get; init; } = ""; [Required] public string EmployeeNumber { get; init; } = ""; [Required] public string Designation { get; init; } = ""; public string? Department { get; init; } [Required] public string MobileNumber { get; init; } = ""; public string? WorkPhoneNumber { get; init; } public bool CanAccessRestrictedClinicalData { get; init; } }
public sealed record UpdateRolesRequest(IReadOnlyList<string> Roles);
public sealed record SetLockRequest(bool Locked);
