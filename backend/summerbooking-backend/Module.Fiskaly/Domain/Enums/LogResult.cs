namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Esito di un passo del flusso (fiskaly_receipt_logs.result). Vocabolario riusabile da altri moduli di
/// log, ma oggi lo usa solo Fiskaly, quindi e' registrato localmente come fiskaly.log_result (stessa
/// scelta di LanguageCode, vedi quel file).
/// </summary>
public enum LogResult
{
	Ok = 0,
	Ko = 1
}
