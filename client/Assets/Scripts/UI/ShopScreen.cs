using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerClimb.UI
{
    /// <summary>
    /// Shop screen for cosmetic items (themes, SFX packs)
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        [Header("UI References")]
        public Button closeButton;
        public Transform itemContainer;
        public GameObject itemPrefab;

        [Header("Tabs")]
        public Button themesTabButton;
        public Button sfxTabButton;
        public Button allTabButton;

        [Header("Preview")]
        public Image previewImage;
        public TextMeshProUGUI previewNameText;
        public TextMeshProUGUI previewDescriptionText;
        public Button equipButton;

        private List<ShopItem> allItems = new List<ShopItem>();
        private List<GameObject> itemObjects = new List<GameObject>();
        private ShopItem selectedItem;
        private string currentTab = "all";

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (themesTabButton != null) themesTabButton.onClick.AddListener(() => ShowTab("themes"));
            if (sfxTabButton != null) sfxTabButton.onClick.AddListener(() => ShowTab("sfx"));
            if (allTabButton != null) allTabButton.onClick.AddListener(() => ShowTab("all"));
            if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);

            LoadItems();
            Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshShop();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void LoadItems()
        {
            // Load from remote config or local definitions
            // For now, hardcode example items
            allItems = new List<ShopItem>
            {
                new ShopItem { id = "theme_retro", name = "Retro Theme", description = "Classic 80s aesthetics", category = "theme", unlockFloor = 10, equipped = false, unlocked = false },
                new ShopItem { id = "theme_neon", name = "Neon Theme", description = "Vibrant cyberpunk colors", category = "theme", unlockFloor = 20, equipped = false, unlocked = false },
                new ShopItem { id = "theme_minimal", name = "Minimal Theme", description = "Clean and simple", category = "theme", unlockFloor = 50, equipped = false, unlocked = false },
                new ShopItem { id = "theme_galaxy", name = "Galaxy Theme", description = "Space-inspired colors", category = "theme", unlockFloor = 100, equipped = false, unlocked = false },

                new ShopItem { id = "sfx_pack_cyber", name = "Cyber SFX", description = "Futuristic sound effects", category = "sfx", unlockFloor = 30, equipped = false, unlocked = false },
                new ShopItem { id = "sfx_pack_orchestral", name = "Orchestral SFX", description = "Classical instruments", category = "sfx", unlockFloor = 75, equipped = false, unlocked = false },
                new ShopItem { id = "sfx_pack_nature", name = "Nature SFX", description = "Organic sounds", category = "sfx", unlockFloor = 0, equipped = false, unlocked = true }, // Free
            };

            LoadUnlockState();
        }

        private void LoadUnlockState()
        {
            foreach (var item in allItems)
            {
                // Load from PlayerPrefs
                item.unlocked = PlayerPrefs.GetInt($"Unlock_{item.id}", 0) == 1 || item.unlockFloor == 0;
                item.equipped = PlayerPrefs.GetInt($"Equipped_{item.id}", 0) == 1;
            }
        }

        private void ShowTab(string tab)
        {
            currentTab = tab;
            RefreshShop();
        }

        private void RefreshShop()
        {
            ClearItems();

            foreach (var item in allItems)
            {
                // Filter by tab
                if (currentTab != "all")
                {
                    if (item.category != currentTab) continue;
                }

                CreateItemEntry(item);
            }
        }

        private void CreateItemEntry(ShopItem item)
        {
            if (itemPrefab == null || itemContainer == null) return;

            GameObject itemObj = Instantiate(itemPrefab, itemContainer);
            itemObjects.Add(itemObj);

            // Get components
            var nameText = itemObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var statusText = itemObj.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            var thumbnail = itemObj.transform.Find("Thumbnail")?.GetComponent<Image>();
            var selectButton = itemObj.transform.Find("SelectButton")?.GetComponent<Button>();
            var lockedIcon = itemObj.transform.Find("LockedIcon")?.gameObject;

            if (nameText != null)
            {
                nameText.text = item.name;
            }

            if (statusText != null)
            {
                if (!item.unlocked)
                {
                    statusText.text = $"Unlock at Floor {item.unlockFloor}";
                    statusText.color = Color.gray;
                }
                else if (item.equipped)
                {
                    statusText.text = "EQUIPPED";
                    statusText.color = Color.green;
                }
                else
                {
                    statusText.text = "Owned";
                    statusText.color = Color.white;
                }
            }

            if (lockedIcon != null)
            {
                lockedIcon.SetActive(!item.unlocked);
            }

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => SelectItem(item));
                selectButton.interactable = item.unlocked;
            }

            itemObj.SetActive(true);
        }

        private void SelectItem(ShopItem item)
        {
            selectedItem = item;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (selectedItem == null) return;

            if (previewNameText != null)
            {
                previewNameText.text = selectedItem.name;
            }

            if (previewDescriptionText != null)
            {
                previewDescriptionText.text = selectedItem.description;
            }

            if (equipButton != null)
            {
                equipButton.interactable = selectedItem.unlocked && !selectedItem.equipped;
                equipButton.GetComponentInChildren<TextMeshProUGUI>().text = selectedItem.equipped ? "EQUIPPED" : "EQUIP";
            }

            // TODO: Show preview image
        }

        private void OnEquipClicked()
        {
            if (selectedItem == null || !selectedItem.unlocked) return;

            // Unequip all items in same category
            foreach (var item in allItems)
            {
                if (item.category == selectedItem.category && item.equipped)
                {
                    item.equipped = false;
                    PlayerPrefs.SetInt($"Equipped_{item.id}", 0);
                }
            }

            // Equip selected item
            selectedItem.equipped = true;
            PlayerPrefs.SetInt($"Equipped_{selectedItem.id}", 1);
            PlayerPrefs.Save();

            Debug.Log($"[ShopScreen] Equipped: {selectedItem.name}");

            // Apply the cosmetic change (theme, SFX pack)
            ApplyCosmetic(selectedItem);

            RefreshShop();
            UpdatePreview();
        }

        private void ApplyCosmetic(ShopItem item)
        {
            // TODO: Actually apply the theme or SFX pack
            Debug.Log($"[ShopScreen] Applying cosmetic: {item.id}");

            switch (item.category)
            {
                case "theme":
                    // Change UI colors, backgrounds, etc.
                    break;

                case "sfx":
                    // Swap audio clips in AudioManager
                    break;
            }
        }

        private void ClearItems()
        {
            foreach (var obj in itemObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            itemObjects.Clear();
        }

        private void OnCloseClicked()
        {
            Hide();

            // Return to home screen
            if (HomeScreen.Instance != null)
            {
                HomeScreen.Instance.Show();
            }
        }

        /// <summary>
        /// Called when player reaches a new milestone floor to unlock items
        /// </summary>
        public void CheckUnlocks(int floor)
        {
            List<ShopItem> newUnlocks = new List<ShopItem>();

            foreach (var item in allItems)
            {
                if (!item.unlocked && floor >= item.unlockFloor && item.unlockFloor > 0)
                {
                    item.unlocked = true;
                    PlayerPrefs.SetInt($"Unlock_{item.id}", 1);
                    newUnlocks.Add(item);
                }
            }

            if (newUnlocks.Count > 0)
            {
                PlayerPrefs.Save();

                // Show unlock notification
                foreach (var item in newUnlocks)
                {
                    ShowUnlockNotification(item);
                }
            }
        }

        private void ShowUnlockNotification(ShopItem item)
        {
            Debug.Log($"[ShopScreen] Unlocked: {item.name}");
            // TODO: Show popup notification
        }
    }

    [System.Serializable]
    public class ShopItem
    {
        public string id;
        public string name;
        public string description;
        public string category; // "theme", "sfx", etc.
        public int unlockFloor;
        public bool unlocked;
        public bool equipped;
    }
}
