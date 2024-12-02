﻿using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    enum SoundEffectTypes
    {
        OnSelect = 0,
        OnTakeDamage = 1
    };
    internal interface ISoundEmitter
    {
        public SFXInstance[] SoundEffects {  get; }
    }
}
