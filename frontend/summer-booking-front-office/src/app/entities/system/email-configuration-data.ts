/** .NET enum names, as the other enums of the API. */
export type SmtpSecurity = 'None' | 'Ssl' | 'Tls';

export interface EmailConfigurationData {
  senderMailAddress: string;
  senderName: string;
  smtpServerAddress: string;
  smtpPort: number;
  smtpUsername: string;
  smtpPassword: string;
  smtpSecurity: SmtpSecurity;
}
