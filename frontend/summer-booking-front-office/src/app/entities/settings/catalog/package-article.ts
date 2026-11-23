import { InventoryArticle } from '../../enums/inventory-article';
import { PackageArticleItem } from './package-article-item';

export class PackageArticle {
  article: InventoryArticle | null = null;
  items: PackageArticleItem[] = [];
}
