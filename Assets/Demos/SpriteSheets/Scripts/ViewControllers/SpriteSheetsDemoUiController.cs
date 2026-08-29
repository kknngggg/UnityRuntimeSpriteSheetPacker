using System.Collections;
using System.Collections.Generic;
using kknngggg.Unity.Sprites.Demos.SpriteSheets.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets
{
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    public sealed class SpriteSheetsDemoUiController : MonoBehaviour
    {
        private const string CUSTOM_DROPDOWN_POPUP_CLASS_NAME = "paginaton-controls__dropdown-items-popup";
        private const string UNITY_DROPDOWN_POPUP_CONTAINER_CLASS_NAME = "unity-base-dropdown__container-outer";

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private StyleSheet _spriteSheetDemoMainUiStyleSheet;

        private ListView _spritePackEntryListView;
        private VisualElement _packingSettingsPanelView;
        private VisualElement _packedTexturePreviewView;
        private DropdownField _texturePageDropdown;

        private Button _addEntryButton;
        private Button _previousPageButton;
        private Button _nextPageButton;
        private Button _packThisSheetButton;

        private SpritePackEntryListViewController _spritePackEntryListViewController;

        private readonly PackingSettingsPanelViewDataSource _packingSettingsPanelViewDataSource = new();
        private readonly PackedTexturePreviewViewDataSource _packedTexturePreviewViewDataSource = new();

        private IFileBrowser _fileBrowser;

        private Coroutine _packingCoroutine;

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (this._uiDocument == null)
            {
                this._uiDocument = GetComponent<UIDocument>();
            }
        }

#endif

        private void Awake()
        {
            QueryUiElements();
        }

        private void OnEnable()
        {
            this._texturePageDropdown.RegisterCallback<PointerDownEvent>(OnTexturePageDropdownClicked);
            this._addEntryButton.clicked += OnAddEntryButtonClicked;
            this._previousPageButton.clicked += OnPreviousPageButtonClicked;
            this._nextPageButton.clicked += OnNextPageButtonClicked;
            this._packThisSheetButton.clicked += OnPackThisSheetButtonClicked;
        }

        private void Start()
        {
            SetupForcePowerOfTwoToggle();

            this._fileBrowser = new UnityEditorFileBrowser();

            this._packingSettingsPanelView.dataSource = this._packingSettingsPanelViewDataSource;
            this._packedTexturePreviewView.dataSource = this._packedTexturePreviewViewDataSource;

            this._spritePackEntryListViewController = new SpritePackEntryListViewController(
                this._spritePackEntryListView,
                this._fileBrowser,
                this);
        }

        private void OnDisable()
        {
            this._texturePageDropdown.UnregisterCallback<PointerDownEvent>(OnTexturePageDropdownClicked);
            this._addEntryButton.clicked -= OnAddEntryButtonClicked;
            this._previousPageButton.clicked -= OnPreviousPageButtonClicked;
            this._nextPageButton.clicked -= OnNextPageButtonClicked;
            this._packThisSheetButton.clicked -= OnPackThisSheetButtonClicked;
        }

        private void QueryUiElements()
        {
            VisualElement root = this._uiDocument.rootVisualElement;

            this._spritePackEntryListView = root.Q<ListView>("SpritePackEntryList");
            this._packingSettingsPanelView = root.Q<VisualElement>("PackingSettingsPanel");
            this._packedTexturePreviewView = root.Q<VisualElement>("PackedTexturePreviewPanel");
            this._texturePageDropdown = root.Q<DropdownField>("paginaton-controls__page-dropdown");

            this._addEntryButton = root.Q<Button>("AddEntryButton");
            this._previousPageButton = root.Q<Button>("paginaton-controls__previous-button");
            this._nextPageButton = root.Q<Button>("paginaton-controls__next-button");
            this._packThisSheetButton = root.Q<Button>("PackThisSheetButton");
        }

        private void SetupForcePowerOfTwoToggle()
        {
            var checkmark = this._packingSettingsPanelView.Q<VisualElement>("unity-checkmark");
            checkmark.style.backgroundImage = new StyleBackground();
        }

        private void OnTexturePageDropdownClicked(PointerDownEvent evt)
        {
            // The dropdown items popup is generated later in the event loop.
            // Schedule a callback to execute right after it's added to the visual tree.
            this._texturePageDropdown.schedule.Execute(() =>
            {
                var visualTreeRoot = this._texturePageDropdown.panel.visualTree;

                // The popup is attached directly to the visual tree root, bypassing our local hierarchy.
                var popup = visualTreeRoot.Q<VisualElement>(UNITY_DROPDOWN_POPUP_CONTAINER_CLASS_NAME);

                if (popup == null)
                {
                    return;
                }

                // Unity sometimes pools popups. Remove first just in case.
                popup.panel.visualTree.styleSheets.Remove(this._spriteSheetDemoMainUiStyleSheet);
                popup.RemoveFromClassList(CUSTOM_DROPDOWN_POPUP_CLASS_NAME);

                popup.panel.visualTree.styleSheets.Add(this._spriteSheetDemoMainUiStyleSheet);
                popup.AddToClassList(CUSTOM_DROPDOWN_POPUP_CLASS_NAME);
            }).StartingIn(0);
        }

        private void OnAddEntryButtonClicked()
        {
            this._spritePackEntryListViewController.OnAddEntryButtonClicked();
        }

        private void OnPreviousPageButtonClicked()
        {
            this._packedTexturePreviewViewDataSource.SelectedTexturePageIndex--;
        }

        private void OnNextPageButtonClicked()
        {
            this._packedTexturePreviewViewDataSource.SelectedTexturePageIndex++;
        }

        private void OnPackThisSheetButtonClicked()
        {
            this._packingCoroutine ??= StartCoroutine(PackingCoroutine());
        }

        private IEnumerator PackingCoroutine()
        {
            yield return this._spritePackEntryListViewController.LoadAllTexturesAsync();

            IReadOnlyList<SpritePackEntry> entries = this._spritePackEntryListViewController.Entries;

            if (entries is not { Count: > 0 })
            {
                this._packingCoroutine = null;
                yield break;
            }

            SpriteSheet spriteSheet = SpriteSheet.Pack(entries,
                                                       this._packingSettingsPanelViewDataSource.PackingSettings,
                                                       this._packingSettingsPanelViewDataSource.PageName);

            this._packedTexturePreviewViewDataSource.UpdatePreview(spriteSheet);

            this._spritePackEntryListViewController.ReleaseLoadedTextures();
            this._packingCoroutine = null;
        }
    }
}
