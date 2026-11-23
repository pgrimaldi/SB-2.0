import { ComboboxItem } from '../../../../entities/combobox/combobox-item';
import { SelectOption } from '../../../shared/ui/selects/select/select';

/** The options of a dropdown, then the chosen ones that are no longer among them. */
export function optionsWithMissing(
  items: readonly ComboboxItem[],
  chosen: readonly string[],
  missingText: string,
): SelectOption[] {
  return [
    ...items.map(({ id, value }) => ({ value: id, label: value })),
    ...chosen
      .filter((idChosen) => !items.some(({ id }) => id === idChosen))
      .map((idChosen) => ({ value: idChosen, label: missingText })),
  ];
}
