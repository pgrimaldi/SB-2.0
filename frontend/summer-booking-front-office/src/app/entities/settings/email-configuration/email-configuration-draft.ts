import { SmtpSecurity } from '../../enums/smtp-security';

/** The recipients are written in one field, separated by commas. */
export class EmailConfigurationDraft {
  fromEmail: string | null = null;
  fromName: string | null = null;
  smtpHost: string | null = null;
  smtpPort: number | null = null;
  smtpUsername: string | null = null;
  smtpPassword: string | null = null;
  security: SmtpSecurity | null = null;
  verifyCertificate = true;
  testRecipients: string | null = null;
  isActive = true;
}
