namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Lingua del PDF/eReceipt (fiskaly_receipts.language_code). E' un vocabolario potenzialmente comune a
/// piu' moduli, ma oggi lo usa solo Fiskaly, quindi e' registrato localmente come fiskaly.language_code
/// (come identity.storage_origin): si estrae in un tipo condiviso solo quando un secondo modulo lo usera'.
/// </summary>
public enum LanguageCode
{
	It = 0,
	En = 1
}
