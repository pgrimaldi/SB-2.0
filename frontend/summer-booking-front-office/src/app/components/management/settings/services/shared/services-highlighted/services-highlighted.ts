import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { IconButton } from '../../../../../shared/ui/buttons/icon-button/icon-button';
import { InfoPopup } from '../../../../../shared/ui/dialogs/info-popup/info-popup';
import { Toggle } from '../../../../../shared/ui/toggles/toggle/toggle';

/** The "Metti in evidenza" toggle of a service, with its (i). */
@Component({
  selector: 'app-services-highlighted',
  imports: [FormField, IconButton, InfoPopup, Toggle, TranslatePipe],
  templateUrl: './services-highlighted.html',
  styleUrl: './services-highlighted.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesHighlighted {
  /** The field of the form of the panel, which saves it. */
  readonly form = input.required<FieldTree<boolean>>();

  protected readonly infoIcon = ['/assets/images/GeneralIcon_Info.svg'] as const;
  /** [X, before the title]. */
  protected readonly infoPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_InfoTitle.svg',
  ] as const;
  protected readonly showingInfo = signal(false);
}
