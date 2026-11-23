import { PackageArticle } from '../../../../entities/settings/catalog/package-article';
import { PackageItemQuantity } from '../../../../entities/settings/catalog/package-item-quantity';

/** The items in the order of the rows of the articles, then those with no row. */
export function orderedItems(
  items: readonly PackageItemQuantity[],
  articles: readonly PackageArticle[],
): PackageItemQuantity[] {
  const idRows = idItemsOf(articles);
  return [
    ...idRows.flatMap((id) => items.filter(({ idInventoryItem }) => idInventoryItem === id)),
    ...items.filter(({ idInventoryItem }) => !idRows.includes(idInventoryItem)),
  ];
}

/** Without the items that had a row before the articles were read again, and have it no more. */
export function withExistingItems(
  items: readonly PackageItemQuantity[],
  before: readonly PackageArticle[],
  now: readonly PackageArticle[],
): PackageItemQuantity[] {
  const idBefore = idItemsOf(before);
  const idNow = idItemsOf(now);
  return items.filter(
    ({ idInventoryItem }) => !idBefore.includes(idInventoryItem) || idNow.includes(idInventoryItem),
  );
}

function idItemsOf(articles: readonly PackageArticle[]): (string | null)[] {
  return articles.flatMap(({ items }) => items.map(({ idInventoryItem }) => idInventoryItem));
}
