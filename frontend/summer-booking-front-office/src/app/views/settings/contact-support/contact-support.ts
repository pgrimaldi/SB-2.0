import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, form, required } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { ValidationTextBehaviour } from '../../../behaviours/validation/validation-text.behaviour';
import { Button } from '../../../components/shared/ui/buttons/button/button';
import { MessagePopup } from '../../../components/shared/ui/dialogs/message-popup/message-popup';
import { FilledTextField } from '../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTextarea } from '../../../components/shared/ui/inputs/filled-textarea/filled-textarea';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { SystemService } from '../../../services/api/system/system.service';

interface ContactFields {
  firstName: string;
  lastName: string;
  email: string;
  mobilePhone: string;
  message: string;
}

const NO_CONTACT: ContactFields = {
  firstName: '',
  lastName: '',
  email: '',
  mobilePhone: '',
  message: '',
};

@Component({
  selector: 'app-contact-support',
  imports: [Button, FilledTextField, FilledTextarea, FormField, MessagePopup, TranslatePipe],
  templateUrl: './contact-support.html',
  styleUrl: './contact-support.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactSupport {
  private readonly system = inject(SystemService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly supportPhone = '050 7916620';
  protected readonly supportPhoneLink = 'tel:+390507916620';
  protected readonly supportEmail = 'info@summerbooking.it';
  protected readonly phoneIcon = '/assets/images/phone.svg';
  protected readonly emailIcon = '/assets/images/email.svg';
  protected readonly clockIcon = '/assets/images/clock.svg';

  private readonly contact = signal<ContactFields>({ ...NO_CONTACT });
  protected readonly contactForm = form(this.contact, (path) => {
    required(path.firstName);
    required(path.lastName);
    required(path.email);
    required(path.message);
  });
  protected readonly firstNameError = this.validationText.message(this.contactForm.firstName);
  protected readonly lastNameError = this.validationText.message(this.contactForm.lastName);
  protected readonly emailError = this.validationText.message(this.contactForm.email);
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

  protected send(event: Event): void {
    event.preventDefault();
    const idProperty = this.auth.user()?.idProperty;
    if (this.isLoading() || !idProperty || this.contactForm().invalid()) {
      return;
    }
    const { firstName, lastName, email, mobilePhone, message } = this.contact();
    this.isLoading.set(true);
    this.error.set(null);
    this.system
      .contactSupport({
        idProperty,
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        mobilePhone: mobilePhone.trim() || null,
        message: message.trim(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.contact.set({ ...NO_CONTACT });
          this.contactForm().reset();
          this.sent.set(true);
        },
        error: (error: unknown) => {
          this.isLoading.set(false);
          this.error.set(toApiProblem(error));
        },
      });
  }
}
