import { DiscountScope } from '../../../../../entities/enums/discount-scope';
import { VoucherAddItemRequest } from '../../../../../entities/settings/vouchers/voucher-add-item-request';

export function withScope<
  T extends Pick<VoucherAddItemRequest, 'discountScope' | 'serviceCategories' | 'idSectors'>,
>(voucher: T): T {
  return voucher.discountScope === DiscountScope.All
    ? { ...voucher, serviceCategories: [], idSectors: [] }
    : voucher;
}
