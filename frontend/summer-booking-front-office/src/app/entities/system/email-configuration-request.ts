import { SmtpSecurity } from './email-configuration-data';

/** The values in the fields of the page: what is saved, or tried with a test email before saving. */
export interface EmailConfigurationRequest {
  idProperty: string;
  senderMailAddress: string;
  senderName: string;
  smtpServerAddress: string;
  /** The page sends only 1-65535 (it validates the field); `null` is the empty field. */
  smtpPort: number | null;
  smtpUsername: string;
  /** A new password typed by the user; `null` keeps the one the server has saved. */
  smtpPassword: string | null;
  smtpSecurity: SmtpSecurity | null;
}
