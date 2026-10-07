import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../behaviours/validation/validation-text.behaviour';
import { DATA_RELOAD } from '../../../components/shared/data/data-reload';
import { requiredText } from '../../../components/shared/forms/validators';
import { Button } from '../../../components/shared/ui/buttons/button/button';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { FilledTextField } from '../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTextarea } from '../../../components/shared/ui/inputs/filled-textarea/filled-textarea';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { ContactSupportRequest } from '../../../entities/settings/contact-support/contact-support-request';
import { SupportInfo } from '../../../entities/settings/contact-support/support-info';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { SystemService } from '../../../services/api/system/system.service';

@Component({
  selector: 'app-contact-support',
  imports: [Button, FilledTextField, FilledTextarea, FormField, MessagePopup, TranslatePipe],
  templateUrl: './contact-support.html',
  styleUrl: './contact-support.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactSupport implements UnsavedChanges, OnDestroy {
  private readonly system = inject(SystemService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });

  protected readonly supportInfo = signal<SupportInfo | null>(null);
  /** Only digits (and a leading +) in a `tel:` link. */
  protected readonly supportPhoneLink = computed(() => {
    const phoneNumber = this.supportInfo()?.phoneNumber;
    return phoneNumber ? `tel:${phoneNumber.replace(/[^\d+]/g, '')}` : null;
  });
  private readonly supportInfoError = signal<ApiProblem | null>(null);
  protected readonly supportInfoErrorMessage = toSignal(
    toObservable(this.supportInfoError).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly phoneIcon = '/assets/images/phone.svg';
  protected readonly emailIcon = '/assets/images/email.svg';
  protected readonly clockIcon = '/assets/images/clock.svg';

  private readonly contact = signal(new ContactSupportRequest());
  protected readonly contactForm = form(this.contact, (path) => {
    // What is sent cannot change until the server answers.
    readonly(path, { when: () => this.isLoading() });
    requiredText(path.firstName);
    requiredText(path.lastName);
    requiredText(path.email);
    requiredText(path.message);
  });
  protected readonly firstNameError = this.validationText.message(this.contactForm.firstName);
  protected readonly lastNameError = this.validationText.message(this.contactForm.lastName);
  protected readonly emailError = this.validationText.message(this.contactForm.email);
  protected readonly mobilePhoneError = this.validationText.message(this.contactForm.mobilePhone);
  protected readonly messageError = this.validationText.message(this.contactForm.message);
  protected readonly isLoading = signal(false);
  protected readonly sent = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    this.unsaved.watch(this);
    // Loaded again when the language changes: the support hours come translated by the server.
    effect((onCleanup) => {
      this.dataReload?.();
      const idProperty = this.auth.user()?.idProperty;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() =>
        this.system.supportInfo({ idProperty }).subscribe({
          next: (supportInfo) => {
            this.supportInfo.set(supportInfo);
            this.supportInfoError.set(null);
          },
          error: (error: unknown) => {
            this.supportInfo.set(null);
            this.supportInfoError.set(toApiProblem(error));
          },
        }),
      );
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected send(event: Event): void {
    event.preventDefault();
    const idProperty = this.auth.user()?.idProperty;
    if (this.isLoading() || !idProperty) {
      return;
    }
    // Every field becomes touched and nothing is sent while one is wrong; an error of the backend on
    // a field goes under that field.
    void submit(this.contactForm, async (): Promise<TreeValidationResult> => {
      const contact = this.contact();
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.system.contactSupport({
            ...contact,
            idProperty,
            firstName: trimmed(contact.firstName),
            lastName: trimmed(contact.lastName),
            email: trimmed(contact.email),
            mobilePhone: trimmed(contact.mobilePhone),
            message: trimmed(contact.message),
          }),
        );
        this.contact.set(new ContactSupportRequest());
        this.contactForm().reset();
        this.sent.set(true);
        return undefined;
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.contactForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isLoading.set(false);
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.contactForm().dirty();
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

function trimmed(text: string | null): string | null {
  return text?.trim() || null;
}
