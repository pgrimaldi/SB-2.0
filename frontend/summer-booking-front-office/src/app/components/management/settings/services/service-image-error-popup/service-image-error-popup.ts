import { ChangeDetectionStrategy, Component, inject, input, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { TableErrorPopup } from '../../../../shared/ui/dialogs/table-error-popup/table-error-popup';

/** The service was saved, but its image, which goes after it, was not: why. */
@Component({
  selector: 'app-service-image-error-popup',
  imports: [TableErrorPopup, TranslatePipe],
  templateUrl: './service-image-error-popup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceImageErrorPopup {
  readonly problem = model<ApiProblem | null>(null);
  /** [X]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly reason = this.errorText.message(this.problem);

  protected closed(isOpen: boolean): void {
    if (!isOpen) {
      this.problem.set(null);
    }
  }
}
