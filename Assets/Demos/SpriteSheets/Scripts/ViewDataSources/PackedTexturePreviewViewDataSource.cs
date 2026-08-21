using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets
{
    [Serializable]
    public sealed class PackedTexturePreviewViewDataSource : VersionedViewDataSource
    {
        [SerializeField, DontCreateProperty] private int _selectedTexturePageIndex;

        private static readonly List<string> _empty = new();

        private SpriteSheet _spriteSheet;

        [CreateProperty] public int SelectedTexturePageIndex {
            get => this._selectedTexturePageIndex;
            set => SetSelectedTexturePageIndex(value);
        }

        [CreateProperty] public List<string> PageDropdownChoices => GetPageDropdownChoices();

        [CreateProperty] public Texture2D SelectedTexturePage => GetSelectedTexture();

        [CreateProperty] public string SelectedTexturePageInfo => GetSelectedTexturePageInfo();

        [CreateProperty] public bool PreviousButtonEnabled => this._selectedTexturePageIndex > 0;

        [CreateProperty] public bool NextButtonEnabled => this._spriteSheet != null && this._selectedTexturePageIndex < this._spriteSheet.PageCount - 1;

        public void UpdatePreview(SpriteSheet spriteSheet)
        {
            this._spriteSheet?.Dispose();

            this._spriteSheet = spriteSheet;
            this._selectedTexturePageIndex = 0;

            Notify(nameof(this.SelectedTexturePageIndex));
            Notify(nameof(this.SelectedTexturePage));
            Notify(nameof(this.SelectedTexturePageInfo));
            Notify(nameof(this.PageDropdownChoices));
            Notify(nameof(this.PreviousButtonEnabled));
            Notify(nameof(this.NextButtonEnabled));
            Publish();
        }

        private void SetSelectedTexturePageIndex(int index)
        {
            if (this._spriteSheet == null)
            {
                return;
            }

            this._selectedTexturePageIndex = Math.Clamp(index, 0, this._spriteSheet.PageCount - 1);

            Notify(nameof(this.SelectedTexturePageIndex));
            Notify(nameof(this.SelectedTexturePage));
            Notify(nameof(this.SelectedTexturePageInfo));
            Notify(nameof(this.PreviousButtonEnabled));
            Notify(nameof(this.NextButtonEnabled));
            Publish();
        }

        private List<string> GetPageDropdownChoices()
        {
            if (this._spriteSheet == null)
            {
                return _empty;
            }

            int pageCount = this._spriteSheet.PageCount;
            List<string> choices = new List<string>(pageCount);

            for (int i = 0; i < pageCount; i++)
            {
                Texture2D texture = this._spriteSheet.GetPage(i);
                choices.Add(texture.name);
            }

            return choices;
        }

        private Texture2D GetSelectedTexture()
        {
            if (this._spriteSheet == null)
            {
                return null;
            }

            if (this._selectedTexturePageIndex < 0 ||
                this._selectedTexturePageIndex >= this._spriteSheet.PageCount)
            {
                return null;
            }

            return this._spriteSheet.GetPage(this._selectedTexturePageIndex);
        }

        private string GetSelectedTexturePageInfo()
        {
            if (this._spriteSheet == null)
            {
                return string.Empty;
            }

            Texture2D selectedTexture = GetSelectedTexture();

            if (selectedTexture == null)
            {
                return string.Empty;
            }

            string pageName = selectedTexture.name;
            int pageWidth = selectedTexture.width;
            int pageHeight = selectedTexture.height;
            return $"{pageName} {pageWidth}x{pageHeight}";
        }
    }
}
