namespace Module.FiscalPrint.Domain.Enums;

/// <summary>
/// Tipo di connessione al dispositivo: tcp (host+port) o usb (usb_device).
/// </summary>
public enum ConnectionKind
{
	Tcp = 0,
	Usb = 1,
}
