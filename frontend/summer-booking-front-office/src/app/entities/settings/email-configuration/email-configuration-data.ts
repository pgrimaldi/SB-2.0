/** .NET enum names, as the other enums of the API. */
export type SmtpSecurity = 'None' | 'Ssl' | 'Tls';

export interface EmailConfigurationData {
  senderMailAddress: string;
  senderName: string;
  smtpServerAddress: string;
  smtpPort: number;
  smtpUsername: string;
  /**
   * The SMTP password itself never leaves the server (a secret of an external system): only whether
   * one is saved.
   */
  hasSmtpPassword: boolean;
  smtpSecurity: SmtpSecurity;
}
