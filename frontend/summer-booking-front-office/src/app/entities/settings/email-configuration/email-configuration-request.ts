import { SmtpSecurity } from './email-configuration-data';

export class EmailConfigurationRequest {
  idProperty: string | null = null;
  senderMailAddress: string | null = null;
  senderName: string | null = null;
  smtpServerAddress: string | null = null;
  smtpPort: number | null = null;
  smtpUsername: string | null = null;
  smtpPassword: string | null = null;
  smtpSecurity: SmtpSecurity | null = null;
}

export type EmailConfigurationMaskedField =
  | {
      name:
        'senderMailAddress' | 'senderName' | 'smtpServerAddress' | 'smtpUsername' | 'smtpPassword';
      key: string;
    }
  | { name: 'smtpPort'; key: string };

export const EMAIL_CONFIGURATION_MASKED_FIELDS: readonly EmailConfigurationMaskedField[] = [
  { name: 'senderMailAddress', key: 'from_email' },
  { name: 'senderName', key: 'from_name' },
  { name: 'smtpServerAddress', key: 'smtp_server' },
  { name: 'smtpPort', key: 'smtp_port' },
  { name: 'smtpUsername', key: 'smtp_username' },
  { name: 'smtpPassword', key: 'smtp_password' },
];

export const MAX_SMTP_PORT = 65535;

export const SMTP_CONNECTION_FIELDS = [
  'smtpServerAddress',
  'smtpPort',
  'smtpUsername',
  'smtpSecurity',
] as const;
