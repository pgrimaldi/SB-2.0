import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';

// Material has no file picker: the system dialog is the browser's `<input type="file">`.
@Component({
  selector: 'app-file-drop-zone',
  imports: [MatIconModule],
  templateUrl: './file-drop-zone.html',
  styleUrl: './file-drop-zone.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(dragover)': 'dragOver($event)',
    '(dragleave)': 'isDragging.set(false)',
    '(drop)': 'drop($event)',
  },
})
export class FileDropZone {
  readonly text = input<string>();
  readonly previewUrl = input<string | null>();
  /** Types offered by the file window, as in `<input accept>`: other files still reach the page. */
  readonly accept = input<string>();
  readonly isMultiple = input(false);
  readonly disabled = input(false);
  /** Icons as Material icon names (Material Symbols font): [icon above the text]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [icon above the text]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly filesSelected = output<File[]>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly isDragging = signal(false);

  protected choose(field: HTMLInputElement): void {
    const files = Array.from(field.files ?? []);
    // The same file chosen again must still count as a choice.
    field.value = '';
    this.give(files);
  }

  protected dragOver(event: DragEvent): void {
    // Without it the browser opens the dropped file in the tab, leaving the app: disabled too.
    event.preventDefault();
    if (this.disabled()) {
      if (event.dataTransfer) {
        event.dataTransfer.dropEffect = 'none';
      }
      return;
    }
    this.isDragging.set(true);
  }

  protected drop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    if (!this.disabled()) {
      this.give(Array.from(event.dataTransfer?.files ?? []));
    }
  }

  /** Every file as chosen: the page checks type and weight and tells the user what is wrong. */
  private give(files: File[]): void {
    const given = this.isMultiple() ? files : files.slice(0, 1);
    if (given.length) {
      this.filesSelected.emit(given);
    }
  }
}
