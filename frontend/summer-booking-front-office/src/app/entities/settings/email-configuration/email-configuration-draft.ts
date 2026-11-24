import { SmtpSecurity } from '../../enums/smtp-security';

export class EmailConfigurationDraft {
  fromEmail: string | null = null;
  fromName: string | null = null;
  smtpHost: string | null = null;
  smtpPort: number | null = null;
  smtpUsername: string | null = null;
  smtpPassword: string | null = null;
  security: SmtpSecurity | null = null;
  verifyCertificate = false;
  testRecipients: string | null = null;
  isActive = false;
}
