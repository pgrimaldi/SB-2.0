interface EmailConfigurationTextField {
  name: 'fromEmail' | 'fromName' | 'smtpHost' | 'smtpUsername' | 'smtpPassword';
  key: string;
  isMandatory: boolean;
}

interface EmailConfigurationNumberField {
  name: 'smtpPort';
  key: string;
  isMandatory: boolean;
}

export type EmailConfigurationMaskedField =
  EmailConfigurationTextField | EmailConfigurationNumberField;

export const EMAIL_CONFIGURATION_MASKED_FIELDS: readonly EmailConfigurationMaskedField[] = [
  { name: 'fromEmail', key: 'from_email', isMandatory: true },
  { name: 'fromName', key: 'from_name', isMandatory: false },
  { name: 'smtpHost', key: 'smtp_server', isMandatory: true },
  { name: 'smtpPort', key: 'smtp_port', isMandatory: true },
  { name: 'smtpUsername', key: 'smtp_username', isMandatory: true },
  { name: 'smtpPassword', key: 'smtp_password', isMandatory: false },
];
