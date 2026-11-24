import { EmailConfigurationRequest } from './email-configuration-request';

interface EmailConfigurationTextField {
  isNumber: false;
  name: 'fromEmail' | 'fromName' | 'smtpHost' | 'smtpUsername' | 'smtpPassword';
  isMandatory: boolean;
  isCode: boolean;
  isPassword: boolean;
}

interface EmailConfigurationNumberField {
  isNumber: true;
  name: 'smtpPort';
  isMandatory: boolean;
}

export type EmailConfigurationMaskedField =
  EmailConfigurationTextField | EmailConfigurationNumberField;

export const EMAIL_CONFIGURATION_MASKED_FIELDS: readonly EmailConfigurationMaskedField[] = [
  { isNumber: false, name: 'fromEmail', isMandatory: true, isCode: true, isPassword: false },
  { isNumber: false, name: 'fromName', isMandatory: false, isCode: false, isPassword: false },
  { isNumber: false, name: 'smtpHost', isMandatory: true, isCode: true, isPassword: false },
  { isNumber: true, name: 'smtpPort', isMandatory: true },
  { isNumber: false, name: 'smtpUsername', isMandatory: true, isCode: true, isPassword: false },
  { isNumber: false, name: 'smtpPassword', isMandatory: false, isCode: true, isPassword: true },
];

/** The texts of each field: `management.settings.email_configuration.fields.<key>`. */
export const EMAIL_CONFIGURATION_FIELD_KEYS: Readonly<
  Record<keyof EmailConfigurationRequest, string>
> = {
  fromEmail: 'from_email',
  fromName: 'from_name',
  smtpHost: 'smtp_server',
  smtpPort: 'smtp_port',
  smtpUsername: 'smtp_username',
  smtpPassword: 'smtp_password',
  security: 'smtp_security',
  verifyCertificate: 'verify_certificate',
  testRecipients: 'test_recipients',
  isActive: 'is_active',
};
