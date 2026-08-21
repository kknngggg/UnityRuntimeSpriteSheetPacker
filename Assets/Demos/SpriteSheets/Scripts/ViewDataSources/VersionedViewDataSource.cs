using System;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets
{
    public abstract class VersionedViewDataSource : IDataSourceViewHashProvider, INotifyBindablePropertyChanged
    {
        public virtual event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        private long _viewVersion;

        public long GetViewHashCode()
        {
            return this._viewVersion;
        }

        protected void Notify([CallerMemberName] string propertyName = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
        }

        protected void Publish()
        {
            ++this._viewVersion;
        }
    }
}
