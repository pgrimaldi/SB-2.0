import { SmtpSecurity } from './email-configuration-data';

export class EmailConfigurationRequest {
  idProperty: string | null = null;
  senderMailAddress: string | null = null;
  senderName: string | null = null;
  smtpServerAddress: string | null = null;
  /** 1-65535. */
  smtpPort: number | null = null;
  smtpUsername: string | null = null;
  /** A new password typed by the user; `null` keeps the one the server has saved. */
  smtpPassword: string | null = null;
  smtpSecurity: SmtpSecurity | null = null;
}

/** `key` is its translation group under `fields`. */
export type EmailConfigurationMaskedField =
  | {
      name:
        'senderMailAddress' | 'senderName' | 'smtpServerAddress' | 'smtpUsername' | 'smtpPassword';
      key: string;
    }
  | { name: 'smtpPort'; key: string };

/** In page order. */
export const EMAIL_CONFIGURATION_MASKED_FIELDS: readonly EmailConfigurationMaskedField[] = [
  { name: 'senderMailAddress', key: 'from_email' },
  { name: 'senderName', key: 'from_name' },
  { name: 'smtpServerAddress', key: 'smtp_server' },
  { name: 'smtpPort', key: 'smtp_port' },
  { name: 'smtpUsername', key: 'smtp_username' },
  { name: 'smtpPassword', key: 'smtp_password' },
];

export const MAX_SMTP_PORT = 65535;

/** Where the saved password goes: changing one of them means sending it somewhere else. */
export const SMTP_CONNECTION_FIELDS = [
  'smtpServerAddress',
  'smtpPort',
  'smtpUsername',
  'smtpSecurity',
] as const;
