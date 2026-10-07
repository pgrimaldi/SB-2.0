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
import { DATA_RELOAD } from '../../../components/shared/data/data-reload';
import { apiFieldErrors } from '../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../behaviours/validation/validation-text.behaviour';
import { Button } from '../../../components/shared/ui/buttons/button/button';
import { FilledNumberField } from '../../../components/shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { FilledSelect } from '../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../components/shared/ui/selects/select/select';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { EmailConfigurationRequest } from '../../../entities/system/email-configuration-request';
import { SmtpSecurity } from '../../../entities/system/email-configuration-data';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { SystemService } from '../../../services/api/system/system.service';

/** The password field holds only a new password: empty keeps the saved one. */
type EmailConfigurationFields = Omit<EmailConfigurationRequest, 'idProperty' | 'smtpPassword'> & {
  smtpPassword: string;
};

type TextFieldName = Exclude<keyof EmailConfigurationFields, 'smtpPort' | 'smtpSecurity'>;

/** A masked field of the page; `key` is its translation group under `fields`. */
type MaskedField = { name: TextFieldName; key: string } | { name: 'smtpPort'; key: string };

const MAX_TCP_PORT = 65535;

/** Where the saved password goes: changing one of them means sending it somewhere else. */
const CONNECTION_FIELDS = [
  'smtpServerAddress',
  'smtpPort',
  'smtpUsername',
  'smtpSecurity',
] as const;

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

  /** In page order. */
  protected readonly maskedFields: readonly MaskedField[] = [
    { name: 'senderMailAddress', key: 'from_email' },
    { name: 'senderName', key: 'from_name' },
    { name: 'smtpServerAddress', key: 'smtp_server' },
    { name: 'smtpPort', key: 'smtp_port' },
    { name: 'smtpUsername', key: 'smtp_username' },
    { name: 'smtpPassword', key: 'smtp_password' },
  ];
  protected readonly sendTestIcon = ['/assets/images/mail-send-white.svg'] as const;
  protected readonly resetIcon = ['/assets/images/trash-white.svg'] as const;
  /** [content shown, content hidden]: the designer's eyes (Eye_close once open, Eye_start at first). */
  protected readonly eyeIcons = [
    '/assets/images/eye-close.svg',
    '/assets/images/eye-start.svg',
  ] as const;
  /** No currency: only the eye. */
  protected readonly portIcons = ['', ...this.eyeIcons] as const;

  private readonly configuration = signal<EmailConfigurationFields>({
    senderMailAddress: '',
    senderName: '',
    smtpServerAddress: '',
    smtpPort: null,
    smtpUsername: '',
    smtpPassword: '',
    smtpSecurity: null,
  });
  /** "Sì" of "Vuoi sovrascrivere le impostazioni predefinite?": until then every field is read-only. */
  protected readonly editable = signal(false);
  /** The server has a password saved (it never sends it). */
  protected readonly passwordSaved = signal(false);
  /** The connection the saved password belongs to, as the server last answered or saved it. */
  private readonly savedConnection = signal<Partial<EmailConfigurationFields>>({});
  /**
   * The saved password must not be sent to another server, port, user or with another security
   * (e.g. a server of whoever changed it, or a connection without encryption): then it is typed again.
   */
  private readonly passwordToRetype = computed(
    () =>
      this.passwordSaved() &&
      CONNECTION_FIELDS.some(
        (field) => this.configuration()[field] !== this.savedConnection()[field],
      ),
  );
  /** The last reading arrived: nothing can be changed before the values of the server are there. */
  private readonly loaded = signal(false);
  protected readonly configurationForm = form(this.configuration, (path) => {
    // Read-only also while a call runs: a reading would overwrite what is typed meanwhile, and Salva
    // or the test email have already sent the values (rule: nothing changes until the server answers).
    readonly(path, { when: () => !this.editable() || this.isBusy() });
    required(path.smtpPort);
    min(path.smtpPort, 1);
    max(path.smtpPort, MAX_TCP_PORT);
    validate(path.smtpPassword, ({ value }) =>
      !value() && this.passwordToRetype()
        ? { kind: 'email_configuration.smtp_password.retype' }
        : undefined,
    );
  });
  /** Under each field: what is wrong in it, found here or by the backend. */
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.configuration()) as (keyof EmailConfigurationFields)[]).map((name) => [
      name,
      this.validationText.message(this.configurationForm[name]),
    ]),
  ) as Readonly<Record<keyof EmailConfigurationFields, Signal<string | null>>>;
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
        this.isReloading.set(true);
        this.error.set(null);
        return this.system.emailConfiguration({ idProperty }).subscribe({
          next: (data) => {
            this.isReloading.set(false);
            const { hasSmtpPassword, ...fields } = data;
            this.configuration.set({ ...fields, smtpPassword: '' });
            this.passwordSaved.set(hasSmtpPassword);
            this.savedConnection.set(fields);
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
      () => {
        // The new password is now the saved one: the field is empty again.
        this.passwordSaved.update((saved) => saved || this.configuration().smtpPassword !== '');
        this.savedConnection.set(this.configuration());
        this.configuration.update((fields) => ({ ...fields, smtpPassword: '' }));
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
    done: () => void,
  ): void {
    const idProperty = this.auth.user()?.idProperty;
    if (this.isBusy() || !idProperty) {
      return;
    }
    void submit(this.configurationForm, async (): Promise<TreeValidationResult> => {
      isRunning.set(true);
      this.error.set(null);
      try {
        const { smtpPassword, ...fields } = this.configuration();
        await firstValueFrom(call({ idProperty, ...fields, smtpPassword: smtpPassword || null }));
        done();
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
    const errors = apiFieldErrors(problem, this.configurationForm);
    if (!this.editable() || !errors.length || errors.length < (problem.errors?.length ?? 0)) {
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
