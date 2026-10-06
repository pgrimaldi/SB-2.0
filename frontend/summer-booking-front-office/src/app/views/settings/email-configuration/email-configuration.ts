import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, form } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { Button } from '../../../components/shared/ui/buttons/button/button';
import { FilledTextField } from '../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledSelect } from '../../../components/shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../components/shared/ui/selects/select/select';
import { ApiProblem } from '../../../entities/errors/api-problem';
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
  imports: [Button, FilledSelect, FilledTextField, FormField, TranslatePipe],
  templateUrl: './email-configuration.html',
  styleUrl: './email-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailConfiguration {
  private readonly translateService = inject(TranslateService);
  private readonly system = inject(SystemService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);

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
  protected readonly configurationForm = form(this.configuration);
  private readonly securityTexts = toSignal(
    this.translateService.stream(
      'management.settings.email_configuration.security',
    ) as Observable<SecurityTexts>,
    { initialValue: { none: '', ssl: '', tls: '' } },
  );
  private readonly loadError = signal<ApiProblem | null>(null);
  protected readonly loadErrorMessage = toSignal(
    toObservable(this.loadError).pipe(
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
      const idProperty = this.auth.user()?.idProperty;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() =>
        this.system.emailConfiguration({ idProperty }).subscribe({
          next: ({ smtpPort, ...data }) => {
            this.configuration.set({ ...data, smtpPort: String(smtpPort) });
            this.configurationForm().reset();
            this.loadError.set(null);
          },
          error: (error: unknown) => this.loadError.set(toApiProblem(error)),
        }),
      );
      onCleanup(() => subscription.unsubscribe());
    });
  }
}
