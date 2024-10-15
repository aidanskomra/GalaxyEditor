using static System.ComponentModel.TypeConverter;
using System.Collections.Generic;
using System.ComponentModel;

public class DiffuseTextureTypeConverter : TypeConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
        var textures = new List<string> { "Grass", "Metal", "HeightMap" };
        return new StandardValuesCollection(textures);
    }
}
