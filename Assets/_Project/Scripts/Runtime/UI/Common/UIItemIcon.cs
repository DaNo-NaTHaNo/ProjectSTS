using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 아이템 아이콘 + 수량 배지 위젯.
    /// 인벤토리, 장비 슬롯, 보상 화면 등에서 공용으로 사용된다.
    /// 아트 에셋 미완성 시 레어도별 테두리 색상 사각형 + 이름 약어로 Placeholder를 표시한다.
    /// </summary>
    public class UIItemIcon : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Icon")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _rarityBorder;
        [SerializeField] private TextMeshProUGUI _nameAbbreviation;

        [Header("Quantity")]
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private GameObject _quantityRoot;

        #endregion

        #region Private Fields

        private ItemData _itemData;

        private static readonly Dictionary<Rarity, Color32> RARITY_BORDER_COLORS =
            new Dictionary<Rarity, Color32>(5)
            {
                { Rarity.Uncommon, new Color32(0x9E, 0x9E, 0x9E, 0xFF) },
                { Rarity.Common,   new Color32(0xBD, 0xBD, 0xBD, 0xFF) },
                { Rarity.Rare,     new Color32(0x42, 0xA5, 0xF5, 0xFF) },
                { Rarity.Unique,   new Color32(0xFF, 0xD5, 0x4F, 0xFF) },
                { Rarity.Epic,     new Color32(0xAB, 0x47, 0xBC, 0xFF) },
            };

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 바인딩된 아이템 마스터 데이터.
        /// </summary>
        public ItemData CurrentItemData => _itemData;

        #endregion

        #region Public Methods

        /// <summary>
        /// 아이템 데이터를 바인딩한다. 수량 없이 단일 표시.
        /// </summary>
        /// <param name="item">아이템 마스터 데이터</param>
        public void SetData(ItemData item)
        {
            SetData(item, 0);
        }

        /// <summary>
        /// 아이템 데이터와 수량을 바인딩한다.
        /// </summary>
        /// <param name="item">아이템 마스터 데이터</param>
        /// <param name="quantity">표시할 수량 (1 이하이면 수량 배지 비표시)</param>
        public void SetData(ItemData item, int quantity)
        {
            _itemData = item;

            if (item == null)
            {
                Clear();
                return;
            }

            ApplyPlaceholderIcon(item);
            ApplyRarityBorder(item.rarity);
            ApplyQuantity(quantity);
        }

        /// <summary>
        /// 인벤토리 아이템 데이터를 바인딩한다. ownStack을 수량으로 표시한다.
        /// </summary>
        /// <param name="inventoryItem">인벤토리 아이템 데이터</param>
        public void SetData(InventoryItemData inventoryItem)
        {
            if (inventoryItem == null)
            {
                _itemData = null;
                Clear();
                return;
            }

            _itemData = null;

            ApplyPlaceholderIconFromInventory(inventoryItem);
            ApplyRarityBorder(inventoryItem.rarity);
            ApplyQuantity(inventoryItem.ownStack);
        }

        /// <summary>
        /// 빈 슬롯 상태로 리셋한다.
        /// </summary>
        public void Clear()
        {
            _itemData = null;

            if (_iconImage != null)
            {
                _iconImage.color = Color.clear;
            }

            if (_rarityBorder != null)
            {
                _rarityBorder.color = Color.clear;
            }

            if (_nameAbbreviation != null)
            {
                _nameAbbreviation.text = string.Empty;
            }

            if (_quantityRoot != null)
            {
                _quantityRoot.SetActive(false);
            }

            if (_quantityText != null)
            {
                _quantityText.text = string.Empty;
            }
        }

        #endregion

        #region Private Methods

        private void ApplyPlaceholderIcon(ItemData item)
        {
            if (_iconImage != null)
            {
                if (RARITY_BORDER_COLORS.TryGetValue(item.rarity, out Color32 color))
                {
                    _iconImage.color = color;
                }
                else
                {
                    _iconImage.color = Color.gray;
                }
            }

            if (_nameAbbreviation != null)
            {
                _nameAbbreviation.text = GetAbbreviation(item.itemName);
            }
        }

        private void ApplyPlaceholderIconFromInventory(InventoryItemData inventoryItem)
        {
            if (_iconImage != null)
            {
                if (RARITY_BORDER_COLORS.TryGetValue(inventoryItem.rarity, out Color32 color))
                {
                    _iconImage.color = color;
                }
                else
                {
                    _iconImage.color = Color.gray;
                }
            }

            if (_nameAbbreviation != null)
            {
                _nameAbbreviation.text = GetAbbreviation(inventoryItem.productName);
            }
        }

        private void ApplyRarityBorder(Rarity rarity)
        {
            if (_rarityBorder == null)
            {
                return;
            }

            if (RARITY_BORDER_COLORS.TryGetValue(rarity, out Color32 color))
            {
                _rarityBorder.color = color;
            }
            else
            {
                _rarityBorder.color = Color.gray;
            }
        }

        private void ApplyQuantity(int quantity)
        {
            bool showQuantity = quantity > 1;

            if (_quantityRoot != null)
            {
                _quantityRoot.SetActive(showQuantity);
            }

            if (_quantityText != null)
            {
                _quantityText.text = showQuantity ? $"×{quantity}" : string.Empty;
            }
        }

        private static string GetAbbreviation(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            return name.Length <= 2 ? name : name.Substring(0, 2);
        }

        #endregion
    }
}
