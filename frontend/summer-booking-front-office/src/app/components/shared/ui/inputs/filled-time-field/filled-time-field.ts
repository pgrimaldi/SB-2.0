import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { DateAdapter, ErrorStateMatcher } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInput, MatInputModule } from '@angular/material/input';
import { MatTimepicker, MatTimepickerInput } from '@angular/material/timepicker';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

/**
 * A time of day, chosen from the list or typed. The value is `HH:mm` on 24 hours (`09:00`), null when
 * the field is empty or what is typed is not a time.
 */
@Component({
  selector: 'app-filled-time-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule, MatTimepicker, MatTimepickerInput],
  templateUrl: './filled-time-field.html',
  styleUrl: './filled-time-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledTimeField {
  readonly label = input<string>();
  readonly isMandatory = input(false);
  readonly readonly = input(false);
  readonly disabled = input(false);
  readonly value = model<string | null>(null);
  readonly name = input<string>();
  readonly error = input<string | null>();
  /** Icons as Material icon names (Material Symbols font): [clock, before the time]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [clock, before the time]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly touch = output<void>();

  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `filled-time-field-${nextId++}`;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly time = computed(() => {
    const [hours, minutes] = this.value()?.split(':').map(Number) ?? [];
    if (hours === undefined || minutes === undefined) {
      return null;
    }
    const time = new Date();
    time.setHours(hours, minutes, 0, 0);
    return time;
  });
  protected readonly isLocked = computed(() => this.readonly() || this.disabled());
  private readonly matInput = viewChild.required(MatInput);

  constructor() {
    effect(() => {
      this.error();
      this.matInput().updateErrorState();
    });
  }

  protected choose(time: Date | null): void {
    if (!time || !this.dateAdapter.isValid(time)) {
      this.value.set(null);
      return;
    }
    const hours = String(time.getHours()).padStart(2, '0');
    const minutes = String(time.getMinutes()).padStart(2, '0');
    this.value.set(`${hours}:${minutes}`);
  }
}
