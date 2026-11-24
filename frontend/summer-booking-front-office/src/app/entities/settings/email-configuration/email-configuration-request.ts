import { SmtpSecurity } from '../../enums/smtp-security';

export const MAX_SMTP_EMAIL_LENGTH = 320;
export const MAX_SMTP_NAME_LENGTH = 160;
export const MAX_SMTP_HOST_LENGTH = 255;
export const MAX_SMTP_PORT = 65_535;
export const SMTP_SSL_PORT = 465;
export const SMTP_START_TLS_PORT = 587;
export const MAX_TEST_RECIPIENT_QUANTITY = 10;

export class EmailConfigurationRequest {
  fromEmail: string | null = null;
  fromName: string | null = null;
  smtpHost: string | null = null;
  smtpPort: number | null = null;
  smtpUsername: string | null = null;
  smtpPassword: string | null = null;
  security: SmtpSecurity | null = null;
  verifyCertificate = true;
  testRecipients: string[] = [];
  isActive = true;
}

/** The saved password goes only where it was saved for: any of these changed asks to type it again. */
export const SMTP_CONNECTION_FIELDS = [
  'smtpHost',
  'smtpPort',
  'smtpUsername',
  'security',
  'verifyCertificate',
] as const;
