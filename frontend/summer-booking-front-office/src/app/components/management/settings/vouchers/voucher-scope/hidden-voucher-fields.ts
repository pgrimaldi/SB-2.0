import { DiscountScope } from '../../../../../entities/enums/discount-scope';
import { VoucherAddItemRequest } from '../../../../../entities/settings/vouchers/voucher-add-item-request';

/** The fields of a voucher with no place for their error, as it is filled now. */
export function hiddenVoucherFields({
  discountScope,
}: VoucherAddItemRequest): readonly (keyof VoucherAddItemRequest)[] {
  return discountScope === DiscountScope.All
    ? ['isCodeRequired', 'serviceCategories']
    : ['isCodeRequired'];
}
