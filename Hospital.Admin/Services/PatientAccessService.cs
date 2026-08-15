using System.Security.Claims;
using Hospital.Domain.Enums;

namespace Hospital.Admin.Services;

public class PatientAccessService
{
	private readonly HospitalDataService _data;

	public PatientAccessService(
		HospitalDataService data)
	{
		_data = data;
	}

	public bool CanAccessPatient(
		ClaimsPrincipal user,
		int patientId)
	{
		if (user.Identity?.IsAuthenticated != true)
		{
			return false;
		}

		var staffMemberIdText =
			user.FindFirstValue(
				ClaimTypes.NameIdentifier);

		if (!int.TryParse(
				staffMemberIdText,
				out var staffMemberId))
		{
			return false;
		}

		var staffMember =
			_data.GetStaffMember(staffMemberId);

		if (staffMember is null ||
			!staffMember.IsActive)
		{
			return false;
		}

		// Administrator mag ieder dossier openen.
		if (staffMember.Role ==
			UserRole.Administrator)
		{
			return true;
		}

		var treatments =
			_data.GetTreatments(patientId);

		// Zonder behandeling is er nog geen
		// gekoppeld zorgteam.
		if (treatments.Count == 0)
		{
			return false;
		}

		foreach (var treatment in treatments)
		{
			// Verantwoordelijke secretaresse.
			if (staffMember.Role ==
					UserRole.Secretary &&
				treatment.SecretaryId ==
					staffMember.Id)
			{
				return true;
			}

			// Gekoppelde chirurg.
			if (staffMember.Role ==
					UserRole.Surgeon &&
				treatment.SurgeonIds.Contains(
					staffMember.Id))
			{
				return true;
			}

			// Gekoppelde verpleegkundige.
			if (staffMember.Role ==
					UserRole.Nurse &&
				treatment.NurseIds.Contains(
					staffMember.Id))
			{
				return true;
			}
		}

		return false;
	}
}