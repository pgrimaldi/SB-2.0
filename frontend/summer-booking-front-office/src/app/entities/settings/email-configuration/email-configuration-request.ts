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

/** The form of the page: the password field holds only a new password, empty keeps the saved one. */
export type EmailConfigurationFields = Omit<
  EmailConfigurationRequest,
  'idProperty' | 'smtpPassword'
> & {
  smtpPassword: string;
};

type EmailConfigurationTextField = Exclude<
  keyof EmailConfigurationFields,
  'smtpPort' | 'smtpSecurity'
>;

/** A field shown masked, as a password; `key` is its translation group under `fields`. */
export type EmailConfigurationMaskedField =
  { name: EmailConfigurationTextField; key: string } | { name: 'smtpPort'; key: string };

/** In page order. The security is a select, not masked. */
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

export type SmtpConnection = Pick<
  EmailConfigurationFields,
  (typeof SMTP_CONNECTION_FIELDS)[number]
>;

/** Only the connection, never the password. */
export function smtpConnectionOf(values: SmtpConnection): SmtpConnection {
  return {
    smtpServerAddress: values.smtpServerAddress,
    smtpPort: values.smtpPort,
    smtpUsername: values.smtpUsername,
    smtpSecurity: values.smtpSecurity,
  };
}
