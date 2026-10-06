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

type EmailConfigurationFields = Omit<EmailConfigurationRequest, 'idProperty'>;

type TextFieldName = Exclude<keyof EmailConfigurationFields, 'smtpPort' | 'smtpSecurity'>;

/** A masked field of the page; `key` is its translation group under `fields`. */
type MaskedField = { name: TextFieldName; key: string } | { name: 'smtpPort'; key: string };

const MAX_TCP_PORT = 65535;

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
  protected readonly configurationForm = form(this.configuration, (path) => {
    readonly(path, { when: () => !this.editable() });
    required(path.smtpPort);
    min(path.smtpPort, 1);
    max(path.smtpPort, MAX_TCP_PORT);
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
            this.configuration.set(data);
            this.configurationForm().reset();
          },
          error: (error: unknown) => {
            this.isReloading.set(false);
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
        await firstValueFrom(call({ idProperty, ...this.configuration() }));
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
