using System;
using System.Drawing;
using System.Runtime.Serialization;
using System.Threading;

namespace SecureMemo.DataModels
{
    [Serializable]
    [DataContract(Name = "SecureMemoFontSettings")]
    public class SecureMemoFontSettings
    {
        private Lazy<FontFamily> _fontFamily;

        public SecureMemoFontSettings()
        {
            FontFamilyUpdated();
        }

        [DataMember(Name = "FontFamilyName", Order = 1)]
        public string FontFamilyName { get; set; }

        [DataMember(Name = "Style", Order = 2)]
        public FontStyle Style { get; set; }

        [DataMember(Name = "FontSize", Order = 3)]
        public float FontSize { get; set; }

        [DataMember(Name = "HasChangedSinceLoaded", Order = 4)]
        public bool HasChangedSinceLoaded { get; set; }

        public FontFamily FontFamily => _fontFamily.Value;

        public void FontFamilyUpdated()
        {
            _fontFamily = new Lazy<FontFamily>(CreateFontFamily, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private FontFamily CreateFontFamily()
        {
            return new FontFamily(FontFamilyName);
        }
    }
}