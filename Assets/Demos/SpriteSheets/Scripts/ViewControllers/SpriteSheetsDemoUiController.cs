using System.Collections;
using System.Collections.Generic;
using kknngggg.Unity.Sprites.Demos.SpriteSheets.IO;
using kknngggg.Unity.Sprites.Errors;
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
        private Label _errorMessageBody;

        private Button _addEntryButton;
        private Button _previousPageButton;
        private Button _nextPageButton;
        private Button _packThisSheetButton;
        private Button _loadSpriteSheetButton;
        private Button _saveTexturePageButton;
        private Button _saveSpriteSheetButton;
        private VisualElement _errorMessagePanel;
        private Button _errorMessageCloseButton;

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
            this._loadSpriteSheetButton.clicked += OnLoadSpriteSheetButtonClicked;
            this._saveTexturePageButton.clicked += OnSaveTexturePageButtonClicked;
            this._saveSpriteSheetButton.clicked += OnSaveSpriteSheetButtonClicked;
            this._errorMessageCloseButton.clicked += OnErrorMessageCloseButtonClicked;
        }

        private void Start()
        {
            SetupForcePowerOfTwoToggle();

            this._fileBrowser = CreateFileBrowser();

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
            this._loadSpriteSheetButton.clicked += OnLoadSpriteSheetButtonClicked;
            this._saveTexturePageButton.clicked -= OnSaveTexturePageButtonClicked;
            this._saveSpriteSheetButton.clicked -= OnSaveSpriteSheetButtonClicked;
            this._errorMessageCloseButton.clicked -= OnErrorMessageCloseButtonClicked;
        }

        private static IFileBrowser CreateFileBrowser()
        {
#if UNITY_EDITOR
            return new UnityEditorFileBrowser();
#elif UNITY_STANDALONE_WIN
            return new StandaloneWindowsFileBrowser();
#elif UNITY_STANDALONE_OSX
            return new StandaloneMacFileBrowser();
#else
            return IFileBrowser.Null;
#endif
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
            this._loadSpriteSheetButton = root.Q<Button>("LoadButton");
            this._saveTexturePageButton = root.Q<Button>("SaveTexturePageButton");
            this._saveSpriteSheetButton = root.Q<Button>("SaveSpriteSheetButton");

            this._errorMessagePanel = root.Q<VisualElement>("ErrorMessagePanel");
            this._errorMessageCloseButton = this._errorMessagePanel.Q<Button>("CloseButton");
            this._errorMessageBody = this._errorMessagePanel.Q<Label>("ErrorMessageBody");
            HideErrorMessagePanel();
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

        private void OnLoadSpriteSheetButtonClicked()
        {
            SelectedFileInfo file = this._fileBrowser.SelectFile("spritesheet");

            if (file == SelectedFileInfo.Null)
            {
                return;
            }

            StartCoroutine(LoadSpriteSheetAsync(file.FullPath));

            return;

            IEnumerator LoadSpriteSheetAsync(string path)
            {
                yield return SpriteSheetFile.LoadAsync(path, spriteSheet =>
                {
                    this._packedTexturePreviewViewDataSource.UpdatePreview(spriteSheet);
                });
            }
        }

        private void OnSaveTexturePageButtonClicked()
        {
            Texture2D page = this._packedTexturePreviewViewDataSource.SelectedTexturePage;

            if (page == null)
            {
                return;
            }

            byte[] png = EncodeTexturePageToPng(page);

            if (png is not { Length: > 0 })
            {
                Debug.LogError($"[{nameof(SpriteSheetsDemoUiController)}] Failed to encode '{page.name}' to PNG.");
                return;
            }

            this._fileBrowser.SaveFile(page.name, png, "png");
        }

        private void OnSaveSpriteSheetButtonClicked()
        {
            if (this._packedTexturePreviewViewDataSource.TrySerializeSheet(out byte[] data) == false)
            {
                return;
            }

            string fileName = this._packingSettingsPanelViewDataSource.PageName;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "spritesheet";
            }

            this._fileBrowser.SaveFile(fileName, data, SpriteSheetFile.FILE_EXTENSION);
        }

        private static byte[] EncodeTexturePageToPng(Texture2D source)
        {
            if (source.isReadable)
            {
                return source.EncodeToPNG();
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, renderTexture);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);

            try
            {
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                return readable.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                Destroy(readable);
            }
        }

        private IEnumerator PackingCoroutine()
        {
            yield return this._spritePackEntryListViewController.LoadAllTexturesAsync();

            IReadOnlyList<SpritePackEntry> entries = this._spritePackEntryListViewController.Entries;

            if (entries is not { Count: > 0 })
            {
                this._packingCoroutine = null;
                PackingError packingError = new PackingError(-9999, "Failed to fetch entry textures.");
                ShowPackingError(packingError);
                yield break;
            }

            PackingResult packingResult = SpriteSheet.Pack(entries,
                                                           this._packingSettingsPanelViewDataSource.PackingSettings,
                                                           this._packingSettingsPanelViewDataSource.PageName);

            if (packingResult.IsSuccess)
            {
                HideErrorMessagePanel();
                this._packedTexturePreviewViewDataSource.UpdatePreview(packingResult.SpriteSheet);
            }
            else
            {
                ShowPackingError(packingResult.Error);
            }

            this._spritePackEntryListViewController.ReleaseLoadedTextures();
            this._packingCoroutine = null;
        }

        private void OnErrorMessageCloseButtonClicked()
        {
            HideErrorMessagePanel();
        }

        private void ShowPackingError(PackingError error)
        {
            this._errorMessageBody.text = error.Message ?? $"ErrorCode: {error.Code}";
            this._errorMessagePanel.style.display = DisplayStyle.Flex;
            this._errorMessageCloseButton.BringToFront();
        }

        private void HideErrorMessagePanel()
        {
            this._errorMessagePanel.style.display = DisplayStyle.None;
        }
    }
}
