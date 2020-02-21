using Serilog;
using System;
using System.Drawing;
using System.Runtime.Serialization;

namespace SecureMemo.DataModels
{
    [Serializable]
    [DataContract(Name = "SecureMemoFontSettings")]
    public class SecureMemoFontSettings
    {
        [DataMember(Name = "FontFamilyName", Order = 1)]
        public string FontFamilyName { get; set; }

        [DataMember(Name = "Style", Order = 2)]
        public FontStyle Style { get; set; }

        [DataMember(Name = "FontSize", Order = 3)]
        public float FontSize { get; set; }

        [DataMember(Name = "HasChangedSinceLoaded", Order = 4)]
        public bool HasChangedSinceLoaded { get; set; }

        private Lazy<FontFamily> _fontFamily;

        public FontFamily FontFamily
        {
            get { return _fontFamily.Value; }
        }

        public void FontFamilyUpdated()
        {
            _fontFamily = new Lazy<FontFamily>(CreateFontFamily, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public SecureMemoFontSettings()
        {
            FontFamilyUpdated();
        }

        private FontFamily CreateFontFamily()
        {
            return new FontFamily(this.FontFamilyName);
        }
    }
}