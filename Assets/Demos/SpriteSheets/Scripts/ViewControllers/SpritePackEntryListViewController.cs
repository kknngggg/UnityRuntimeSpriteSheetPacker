using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using kknngggg.Unity.Sprites.Demos.SpriteSheets.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets
{
    public sealed class SpritePackEntryListViewController
    {
        private readonly ListView _listView;
        private readonly IFileBrowser _fileBrowser;
        private readonly List<SpritePackEntryViewDatasource> _spritePackEntryViewDataSources = new();

        public SpritePackEntryListViewController(ListView listView, IFileBrowser fileBrowser)
        {
            this._listView = listView;
            this._fileBrowser = fileBrowser;
            listView.focusable = true;
            listView.delegatesFocus = true;
            listView.selectionType = SelectionType.None;
            listView.itemsSource = this._spritePackEntryViewDataSources;
            listView.bindItem = BindItem;
            listView.unbindItem = UnbindItem;

            ScrollView scrollView = listView.Q<ScrollView>();
            if (scrollView != null)
            {
                scrollView.focusable = true;
                scrollView.delegatesFocus = true;
            }
        }

        public IReadOnlyList<SpritePackEntry> Entries { get; private set; }

        public void OnAddEntryButtonClicked()
        {
            this._spritePackEntryViewDataSources.Add(new SpritePackEntryViewDatasource());
            this._listView.RefreshItems();
        }

        public IEnumerator LoadAllTexturesAsync()
        {
            if (this._spritePackEntryViewDataSources is not { Count: > 0 })
            {
                yield break;
            }

            IEnumerable<string> diskPaths = this._spritePackEntryViewDataSources
                                                .Select(datasource => datasource.DiskPath);
            BatchTextureLoader loader = new BatchTextureLoader(
                diskPaths, this._spritePackEntryViewDataSources.Count);

            yield return loader.LoadAllAsync();

            this.Entries = loader.Textures
                                 .Select(ToEntry)
                                 .Where(entry => entry.HasValue)
                                 .Select(entry => entry.Value)
                                 .ToList();
        }

        public void ReleaseLoadedTextures()
        {
            if (this.Entries == null)
            {
                return;
            }

            foreach (SpritePackEntry entry in this.Entries)
            {
                if (entry.Texture != null)
                {
                    Object.Destroy(entry.Texture);
                }
            }
        }

        private SpritePackEntry? ToEntry(Texture2D texture, int index)
        {
            if (texture == null)
            {
                return null;
            }

            SpritePackEntryViewDatasource datasource = this._spritePackEntryViewDataSources[index];
            string name = datasource.Name;
            name = string.IsNullOrWhiteSpace(name) ? texture.name : name.Trim();

            return new SpritePackEntry(texture,
                                       name,
                                       datasource.PixelsPerUnit,
                                       datasource.Pivot,
                                       datasource.MeshType);
        }

        private void BindItem(VisualElement spritePackEntry, int dataIndex)
        {
            Button removeButton = spritePackEntry.Q<Button>("RemoveButton");
            removeButton.userData = dataIndex;
            removeButton.clickable.clickedWithEventInfo += OnRemoveButtonClicked;

            Button browseButton = spritePackEntry.Q<Button>("BrowseButton");
            browseButton.userData = dataIndex;
            browseButton.clickable.clickedWithEventInfo += OnBrowseButtonClicked;

            RegisterInputNavigationGuards(spritePackEntry);
            spritePackEntry.dataSource = this._spritePackEntryViewDataSources[dataIndex];
        }

        private void UnbindItem(VisualElement spritePackEntry, int dataIndex)
        {
            Button removeButton = spritePackEntry.Q<Button>("RemoveButton");
            removeButton.userData = null;
            removeButton.clickable.clickedWithEventInfo -= OnRemoveButtonClicked;

            Button browseButton = spritePackEntry.Q<Button>("BrowseButton");
            browseButton.userData = null;
            browseButton.clickable.clickedWithEventInfo -= OnBrowseButtonClicked;

            UnregisterInputNavigationGuards(spritePackEntry);
            spritePackEntry.dataSource = null;
        }

        private void OnRemoveButtonClicked(EventBase eventInfo)
        {
            if (eventInfo.target is not Button removeButton)
            {
                return;
            }

            if (removeButton.userData is not int dataIndex)
            {
                return;
            }

            this._spritePackEntryViewDataSources.RemoveAt(dataIndex);
            this._listView.RefreshItems();
        }

        private void OnBrowseButtonClicked(EventBase eventInfo)
        {
            if (eventInfo.target is not Button removeButton)
            {
                return;
            }

            if (removeButton.userData is not int dataIndex)
            {
                return;
            }

            FileSystemInfo file = this._fileBrowser.SelectFile("jpg", "jpeg", "png", "webp", "heif");

            if (file == null)
            {
                return;
            }

            string diskPath = file.FullName;

            SpritePackEntryViewDatasource dataSource = this._spritePackEntryViewDataSources[dataIndex];
            dataSource.DiskPath = diskPath;
        }

        private static void RegisterInputNavigationGuards(VisualElement item)
        {
            item.Query(className: "sprite-pack-entry__input--field").ForEach(RegisterNavigationGuard);
            item.Query(className: "pivot-input-field__field").ForEach(RegisterNavigationGuard);
        }

        private static void UnregisterInputNavigationGuards(VisualElement item)
        {
            item.Query(className: "sprite-pack-entry__input--field").ForEach(UnregisterNavigationGuard);
            item.Query(className: "pivot-input-field__field").ForEach(UnregisterNavigationGuard);
        }

        private static void RegisterNavigationGuard(VisualElement field)
        {
            field.RegisterCallback<KeyDownEvent>(StopListNavigation);
            field.RegisterCallback<NavigationMoveEvent>(StopListNavigation);
            field.RegisterCallback<PointerDownEvent>(FocusFieldOnPointerDown, TrickleDown.TrickleDown);
            field.RegisterCallback<PointerDownEvent>(StopListNavigation);
        }

        private static void UnregisterNavigationGuard(VisualElement field)
        {
            field.UnregisterCallback<KeyDownEvent>(StopListNavigation);
            field.UnregisterCallback<NavigationMoveEvent>(StopListNavigation);
            field.UnregisterCallback<PointerDownEvent>(FocusFieldOnPointerDown, TrickleDown.TrickleDown);
            field.UnregisterCallback<PointerDownEvent>(StopListNavigation);
        }

        private static void FocusFieldOnPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement field)
            {
                return;
            }

            FocusTextInput(field);
        }

        private static void FocusTextInput(VisualElement field)
        {
            VisualElement input = field.Q(className: "unity-base-field__input");
            if (input is { focusable: true, canGrabFocus: true })
            {
                input.Focus();
                return;
            }

            field.Focus();
        }

        private static void StopListNavigation(EventBase evt)
        {
            evt.StopPropagation();
        }
    }
}
