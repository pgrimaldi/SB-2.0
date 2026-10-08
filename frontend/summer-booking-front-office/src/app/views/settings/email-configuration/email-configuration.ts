import {
  ChangeDetectionStrategy,
  Component,
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
  FormField,
  TreeValidationResult,
  form,
  max,
  min,
  readonly,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../behaviours/forms/unsaved-changes.behaviour';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { apiFieldErrorsOrGeneral } from '../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../behaviours/validation/validation-text.behaviour';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
import { Button, ButtonSize } from '../../../components/shared/ui/buttons/button/button';
import { FilledNumberField } from '../../../components/shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { FilledSelect } from '../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../components/shared/ui/selects/select/select';
import { ApiProblem } from '../../../entities/errors/api-problem';
import {
  EMAIL_CONFIGURATION_MASKED_FIELDS,
  EmailConfigurationRequest,
  MAX_SMTP_PORT,
  SMTP_CONNECTION_FIELDS,
} from '../../../entities/settings/email-configuration/email-configuration-request';
import { SmtpSecurity } from '../../../entities/settings/email-configuration/email-configuration-data';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { SystemService } from '../../../services/api/system/system.service';

interface SecurityTexts {
  none: string;
  ssl: string;
  tls: string;
}

@Component({
  selector: 'app-email-configuration',
  imports: [
    Button,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FormField,
    I18nText,
    MessagePopup,
    TranslatePipe,
  ],
  templateUrl: './email-configuration.html',
  styleUrl: './email-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailConfiguration implements UnsavedChanges, OnDestroy {
  private readonly translateService = inject(TranslateService);
  private readonly system = inject(SystemService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  protected readonly ButtonSize = ButtonSize;

  protected readonly maskedFields = EMAIL_CONFIGURATION_MASKED_FIELDS;
  protected readonly sendTestIcon = ['/assets/images/mail-send-white.svg'] as const;
  protected readonly resetIcon = ['/assets/images/trash-white.svg'] as const;
  /** [content shown, content hidden]: the designer's eyes (Eye_close once open, Eye_start at first). */
  protected readonly eyeIcons = [
    '/assets/images/eye-close.svg',
    '/assets/images/eye-start.svg',
  ] as const;
  /** No currency: only the eye. */
  protected readonly portIcons = ['', ...this.eyeIcons] as const;

  private readonly configuration = signal(new EmailConfigurationRequest());
  /** "Sì" of "Vuoi sovrascrivere le impostazioni predefinite?": until then every field is read-only. */
  protected readonly editable = signal(false);
  /** The server has a password saved (it never sends it). */
  protected readonly passwordSaved = signal(false);
  /**
   * The values the saved password belongs to, as the server last answered or saved them; never with
   * the password.
   */
  private readonly savedConnection = signal<EmailConfigurationRequest | null>(null);
  /** Goes up with every save that succeeds: a reading started before it answers older values. */
  private savesDone = 0;
  /**
   * The saved password must not be sent to another server, port, user or with another security
   * (e.g. a server of whoever changed it, or a connection without encryption): then it is typed again.
   */
  private readonly passwordToRetype = computed(() => {
    const saved = this.savedConnection();
    return (
      this.passwordSaved() &&
      !!saved &&
      // An emptied field is null, while the server may answer an empty text.
      SMTP_CONNECTION_FIELDS.some(
        (field) => (this.configuration()[field] || null) !== (saved[field] || null),
      )
    );
  });
  /** The last reading arrived: nothing can be changed before the values of the server are there. */
  private readonly loaded = signal(false);
  protected readonly configurationForm = form(this.configuration, (path) => {
    // Read-only also while a call runs: a reading would overwrite what is typed meanwhile, and Salva
    // or the test email have already sent the values (rule: nothing changes until the server answers).
    readonly(path, { when: () => !this.editable() || this.isBusy() });
    required(path.smtpPort);
    min(path.smtpPort, 1);
    max(path.smtpPort, MAX_SMTP_PORT);
    validate(path.smtpPassword, ({ value }) =>
      !value() && this.passwordToRetype()
        ? { kind: 'email_configuration.smtp_password.retype' }
        : undefined,
    );
  });
  /** Under each field: what is wrong in it, found here or by the backend. */
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.configuration()) as (keyof EmailConfigurationRequest)[]).map((name) => [
      name,
      this.validationText.message(this.configurationForm[name]),
    ]),
  ) as Readonly<Record<keyof EmailConfigurationRequest, Signal<string | null>>>;
  private readonly securityTexts = toSignal(
    this.translateService.stream(
      'management.settings.email_configuration.security',
    ) as Observable<SecurityTexts>,
    { initialValue: { none: '', ssl: '', tls: '' } },
  );
  /** Asked again by Reset: the effect that loads the configuration reads it. */
  private readonly reloads = signal(0);
  protected readonly isReloading = signal(false);
  protected readonly isSendingTest = signal(false);
  protected readonly testSent = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly saved = signal(false);
  protected readonly canOverride = computed(
    () => !this.editable() && this.loaded() && !this.isReloading(),
  );
  protected readonly isBusy = computed(
    () => this.isReloading() || this.isSendingTest() || this.isSaving(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly securityOptions = computed<readonly SelectOption<SmtpSecurity>[]>(() => [
    { value: 'None', label: this.securityTexts().none },
    { value: 'Ssl', label: this.securityTexts().ssl },
    { value: 'Tls', label: this.securityTexts().tls },
  ]);

  constructor() {
    this.unsaved.watch(this);
    // The request to type the password again shows at once, not only after leaving its field.
    effect(() => {
      if (this.passwordToRetype()) {
        untracked(() => this.configurationForm.smtpPassword().markAsTouched());
      }
    });
    // Loaded again with Reset and when the language changes (what was typed is lost: user's choice).
    effect((onCleanup) => {
      this.reloads();
      this.dataReload?.();
      const idProperty = this.auth.user()?.idProperty;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() => {
        const savesAtStart = this.savesDone;
        this.isReloading.set(true);
        this.error.set(null);
        return this.system.emailConfiguration({ idProperty }).subscribe({
          next: (data) => {
            this.isReloading.set(false);
            if (savesAtStart !== this.savesDone) {
              return; // the values just saved are newer than this answer
            }
            const { hasSmtpPassword, ...fields } = data;
            const configuration = { ...fields, idProperty, smtpPassword: null };
            this.configuration.set(configuration);
            this.passwordSaved.set(hasSmtpPassword);
            this.savedConnection.set(configuration);
            this.configurationForm().reset();
            this.loaded.set(true);
          },
          error: (error: unknown) => {
            this.isReloading.set(false);
            this.loaded.set(false);
            this.error.set(toApiProblem(error));
          },
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  /** Reset: the values of the server again (what was typed is lost), read-only until "Sì" again. */
  protected reset(): void {
    this.editable.set(false);
    this.reloads.update((reloads) => reloads + 1);
  }

  protected sendTestEmail(): void {
    this.submitWith(
      this.isSendingTest,
      (request) => this.system.sendTestEmail(request),
      () => this.testSent.set(true),
    );
  }

  protected save(): void {
    this.submitWith(
      this.isSaving,
      (request) => this.system.saveEmailConfiguration(request),
      (sent) => {
        // From what was sent, not from the form: a reading may have changed it meanwhile. The
        // password, now saved, leaves the page: the field is empty again.
        const savedValues = { ...sent, smtpPassword: null };
        this.savesDone++;
        this.passwordSaved.update((saved) => saved || sent.smtpPassword !== null);
        this.savedConnection.set(savedValues);
        this.configuration.set(savedValues);
        this.configurationForm().reset();
        this.saved.set(true);
      },
    );
  }

  /**
   * Through Signal Forms `submit()`: nothing is sent while a field is wrong (every field becomes
   * touched, so each shows its error), and an error of the backend on a field goes under that field.
   */
  private submitWith(
    isRunning: WritableSignal<boolean>,
    call: (request: EmailConfigurationRequest) => Observable<void>,
    done: (sent: EmailConfigurationRequest) => void,
  ): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isBusy() || !idProperty) {
      return;
    }
    void submit(this.configurationForm, async (): Promise<TreeValidationResult> => {
      isRunning.set(true);
      this.error.set(null);
      try {
        const request = { ...this.configuration(), idProperty };
        await firstValueFrom(call(request));
        done(request);
        return undefined;
      } catch (error: unknown) {
        return this.failed(toApiProblem(error));
      } finally {
        isRunning.set(false);
      }
    });
  }

  /**
   * The errors on a field go under it; any other error under the panel. Read-only fields show no
   * error (Signal Forms does not validate them): then the message goes under the panel too.
   */
  private failed(problem: ApiProblem): TreeValidationResult {
    const errors = apiFieldErrorsOrGeneral(problem, this.configurationForm, (general) =>
      this.error.set(general),
    );
    if (!this.editable()) {
      this.error.set(problem);
    }
    return errors;
  }

  hasUnsavedChanges(): boolean {
    return this.configurationForm().dirty();
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
