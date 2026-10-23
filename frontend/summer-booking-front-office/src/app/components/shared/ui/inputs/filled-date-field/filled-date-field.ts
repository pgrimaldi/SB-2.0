import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatDatepicker, MatDatepickerInput } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInput, MatInputModule } from '@angular/material/input';
import { resolveIcons } from '../../icons/icons';

let nextId = 0;

/**
 * One calendar day, chosen from the calendar (it is not typed). The value is that day at midnight
 * UTC as an ISO string (`2026-08-01T00:00:00.000Z`), so it does not move with the time zone.
 */
@Component({
  selector: 'app-filled-date-field',
  imports: [MatDatepicker, MatDatepickerInput, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './filled-date-field.html',
  styleUrl: './filled-date-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilledDateField {
  readonly label = input<string>();
  readonly isMandatory = input(false);
  readonly readonly = input(false);
  readonly disabled = input(false);
  readonly value = model<string | null>(null);
  readonly name = input<string>();
  readonly error = input<string | null>();
  /** Icons as Material icon names (Material Symbols font): [calendar, before the date]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [calendar, before the date]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly touch = output<void>();

  protected readonly errorMatcher: ErrorStateMatcher = { isErrorState: () => !!this.error() };
  protected readonly id = `filled-date-field-${nextId++}`;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly day = computed(() => {
    const value = this.value();
    if (!value) {
      return null;
    }
    const utc = new Date(value);
    return new Date(utc.getUTCFullYear(), utc.getUTCMonth(), utc.getUTCDate());
  });
  protected readonly isLocked = computed(() => this.readonly() || this.disabled());
  private readonly matInput = viewChild.required(MatInput);

  constructor() {
    effect(() => {
      this.error();
      this.matInput().updateErrorState();
    });
  }

  protected openCalendar(calendar: MatDatepicker<Date>): void {
    if (!this.isLocked()) {
      calendar.open();
    }
  }

  protected choose(day: Date | null): void {
    this.value.set(
      day
        ? new Date(Date.UTC(day.getFullYear(), day.getMonth(), day.getDate())).toISOString()
        : null,
    );
  }
}
