import { SmtpSecurity } from '../../enums/smtp-security';

export interface EmailConfigurationData {
  fromEmail: string | null;
  fromName: string | null;
  smtpHost: string | null;
  smtpPort: number;
  smtpUsername: string | null;
  /**
   * The SMTP password itself never leaves the server (a secret of an external system): only whether
   * one is saved.
   */
  hasSmtpPassword: boolean;
  security: SmtpSecurity;
  verifyCertificate: boolean;
  testRecipients: string[];
  isActive: boolean;
  lastVerificationAt: string | null;
  lastVerificationError: string | null;
  version: string;
}
