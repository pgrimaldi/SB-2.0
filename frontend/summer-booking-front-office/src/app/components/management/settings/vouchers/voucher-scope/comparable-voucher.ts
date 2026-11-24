import { VoucherAddItemRequest } from '../../../../../entities/settings/vouchers/voucher-add-item-request';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { withScope } from './with-scope';

/** The order of the categories and of the sectors does not count as a change. */
export function comparableVoucher(voucher: VoucherAddItemRequest): string {
  const saved = withScope(voucher);
  return comparableJson({
    ...saved,
    serviceCategories: [...saved.serviceCategories].sort(),
    idSectors: [...saved.idSectors].sort(),
  });
}
