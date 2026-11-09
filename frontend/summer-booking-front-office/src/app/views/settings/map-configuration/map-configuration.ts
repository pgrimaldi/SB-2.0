import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { MapBackgroundPanel } from '../../../components/management/settings/map-configuration/map-background-panel/map-background-panel';
import { MapGridPanel } from '../../../components/management/settings/map-configuration/map-grid-panel/map-grid-panel';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../components/shared/ui/buttons/button/button';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { WarningPopup } from '../../../components/shared/ui/dialogs/warning-popup/warning-popup';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { MapSettings } from '../../../entities/settings/map-configuration/map-settings';
import { MapService } from '../../../services/api/catalog/map.service';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';

@Component({
  selector: 'app-map-configuration',
  imports: [Button, MapBackgroundPanel, MapGridPanel, TranslatePipe, WarningPopup],
  templateUrl: './map-configuration.html',
  styleUrl: './map-configuration.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapConfiguration {
  private readonly auth = inject(AuthBehaviour);
  private readonly mapService = inject(MapService);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly resetIcon = ['/assets/images/MapConfiguration_Reset.svg'] as const;
  /** [X, warning]. */
  protected readonly resetPopupIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/MapConfiguration_Warning.svg',
  ] as const;
  protected readonly isResetAsked = signal(false);
  /** null until the answer: the panels cannot be edited. */
  protected readonly settings = signal<MapSettings | null>(null);
  protected readonly loadError = signal<ApiProblem | null>(null);
  protected readonly isResetting = signal(false);
  private readonly resetError = signal<ApiProblem | null>(null);
  protected readonly resetErrorMessage = toSignal(
    toObservable(this.resetError).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.auth.user()) {
        return;
      }
      const subscription = untracked(() => {
        this.settings.set(null);
        this.loadError.set(null);
        return this.mapService.settings().subscribe({
          next: (settings) => this.settings.set(settings),
          error: (error: unknown) => this.loadError.set(toApiProblem(error)),
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected askReset(): void {
    this.resetError.set(null);
    this.isResetAsked.set(true);
  }

  /** The answer carries the settings after the reset: no further GET. */
  protected reset(): void {
    if (this.isResetting()) {
      return;
    }
    this.isResetting.set(true);
    this.resetError.set(null);
    this.mapService
      .reset()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ settings }) => {
          this.settings.set(settings);
          this.isResetting.set(false);
          this.isResetAsked.set(false);
        },
        error: (error: unknown) => {
          this.isResetting.set(false);
          this.resetError.set(toApiProblem(error));
        },
      });
  }
}
