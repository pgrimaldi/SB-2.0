using Module.ZTest.Application.Dtos;

namespace Module.ZTest.Application.Validators;

/// <summary>
/// Validatore del payload di creazione.
/// Esempio senza dipendenze esterne; in produzione si puo sostituire con FluentValidation
/// (AbstractValidator&lt;CreateReservationRequest&gt;) mantenendo la stessa collocazione.
/// </summary>
public sealed class CreateReservationValidator
{
	public IReadOnlyList<string> Validate(CreateReservationRequest request)
	{
		var errors = new List<string>();

		if (request.GuestId == Guid.Empty)
			errors.Add("GuestId e obbligatorio.");

		if (request.To <= request.From)
			errors.Add("La data di check-out deve essere successiva al check-in.");

		if (request.PricePerNight <= 0)
			errors.Add("Il prezzo per notte deve essere positivo.");

		return errors;
	}

	public void ValidateAndThrow(CreateReservationRequest request)
	{
		var errors = new List<string>();

		if (request.GuestId == Guid.Empty)
			errors.Add("GuestId e obbligatorio.");

		if (request.To <= request.From)
			errors.Add("La data di check-out deve essere successiva al check-in.");

		if (request.PricePerNight <= 0)
			errors.Add("Il prezzo per notte deve essere positivo.");

		if (errors.Count > 0)
			throw new ArgumentException(string.Join(" ", errors));
	}
}
