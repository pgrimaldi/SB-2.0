import { DropdownList } from '../../../../behaviours/forms/dropdown-list';
import { ComboboxItem } from '../../../../entities/combobox/combobox-item';
import { SelectOption } from '../../../shared/ui/selects/select/select';

/** The options of a dropdown, then the chosen ones that are no longer among them. */
export function optionsWithMissing(
  list: DropdownList<ComboboxItem>,
  chosen: readonly string[],
  missingText: string,
): SelectOption[] {
  const items = list.items();
  const missing = list.isRead()
    ? chosen.filter((idChosen) => !items.some(({ id }) => id === idChosen))
    : [];
  return [
    ...items.map(({ id, value }) => ({ value: id, label: value })),
    ...missing.map((idChosen) => ({ value: idChosen, label: missingText })),
  ];
}
