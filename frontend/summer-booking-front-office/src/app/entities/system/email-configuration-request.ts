import { SmtpSecurity } from './email-configuration-data';

/** The values in the fields of the page: what is saved, or tried with a test email before saving. */
export interface EmailConfigurationRequest {
  idProperty: string;
  senderMailAddress: string;
  senderName: string;
  smtpServerAddress: string;
  /** `null` when the field does not hold a whole number. */
  smtpPort: number | null;
  smtpUsername: string;
  smtpPassword: string;
  smtpSecurity: SmtpSecurity | null;
}
