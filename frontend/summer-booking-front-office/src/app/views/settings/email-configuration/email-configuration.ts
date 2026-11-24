import {
  ChangeDetectionStrategy,
  Component,
  EnvironmentInjector,
  ElementRef,
  OnDestroy,
  Signal,
  WritableSignal,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FieldTree,
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  ValidationError,
  disabled,
  form,
  max,
  metadata,
  min,
  readonly,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../behaviours/forms/unsaved-changes.behaviour';
import { showFirstError } from '../../../components/shared/forms/first-error';
import {
  comparableJson,
  sameTextIgnoringCase,
  trimmedText,
} from '../../../components/shared/forms/text-comparison';
import {
  emailAddress,
  isEmailAddress,
  requiredText,
} from '../../../components/shared/forms/validators';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import {
  apiFieldErrorsOrGeneral,
  clearApiFieldErrors,
} from '../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../behaviours/validation/validation-text.behaviour';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
import { VersionConflictPopup } from '../../../components/management/version-conflict-popup/version-conflict-popup';
import { Button, ButtonSize } from '../../../components/shared/ui/buttons/button/button';
import { FormRow } from '../../../components/shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../components/shared/ui/forms/form-row/form-row-label';
import { FilledNumberField } from '../../../components/shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { FilledSelect } from '../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../components/shared/ui/selects/select/select';
import { Toggle } from '../../../components/shared/ui/toggles/toggle/toggle';
import { EmailVerification } from '../../../entities/enums/email-verification';
import { SmtpSecurity } from '../../../entities/enums/smtp-security';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { EmailConfigurationData } from '../../../entities/settings/email-configuration/email-configuration-data';
import { EmailConfigurationDraft } from '../../../entities/settings/email-configuration/email-configuration-draft';
import {
  EMAIL_CONFIGURATION_FIELD_KEYS,
  EMAIL_CONFIGURATION_MASKED_FIELDS,
} from '../../../entities/settings/email-configuration/email-configuration-field';
import {
  EmailConfigurationRequest,
  MAX_SMTP_EMAIL_LENGTH,
  MAX_SMTP_HOST_LENGTH,
  MAX_SMTP_NAME_LENGTH,
  MAX_SMTP_PORT,
  MAX_TEST_RECIPIENT_QUANTITY,
  SMTP_CONNECTION_FIELDS,
  SMTP_SSL_PORT,
  SMTP_START_TLS_PORT,
  SMTP_VERIFIED_FIELDS,
} from '../../../entities/settings/email-configuration/email-configuration-request';
import { cutText, toApiProblem } from '../../../services/api/errors/to-api-problem';
import { NotificationService } from '../../../services/api/notification/notification.service';

interface EmailConfigurationError extends ValidationError {
  readonly kind: EmailConfigurationErrorKind;
  readonly max?: number;
}

enum EmailConfigurationErrorKind {
  PasswordToRetype = 'email_configuration.smtp_password.retype',
  SslPort = 'email_configuration.security.ssl_port',
  StartTlsPort = 'email_configuration.security.start_tls_port',
  WrongRecipients = 'email_configuration.test_recipients.wrong',
  TooManyRecipients = 'email_configuration.test_recipients.max',
}

const RECIPIENT_SEPARATOR = ', ';

function recipientsOf(text: string | null): string[] {
  const recipients = (text ?? '').split(/[,;\s\uff0c\u3001\uff1b\u060c\u061b]+/).filter(Boolean);
  return recipients.filter(
    (email, index) => recipients.findIndex((other) => sameTextIgnoringCase(other, email)) === index,
  );
}

function recipientsText(recipients: readonly string[]): string | null {
  return recipients.join(RECIPIENT_SEPARATOR) || null;
}

function hasOther<TField extends string>(
  fields: readonly TField[],
  first: Readonly<Record<TField, unknown>>,
  second: Readonly<Record<TField, unknown>>,
): boolean {
  return fields.some((field) => comparableJson(first[field]) !== comparableJson(second[field]));
}

function draftOf(data: EmailConfigurationData): EmailConfigurationDraft {
  return {
    fromEmail: data.fromEmail,
    fromName: data.fromName,
    smtpHost: data.smtpHost,
    smtpPort: data.smtpPort,
    smtpUsername: data.smtpUsername,
    smtpPassword: null,
    security: data.security,
    verifyCertificate: data.verifyCertificate,
    testRecipients: recipientsText(data.testRecipients),
    isActive: data.isActive,
  };
}

function requestOf(draft: EmailConfigurationDraft): EmailConfigurationRequest {
  return {
    ...draft,
    fromEmail: trimmedText(draft.fromEmail),
    fromName: trimmedText(draft.fromName),
    smtpHost: trimmedText(draft.smtpHost),
    smtpUsername: trimmedText(draft.smtpUsername),
    smtpPassword: trimmedText(draft.smtpPassword) && draft.smtpPassword,
    testRecipients: recipientsOf(draft.testRecipients),
  };
}

@Component({
  selector: 'app-email-configuration',
  imports: [
    Button,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FormField,
    FormRow,
    FormRowLabel,
    I18nText,
    MessagePopup,
    Toggle,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './email-configuration.html',
  styleUrl: './email-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailConfiguration implements UnsavedChanges, OnDestroy {
  private readonly translateService = inject(TranslateService);
  private readonly notification = inject(NotificationService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(EnvironmentInjector);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  protected readonly ButtonSize = ButtonSize;

  protected readonly maskedFields = EMAIL_CONFIGURATION_MASKED_FIELDS;
  protected readonly fieldKeys = EMAIL_CONFIGURATION_FIELD_KEYS;
  protected readonly sendTestIcon = ['/assets/images/EmailConfiguration_MailSend.svg'] as const;
  protected readonly resetIcon = ['/assets/images/GeneralIcon_DeleteWhite.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [content shown, content hidden]: the eye icons (Eye_close once open, Eye_start at first). */
  protected readonly eyeIcons = [
    '/assets/images/GeneralIcon_EyeClose.svg',
    '/assets/images/GeneralIcon_EyeStart.svg',
  ] as const;
  /** No currency: only the eye. */
  protected readonly portIcons = ['', ...this.eyeIcons] as const;

  private readonly configuration = signal(new EmailConfigurationDraft());
  protected readonly editable = signal(false);
  protected readonly passwordSaved = signal(false);
  /**
   * The values the saved password belongs to, as the server last answered or saved them; never with
   * the password.
   */
  private readonly savedConfiguration = signal<EmailConfigurationDraft | null>(null);
  private readonly lastVerificationAt = signal<string | null>(null);
  private readonly lastVerificationError = signal<string | null>(null);
  private verificationRead: Subscription | null = null;
  private readonly idProperty = computed(() => this.auth.user()?.propertyId ?? null);
  private version: string | null = null;
  /** Goes up with every save that succeeds: a reading started before it answers older values. */
  private savesDone = 0;
  private isConflictPending = false;
  private isDestroyed = false;
  /**
   * The saved password must not be sent to another server, port, user or with another security
   * (e.g. a server of whoever changed it, or a connection without encryption): then it is typed again.
   */
  private readonly passwordToRetype = computed(() => {
    const saved = this.savedConfiguration();
    return (
      this.passwordSaved() &&
      !!saved &&
      hasOther(SMTP_CONNECTION_FIELDS, this.configuration(), saved)
    );
  });
  protected readonly loaded = signal(false);
  protected readonly configurationForm = form(this.configuration, (path) => {
    // Read-only also while a call runs: a reading would overwrite what is typed meanwhile, and Salva
    // or the test email have already sent the values (rule: nothing changes until the server answers).
    readonly(path, { when: () => this.isLocked() });
    disabled(path.isActive, () => this.isLocked());
    disabled(path.verifyCertificate, () => this.isLocked());
    requiredText(path.fromEmail);
    emailAddress(path.fromEmail);
    metadata(path.fromEmail, MAX_LENGTH, () => MAX_SMTP_EMAIL_LENGTH);
    metadata(path.fromName, MAX_LENGTH, () => MAX_SMTP_NAME_LENGTH);
    requiredText(path.smtpHost);
    metadata(path.smtpHost, MAX_LENGTH, () => MAX_SMTP_HOST_LENGTH);
    requiredText(path.smtpUsername);
    metadata(path.smtpUsername, MAX_LENGTH, () => MAX_SMTP_EMAIL_LENGTH);
    required(path.smtpPort);
    min(path.smtpPort, 1);
    max(path.smtpPort, MAX_SMTP_PORT);
    required(path.security);
    validate(path.security, ({ value, valueOf }): EmailConfigurationError | undefined => {
      const port = valueOf(path.smtpPort);
      if (value() && port === SMTP_SSL_PORT && value() !== SmtpSecurity.Ssl) {
        return { kind: EmailConfigurationErrorKind.SslPort };
      }
      return value() && port === SMTP_START_TLS_PORT && value() !== SmtpSecurity.StartTls
        ? { kind: EmailConfigurationErrorKind.StartTlsPort }
        : undefined;
    });
    validate(path.testRecipients, ({ value }): EmailConfigurationError | undefined => {
      const recipients = recipientsOf(value());
      if (recipients.length > MAX_TEST_RECIPIENT_QUANTITY) {
        return {
          kind: EmailConfigurationErrorKind.TooManyRecipients,
          max: MAX_TEST_RECIPIENT_QUANTITY,
        };
      }
      return recipients.every(
        (email) => email.length <= MAX_SMTP_EMAIL_LENGTH && isEmailAddress(email),
      )
        ? undefined
        : { kind: EmailConfigurationErrorKind.WrongRecipients };
    });
    validate(path.smtpPassword, ({ value }): EmailConfigurationError | undefined =>
      !value()?.trim() && this.passwordToRetype()
        ? { kind: EmailConfigurationErrorKind.PasswordToRetype }
        : undefined,
    );
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.configuration()) as (keyof EmailConfigurationDraft)[]).map((name) => [
      name,
      this.validationText.message(this.configurationForm[name] as FieldTree<unknown>),
    ]),
  ) as Readonly<Record<keyof EmailConfigurationDraft, Signal<string | null>>>;
  private readonly securityTexts = toSignal(
    this.translateService.stream('management.settings.email_configuration.security') as Observable<
      Record<SmtpSecurity, string>
    >,
    { initialValue: null },
  );
  private readonly reloads = signal(0);
  protected readonly isReloading = signal(false);
  protected readonly isSendingTest = signal(false);
  protected readonly testSent = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly saved = signal(false);
  protected readonly isConflictShown = signal(false);
  protected readonly isBusy = computed(
    () => this.isReloading() || this.isSendingTest() || this.isSaving(),
  );
  protected readonly canOverride = computed(
    () => !this.editable() && this.loaded() && !this.isBusy(),
  );
  private readonly isLocked = computed(() => !this.editable() || !this.loaded() || this.isBusy());
  protected readonly canSendTest = computed(
    () =>
      this.loaded() &&
      !this.isBusy() &&
      recipientsOf(this.configuration().testRecipients).length > 0 &&
      (this.editable() || !!this.savedConfiguration()?.fromEmail),
  );
  protected readonly canSave = computed(
    () => !this.isLocked() && !this.configurationForm().invalid(),
  );
  protected readonly EmailVerification = EmailVerification;
  /** About the configuration saved, whatever is being typed. */
  protected readonly verification = computed(() => {
    const saved = this.savedConfiguration();
    if (!saved) {
      return null;
    }
    if (!saved.fromEmail) {
      return EmailVerification.NotConfigured;
    }
    if (!saved.isActive) {
      return EmailVerification.Off;
    }
    if (!this.lastVerificationAt()) {
      return EmailVerification.Never;
    }
    return this.lastVerificationError() ? EmailVerification.Failed : EmailVerification.Done;
  });
  protected readonly verificationParams = computed(() => ({
    at: this.lastVerificationAt(),
    detail: this.lastVerificationError(),
  }));
  private readonly readError = signal<ApiProblem | null>(null);
  protected readonly readErrorMessage = this.messageOf(this.readError);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = this.messageOf(this.error);
  /** The field `error` is about, when nothing can show under it. */
  protected readonly errorField = signal<keyof EmailConfigurationRequest | null>(null);
  protected readonly errorDetail = computed(() => {
    const problem = this.error();
    const detail = problem?.args?.['detail'];
    return problem?.code === ApiErrorCode.NotificationTestEmailFailed ? (detail ?? null) : null;
  });
  protected readonly securityOptions = computed<readonly SelectOption<SmtpSecurity>[]>(() =>
    Object.values(SmtpSecurity).map((value) => ({
      value,
      label: this.securityTexts()?.[value] ?? '',
    })),
  );

  constructor() {
    this.unsaved.watch(this);
    // The request to type the password again shows at once, not only after leaving its field.
    effect(() => {
      if (this.passwordToRetype()) {
        untracked(() => this.configurationForm.smtpPassword().markAsTouched());
      }
    });
    effect((onCleanup) => {
      this.reloads();
      this.dataReload?.();
      if (!this.idProperty()) {
        return;
      }
      const subscription = untracked(() => this.load());
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    const saved = this.savedConfiguration();
    return (
      !!saved &&
      comparableJson(requestOf(this.configuration())) !== comparableJson(requestOf(saved))
    );
  }

  protected allowChanges(): void {
    if (!this.canOverride()) {
      return;
    }
    this.editable.set(true);
    this.clearError();
    if (this.savedConfiguration()?.fromEmail) {
      this.configurationForm().markAsTouched();
    }
  }

  protected async requestReset(): Promise<void> {
    if (!this.isBusy() && (await this.unsaved.confirmDiscard(this))) {
      this.reset();
    }
  }

  protected sendTestEmail(): void {
    this.submitWith(
      this.isSendingTest,
      (request) => this.notification.sendTestEmail(request),
      () => this.testSent.set(true),
      () => this.readVerification(),
    );
  }

  protected save(): void {
    if (!this.editable()) {
      return;
    }
    this.submitWith(
      this.isSaving,
      (request) => this.notification.saveEmailConfiguration(request, this.version),
      (sent, version) => {
        // From what was sent, not from the form: a reading may have changed it meanwhile. The
        // password, now saved, leaves the page: the field is empty again.
        const savedValues: EmailConfigurationDraft = {
          ...sent,
          smtpPassword: null,
          testRecipients: recipientsText(sent.testRecipients),
        };
        const previous = this.savedConfiguration();
        if (
          sent.smtpPassword !== null ||
          (previous && hasOther(SMTP_VERIFIED_FIELDS, savedValues, previous))
        ) {
          this.showVerification(null, null);
        }
        this.savesDone++;
        this.version = version;
        this.passwordSaved.update((saved) => saved || sent.smtpPassword !== null);
        this.savedConfiguration.set(savedValues);
        this.configuration.set(savedValues);
        this.configurationForm().reset();
        this.editable.set(false);
        this.saved.set(true);
      },
    );
  }

  protected reload(): void {
    this.reloads.update((reloads) => reloads + 1);
  }

  private load(): Subscription {
    const savesAtStart = this.savesDone;
    this.isReloading.set(true);
    this.readError.set(null);
    this.clearError();
    return this.notification.emailConfiguration().subscribe({
      next: (data) => {
        this.isReloading.set(false);
        if (savesAtStart !== this.savesDone) {
          return; // the values just saved are newer than this answer
        }
        const configuration = draftOf(data);
        this.version = data.version;
        clearApiFieldErrors(this.configurationForm);
        this.configuration.set(configuration);
        this.passwordSaved.set(data.hasSmtpPassword);
        this.savedConfiguration.set(configuration);
        this.showVerification(data.lastVerificationAt, data.lastVerificationError);
        this.configurationForm().reset();
        this.loaded.set(true);
        if (this.isConflictPending) {
          this.isConflictPending = false;
          this.isConflictShown.set(true);
        }
      },
      error: (error: unknown) => {
        this.isReloading.set(false);
        this.loaded.set(false);
        this.readError.set(toApiProblem(error));
      },
    });
  }

  private reset(): void {
    // While the fields can still change: read-only ones keep the errors of the backend.
    clearApiFieldErrors(this.configurationForm);
    this.editable.set(false);
    const saved = this.savedConfiguration();
    if (saved) {
      this.configuration.set(saved);
      this.configurationForm().reset();
    }
    this.reload();
  }

  private submitWith<T>(
    isRunning: WritableSignal<boolean>,
    call: (request: EmailConfigurationRequest) => Observable<T>,
    done: (sent: EmailConfigurationRequest, answer: T) => void,
    always: () => void = () => undefined,
  ): void {
    if (this.isBusy() || !this.loaded() || !this.idProperty()) {
      return;
    }
    void submit(this.configurationForm, async (): Promise<TreeValidationResult> => {
      isRunning.set(true);
      this.clearError();
      try {
        const request = requestOf(this.configuration());
        done(request, await firstValueFrom(call(request)));
      } catch (error: unknown) {
        return this.failed(toApiProblem(error));
      } finally {
        isRunning.set(false);
        always();
      }
    }).then((isDone) => {
      if (!isDone || this.error()) {
        showFirstError(this.host, '.email__configuration__error', this.injector);
      }
    });
  }

  private readVerification(): void {
    if (this.isDestroyed || this.hasUnsavedChanges()) {
      return;
    }
    this.verificationRead?.unsubscribe();
    this.verificationRead = this.notification.emailConfiguration().subscribe({
      next: ({ version, lastVerificationAt, lastVerificationError }) => {
        if (version === this.version) {
          this.showVerification(lastVerificationAt, lastVerificationError);
        }
      },
      error: () => undefined,
    });
  }

  private showVerification(at: string | null, error: string | null): void {
    this.lastVerificationAt.set(at);
    this.lastVerificationError.set(error && cutText(error));
  }

  private failed(problem: ApiProblem): TreeValidationResult {
    if (problem.code === ApiErrorCode.ResourceVersionConflict) {
      this.isConflictPending = true;
      this.reset();
      return undefined;
    }
    // Read-only fields show no error: the message says which one it is about.
    const isReadOnly = !this.editable();
    const names = Object.keys(
      EMAIL_CONFIGURATION_FIELD_KEYS,
    ) as (keyof EmailConfigurationRequest)[];
    return apiFieldErrorsOrGeneral(
      problem,
      this.configurationForm,
      (general) => {
        this.error.set(general);
        const field = isReadOnly ? problem.errors?.[0]?.field : null;
        this.errorField.set(names.find((name) => name === field) ?? null);
      },
      isReadOnly ? names : [],
    );
  }

  private clearError(): void {
    this.error.set(null);
    this.errorField.set(null);
  }

  private messageOf(problem: Signal<ApiProblem | null>): Signal<string | null> {
    return toSignal(
      toObservable(problem).pipe(
        switchMap((shown) => (shown ? this.errorText.text(shown) : of(null))),
      ),
      { initialValue: null },
    );
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.verificationRead?.unsubscribe();
    this.unsaved.unwatch(this);
  }
}
