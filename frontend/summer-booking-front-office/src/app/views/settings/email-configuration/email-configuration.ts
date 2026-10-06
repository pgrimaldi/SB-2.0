import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, form, readonly } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { Button } from '../../../components/shared/ui/buttons/button/button';
import { FilledTextField } from '../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { FilledSelect } from '../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../components/shared/ui/selects/select/select';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { EmailConfigurationRequest } from '../../../entities/system/email-configuration-request';
import { SmtpSecurity } from '../../../entities/system/email-configuration-data';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { SystemService } from '../../../services/api/system/system.service';

/** The answer of the API, with the port as text for the field. */
interface EmailConfigurationFields {
  senderMailAddress: string;
  senderName: string;
  smtpServerAddress: string;
  smtpPort: string;
  smtpUsername: string;
  smtpPassword: string;
  smtpSecurity: SmtpSecurity | null;
}

type MaskedField = Exclude<keyof EmailConfigurationFields, 'smtpSecurity'>;

interface SecurityTexts {
  none: string;
  ssl: string;
  tls: string;
}

@Component({
  selector: 'app-email-configuration',
  imports: [Button, FilledSelect, FilledTextField, FormField, MessagePopup, TranslatePipe],
  templateUrl: './email-configuration.html',
  styleUrl: './email-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailConfiguration {
  private readonly translateService = inject(TranslateService);
  private readonly system = inject(SystemService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly destroyRef = inject(DestroyRef);

  /** In page order; `key` is the translation group under `fields`. */
  protected readonly maskedFields: readonly { name: MaskedField; key: string }[] = [
    { name: 'senderMailAddress', key: 'from_email' },
    { name: 'senderName', key: 'from_name' },
    { name: 'smtpServerAddress', key: 'smtp_server' },
    { name: 'smtpPort', key: 'smtp_port' },
    { name: 'smtpUsername', key: 'smtp_username' },
    { name: 'smtpPassword', key: 'smtp_password' },
  ];
  protected readonly sendTestIcon = ['/assets/images/mail-send-white.svg'] as const;
  protected readonly resetIcon = ['/assets/images/trash-white.svg'] as const;

  private readonly configuration = signal<EmailConfigurationFields>({
    senderMailAddress: '',
    senderName: '',
    smtpServerAddress: '',
    smtpPort: '',
    smtpUsername: '',
    smtpPassword: '',
    smtpSecurity: null,
  });
  /** "Sì" of "Vuoi sovrascrivere le impostazioni predefinite?": until then every field is read-only. */
  protected readonly editable = signal(false);
  protected readonly configurationForm = form(this.configuration, (path) => {
    readonly(path, { when: () => !this.editable() });
  });
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
    effect((onCleanup) => {
      this.reloads();
      const idProperty = this.auth.user()?.idProperty;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() => {
        this.isReloading.set(true);
        this.error.set(null);
        return this.system.emailConfiguration({ idProperty }).subscribe({
          next: ({ smtpPort, ...data }) => {
            this.isReloading.set(false);
            this.configuration.set({ ...data, smtpPort: String(smtpPort) });
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

  /** Reset: the values of the server again, what was typed is lost. */
  protected reset(): void {
    this.reloads.update((reloads) => reloads + 1);
  }

  protected sendTestEmail(): void {
    const request = this.request();
    if (this.isBusy() || !request) {
      return;
    }
    this.isSendingTest.set(true);
    this.error.set(null);
    this.system
      .sendTestEmail(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSendingTest.set(false);
          this.testSent.set(true);
        },
        error: (error: unknown) => {
          this.isSendingTest.set(false);
          this.error.set(toApiProblem(error));
        },
      });
  }

  protected save(): void {
    const request = this.request();
    if (this.isBusy() || !request) {
      return;
    }
    this.isSaving.set(true);
    this.error.set(null);
    this.system
      .saveEmailConfiguration(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.configurationForm().reset();
          this.saved.set(true);
        },
        error: (error: unknown) => {
          this.isSaving.set(false);
          this.error.set(toApiProblem(error));
        },
      });
  }

  /** The values in the fields; the port as a number, `null` when it is not a whole number. */
  private request(): EmailConfigurationRequest | null {
    const idProperty = this.auth.user()?.idProperty;
    if (!idProperty) {
      return null;
    }
    const { smtpPort, ...fields } = this.configuration();
    const port = Number(smtpPort.trim());
    return {
      idProperty,
      ...fields,
      smtpPort: smtpPort.trim() && Number.isInteger(port) ? port : null,
    };
  }
}
