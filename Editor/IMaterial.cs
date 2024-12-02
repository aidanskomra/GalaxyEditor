using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    internal interface IMaterial
    {
        Material Material { get; }

        void SetTexture(GameEditor _game, string _texture);
        void SetShader(GameEditor _game, string _shader);
    }
}
